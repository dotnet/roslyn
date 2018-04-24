// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static partial class ISymbolExtensions
    {
        /// <summary>
        /// Checks if 'symbol' is accessible from within 'within', with
        /// an optional qualifier of type 'throughTypeOpt' to be used to resolve
        /// protected access.
        /// </summary>
        public static bool IsAccessibleWithin(
            this ISymbol symbol,
            ISymbol within,
            ITypeSymbol throughTypeOpt = null)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (within == null)
            {
                throw new ArgumentNullException(nameof(within));
            }

            if (!(within is INamedTypeSymbol || within is IAssemblySymbol))
            {
                throw new ArgumentException("must be an INamedTypeSymbol or an IAssemblySymbol", nameof(within));
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Alias:
                    return IsAccessibleWithin(((IAliasSymbol)symbol).Target, within, throughTypeOpt);
                case SymbolKind.ArrayType:
                    return IsAccessibleWithin(((IArrayTypeSymbol)symbol).ElementType, within, throughTypeOpt);
                case SymbolKind.PointerType:
                    return IsAccessibleWithin(((IPointerTypeSymbol)symbol).PointedAtType, within, throughTypeOpt);
                case SymbolKind.ErrorType:
                    return true;
                case SymbolKind.NamedType:
                    return IsNamedTypeAccessibleWithin((INamedTypeSymbol)symbol, within, throughTypeOpt);
                case SymbolKind.TypeParameter:
                case SymbolKind.Parameter:
                case SymbolKind.Local:
                case SymbolKind.Label:
                case SymbolKind.Namespace:
                case SymbolKind.DynamicType:
                case SymbolKind.Assembly:
                case SymbolKind.NetModule:
                case SymbolKind.RangeVariable:
                case SymbolKind.Discard:
                case SymbolKind.Preprocessing:
                    // These types of symbols are always accessible (if visible).
                    return true;
                case SymbolKind.Method:
                    var method = (IMethodSymbol)symbol;
                    if (((IMethodSymbol)symbol).MethodKind == MethodKind.BuiltinOperator)
                    {
                        return true;
                    }

                    goto case SymbolKind.Field;

                case SymbolKind.Property:
                case SymbolKind.Event:
                case SymbolKind.Field:
                    return IsMemberAccessible(symbol.ContainingType, symbol.DeclaredAccessibility, within, symbol.IsStatic ? null : throughTypeOpt);

                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        private static bool IsNamedTypeAccessibleWithin(INamedTypeSymbol type, ISymbol within, ITypeSymbol throughTypeOpt)
        {
            Debug.Assert(within is INamedTypeSymbol || within is IAssemblySymbol);
            Debug.Assert(type != null);

            if (!type.IsDefinition)
            {
                foreach (var typeArg in type.TypeArguments)
                {
                    if (!IsAccessibleWithin(typeArg, within, throughTypeOpt))
                    {
                        return false;
                    }
                }
            }

            var containingType = type.ContainingType;
            if (containingType == null)
            {
                return IsNonNestedTypeAccessible(type.ContainingAssembly, type.DeclaredAccessibility, within);
            }
            else
            {
                return IsMemberAccessible(containingType, type.DeclaredAccessibility, within, null);
            }
        }

        private static bool IsNonNestedTypeAccessible(IAssemblySymbol assembly, Accessibility declaredAccessibility, ISymbol within)
        {
            Debug.Assert(within is INamedTypeSymbol || within is IAssemblySymbol);
            Debug.Assert(assembly != null);

            switch (declaredAccessibility)
            {
                case Accessibility.NotApplicable:
                case Accessibility.Public:
                    // Public symbols are always accessible from any context
                    return true;

                case Accessibility.Private:
                case Accessibility.Protected:
                case Accessibility.ProtectedAndInternal:
                    // Shouldn't happen except in error cases as these access levels cannot be used to declare top-level types.
                    return false;

                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:

                    // An internal type is accessible if we're in the same assembly or we have
                    // friend access to the assembly it was defined in.
                    var withinAssembly = (within as IAssemblySymbol) ?? ((INamedTypeSymbol)within).ContainingAssembly;
                    return withinAssembly.HasInternalAccessTo(assembly);

                default:
                    throw ExceptionUtilities.UnexpectedValue(declaredAccessibility);
            }
        }

        private static bool HasInternalAccessTo(this IAssemblySymbol fromAssembly, IAssemblySymbol toAssembly)
        {
            if (fromAssembly == toAssembly)
            {
                return true;
            }

            if (fromAssembly.IsInteractive && toAssembly.IsInteractive)
            {
                return true;
            }

            return toAssembly.GivesAccessTo(fromAssembly);
        }

        /// <summary>
        /// Is a member with declared accessibility "declaredAccessibility" accessible from within
        /// "within", which must be a named type or an assembly.
        /// </summary>
        private static bool IsMemberAccessible(INamedTypeSymbol containingType, Accessibility declaredAccessibility, ISymbol within, ITypeSymbol throughTypeOpt)
        {
            Debug.Assert(within is INamedTypeSymbol || within is IAssemblySymbol);
            Debug.Assert(containingType != null);

            if (containingType == within)
            {
                return true;
            }

            // A nested symbol is only accessible to us if its container is accessible as well.
            if (!IsAccessibleWithin(containingType, within, throughTypeOpt))
            {
                return false;
            }

            switch (declaredAccessibility)
            {
                case Accessibility.Public:
                case Accessibility.NotApplicable:
                    return true;

                case Accessibility.Private:
                    if (containingType.TypeKind == TypeKind.Submission)
                    {
                        return true;
                    }

                    return within != null && IsPrivateSymbolAccessible(within, containingType);
            }

            var withinType = within as INamedTypeSymbol;
            var withinAssembly = withinType?.ContainingAssembly ?? (IAssemblySymbol)within;
            switch (declaredAccessibility)
            {
                case Accessibility.Internal:
                    return
                        withinAssembly.HasInternalAccessTo(containingType.ContainingAssembly);

                case Accessibility.ProtectedAndInternal:
                    return
                        IsProtectedSymbolAccessible(withinType, throughTypeOpt, containingType) &&
                        withinAssembly.HasInternalAccessTo(containingType.ContainingAssembly);

                case Accessibility.ProtectedOrInternal:
                    return
                        IsProtectedSymbolAccessible(withinType, throughTypeOpt, containingType) ||
                        withinAssembly.HasInternalAccessTo(containingType.ContainingAssembly);

                case Accessibility.Protected:
                    return
                        IsProtectedSymbolAccessible(withinType, throughTypeOpt, containingType);

                default:
                    throw ExceptionUtilities.UnexpectedValue(declaredAccessibility);
            }
        }

        private static bool IsProtectedSymbolAccessible(INamedTypeSymbol withinType, ITypeSymbol throughTypeOpt, INamedTypeSymbol containingType)
        {
            INamedTypeSymbol originalContainingType = containingType.OriginalDefinition;
            if (originalContainingType.TypeKind == TypeKind.Submission)
            {
                return true;
            }

            if (withinType == null)
            {
                // If we're not within a type, we can't access a protected symbol
                return false;
            }

            if (withinType.IsNestedWithinOriginalContainingType(originalContainingType))
            {
                return true;
            }

            var originalThroughTypeOpt = throughTypeOpt == null ? null : throughTypeOpt.OriginalDefinition as ITypeSymbol;
            for (INamedTypeSymbol current = withinType.OriginalDefinition; current != null; current = current.ContainingType)
            {
                if (current.InheritsFromIgnoringConstruction(originalContainingType))
                {
                    if (originalThroughTypeOpt == null ||
                        originalThroughTypeOpt.InheritsFromIgnoringConstruction(current))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsPrivateSymbolAccessible(ISymbol within, INamedTypeSymbol containingType)
        {
            var withinType = within as INamedTypeSymbol;

            // A private symbol is accessible if we're (optionally nested) inside the type that it
            // was defined in.
            return withinType?.IsNestedWithinOriginalContainingType(containingType.OriginalDefinition) == true;
        }

        private static bool IsNestedWithinOriginalContainingType(this INamedTypeSymbol type, INamedTypeSymbol possiblyContainingOriginalType)
        {
            Debug.Assert(possiblyContainingOriginalType.IsDefinition);
            Debug.Assert(type != null);

            for (var current = type.OriginalDefinition; current != null; current = current.ContainingType)
            {
                if (current == possiblyContainingOriginalType)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool InheritsFromIgnoringConstruction(this ITypeSymbol type, INamedTypeSymbol baseType)
        {
            Debug.Assert(type.IsDefinition);
            Debug.Assert(baseType.IsDefinition);

            for (var current = type; current != null; current = current.BaseType?.OriginalDefinition)
            {
                if (current == baseType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
