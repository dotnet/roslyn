// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Recommendations
{
    internal partial class CSharpRecommendationServiceRunner
    {
        private static void AddOperators(ITypeSymbol container, ArrayBuilder<ISymbol> symbols)
        {
            if (IsExcludedSymbol(container))
                return;

            var containerIsNullable = container.IsNullable();
            container = container.RemoveNullableIfPresent();

            foreach (var type in container.GetBaseTypesAndThis())
            {
                foreach (var member in type.GetMembers())
                {
                    if (member is not IMethodSymbol method)
                        continue;

                    if (!method.IsUserDefinedOperator())
                        continue;

                    if (method.Name is WellKnownMemberNames.TrueOperatorName or WellKnownMemberNames.FalseOperatorName)
                        continue;

                    if (containerIsNullable && !IsLiftable(method))
                        continue;

                    symbols.Add(method);
                }
            }
        }

        private static bool IsExcludedSymbol(ITypeSymbol container)
        {
            return container.IsSpecialType() ||
                container.SpecialType == SpecialType.System_IntPtr ||
                container.SpecialType == SpecialType.System_UIntPtr;
        }

        private static bool IsLiftable(IMethodSymbol symbol)
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/expressions#lifted-operators

            // Common for all:
            if (symbol.IsUserDefinedOperator() && symbol.Parameters.All(p => p.Type.IsValueType))
            {
                switch (symbol.Name)
                {
                    // Unary
                    case WellKnownMemberNames.UnaryPlusOperatorName:
                    case WellKnownMemberNames.IncrementOperatorName:
                    case WellKnownMemberNames.UnaryNegationOperatorName:
                    case WellKnownMemberNames.DecrementOperatorName:
                    case WellKnownMemberNames.LogicalNotOperatorName:
                    case WellKnownMemberNames.OnesComplementOperatorName:
                        return symbol.Parameters.Length == 1 && symbol.ReturnType.IsValueType;
                    // Binary 
                    case WellKnownMemberNames.AdditionOperatorName:
                    case WellKnownMemberNames.SubtractionOperatorName:
                    case WellKnownMemberNames.MultiplyOperatorName:
                    case WellKnownMemberNames.DivisionOperatorName:
                    case WellKnownMemberNames.ModulusOperatorName:
                    case WellKnownMemberNames.BitwiseAndOperatorName:
                    case WellKnownMemberNames.BitwiseOrOperatorName:
                    case WellKnownMemberNames.ExclusiveOrOperatorName:
                    case WellKnownMemberNames.LeftShiftOperatorName:
                    case WellKnownMemberNames.RightShiftOperatorName:
                        return symbol.Parameters.Length == 2 && symbol.ReturnType.IsValueType;
                    // Equality + Relational 
                    case WellKnownMemberNames.EqualityOperatorName:
                    case WellKnownMemberNames.InequalityOperatorName:

                    case WellKnownMemberNames.LessThanOperatorName:
                    case WellKnownMemberNames.GreaterThanOperatorName:
                    case WellKnownMemberNames.LessThanOrEqualOperatorName:
                    case WellKnownMemberNames.GreaterThanOrEqualOperatorName:
                        return symbol.Parameters.Length == 2 && symbol.ReturnType.SpecialType == SpecialType.System_Boolean;
                }
            }

            return false;
        }
    }
}
