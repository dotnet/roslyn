// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Recommendations
{
    internal partial class CSharpRecommendationServiceRunner
    {
        private void AddOperators(ExpressionSyntax originalExpression, ITypeSymbol _, ArrayBuilder<ISymbol> symbols)
        {
            var semanticModel = _context.SemanticModel;
            var container = semanticModel.GetTypeInfo(originalExpression, _cancellationToken).Type;
            if (container == null)
                return;

            var containerWithoutNullable = container.RemoveNullableIfPresent();

            if (IsExcludedOperator(container))
                return;

            var containerIsNullable = container.IsNullable();
            foreach (var type in containerWithoutNullable.GetBaseTypesAndThis())
            {
                foreach (var member in type.GetMembers())
                {
                    if (member is not IMethodSymbol method)
                        continue;

                    if (!method.IsUserDefinedOperator())
                        continue;

                    if (method.Name is WellKnownMemberNames.TrueOperatorName or WellKnownMemberNames.FalseOperatorName)
                        continue;

                    if (containerIsNullable && !IsLiftableOperator(method))
                        continue;

                    symbols.Add(method);
                }
            }
        }

        private static bool IsExcludedOperator(ITypeSymbol container)
        {
            return container.IsSpecialType() ||
                container.SpecialType == SpecialType.System_IntPtr ||
                container.SpecialType == SpecialType.System_UIntPtr;
        }

        private static bool IsLiftableOperator(IMethodSymbol symbol)
        {
            // https://github.com/dotnet/csharplang/blob/main/spec/expressions.md#lifted-operators

            // Common for all:
            if (symbol.IsUserDefinedOperator() && symbol.Parameters.All(p => p.Type.IsValueType && !p.Type.IsNullable()))
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
