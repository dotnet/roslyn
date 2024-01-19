// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PopulateSwitch
{
    internal static class PopulateSwitchExpressionHelpers
    {
        public static ICollection<ISymbol> GetMissingEnumMembers(ISwitchExpressionOperation operation)
        {
            var switchExpression = operation.Value;
            var switchExpressionType = switchExpression?.Type;

            // Check if the type of the expression is a nullable INamedTypeSymbol
            // if the type is both nullable and an INamedTypeSymbol extract the type argument from the nullable
            // and check if it is of enum type
            if (switchExpressionType != null)
                switchExpressionType = switchExpressionType.IsNullable(out var underlyingType) ? underlyingType : switchExpressionType;

            if (switchExpressionType?.TypeKind == TypeKind.Enum)
            {
                var enumMembers = new Dictionary<long, ISymbol>();
                if (PopulateSwitchStatementHelpers.TryGetAllEnumMembers(switchExpressionType, enumMembers))
                {
                    RemoveExistingEnumMembers(operation, enumMembers);
                    return enumMembers.Values;
                }
            }

            return SpecializedCollections.EmptyCollection<ISymbol>();
        }

        public static bool HasNullSwitchArm(ISwitchExpressionOperation operation)
        {
            foreach (var arm in operation.Arms)
            {
                if (arm.Pattern is IConstantPatternOperation { Value.ConstantValue: { HasValue: true, Value: null } })
                    return true;
            }

            return false;
        }

        private static void RemoveExistingEnumMembers(
            ISwitchExpressionOperation operation, Dictionary<long, ISymbol> enumMembers)
        {
            foreach (var arm in operation.Arms)
            {
                RemoveIfConstantPatternHasValue(arm.Pattern, enumMembers);
                if (arm.Pattern is IBinaryPatternOperation binaryPattern)
                {
                    HandleBinaryPattern(binaryPattern, enumMembers);
                }
            }
        }

        private static void HandleBinaryPattern(IBinaryPatternOperation? binaryPattern, Dictionary<long, ISymbol> enumMembers)
        {
            if (binaryPattern?.OperatorKind == BinaryOperatorKind.Or)
            {
                RemoveIfConstantPatternHasValue(binaryPattern.LeftPattern, enumMembers);
                RemoveIfConstantPatternHasValue(binaryPattern.RightPattern, enumMembers);

                HandleBinaryPattern(binaryPattern.LeftPattern as IBinaryPatternOperation, enumMembers);
                HandleBinaryPattern(binaryPattern.RightPattern as IBinaryPatternOperation, enumMembers);
            }
        }

        private static void RemoveIfConstantPatternHasValue(IOperation operation, Dictionary<long, ISymbol> enumMembers)
        {
            if (operation is IConstantPatternOperation { Value.ConstantValue: { HasValue: true, Value: not null and var value } })
                enumMembers.Remove(IntegerUtilities.ToInt64(value));
        }

        public static bool HasDefaultCase(ISwitchExpressionOperation operation)
            => operation.Arms.Any(IsDefault);

        public static bool IsDefault(ISwitchExpressionArmOperation arm)
            => IsDefault(arm.Pattern);

        public static bool IsDefault(IPatternOperation pattern)
            => pattern switch
            {
                // _ => ...
                IDiscardPatternOperation => true,
                // var v => ...
                IDeclarationPatternOperation declarationPattern => declarationPattern.MatchesNull,
                IBinaryPatternOperation binaryPattern => binaryPattern.OperatorKind switch
                {
                    // x or _ => ...
                    BinaryOperatorKind.Or => IsDefault(binaryPattern.LeftPattern) || IsDefault(binaryPattern.RightPattern),
                    // _ and var x => ...
                    BinaryOperatorKind.And => IsDefault(binaryPattern.LeftPattern) && IsDefault(binaryPattern.RightPattern),
                    _ => false,
                },
                _ => false
            };
    }
}
