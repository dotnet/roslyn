// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CodeFixes.AddExplicitCast
{
    /// <summary>
    /// The item is the pair of target argument expression and its conversion type
    /// <para/>
    /// Sort pairs using conversion types by inheritance distance from the base type in ascending order,
    /// i.e., less specific type has higher priority because it has less probability to make mistakes
    /// <para/>
    /// For example:
    /// class Base { }
    /// class Derived1 : Base { }
    /// class Derived2 : Derived1 { }
    /// 
    /// void Foo(Derived1 d1) { }
    /// void Foo(Derived2 d2) { }
    /// 
    /// Base b = new Derived1();
    /// Foo([||]b);
    /// 
    /// operations:
    /// 1. Convert type to 'Derived1'
    /// 2. Convert type to 'Derived2'
    /// 
    /// 'Derived1' is less specific than 'Derived2' compared to 'Base'
    /// </summary>
    internal sealed class InheritanceDistanceComparer<TExpressionSyntax>(SemanticModel semanticModel)
    : IComparer<(TExpressionSyntax syntax, ITypeSymbol symbol)>
    where TExpressionSyntax : SyntaxNode
    {
        private readonly SemanticModel _semanticModel = semanticModel;

        public int Compare((TExpressionSyntax syntax, ITypeSymbol symbol) x,
            (TExpressionSyntax syntax, ITypeSymbol symbol) y)
        {
            // if the argument is different, keep the original order
            if (!x.syntax.Equals(y.syntax))
            {
                return 0;
            }
            else
            {
                var baseType = _semanticModel.GetTypeInfo(x.syntax).Type;
                var xDist = GetInheritanceDistance(baseType, x.symbol);
                var yDist = GetInheritanceDistance(baseType, y.symbol);
                return xDist.CompareTo(yDist);
            }
        }

        /// <summary>
        /// Calculate the inheritance distance between baseType and derivedType.
        /// </summary>
        private static int GetInheritanceDistanceRecursive(ITypeSymbol baseType, ITypeSymbol? derivedType)
        {
            if (derivedType == null)
                return int.MaxValue;
            if (derivedType.Equals(baseType))
                return 0;

            var distance = GetInheritanceDistanceRecursive(baseType, derivedType.BaseType);

            if (derivedType.Interfaces.Length != 0)
            {
                foreach (var interfaceType in derivedType.Interfaces)
                {
                    distance = Math.Min(GetInheritanceDistanceRecursive(baseType, interfaceType), distance);
                }
            }

            return distance == int.MaxValue ? distance : distance + 1;
        }

        /// <summary>
        /// Wrapper function of [GetInheritanceDistance], also consider the class with explicit conversion operator
        /// has the highest priority.
        /// </summary>
        private int GetInheritanceDistance(ITypeSymbol? baseType, ITypeSymbol castType)
        {
            if (baseType is null)
                return 0;

            var conversion = _semanticModel.Compilation.ClassifyCommonConversion(baseType, castType);

            // If the node has the explicit conversion operator, then it has the shortest distance
            // since explicit conversion operator is defined by users and has the highest priority 
            var distance = conversion.IsUserDefined ? 0 : GetInheritanceDistanceRecursive(baseType, castType);
            return distance;
        }
    }
}
