// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.AnalyzerPowerPack.Utilities
{
    public static class ITypeSymbolExtensions
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

            if (type.Equals(possibleBase))
            {
                return true;
            }

            switch (possibleBase.TypeKind)
            {
                case TypeKind.Class:
                    for (ITypeSymbol t = type.BaseType; t != null; t = t.BaseType)
                    {
                        if (t.Equals(possibleBase))
                        {
                            return true;
                        }
                    }

                    return false;

                case TypeKind.Interface:
                    foreach (var i in type.AllInterfaces)
                    {
                        if (i.Equals(possibleBase))
                        {
                            return true;
                        }
                    }

                    return false;

                default:
                    return false;
            }
        }

        public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol type)
        {
            var current = type.BaseType;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
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
                var accessibility = symbol.DeclaredAccessibility;

                foreach (var arg in symbol.TypeArguments)
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
