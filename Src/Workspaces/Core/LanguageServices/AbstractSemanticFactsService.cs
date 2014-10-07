using Roslyn.Compilers.Common;
using Roslyn.Services.Shared.Extensions;
using Roslyn.Utilities;

namespace Roslyn.Services.Shared.LanguageServices
{
    internal abstract class AbstractSemanticFactsService : ISemanticFactsService
    {
        public abstract bool IsInstanceConstructorName(string memberName);
        public abstract bool IsStaticConstructorName(string memberName);
        public abstract string GetDisplayName(INamedTypeSymbol typeSymbol, string metadataMemberName);
        public abstract bool HaveSameSignature(IMethodSymbol method1, IMethodSymbol method2);
        public abstract bool SupportsImplicitInterfaceImplementation { get; }

        /// <summary>
        /// Checks if 'symbol' is accessible from within assembly 'within'.  
        /// </summary>
        public bool IsSymbolAccessible(
            ISymbol symbol,
            IAssemblySymbol within)
        {
            bool failedThroughTypeCheck;
            return IsSymbolAccessibleCore(symbol, within, null, out failedThroughTypeCheck);
        }

        /// <summary>
        /// Checks if 'symbol' is accessible from within name type 'within', with an optional
        /// qualifier of type "throughTypeOpt".
        /// </summary>
        public bool IsSymbolAccessible(
            ISymbol symbol,
            INamedTypeSymbol within,
            ITypeSymbol throughTypeOpt = null)
        {
            bool failedThroughTypeCheck;
            return IsSymbolAccessible(symbol, within, throughTypeOpt, out failedThroughTypeCheck);
        }

        /// <summary>
        /// Checks if 'symbol' is accessible from within assembly 'within', with an qualifier of
        /// type "throughTypeOpt". Sets "failedThroughTypeCheck" to true if it failed the "through
        /// type" check.
        /// </summary>
        private bool IsSymbolAccessible(
            ISymbol symbol,
            INamedTypeSymbol within,
            ITypeSymbol throughTypeOpt,
            out bool failedThroughTypeCheck)
        {
            return IsSymbolAccessibleCore(symbol, within, throughTypeOpt, out failedThroughTypeCheck);
        }

        /// <summary>
        /// Checks if 'symbol' is accessible from within 'within', which must be a INamedTypeSymbol
        /// or an IAssemblySymbol.  If 'symbol' is accessed off of an expression then
        /// 'throughTypeOpt' is the type of that expression. This is needed to properly do protected
        /// access checks. Sets "failedThroughTypeCheck" to true if this protected check failed.
        /// 
        /// NOTE(cyrusn): I expect this function to be called a lot.  As such, i do not do any memory
        /// allocations in the function itself (including not making any iterators).  This does mean
        /// that certain helper functions that we'd like to call are inlined in this method to
        /// prevent the overhead of returning collections or enumerators.  
        /// </summary>
        private bool IsSymbolAccessibleCore(
            ISymbol symbol,
            ISymbol within,  // must be assembly or named type symbol
            ITypeSymbol throughTypeOpt,
            out bool failedThroughTypeCheck)
        {
            Contract.ThrowIfNull(symbol);
            Contract.ThrowIfNull(within);
            Contract.Requires(within is INamedTypeSymbol || within is IAssemblySymbol);

            failedThroughTypeCheck = false;
            var withinAssembly = (within as IAssemblySymbol) ?? ((INamedTypeSymbol)within).ContainingAssembly;

            switch (symbol.Kind)
            {
                case CommonSymbolKind.Alias:
                    return IsSymbolAccessibleCore(((IAliasSymbol)symbol).Target, within, throughTypeOpt, out failedThroughTypeCheck);

                case CommonSymbolKind.ArrayType:
                    return IsSymbolAccessibleCore(((IArrayTypeSymbol)symbol).ElementType, within, null, out failedThroughTypeCheck);

                case CommonSymbolKind.PointerType:
                    return IsSymbolAccessibleCore(((IPointerTypeSymbol)symbol).PointedAtType, within, null, out failedThroughTypeCheck);

                case CommonSymbolKind.NamedType:
                    return IsNamedTypeAccessible((INamedTypeSymbol)symbol, within);

                case CommonSymbolKind.TypeParameterType:
                case CommonSymbolKind.Parameter:
                case CommonSymbolKind.Local:
                case CommonSymbolKind.Label:
                case CommonSymbolKind.Namespace:
                case CommonSymbolKind.DynamicType:
                case CommonSymbolKind.Assembly:
                case CommonSymbolKind.Module:
                case CommonSymbolKind.RangeVariable:
                    // These types of symbols are always accessible (if visible).
                    return true;

                case CommonSymbolKind.Method:
                case CommonSymbolKind.Property:
                case CommonSymbolKind.Field:
                case CommonSymbolKind.Event:
                    if (symbol.IsStatic)
                    {
                        // static members aren't accessed "through" an "instance" of any type.  So we
                        // null out the "through" instance here.  This ensures that we'll understand
                        // accessing protected statics properly.
                        throughTypeOpt = null;
                    }

                    return IsMemberAccessible(symbol.ContainingType, symbol.DeclaredAccessibility, within, throughTypeOpt, out failedThroughTypeCheck);

                default:
                    throw Contract.Unreachable;
            }
        }

        // Is the named type "type accessible from within "within", which must be a named type or an
        // assembly.
        private bool IsNamedTypeAccessible(INamedTypeSymbol type, ISymbol within)
        {
            Contract.Requires(within is INamedTypeSymbol || within is IAssemblySymbol);
            Contract.ThrowIfNull(type);

            if (type.IsErrorType())
            {
                // Always assume that error types are accessible.
                return true;
            }

            bool unused;
            if (!type.IsDefinition)
            {
                // All type argument must be accessible.
                foreach (var typeArg in type.TypeArguments)
                {
                    // type parameters are always accessible, so don't check those (so common it's
                    // worth optimizing this).
                    if (typeArg.Kind != CommonSymbolKind.TypeParameterType &&
                        typeArg.TypeKind != CommonTypeKind.Error &&
                        !IsSymbolAccessibleCore(typeArg, within, null, out unused))
                    {
                        return false;
                    }
                }
            }

            var containingType = type.ContainingType;
            return containingType == null
                ? IsNonNestedTypeAccessible(type.ContainingAssembly, type.DeclaredAccessibility, within)
                : IsMemberAccessible(type.ContainingType, type.DeclaredAccessibility, within, null, out unused);
        }

        // Is a top-level type with accessibility "declaredAccessibility" inside assembly "assembly"
        // accessible from "within", which must be a named type of an assembly.
        private bool IsNonNestedTypeAccessible(
            IAssemblySymbol assembly,
            CommonAccessibility declaredAccessibility,
            ISymbol within)
        {
            Contract.Requires(within is INamedTypeSymbol || within is IAssemblySymbol);
            Contract.ThrowIfNull(assembly);
            var withinAssembly = (within as IAssemblySymbol) ?? ((INamedTypeSymbol)within).ContainingAssembly;

            switch (declaredAccessibility)
            {
                case CommonAccessibility.NotApplicable:
                case CommonAccessibility.Public:
                    // Public symbols are always accessible from any context
                    return true;

                case CommonAccessibility.Private:
                case CommonAccessibility.Protected:
                case CommonAccessibility.ProtectedAndInternal:
                    // Shouldn't happen except in error cases.
                    return false;

                case CommonAccessibility.Internal:
                case CommonAccessibility.ProtectedOrInternal:
                    // An internal type is accessible if we're in the same assembly or we have
                    // friend access to the assembly it was defined in.
                    return withinAssembly.IsSameAssemblyOrHasFriendAccessTo(assembly);

                default:
                    throw Contract.Unreachable;
            }
        }

        // Is a member with declared accessibily "declaredAccessiblity" accessible from within
        // "within", which must be a named type or an assembly.
        private bool IsMemberAccessible(
            INamedTypeSymbol containingType,
            CommonAccessibility declaredAccessibility,
            ISymbol within,
            ITypeSymbol throughTypeOpt,
            out bool failedThroughTypeCheck)
        {
            Contract.Requires(within is INamedTypeSymbol || within is IAssemblySymbol);
            Contract.ThrowIfNull(containingType);

            failedThroughTypeCheck = false;

            var originalContainingType = containingType.OriginalDefinition;
            var withinNamedType = within as INamedTypeSymbol;
            var withinAssembly = (within as IAssemblySymbol) ?? ((INamedTypeSymbol)within).ContainingAssembly;

            // A nested symbol is only accessible to us if its container is accessible as well.
            if (!IsNamedTypeAccessible(containingType, within))
            {
                return false;
            }

            switch (declaredAccessibility)
            {
                case CommonAccessibility.NotApplicable:
                    // TODO(cyrusn): Is this the right thing to do here?  Should the caller ever be
                    // asking about the accessibility of a symbol that has "NotApplicable" as its
                    // value?  For now, i'm preserving the behavior of the existing code.  But perhaps
                    // we should fail here and require the caller to not do this?
                    return true;

                case CommonAccessibility.Public:
                    // Public symbols are always accessible from any context
                    return true;

                case CommonAccessibility.Private:
                    // All expressions in the current submission (top-level or nested in a method or
                    // type) can access previous submission's private top-level members. Previous
                    // submissions are treated like outer classes for the current submission - the
                    // inner class can access private members of the outer class.
                    if (withinAssembly.IsInteractive && containingType.IsScriptClass)
                    {
                        return true;
                    }

                    // private members never accessible from outside a type.
                    return withinNamedType != null && IsPrivateSymbolAccessible(withinNamedType, originalContainingType);

                case CommonAccessibility.Internal:
                    // An internal type is accessible if we're in the same assembly or we have
                    // friend access to the assembly it was defined in.
                    return withinAssembly.IsSameAssemblyOrHasFriendAccessTo(containingType.ContainingAssembly);

                case CommonAccessibility.ProtectedAndInternal:
                    if (!withinAssembly.IsSameAssemblyOrHasFriendAccessTo(containingType.ContainingAssembly))
                    {
                        // We require internal access.  If we don't have it, then this symbol is
                        // definitely not accessible to us.
                        return false;
                    }

                    // We had internal access.  Also have to make sure we have protected access.
                    return IsProtectedSymbolAccessible(withinNamedType, withinAssembly, throughTypeOpt, originalContainingType, out failedThroughTypeCheck);

                case CommonAccessibility.ProtectedOrInternal:
                    if (withinAssembly.IsSameAssemblyOrHasFriendAccessTo(containingType.ContainingAssembly))
                    {
                        // If we have internal access to this symbol, then that's sufficient.  no
                        // need to do the complicated protected case.
                        return true;
                    }

                    // We don't have internal access.  But if we have protected access then that's
                    // sufficient.
                    return IsProtectedSymbolAccessible(withinNamedType, withinAssembly, throughTypeOpt, originalContainingType, out failedThroughTypeCheck);

                case CommonAccessibility.Protected:
                    return IsProtectedSymbolAccessible(withinNamedType, withinAssembly, throughTypeOpt, originalContainingType, out failedThroughTypeCheck);

                default:
                    throw Contract.Unreachable;
            }
        }

        // Is a protected symbol inside "originalContainingType" accessible from within "within",
        // which much be a named type or an assembly.
        private bool IsProtectedSymbolAccessible(
            INamedTypeSymbol withinType,
            IAssemblySymbol withinAssembly,
            ITypeSymbol throughTypeOpt,
            INamedTypeSymbol originalContainingType,
            out bool failedThroughTypeCheck)
        {
            failedThroughTypeCheck = false;

            // It is not an error to define protected member in a sealed Script class, 
            // it's just a warning. The member behaves like a private one - it is visible 
            // in all subsequent submissions.
            if (withinAssembly.IsInteractive && originalContainingType.IsScriptClass)
            {
                return true;
            }

            if (withinType == null)
            {
                // If we're not within a type, we can't access a protected symbol
                return false;
            }

            // A protected symbol is accessible if we're (optionally nested) inside the type that it
            // was defined in.  NOTE(cyrusn): This seems incredibly weird to me (as i consider
            // protectedness something that limits visibility to *subclasses*), but that's how the
            // C# language works.

            // NOTE(ericli): It is less weird if you think about 'protected' as *increasing* the
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
                var originalThroughTypeOpt = throughTypeOpt == null ? null : throughTypeOpt.OriginalDefinition;
                while (current != null)
                {
                    Contract.Requires(current.IsDefinition);

                    if (current.InheritsFromOrEqualsIgnoringConstruction(originalContainingType))
                    {
                        // NOTE(cyrusn): We're continually walking up the 'throughType's inheritance
                        // chain.  We could compute it up front and cache it in a set.  However, i
                        // don't want to allocate memory in this function.  Also, in practice
                        // inheritance chains should be very short.  As such, it might actually be
                        // slower to create and check inside the set versus just walking the
                        // inheritance chain.
                        if (originalThroughTypeOpt == null ||
                            originalThroughTypeOpt.InheritsFromOrEqualsIgnoringConstruction(current))
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
        private bool IsPrivateSymbolAccessible(
            ISymbol within,
            INamedTypeSymbol originalContainingType)
        {
            Contract.Requires(within is INamedTypeSymbol || within is IAssemblySymbol);

            var withinType = within as INamedTypeSymbol;
            if (withinType == null)
            {
                // If we're not within a type, we can't access a private symbol
                return false;
            }

            // A private symbol is accessible if we're (optionally nested) inside the type that it
            // was defined in.
            return IsNestedWithinOriginalContainingType(withinType, originalContainingType);
        }

        // Is the type "withinType" nested withing the original type "originalContainingType".
        private bool IsNestedWithinOriginalContainingType(
            INamedTypeSymbol withinType,
            INamedTypeSymbol originalContainingType)
        {
            Contract.ThrowIfNull(withinType);
            Contract.ThrowIfNull(originalContainingType);

            // Walk up my parent chain and see if I eventually hit the owner.  If so then i'm a
            // nested type of that owner and i'm allowed access to everything inside of it.
            var current = withinType.OriginalDefinition;
            while (current != null)
            {
                Contract.Requires(current.IsDefinition);
                if (current.Equals(originalContainingType))
                {
                    return true;
                }

                // NOTE(cyrusn): The container of an 'original' type is always original. 
                current = current.ContainingType;
            }

            return false;
        }
    }
}