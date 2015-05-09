// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace System.Runtime.Analyzers
{
    internal static class INamedTypeSymbolExtensions
    {
        public static IEnumerable<INamedTypeSymbol> GetBaseTypesAndThis(this INamedTypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static bool IsOperatorImplemented(this INamedTypeSymbol symbol, string op)
        {
            // TODO: should this filter on the right-hand-side operator type?
            return symbol.GetMembers(op).OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.UserDefinedOperator).Any();
        }

        public static bool DoesOverrideEquals(this INamedTypeSymbol symbol)
        {
            // Does the symbol override Object.Equals?
            return symbol.GetMembers(WellKnownMemberNames.ObjectEquals).OfType<IMethodSymbol>().Where(m => IsEqualsOverride(m)).Any();
        }

        private static bool IsEqualsOverride(IMethodSymbol method)
        {
            return method.IsOverride &&
                   method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                   method.Parameters.Length == 1 &&
                   method.Parameters[0].Type.SpecialType == SpecialType.System_Object;
        }

        public static bool DoesOverrideGetHashCode(this INamedTypeSymbol symbol)
        {
            // Does the symbol override Object.GetHashCode?
            return symbol.GetMembers(WellKnownMemberNames.ObjectGetHashCode).OfType<IMethodSymbol>().Where(m => IsGetHashCodeOverride(m)).Any();
        }

        private static bool IsGetHashCodeOverride(IMethodSymbol method)
        {
            return method.IsOverride &&
                   method.ReturnType.SpecialType == SpecialType.System_Int32 &&
                   method.Parameters.Length == 0;
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
    }
}
