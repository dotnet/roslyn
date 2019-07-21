// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Contains the code for determining C# accessibility rules.
    /// </summary>
    internal static class AccessCheck
    {
        /// <summary>
        /// Checks if 'symbol' is accessible from within assembly 'within'.
        /// </summary>
        public static bool IsSymbolAccessible(
            Symbol symbol,
            AssemblySymbol within,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            bool failedThroughTypeCheck;
            return IsSymbolAccessibleCore(symbol, within, null, out failedThroughTypeCheck, within.DeclaringCompilation, ref useSiteDiagnostics);
        }

        /// <summary>
        /// Checks if 'symbol' is accessible from within type 'within', with
        /// an optional qualifier of type "throughTypeOpt".
        /// </summary>
        public static bool IsSymbolAccessible(
            Symbol symbol,
            NamedTypeSymbol within,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            TypeSymbol throughTypeOpt = null)
        {
            bool failedThroughTypeCheck;
            return IsSymbolAccessibleCore(symbol, within, throughTypeOpt, out failedThroughTypeCheck, within.DeclaringCompilation, ref useSiteDiagnostics);
        }

        /// <summary>
        /// Checks if 'symbol' is accessible from within type 'within', with
        /// an qualifier of type "throughTypeOpt". Sets "failedThroughTypeCheck" to true
        /// if it failed the "through type" check.
        /// </summary>
        public static bool IsSymbolAccessible(
            Symbol symbol,
            NamedTypeSymbol within,
            TypeSymbol throughTypeOpt,
            out bool failedThroughTypeCheck,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            ConsList<TypeSymbol> basesBeingResolved = null)
        {
            return IsSymbolAccessibleCore(symbol, within, throughTypeOpt, out failedThroughTypeCheck, within.DeclaringCompilation, ref useSiteDiagnostics, basesBeingResolved);
        }

        /// <summary>
        /// Returns true if the symbol is effectively public or internal based on
        /// the declared accessibility of the symbol and any containing symbols.
        /// </summary>
        internal static bool IsEffectivelyPublicOrInternal(Symbol symbol, out bool isInternal)
        {
            Debug.Assert(!(symbol is null));

            switch (symbol.Kind)
            {
                case SymbolKind.NamedType:
                case SymbolKind.Event:
                case SymbolKind.Field:
                case SymbolKind.Method:
                case SymbolKind.Property:
                    break;
                case SymbolKind.TypeParameter:
                    symbol = symbol.ContainingSymbol;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }

            isInternal = false;

            do
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Public:
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                        break;
                    case Accessibility.Internal:
                    case Accessibility.ProtectedAndInternal:
                        isInternal = true;
                        break;
                    case Accessibility.Private:
                        return false;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(symbol.DeclaredAccessibility);
                }

                symbol = symbol.ContainingType;
            }
            while (!(symbol is null));

            return true;
        }

        /// <summary>
        /// Checks if 'symbol' is accessible from within 'within', which must be a NamedTypeSymbol
        /// or an AssemblySymbol. 
        /// </summary>
        /// <remarks>
        /// Note that NamedTypeSymbol, if available, is the type that is associated with the binder 
        /// that found the 'symbol', not the inner-most type that contains the access to the
        /// 'symbol'.
        /// <para>
        /// If 'symbol' is accessed off of an expression then 'throughTypeOpt' is the type of that
        /// expression. This is needed to properly do protected access checks. Sets
        /// "failedThroughTypeCheck" to true if this protected check failed.
        /// </para>
        /// <para>
        /// This function is expected to be called a lot.  As such, it avoids memory
        /// allocations in the function itself (including not making any iterators).  This means
        /// that certain helper functions that could otherwise be called are inlined in this method to
        /// prevent the overhead of returning collections or enumerators.
        /// </para>
        /// </remarks>
        private static bool IsSymbolAccessibleCore(
            Symbol symbol,
            Symbol within,  // must be assembly or named type symbol
            TypeSymbol throughTypeOpt,
            out bool failedThroughTypeCheck,
            CSharpCompilation compilation,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            ConsList<TypeSymbol> basesBeingResolved = null)
        {
            Debug.Assert((object)symbol != null);
            Debug.Assert((object)within != null);
            Debug.Assert(within.IsDefinition);
            Debug.Assert(within is NamedTypeSymbol || within is AssemblySymbol);

            failedThroughTypeCheck = false;

            switch (symbol.Kind)
            {
                case SymbolKind.ArrayType:
                    return IsSymbolAccessibleCore(((ArrayTypeSymbol)symbol).ElementType, within, null, out failedThroughTypeCheck, compilation, ref useSiteDiagnostics, basesBeingResolved);

                case SymbolKind.PointerType:
                    return IsSymbolAccessibleCore(((PointerTypeSymbol)symbol).PointedAtType, within, null, out failedThroughTypeCheck, compilation, ref useSiteDiagnostics, basesBeingResolved);

                case SymbolKind.NamedType:
                    return IsNamedTypeAccessible((NamedTypeSymbol)symbol, within, ref useSiteDiagnostics, basesBeingResolved);

                case SymbolKind.Alias:
                    return IsSymbolAccessibleCore(((AliasSymbol)symbol).Target, within, null, out failedThroughTypeCheck, compilation, ref useSiteDiagnostics, basesBeingResolved);

                case SymbolKind.Discard:
                    return IsSymbolAccessibleCore(((DiscardSymbol)symbol).Type, within, null, out failedThroughTypeCheck, compilation, ref useSiteDiagnostics, basesBeingResolved);

                case SymbolKind.ErrorType:
                    // Always assume that error types are accessible.
                    return true;

                case SymbolKind.TypeParameter:
                case SymbolKind.Parameter:
                case SymbolKind.Local:
                case SymbolKind.Label:
                case SymbolKind.Namespace:
                case SymbolKind.DynamicType:
                case SymbolKind.Assembly:
                case SymbolKind.NetModule:
                case SymbolKind.RangeVariable:
                    // These types of symbols are always accessible (if visible).
                    return true;

                case SymbolKind.Method:
                case SymbolKind.Property:
                case SymbolKind.Event:
                case SymbolKind.Field:
                    if (!symbol.RequiresInstanceReceiver())
                    {
                        // static members aren't accessed "through" an "instance" of any type.  So we
                        // null out the "through" instance here.  This ensures that we'll understand
                        // accessing protected statics properly.
                        throughTypeOpt = null;
                    }

                    return IsMemberAccessible(symbol.ContainingType, symbol.DeclaredAccessibility, within, throughTypeOpt, out failedThroughTypeCheck, compilation, ref useSiteDiagnostics);

                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        /// <summary>
        /// Is the named type <paramref name="type"/> accessible from within <paramref name="within"/>,
        /// which must be a named type or an assembly.
        /// </summary>
        private static bool IsNamedTypeAccessible(NamedTypeSymbol type, Symbol within, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConsList<TypeSymbol> basesBeingResolved = null)
        {
            Debug.Assert(within is NamedTypeSymbol || within is AssemblySymbol);
            Debug.Assert((object)type != null);

            var compilation = within.DeclaringCompilation;

            bool unused;
            if (!type.IsDefinition)
            {
                // All type argument must be accessible.
                var typeArgs = type.TypeArgumentsWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);
                foreach (var typeArg in typeArgs)
                {
                    // type parameters are always accessible, so don't check those (so common it's
                    // worth optimizing this).
                    if (typeArg.Type.Kind != SymbolKind.TypeParameter && !IsSymbolAccessibleCore(typeArg.Type, within, null, out unused, compilation, ref useSiteDiagnostics, basesBeingResolved))
                    {
                        return false;
                    }
                }
            }

            var containingType = type.ContainingType;
            return (object)containingType == null
                ? IsNonNestedTypeAccessible(type.ContainingAssembly, type.DeclaredAccessibility, within)
                : IsMemberAccessible(containingType, type.DeclaredAccessibility, within, null, out unused, compilation, ref useSiteDiagnostics, basesBeingResolved);
        }

        /// <summary>
        /// Is a top-level type with accessibility "declaredAccessibility" inside assembly "assembly"
        /// accessible from "within", which must be a named type of an assembly.
        /// </summary>
        private static bool IsNonNestedTypeAccessible(
            AssemblySymbol assembly,
            Accessibility declaredAccessibility,
            Symbol within)
        {
            Debug.Assert(within is NamedTypeSymbol || within is AssemblySymbol);
            Debug.Assert((object)assembly != null);

            switch (declaredAccessibility)
            {
                case Accessibility.NotApplicable:
                case Accessibility.Public:
                    // Public symbols are always accessible from any context
                    return true;

                case Accessibility.Private:
                case Accessibility.Protected:
                case Accessibility.ProtectedAndInternal:
                    // Shouldn't happen except in error cases.
                    return false;

                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:

                    // within is typically a type
                    var withinType = within as NamedTypeSymbol;
                    var withinAssembly = (object)withinType != null ? withinType.ContainingAssembly : (AssemblySymbol)within;

                    // An internal type is accessible if we're in the same assembly or we have
                    // friend access to the assembly it was defined in.
                    return (object)withinAssembly == (object)assembly || withinAssembly.HasInternalAccessTo(assembly);

                default:
                    throw ExceptionUtilities.UnexpectedValue(declaredAccessibility);
            }
        }

        /// <summary>
        /// Is a member with declared accessibility "declaredAccessibility" accessible from within
        /// "within", which must be a named type or an assembly.
        /// </summary>
        private static bool IsMemberAccessible(
            NamedTypeSymbol containingType,              // the symbol's containing type
            Accessibility declaredAccessibility,
            Symbol within,
            TypeSymbol throughTypeOpt,
            out bool failedThroughTypeCheck,
            CSharpCompilation compilation,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            ConsList<TypeSymbol> basesBeingResolved = null)
        {
            Debug.Assert(within is NamedTypeSymbol || within is AssemblySymbol);
            Debug.Assert((object)containingType != null);

            failedThroughTypeCheck = false;

            if (containingType.IsTupleType)
            {
                containingType = containingType.TupleUnderlyingType;
            }

            // easy case - members of containing type are accessible.
            if ((object)containingType == (object)within)
            {
                return true;
            }

            // A nested symbol is only accessible to us if its container is accessible as well.
            if (!IsNamedTypeAccessible(containingType, within, ref useSiteDiagnostics, basesBeingResolved))
            {
                return false;
            }

            // public in accessible type is accessible
            if (declaredAccessibility == Accessibility.Public)
            {
                return true;
            }

            return IsNonPublicMemberAccessible(
                containingType,
                declaredAccessibility,
                within,
                throughTypeOpt,
                out failedThroughTypeCheck,
                compilation,
                ref useSiteDiagnostics,
                basesBeingResolved);
        }

        private static bool IsNonPublicMemberAccessible(
            NamedTypeSymbol containingType,              // the symbol's containing type
            Accessibility declaredAccessibility,
            Symbol within,
            TypeSymbol throughTypeOpt,
            out bool failedThroughTypeCheck,
            CSharpCompilation compilation,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            ConsList<TypeSymbol> basesBeingResolved = null)
        {
            failedThroughTypeCheck = false;

            var originalContainingType = containingType.OriginalDefinition;
            var withinType = within as NamedTypeSymbol;
            var withinAssembly = (object)withinType != null ? withinType.ContainingAssembly : (AssemblySymbol)within;

            switch (declaredAccessibility)
            {
                case Accessibility.NotApplicable:
                    return true;

                case Accessibility.Private:
                    // All expressions in the current submission (top-level or nested in a method or
                    // type) can access previous submission's private top-level members. Previous
                    // submissions are treated like outer classes for the current submission - the
                    // inner class can access private members of the outer class.
                    if (containingType.TypeKind == TypeKind.Submission)
                    {
                        return true;
                    }

                    // private members never accessible from outside a type.
                    return (object)withinType != null && IsPrivateSymbolAccessible(withinType, originalContainingType);

                case Accessibility.Internal:
                    // An internal type is accessible if we're in the same assembly or we have
                    // friend access to the assembly it was defined in.
                    return withinAssembly.HasInternalAccessTo(containingType.ContainingAssembly);

                case Accessibility.ProtectedAndInternal:
                    if (!withinAssembly.HasInternalAccessTo(containingType.ContainingAssembly))
                    {
                        // We require internal access.  If we don't have it, then this symbol is
                        // definitely not accessible to us.
                        return false;
                    }

                    // We had internal access.  Also have to make sure we have protected access.
                    return IsProtectedSymbolAccessible(withinType, throughTypeOpt, originalContainingType, out failedThroughTypeCheck, compilation, ref useSiteDiagnostics, basesBeingResolved);

                case Accessibility.ProtectedOrInternal:
                    if (withinAssembly.HasInternalAccessTo(containingType.ContainingAssembly))
                    {
                        // If we have internal access to this symbol, then that's sufficient.  no
                        // need to do the complicated protected case.
                        return true;
                    }

                    // We don't have internal access.  But if we have protected access then that's
                    // sufficient.
                    return IsProtectedSymbolAccessible(withinType, throughTypeOpt, originalContainingType, out failedThroughTypeCheck, compilation, ref useSiteDiagnostics, basesBeingResolved);

                case Accessibility.Protected:
                    return IsProtectedSymbolAccessible(withinType, throughTypeOpt, originalContainingType, out failedThroughTypeCheck, compilation, ref useSiteDiagnostics, basesBeingResolved);

                default:
                    throw ExceptionUtilities.UnexpectedValue(declaredAccessibility);
            }
        }


        /// <summary>
        /// Is a protected symbol inside "originalContainingType" accessible from within "within",
        /// which much be a named type or an assembly.
        /// </summary>
        private static bool IsProtectedSymbolAccessible(
            NamedTypeSymbol withinType,
            TypeSymbol throughTypeOpt,
            NamedTypeSymbol originalContainingType,
            out bool failedThroughTypeCheck,
            CSharpCompilation compilation,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            ConsList<TypeSymbol> basesBeingResolved = null)
        {
            failedThroughTypeCheck = false;

            // It is not an error to define protected member in a sealed Script class, it's just a
            // warning. The member behaves like a private one - it is visible in all subsequent
            // submissions.
            if (originalContainingType.TypeKind == TypeKind.Submission)
            {
                return true;
            }

            if ((object)withinType == null)
            {
                // If we're not within a type, we can't access a protected symbol
                return false;
            }

            // A protected symbol is accessible if we're (optionally nested) inside the type that it
            // was defined in.  

            // It is helpful to think about 'protected' as *increasing* the
            // accessibility domain of a private member, rather than *decreasing* that of a public
            // member. Members are naturally private; the protected, internal and public access
            // modifiers all increase the accessibility domain. Since private members are accessible
            // to nested types, so are protected members.

            // We do this check up front as it is very fast and easy to do.
            if (IsNestedWithinOriginalContainingType(withinType, originalContainingType))
            {
                return true;
            }

            // Protected is really confusing.  Check out 3.5.3 of the language spec "protected access
            // for instance members" to see how it works.  I actually got the code for this from
            // LangCompiler::CheckAccessCore
            {
                var current = withinType.OriginalDefinition;
                var originalThroughTypeOpt = (object)throughTypeOpt == null ? null : throughTypeOpt.OriginalDefinition as TypeSymbol;
                while ((object)current != null)
                {
                    Debug.Assert(current.IsDefinition);

                    if (current.InheritsFromOrImplementsIgnoringConstruction(originalContainingType, compilation, ref useSiteDiagnostics, basesBeingResolved))
                    {
                        // NOTE(cyrusn): We're continually walking up the 'throughType's inheritance
                        // chain.  We could compute it up front and cache it in a set.  However, we
                        // don't want to allocate memory in this function.  Also, in practice
                        // inheritance chains should be very short.  As such, it might actually be
                        // slower to create and check inside the set versus just walking the
                        // inheritance chain.
                        if ((object)originalThroughTypeOpt == null ||
                            originalThroughTypeOpt.InheritsFromOrImplementsIgnoringConstruction(current, compilation, ref useSiteDiagnostics))
                        {
                            return true;
                        }
                        else
                        {
                            failedThroughTypeCheck = true;
                        }
                    }

                    // NOTE(cyrusn): The container of an original type is always original.
                    current = current.ContainingType;
                }
            }

            return false;
        }

        private static bool IsPrivateSymbolAccessible(
            Symbol within,
            NamedTypeSymbol originalContainingType)
        {
            Debug.Assert(within is NamedTypeSymbol || within is AssemblySymbol);

            var withinType = within as NamedTypeSymbol;
            if ((object)withinType == null)
            {
                // If we're not within a type, we can't access a private symbol
                return false;
            }

            // A private symbol is accessible if we're (optionally nested) inside the type that it
            // was defined in.
            return IsNestedWithinOriginalContainingType(withinType, originalContainingType);
        }

        /// <summary>
        /// Is the type "withinType" nested within the original type "originalContainingType".
        /// </summary>
        private static bool IsNestedWithinOriginalContainingType(
            NamedTypeSymbol withinType,
            NamedTypeSymbol originalContainingType)
        {
            Debug.Assert((object)withinType != null);
            Debug.Assert((object)originalContainingType != null);
            Debug.Assert(originalContainingType.IsDefinition);

            // Walk up my parent chain and see if I eventually hit the owner.  If so then I'm a
            // nested type of that owner and I'm allowed access to everything inside of it.
            var current = withinType.OriginalDefinition;
            while ((object)current != null)
            {
                Debug.Assert(current.IsDefinition);
                if (current == (object)originalContainingType)
                {
                    return true;
                }

                // NOTE(cyrusn): The container of an 'original' type is always original. 
                current = current.ContainingType;
            }

            return false;
        }

        /// <summary>
        /// Determine if "type" inherits from or implements "baseType", ignoring constructed types, and dealing
        /// only with original types.
        /// </summary>
        private static bool InheritsFromOrImplementsIgnoringConstruction(
            this TypeSymbol type,
            NamedTypeSymbol baseType,
            CSharpCompilation compilation,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            ConsList<TypeSymbol> basesBeingResolved = null)
        {
            Debug.Assert(type.IsDefinition);
            Debug.Assert(baseType.IsDefinition);

            PooledHashSet<NamedTypeSymbol> interfacesLookedAt = null;
            ArrayBuilder<NamedTypeSymbol> baseInterfaces = null;

            bool baseTypeIsInterface = baseType.IsInterface;
            if (baseTypeIsInterface)
            {
                interfacesLookedAt = PooledHashSet<NamedTypeSymbol>.GetInstance();
                baseInterfaces = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            }

            PooledHashSet<NamedTypeSymbol> visited = null;
            var current = type;
            bool result = false;

            while ((object)current != null)
            {
                Debug.Assert(current.IsDefinition);
                if (baseTypeIsInterface == current.IsInterfaceType() &&
                    current == (object)baseType)
                {
                    result = true;
                    break;
                }

                if (baseTypeIsInterface)
                {
                    getBaseInterfaces(current, baseInterfaces, interfacesLookedAt, basesBeingResolved);
                }

                // NOTE(cyrusn): The base type of an 'original' type may not be 'original'. i.e. 
                // "class Goo : IBar<int>".  We must map it back to the 'original' when as we walk up
                // the base type hierarchy.
                var next = current.GetNextBaseTypeNoUseSiteDiagnostics(basesBeingResolved, compilation, ref visited);
                if ((object)next == null)
                {
                    current = null;
                }
                else
                {
                    current = (TypeSymbol)next.OriginalDefinition;
                    current.AddUseSiteDiagnostics(ref useSiteDiagnostics);
                }
            }

            visited?.Free();

            if (!result && baseTypeIsInterface)
            {
                Debug.Assert(!result);

                while (baseInterfaces.Count != 0)
                {
                    NamedTypeSymbol currentBase = baseInterfaces.Pop();

                    if (!currentBase.IsInterface)
                    {
                        continue;
                    }

                    Debug.Assert(currentBase.IsDefinition);
                    if (currentBase == (object)baseType)
                    {
                        result = true;
                        break;
                    }

                    getBaseInterfaces(currentBase, baseInterfaces, interfacesLookedAt, basesBeingResolved);
                }

                if (!result)
                {
                    foreach (var candidate in interfacesLookedAt)
                    {
                        candidate.AddUseSiteDiagnostics(ref useSiteDiagnostics);
                    }
                }
            }

            interfacesLookedAt?.Free();
            baseInterfaces?.Free();
            return result;

            static void getBaseInterfaces(TypeSymbol derived, ArrayBuilder<NamedTypeSymbol> baseInterfaces, PooledHashSet<NamedTypeSymbol> interfacesLookedAt, ConsList<TypeSymbol> basesBeingResolved)
            {
                if (basesBeingResolved != null && basesBeingResolved.ContainsReference(derived))
                {
                    return;
                }

                ImmutableArray<NamedTypeSymbol> declaredInterfaces;

                switch (derived)
                {
                    case TypeParameterSymbol typeParameter:
                        declaredInterfaces = typeParameter.AllEffectiveInterfacesNoUseSiteDiagnostics;
                        break;

                    case NamedTypeSymbol namedType:
                        declaredInterfaces = namedType.GetDeclaredInterfaces(basesBeingResolved);
                        break;

                    default:
                        declaredInterfaces = derived.InterfacesNoUseSiteDiagnostics(basesBeingResolved);
                        break;
                }

                foreach (var @interface in declaredInterfaces)
                {
                    NamedTypeSymbol definition = @interface.OriginalDefinition;
                    if (interfacesLookedAt.Add(definition))
                    {
                        baseInterfaces.Add(definition);
                    }
                }
            }
        }

        /// <summary>
        /// Does the assembly has internal accessibility to "toAssembly"?
        /// </summary>
        /// <param name="fromAssembly">The assembly wanting access.</param>
        /// <param name="toAssembly">The assembly possibly providing symbols to be accessed.</param>
        internal static bool HasInternalAccessTo(this AssemblySymbol fromAssembly, AssemblySymbol toAssembly)
        {
            if (Equals(fromAssembly, toAssembly))
            {
                return true;
            }

            if (fromAssembly.AreInternalsVisibleToThisAssembly(toAssembly))
            {
                return true;
            }

            // all interactive assemblies are friends of each other:
            if (fromAssembly.IsInteractive && toAssembly.IsInteractive)
            {
                return true;
            }

            return false;
        }

        internal static ErrorCode GetProtectedMemberInSealedTypeError(NamedTypeSymbol containingType)
        {
            return containingType.TypeKind == TypeKind.Struct ? ErrorCode.ERR_ProtectedInStruct : ErrorCode.WRN_ProtectedInSealed;
        }
    }
}
