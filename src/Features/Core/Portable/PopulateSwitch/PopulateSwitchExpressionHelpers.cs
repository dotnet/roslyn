// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PopulateSwitch;
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
            }

            return true;
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
