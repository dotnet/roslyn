// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PopulateSwitch
{
    internal static class PopulateSwitchHelpers
    {
        public const string MissingCases = nameof(MissingCases);
        public const string MissingDefaultCase = nameof(MissingDefaultCase);

        public static bool HasDefaultCase(ISwitchOperation switchStatement)
        {
            for (var index = switchStatement.Cases.Length - 1; index >= 0; index--)
            {
                if (HasDefaultCase(switchStatement.Cases[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasDefaultCase(ISwitchCaseOperation switchCase)
        {
            foreach (var clause in switchCase.Clauses)
            {
                if (clause.CaseKind == CaseKind.Default)
                {
                    return true;
                }
            }

            return false;
        }

        public static ICollection<ISymbol> GetMissingEnumMembers(ISwitchOperation switchStatement)
        {
            var switchExpression = switchStatement.Value;
            var switchExpressionType = switchExpression?.Type;

            var enumMembers = new Dictionary<long, ISymbol>();
            if (switchExpressionType?.TypeKind == TypeKind.Enum)
            {
                if (!TryGetAllEnumMembers(switchExpressionType, enumMembers) ||
                    !TryRemoveExistingEnumMembers(switchStatement, enumMembers))
                {
                    return SpecializedCollections.EmptyCollection<ISymbol>();
                }
            }

            return enumMembers.Values;
        }

        private static bool TryRemoveExistingEnumMembers(ISwitchOperation switchStatement, Dictionary<long, ISymbol> enumValues)
        {
            foreach (var switchCase in switchStatement.Cases)
            {
                foreach (var clause in switchCase.Clauses)
                {
                    switch (clause.CaseKind)
                    {
                        default:
                        case CaseKind.None:
                        case CaseKind.Relational:
                        case CaseKind.Range:
                            // This was some sort of complex switch.  For now just ignore
                            // these and assume that they're complete.
                            return false;

                        case CaseKind.Default:
                            // ignore the 'default/else' clause.
                            continue;

                        case CaseKind.SingleValue:
                            var value = ((ISingleValueCaseClauseOperation)clause).Value;
                            if (value == null || !value.ConstantValue.HasValue)
                            {
                                // We had a case which didn't resolve properly.  
                                // Assume the switch is complete.
                                return false;
                            }

                            var caseValue = IntegerUtilities.ToInt64(value.ConstantValue.Value);
                            enumValues.Remove(caseValue);

                            break;
                    }
                }
            }

            return true;
        }

        private static bool TryGetAllEnumMembers(
            ITypeSymbol enumType,
            Dictionary<long, ISymbol> enumValues)
        {
            foreach (var member in enumType.GetMembers())
            {
                // skip `.ctor` and `__value`
                if (!(member is IFieldSymbol fieldSymbol) || fieldSymbol.Type.SpecialType != SpecialType.None)
                {
                    continue;
                }

                if (fieldSymbol.ConstantValue == null)
                {
                    // We have an enum that has problems with it (i.e. non-const members).  We won't
                    // be able to determine properly if the switch is complete.  Assume it is so we
                    // don't offer to do anything.
                    return false;
                }

                // Multiple enum members may have the same value.  Only consider the first one
                // we run int.
                var enumValue = IntegerUtilities.ToInt64(fieldSymbol.ConstantValue);
                if (!enumValues.ContainsKey(enumValue))
                {
                    enumValues.Add(enumValue, fieldSymbol);
                }
            }

            return true;
        }
    }
}
