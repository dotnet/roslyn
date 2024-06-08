// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PopulateSwitch;

internal static class PopulateSwitchStatementHelpers
{
    public const string MissingCases = nameof(MissingCases);
    public const string MissingDefaultCase = nameof(MissingDefaultCase);

    public static bool HasDefaultCase(ISwitchOperation switchStatement)
    {
        // Walk backwards as it's most normally the case that the default case comes at the end.
        for (var index = switchStatement.Cases.Length - 1; index >= 0; index--)
        {
            if (HasDefaultCase(switchStatement.Cases[index]))
                return true;
        }

        return false;
    }

    private static bool HasDefaultCase(ISwitchCaseOperation switchCase)
    {
        foreach (var clause in switchCase.Clauses)
        {
            switch (clause)
            {
                case IDefaultCaseClauseOperation:
                    return true;

                case IPatternCaseClauseOperation patternCaseClause:
                    if (PopulateSwitchExpressionHelpers.IsDefault(patternCaseClause.Pattern))
                    {
                        if (patternCaseClause.Guard is null)
                            return true;

                        if (patternCaseClause.Guard.ConstantValue.Value is true)
                            return true;
                    }

                    continue;
            }
        }

        return false;
    }

    public static ICollection<ISymbol> GetMissingEnumMembers(ISwitchOperation switchStatement)
    {
        var switchExpression = switchStatement.Value;
        var switchExpressionType = switchExpression?.Type;

        var enumMembers = new Dictionary<long, ISymbol>();

        // Check if the type of the expression is a nullable INamedTypeSymbol
        // if the type is both nullable and an INamedTypeSymbol extract the type argument from the nullable
        // and check if it is of enum type
        if (switchExpressionType != null)
            switchExpressionType = switchExpressionType.IsNullable(out var underlyingType) ? underlyingType : switchExpressionType;

        if (switchExpressionType?.TypeKind == TypeKind.Enum)
        {
            if (!TryGetAllEnumMembers(switchExpressionType, enumMembers) ||
                !TryRemoveExistingEnumMembers(switchStatement, enumMembers))
            {
                return [];
            }
        }

        return enumMembers.Values;
    }

    public static bool HasNullSwitchArm(ISwitchOperation operation)
    {
        foreach (var switchCase in operation.Cases)
        {
            foreach (var clause in switchCase.Clauses)
            {
                if (clause is not ISingleValueCaseClauseOperation { Value: var value })
                    continue;

                if (value.ConstantValue is { HasValue: true, Value: null })
                    return true;
            }
        }

        return false;
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
                        if (value is null || !value.ConstantValue.HasValue)
                        {
                            // We had a case which didn't resolve properly.  
                            // Assume the switch is complete.
                            return false;
                        }

                        // null will be casted to 0, which creates a bug,
                        // when a switch with null arm will not add enum's 0 value equivalent.
                        // So we need to avoid it.
                        if (value.ConstantValue.Value is null)
                            continue;

                        var caseValue = IntegerUtilities.ToInt64(value.ConstantValue.Value);
                        enumValues.Remove(caseValue);

                        break;

                    case CaseKind.Pattern:
                        if (((IPatternCaseClauseOperation)clause).Pattern is IBinaryPatternOperation pattern)
                        {
                            PopulateSwitchExpressionHelpers.HandleBinaryPattern(pattern, enumValues);
                        }

                        break;
                }
            }
        }

        return true;
    }

    public static bool TryGetAllEnumMembers(
        ITypeSymbol enumType,
        Dictionary<long, ISymbol> enumValues)
    {
        foreach (var member in enumType.GetMembers())
        {
            // skip `.ctor` and `__value`
            if (member is not IFieldSymbol fieldSymbol || fieldSymbol.Type.SpecialType != SpecialType.None)
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
