// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Recommendations;

internal partial class CSharpRecommendationService
{
    /// <summary>
    /// Adds user defined operators to the unnamed recommendation set.
    /// </summary>
    private sealed partial class CSharpRecommendationServiceRunner
    {
        private static void AddOperators(ITypeSymbol container, ArrayBuilder<ISymbol> symbols)
        {
            var containerWithoutNullable = container.RemoveNullableIfPresent();

            // Don't bother showing operators for basic built-in types.  They're well known already and will only
            // clutter the display.
            if (ExcludeOperatorType(containerWithoutNullable))
                return;

            var containerIsNullable = container.IsNullable();
            foreach (var type in containerWithoutNullable.GetBaseTypesAndThis())
            {
                foreach (var member in type.GetMembers())
                {
                    if (member is not IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator } method)
                        continue;

                    // Don't add operator true/false.  They only are used for conversions in special boolean contexts
                    // (like `if` statement conditions), and are not invoked explicitly by the user.
                    if (method.Name is WellKnownMemberNames.TrueOperatorName or WellKnownMemberNames.FalseOperatorName)
                        continue;

                    // If we're on a nullable version of the type, but this operator wouldn't naturally 'lift' to be
                    // available for it, then don't include it.
                    if (containerIsNullable && !IsLiftableOperator(method))
                        continue;

                    // We don't need to bother lifting operators. We'll just show the basic operator in the list as the
                    // information for it is sufficient for completion (i.e. we only insert the operator itself, not any
                    // of the parameter or return types).
                    symbols.Add(method);
                }
            }
        }

        private static bool ExcludeOperatorType(ITypeSymbol container)
            => container.IsSpecialType() || container.SpecialType is SpecialType.System_IntPtr or SpecialType.System_UIntPtr;

        private static bool IsLiftableOperator(IMethodSymbol symbol)
        {
            // https://github.com/dotnet/csharplang/blob/main/spec/expressions.md#lifted-operators

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
                    case WellKnownMemberNames.UnsignedRightShiftOperatorName:
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
