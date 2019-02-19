// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Extensions
{
    internal static class ITypeSymbolExtensions
    {
        public static bool IsPrimitiveType(this ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_Double:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_SByte:
                case SpecialType.System_Single:
                    return true;
                default:
                    return false;
            }
        }

        public static bool Inherits(this ITypeSymbol type, ITypeSymbol possibleBase)
        {
            if (type == null || possibleBase == null)
            {
                return false;
            }

            switch (possibleBase.TypeKind)
            {
                case TypeKind.Class:
                    if (type.TypeKind == TypeKind.Interface)
                    {
                        return false;
                    }

                    return DerivesFrom(type, possibleBase, baseTypesOnly: true);

                case TypeKind.Interface:
                    return DerivesFrom(type, possibleBase);

                default:
                    return false;
            }
        }

        public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol type)
        {
            INamedTypeSymbol current = type.BaseType;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
        {
            ITypeSymbol current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static bool DerivesFrom(this ITypeSymbol symbol, ITypeSymbol candidateBaseType, bool baseTypesOnly = false, bool checkTypeParameterConstraints = true)
        {
            if (candidateBaseType == null || symbol == null)
            {
                return false;
            }

            if (!baseTypesOnly)
            {
                var allInterfaces = symbol.AllInterfaces.OfType<ITypeSymbol>();
                if (candidateBaseType.OriginalDefinition.Equals(candidateBaseType))
                {
                    // Candidate base type is not a constructed generic type, so use original definition for interfaces.
                    allInterfaces = allInterfaces.Select(i => i.OriginalDefinition);
                }

                if (allInterfaces.Contains(candidateBaseType))
                {
                    return true;
                }
            }

            if (checkTypeParameterConstraints && symbol.TypeKind == TypeKind.TypeParameter)
            {
                var typeParameterSymbol = (ITypeParameterSymbol)symbol;
                foreach (var constraintType in typeParameterSymbol.ConstraintTypes)
                {
                    if (constraintType.DerivesFrom(candidateBaseType, baseTypesOnly, checkTypeParameterConstraints))
                    {
                        return true;
                    }
                }
            }

            while (symbol != null)
            {
                if (symbol.Equals(candidateBaseType))
                {
                    return true;
                }

                symbol = symbol.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Indicates if the given <paramref name="type"/> implements <paramref name="iDisposable"/>.
        /// </summary>
        public static bool ImplementsIDisposable(this ITypeSymbol type, INamedTypeSymbol iDisposable)
            => iDisposable != null && type.AllInterfaces.Contains(iDisposable);

        /// <summary>
        /// Indicates if the given <paramref name="type"/> is a reference type that implements <paramref name="iDisposable"/> or is <see cref="IDisposable"/> type itself.
        /// </summary>
        public static bool IsDisposable(this ITypeSymbol type, INamedTypeSymbol iDisposable)
            => type.IsReferenceType && (type == iDisposable || type.ImplementsIDisposable(iDisposable));

        public static IEnumerable<AttributeData> GetApplicableAttributes(this INamedTypeSymbol type)
        {
            var attributes = new List<AttributeData>();

            while (type != null)
            {
                attributes.AddRange(type.GetAttributes());

                type = type.BaseType;
            }

            return attributes;
        }

        public static bool IsAttribute(this ITypeSymbol symbol)
        {
            for (INamedTypeSymbol b = symbol.BaseType; b != null; b = b.BaseType)
            {
                if (b.MetadataName == "Attribute" &&
                     b.ContainingType == null &&
                     b.ContainingNamespace != null &&
                     b.ContainingNamespace.Name == "System" &&
                     b.ContainingNamespace.ContainingNamespace != null &&
                     b.ContainingNamespace.ContainingNamespace.IsGlobalNamespace)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasValueCopySemantics(this ITypeSymbol typeSymbol)
            => typeSymbol.IsValueType || typeSymbol.SpecialType == SpecialType.System_String;

        public static bool IsNonNullableValueType(this ITypeSymbol typeSymbol)
            => typeSymbol != null && typeSymbol.IsValueType && typeSymbol.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T;

        public static bool IsNullableValueType(this ITypeSymbol typeSymbol)
            => typeSymbol != null && typeSymbol.IsValueType && typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

        public static bool IsReferenceTypeOrNullableValueType(this ITypeSymbol typeSymbol)
            => typeSymbol != null && (typeSymbol.IsReferenceType || typeSymbol.IsNullableValueType());

        public static Accessibility DetermineMinimalAccessibility(this ITypeSymbol typeSymbol)
        {
            return typeSymbol.Accept(MinimalAccessibilityVisitor.Instance);
        }

        private class MinimalAccessibilityVisitor : SymbolVisitor<Accessibility>
        {
            public static readonly SymbolVisitor<Accessibility> Instance = new MinimalAccessibilityVisitor();

            public override Accessibility DefaultVisit(ISymbol node)
            {
                throw new NotImplementedException();
            }

            public override Accessibility VisitAlias(IAliasSymbol symbol)
            {
                return symbol.Target.Accept(this);
            }

            public override Accessibility VisitArrayType(IArrayTypeSymbol symbol)
            {
                return symbol.ElementType.Accept(this);
            }

            public override Accessibility VisitDynamicType(IDynamicTypeSymbol symbol)
            {
                return Accessibility.Public;
            }

            public override Accessibility VisitNamedType(INamedTypeSymbol symbol)
            {
                Accessibility accessibility = symbol.DeclaredAccessibility;

                foreach (ITypeSymbol arg in symbol.TypeArguments)
                {
                    accessibility = CommonAccessibilityUtilities.Minimum(accessibility, arg.Accept(this));
                }

                if (symbol.ContainingType != null)
                {
                    accessibility = CommonAccessibilityUtilities.Minimum(accessibility, symbol.ContainingType.Accept(this));
                }

                return accessibility;
            }

            public override Accessibility VisitPointerType(IPointerTypeSymbol symbol)
            {
                return symbol.PointedAtType.Accept(this);
            }

            public override Accessibility VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                // TODO(cyrusn): Do we have to consider the constraints?
                return Accessibility.Public;
            }
        }
    }
}
