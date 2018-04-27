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
                throw new ArgumentException(CodeAnalysisResources.IsAccessibleBadWithin, nameof(within));
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Alias:
                    return IsAccessibleWithin(((IAliasSymbol)symbol).Target, within);
                case SymbolKind.ArrayType:
                    return IsAccessibleWithin(((IArrayTypeSymbol)symbol).ElementType, within);
                case SymbolKind.PointerType:
                    return IsAccessibleWithin(((IPointerTypeSymbol)symbol).PointedAtType, within);
                case SymbolKind.ErrorType:
                    // Error types arise from error recovery. We permit access to enable further analysis.
                    return true;
                case SymbolKind.NamedType:
                    return IsNamedTypeAccessibleWithin((INamedTypeSymbol)symbol, within);
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
                    if (method.MethodKind == MethodKind.BuiltinOperator)
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

        private static bool IsNamedTypeAccessibleWithin(INamedTypeSymbol type, ISymbol within)
        {
            Debug.Assert(within is INamedTypeSymbol || within is IAssemblySymbol);
            Debug.Assert(type != null);

            if (!type.IsDefinition)
            {
                foreach (var typeArg in type.TypeArguments)
                {
                    if (!IsAccessibleWithin(typeArg, within))
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
                return IsMemberAccessible(containingType, type.DeclaredAccessibility, within, throughTypeOpt: null);
            }
        }

        private static bool IsNonNestedTypeAccessible(IAssemblySymbol assembly, Accessibility declaredAccessibility, ISymbol within)
        {
            Debug.Assert(within is INamedTypeSymbol || within is IAssemblySymbol);
            Debug.Assert(assembly != null);

            switch (declaredAccessibility)
            {
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

                case Accessibility.NotApplicable:
                default:
                    throw ExceptionUtilities.UnexpectedValue(declaredAccessibility);
            }
        }

        private static bool HasInternalAccessTo(this IAssemblySymbol fromAssembly, IAssemblySymbol toAssembly)
        {
            if (fromAssembly.Identity.Equals(toAssembly.Identity))
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

            // This is a shortcut optimization of the more complex test for the most common situation.
            if (within == containingType)
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

            var originalThroughTypeOpt = throughTypeOpt?.OriginalDefinition;
            for (INamedTypeSymbol current = withinType.OriginalDefinition; current != null; current = current.ContainingType)
            {
                if (current.InheritsFromIgnoringConstruction(originalContainingType))
                {
                    if (originalThroughTypeOpt?.InheritsFromIgnoringConstruction(current) != false)
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
                if (SameOriginalNamedType(current, possiblyContainingOriginalType))
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
                if (SameOriginalNamedType(baseType, current as INamedTypeSymbol))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SameOriginalNamedType(INamedTypeSymbol t1, INamedTypeSymbol t2)
        {
            Debug.Assert(t1 != null);
            Debug.Assert(t1.IsDefinition);
            Debug.Assert(t2?.IsDefinition != false);

            // We expect a given named type definition to satisfy reference identity.
            if (t1 == t2)
            {
                return true;
            }

            // We relax this to permit a type to be represented by multiple symbols (e.g. separate compilations).
            // Note that the same symbol is expected to have the identical name as itself, despite VB language rules.
            return t2 != null && t1.Name == t2.Name && t1.Arity == t2.Arity && SameOriginalSymbol(t1.ContainingSymbol, t2.ContainingSymbol);
        }

        private static bool SameOriginalSymbol(ISymbol s1, ISymbol s2)
        {
            if (s1 == s2)
            {
                return true;
            }

            if (s1 == null || s2 == null)
            {
                return false;
            }

            switch (s1.Kind)
            {
                case SymbolKind.NamedType:
                    return SameOriginalNamedType(s1 as INamedTypeSymbol, s2 as INamedTypeSymbol);
                case SymbolKind.Namespace:
                    return SameNamespace(s1 as INamespaceSymbol, s2 as INamespaceSymbol);
                case SymbolKind.Assembly:
                    return SameAssembly(s1 as IAssemblySymbol, s2 as IAssemblySymbol);
                case SymbolKind.NetModule:
                    return SameModule(s1 as IModuleSymbol, s2 as IModuleSymbol);
                default:
                    return false;
            }
        }

        private static bool SameModule(IModuleSymbol m1, IModuleSymbol m2)
        {
            Debug.Assert(m1 != null);
            // We don't need to check the module name, as modules are effectively merged in the containing assembly.
            return m2 != null && SameOriginalSymbol(m1.ContainingSymbol, m2.ContainingSymbol);
        }

        private static bool SameAssembly(IAssemblySymbol a1, IAssemblySymbol a2)
        {
            Debug.Assert(a1 != null);
            return a2 != null && a1.Identity.Equals(a2.Identity);
        }

        private static bool SameNamespace(INamespaceSymbol n1, INamespaceSymbol n2)
        {
            Debug.Assert(n1 != null);
            // Note that the same symbol is expected to have the identical name as itself, despite VB language rules.
            return n2 != null && n1.Name == n2.Name && SameOriginalSymbol(n1.ContainingSymbol, n2.ContainingSymbol);
        }
    }
}
