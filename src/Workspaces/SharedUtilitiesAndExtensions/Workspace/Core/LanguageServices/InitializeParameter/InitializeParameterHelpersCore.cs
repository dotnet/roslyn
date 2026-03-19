// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.InitializeParameter;

internal static class InitializeParameterHelpersCore
{
    public static ImmutableArray<(IParameterSymbol parameter, bool before)> GetSiblingParameters(IParameterSymbol parameter)
    {
        using var _ = ArrayBuilder<(IParameterSymbol, bool before)>.GetInstance(out var siblings);

        if (parameter.ContainingSymbol is IMethodSymbol method)
        {
            var parameterIndex = method.Parameters.IndexOf(parameter);

            // look for an existing assignment for a parameter that comes before us.
            // If we find one, we'll add ourselves after that parameter check.
            for (var i = parameterIndex - 1; i >= 0; i--)
                siblings.Add((method.Parameters[i], before: true));

            // look for an existing check for a parameter that comes before us.
            // If we find one, we'll add ourselves after that parameter check.
            for (var i = parameterIndex + 1; i < method.Parameters.Length; i++)
                siblings.Add((method.Parameters[i], before: false));
        }

        return siblings.ToImmutableAndClear();
    }

    public static bool IsParameterReference(IOperation? operation, IParameterSymbol parameter)
        => operation.UnwrapImplicitConversion() is IParameterReferenceOperation parameterReference &&
           parameter.Equals(parameterReference.Parameter);

    public static bool IsParameterReferenceOrCoalesceOfParameterReference(
       IOperation? value, IParameterSymbol parameter)
    {
        if (IsParameterReference(value, parameter))
        {
            // We already have a member initialized with this parameter like:
            //      this.field = parameter
            return true;
        }

        if (value.UnwrapImplicitConversion() is ICoalesceOperation coalesceExpression &&
            IsParameterReference(coalesceExpression.Value, parameter))
        {
            // We already have a member initialized with this parameter like:
            //      this.field = parameter ?? ...
            return true;
        }

        return false;
    }

    public static string GenerateUniqueName(IParameterSymbol parameter, ImmutableArray<string> parameterNameParts, NamingRule rule)
    {
        // Determine an appropriate name to call the new field.
        var containingType = parameter.ContainingType;
        var baseName = rule.NamingStyle.CreateName(parameterNameParts);

        // Ensure that the name is unique in the containing type so we
        // don't stomp on an existing member.
        var uniqueName = NameGenerator.GenerateUniqueName(
            baseName, n => containingType.GetMembers(n).IsEmpty);
        return uniqueName;
    }

    public static IOperation? TryFindFieldOrPropertyAssignmentStatement(IParameterSymbol parameter, IBlockOperation? blockStatement)
        => TryFindFieldOrPropertyAssignmentStatement(parameter, blockStatement, out _);

    private static IOperation? TryFindFieldOrPropertyAssignmentStatement(
        IParameterSymbol parameter, IBlockOperation? blockStatement, out ISymbol? fieldOrProperty)
    {
        if (blockStatement != null)
        {
            var containingType = parameter.ContainingType;
            foreach (var statement in blockStatement.Operations)
            {
                // look for something of the form:  "this.s = s" or "this.s = s ?? ..."
                if (IsFieldOrPropertyAssignment(statement, containingType, out var assignmentExpression, out fieldOrProperty) &&
                    IsParameterReferenceOrCoalesceOfParameterReference(assignmentExpression, parameter))
                {
                    return statement;
                }

                // look inside the form `(this.s, this.t) = (s, t)`
                if (TryGetPartsOfTupleAssignmentOperation(statement, out var targetTuple, out var valueTuple))
                {
                    for (int i = 0, n = targetTuple.Elements.Length; i < n; i++)
                    {
                        var target = targetTuple.Elements[i];
                        var value = valueTuple.Elements[i];

                        if (IsFieldOrPropertyReference(target, containingType, out fieldOrProperty) &&
                            IsParameterReference(value, parameter))
                        {
                            return statement;
                        }
                    }
                }
            }
        }

        fieldOrProperty = null;
        return null;
    }

    public static bool TryGetPartsOfTupleAssignmentOperation(
        IOperation operation,
        [NotNullWhen(true)] out ITupleOperation? targetTuple,
        [NotNullWhen(true)] out ITupleOperation? valueTuple)
    {
        if (operation is IExpressionStatementOperation
            {
                Operation: IDeconstructionAssignmentOperation
                {
                    Target: ITupleOperation targetTupleTemp,
                    Value: IConversionOperation { Operand: ITupleOperation valueTupleTemp },
                }
            } &&
            targetTupleTemp.Elements.Length == valueTupleTemp.Elements.Length)
        {
            targetTuple = targetTupleTemp;
            valueTuple = valueTupleTemp;
            return true;
        }

        targetTuple = null;
        valueTuple = null;
        return false;
    }

    private static bool IsParameterReferenceOrCoalesceOfParameterReference(
        IAssignmentOperation assignmentExpression, IParameterSymbol parameter)
        => IsParameterReferenceOrCoalesceOfParameterReference(assignmentExpression.Value, parameter);

    public static bool IsFieldOrPropertyReference(
        IOperation? operation, INamedTypeSymbol containingType,
        [NotNullWhen(true)] out ISymbol? fieldOrProperty)
    {
        if (operation is IMemberReferenceOperation memberReference &&
            memberReference.Member.ContainingType.Equals(containingType))
        {
            if (memberReference.Member is IFieldSymbol or IPropertySymbol)
            {
                fieldOrProperty = memberReference.Member;
                return true;
            }
        }

        fieldOrProperty = null;
        return false;
    }

    public static bool IsFieldOrPropertyAssignment(IOperation statement, INamedTypeSymbol containingType, [NotNullWhen(true)] out IAssignmentOperation? assignmentExpression)
        => IsFieldOrPropertyAssignment(statement, containingType, out assignmentExpression, out _);

    public static bool IsFieldOrPropertyAssignment(
        IOperation statement, INamedTypeSymbol containingType,
        [NotNullWhen(true)] out IAssignmentOperation? assignmentExpression,
        [NotNullWhen(true)] out ISymbol? fieldOrProperty)
    {
        if (statement is IExpressionStatementOperation expressionStatement &&
            expressionStatement.Operation is IAssignmentOperation assignment)
        {
            assignmentExpression = assignment;
            return InitializeParameterHelpersCore.IsFieldOrPropertyReference(assignmentExpression.Target, containingType, out fieldOrProperty);
        }

        fieldOrProperty = null;
        assignmentExpression = null;
        return false;
    }
}
