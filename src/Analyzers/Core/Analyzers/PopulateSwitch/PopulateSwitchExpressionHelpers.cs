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

            var enumMembers = new Dictionary<long, ISymbol>();

            // Check if the type of the expression is a nullable INamedTypeSymbol
            // if the type is both nullable and an INamedTypeSymbol extract the type argument from the nullable
            // and check if it is of enum type
            if (switchExpressionType != null)
                switchExpressionType = switchExpressionType.IsNullable(out var underlyingType) ? underlyingType : switchExpressionType;

            if (switchExpressionType?.TypeKind == TypeKind.Enum)
            {
                if (!PopulateSwitchStatementHelpers.TryGetAllEnumMembers(switchExpressionType, enumMembers) ||
                    !TryRemoveExistingEnumMembers(operation, enumMembers))
                {
                    return SpecializedCollections.EmptyCollection<ISymbol>();
                }
            }

            return enumMembers.Values;
        }

        private static bool TryRemoveExistingEnumMembers(
            ISwitchExpressionOperation operation, Dictionary<long, ISymbol> enumMembers)
        {
            foreach (var arm in operation.Arms)
            {
                if (arm.Pattern is IConstantPatternOperation constantPattern)
                {
                    var constantValue = constantPattern.Value.ConstantValue;
                    if (!constantValue.HasValue)
                    {
                        // We had a case which didn't resolve properly.
                        // Assume the switch is complete.
                        return false;
                    }

                    enumMembers.Remove(IntegerUtilities.ToInt64(constantValue.Value));
                }
                else if (arm.Pattern is IBinaryPatternOperation binaryPattern)
                {
                    HandleBinaryPattern(binaryPattern, enumMembers);
                }
            }

            return true;
        }

        private static void HandleBinaryPattern(IBinaryPatternOperation? binaryPattern, Dictionary<long, ISymbol> enumMembers)
        {
            if (binaryPattern?.OperatorKind == BinaryOperatorKind.Or)
            {
                RemoveIfConstantPatternHasValue(binaryPattern.LeftPattern);
                RemoveIfConstantPatternHasValue(binaryPattern.RightPattern);
                
                HandleBinaryPattern(binaryPattern.LeftPattern as IBinaryPatternOperation, enumMembers);
                HandleBinaryPattern(binaryPattern.RightPattern as IBinaryPatternOperation, enumMembers);
            }

            void RemoveIfConstantPatternHasValue(IOperation operation)
            {
                if (operation is IConstantPatternOperation { Value: { ConstantValue: { HasValue: true, Value: var value } } })
                        enumMembers.Remove(IntegerUtilities.ToInt64(value));
            }
        }

        public static bool HasDefaultCase(ISwitchExpressionOperation operation)
            => operation.Arms.Any(a => IsDefault(a));

        public static bool IsDefault(ISwitchExpressionArmOperation arm)
        {
            if (arm.Pattern.Kind == OperationKind.DiscardPattern)
                return true;

            if (arm.Pattern is IDeclarationPatternOperation declarationPattern)
                return declarationPattern.MatchesNull;

            return false;
        }
    }
}
