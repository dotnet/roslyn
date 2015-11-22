// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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
            ConsList<Symbol> basesBeingResolved = null)
        {
            return IsSymbolAccessibleCore(symbol, within, throughTypeOpt, out failedThroughTypeCheck, within.DeclaringCompilation, ref useSiteDiagnostics, basesBeingResolved);
        }

        /// <summary>
        /// Checks if 'symbol' is accessible from within 'within', which must be a NamedTypeSymbol
        /// or an AssemblySymbol. 
        /// 
        /// Note that NamedTypeSymbol, if available, is the type that is associated with the binder 
        /// that found the 'symbol', not the inner-most type that contains the access to the
        /// 'symbol'.
        /// 
        /// If 'symbol' is accessed off of an expression then 'throughTypeOpt' is the type of that
        /// expression. This is needed to properly do protected access checks. Sets
        /// "failedThroughTypeCheck" to true if this protected check failed.
        /// 
        /// NOTE(cyrusn): I expect this function to be called a lot.  As such, i do not do any memory
        /// allocations in the function itself (including not making any iterators).  This does mean
        /// that certain helper functions that we'd like to call are inlined in this method to
        /// prevent the overhead of returning collections or enumerators.  
        /// </summary>
        private static bool IsSymbolAccessibleCore(
            Symbol symbol,
            Symbol within,  // must be assembly or named type symbol
            TypeSymbol throughTypeOpt,
            out bool failedThroughTypeCheck,
            CSharpCompilation compilation,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            ConsList<Symbol> basesBeingResolved = null)
        {
            Debug.Assert((object)symbol != null);
            Debug.Assert((object)within != null);
            Debug.Assert(within.IsDefinition);
            Debug.Assert(within is NamedTypeSymbol || within is AssemblySymbol);

            failedThroughTypeCheck = false;

            switch (symbol.Kind)
            {
                case SymbolKind.ArrayType:
                    return IsSymbolAccessibleCore(((ArrayTypeSymbol)symbol).ElementType.TypeSymbol, within, null, out failedThroughTypeCheck, compilation, ref useSiteDiagnostics);

                case SymbolKind.PointerType:
                    return IsSymbolAccessibleCore(((PointerTypeSymbol)symbol).PointedAtType.TypeSymbol, within, null, out failedThroughTypeCheck, compilation, ref useSiteDiagnostics);

                case SymbolKind.NamedType:
                    return IsNamedTypeAccessible((NamedTypeSymbol)symbol, within, ref useSiteDiagnostics, basesBeingResolved);

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
                    if (symbol.IsStatic)
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

        // Is the named type "type accessible from within "within", which must be a named type or an
        // assembly.
        private static bool IsNamedTypeAccessible(NamedTypeSymbol type, Symbol within, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConsList<Symbol> basesBeingResolved = null)
        {
            Debug.Assert(within is NamedTypeSymbol || within is AssemblySymbol);
            Debug.Assert((object)type != null);

            var compilation = within.DeclaringCompilation;

            bool unused;
            if (!type.IsDefinition)
            {
                // All type argument must be accessible.
                var typeArgs = type.TypeArgumentsWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);
                for (int i = 0; i < typeArgs.Length; ++i)
                {
                    // type parameters are always accessible, so don't check those (so common it's
                    // worth optimizing this).
                    if (typeArgs[i].Kind != SymbolKind.TypeParameter && !IsSymbolAccessibleCore(typeArgs[i].TypeSymbol, within, null, out unused, compilation, ref useSiteDiagnostics))
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

        // Is a top-level type with accessibility "declaredAccessibility" inside assembly "assembly"
        // accessible from "within", which must be a named type of an assembly.
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

        // Is a member with declared accessibility "declaredAccessibility" accessible from within
        // "within", which must be a named type or an assembly.
        private static bool IsMemberAccessible(
            NamedTypeSymbol containingType,              // the symbol's containing type
            Accessibility declaredAccessibility,
            Symbol within,
            TypeSymbol throughTypeOpt,
            out bool failedThroughTypeCheck,
            CSharpCompilation compilation,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            ConsList<Symbol> basesBeingResolved = null)
        {
            Debug.Assert(within is NamedTypeSymbol || within is AssemblySymbol);
            Debug.Assert((object)containingType != null);

            failedThroughTypeCheck = false;

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
            ConsList<Symbol> basesBeingResolved = null)
        {
            failedThroughTypeCheck = false;

            var originalContainingType = containingType.OriginalDefinition;
            var withinType = within as NamedTypeSymbol;
            var withinAssembly = (object)withinType != null ? withinType.ContainingAssembly : (AssemblySymbol)within;

            switch (declaredAccessibility)
            {
                case Accessibility.NotApplicable:
                    // TODO(cyrusn): Is this the right thing to do here?  Should the caller ever be
                    // asking about the accessibility of a symbol that has "NotApplicable" as its
                    // value?  For now, I'm preserving the behavior of the existing code.  But perhaps
                    // we should fail here and require the caller to not do this?
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


        // Is a protected symbol inside "originalContainingType" accessible from within "within",
        // which much be a named type or an assembly.
        private static bool IsProtectedSymbolAccessible(
            NamedTypeSymbol withinType,
            TypeSymbol throughTypeOpt,
            NamedTypeSymbol originalContainingType,
            out bool failedThroughTypeCheck,
            CSharpCompilation compilation,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            ConsList<Symbol> basesBeingResolved = null)
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

            // NOTE(ericli): It is helpful to think about 'protected' as *increasing* the
            // accessibility domain of a private member, rather than *decreasing* that of a public
            // member. Members are naturally private; the protected, internal and public access
            // modifiers all increase the accessibility domain. Since private members are accessible
            // to nested types, so are protected members.

            // NOTE(cyrusn): We do this check up front as it is very fast and easy to do.
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

                    if (current.InheritsFromIgnoringConstruction(originalContainingType, compilation, ref useSiteDiagnostics, basesBeingResolved))
                    {
                        // NOTE(cyrusn): We're continually walking up the 'throughType's inheritance
                        // chain.  We could compute it up front and cache it in a set.  However, i
                        // don't want to allocate memory in this function.  Also, in practice
                        // inheritance chains should be very short.  As such, it might actually be
                        // slower to create and check inside the set versus just walking the
                        // inheritance chain.
                        if ((object)originalThroughTypeOpt == null ||
                            originalThroughTypeOpt.InheritsFromIgnoringConstruction(current, compilation, ref useSiteDiagnostics))
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

        // Is a private symbol access
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

        // Is the type "withinType" nested within the original type "originalContainingType".
        private static bool IsNestedWithinOriginalContainingType(
            NamedTypeSymbol withinType,
            NamedTypeSymbol originalContainingType)
        {
            Debug.Assert((object)withinType != null);
            Debug.Assert((object)originalContainingType != null);

            // Walk up my parent chain and see if I eventually hit the owner.  If so then I'm a
            // nested type of that owner and I'm allowed access to everything inside of it.
            var current = withinType.OriginalDefinition;
            while ((object)current != null)
            {
                Debug.Assert(current.IsDefinition);
                if (current.Equals(originalContainingType))
                {
                    return true;
                }

                // NOTE(cyrusn): The container of an 'original' type is always original. 
                current = current.ContainingType;
            }

            return false;
        }

        // Determine if "type" inherits from "baseType", ignoring constructed types, and dealing
        // only with original types.
        private static bool InheritsFromIgnoringConstruction(
            this TypeSymbol type,
            NamedTypeSymbol baseType,
            CSharpCompilation compilation,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            ConsList<Symbol> basesBeingResolved = null)
        {
            Debug.Assert(type.IsDefinition);
            Debug.Assert(baseType.IsDefinition);

            PooledHashSet<NamedTypeSymbol> visited = null;
            var current = type;
            bool result = false;

            while ((object)current != null)
            {
                if (current.Equals(baseType))
                {
                    result = true;
                    break;
                }

                // NOTE(cyrusn): The base type of an 'original' type may not be 'original'. i.e. 
                // "class Foo : IBar<int>".  We must map it back to the 'original' when as we walk up
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
            return result;
        }

        // Does the assembly has internal accessibility to "toAssembly"?
        internal static bool HasInternalAccessTo(this AssemblySymbol assembly, AssemblySymbol toAssembly)
        {
            if (Equals(assembly, toAssembly))
            {
                return true;
            }

            if (assembly.AreInternalsVisibleToThisAssembly(toAssembly))
            {
                return true;
            }

            // all interactive assemblies are friends of each other:
            if (assembly.IsInteractive && toAssembly.IsInteractive)
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
