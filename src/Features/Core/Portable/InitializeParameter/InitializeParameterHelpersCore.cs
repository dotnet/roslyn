// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.InitializeParameter;

using static InitializeParameterHelpersCore;

internal interface IInitializeParameterService : ILanguageService
{
    void AddAssignment(
        SyntaxNode constructorDeclaration,
        IBlockOperation? blockStatement,
        IParameterSymbol parameter,
        ISymbol fieldOrProperty,
        SyntaxEditor editor);
}

internal abstract class AbstractInitializerParameterService<TStatementSyntax>
    : IInitializeParameterService
    where TStatementSyntax : SyntaxNode

{
    protected abstract SyntaxNode? TryGetLastStatement(IBlockOperation? blockStatement);
    protected abstract bool TryUpdateTupleAssignment(IBlockOperation? blockStatement, IParameterSymbol parameter, ISymbol fieldOrProperty, SyntaxEditor editor);

    protected abstract void InsertStatement(
        SyntaxEditor editor, SyntaxNode functionDeclaration, bool returnsVoid,
        SyntaxNode? statementToAddAfter, TStatementSyntax statement);

    public void AddAssignment(
        SyntaxNode constructorDeclaration,
        IBlockOperation? blockStatement,
        IParameterSymbol parameter,
        ISymbol fieldOrProperty,
        SyntaxEditor editor)
    {
        // First see if the user has `(_x, y) = (x, y);` and attempt to update that. 
        if (TryUpdateTupleAssignment(blockStatement, parameter, fieldOrProperty, editor))
            return;

        var generator = editor.Generator;

        // Now that we've added any potential members, create an assignment between it
        // and the parameter.
        var initializationStatement = (TStatementSyntax)generator.ExpressionStatement(
            generator.AssignmentStatement(
                generator.MemberAccessExpression(
                    generator.ThisExpression(),
                    generator.IdentifierName(fieldOrProperty.Name)),
                generator.IdentifierName(parameter.Name)));

        // Attempt to place the initialization in a good location in the constructor
        // We'll want to keep initialization statements in the same order as we see
        // parameters for the constructor.
        var statementToAddAfter = TryGetStatementToAddInitializationAfter(parameter, blockStatement);

        InsertStatement(editor, constructorDeclaration, returnsVoid: true, statementToAddAfter, initializationStatement);
    }

    private SyntaxNode? TryGetStatementToAddInitializationAfter(
        IParameterSymbol parameter, IBlockOperation? blockStatement)
    {
        // look for an existing assignment for a parameter that comes before/after us.
        // If we find one, we'll add ourselves before/after that parameter check.
        foreach (var (sibling, before) in GetSiblingParameters(parameter))
        {
            var statement = TryFindFieldOrPropertyAssignmentStatement(sibling, blockStatement);
            if (statement != null)
            {
                if (before)
                {
                    return statement.Syntax;
                }
                else
                {
                    var statementIndex = blockStatement!.Operations.IndexOf(statement);
                    return statementIndex > 0 && blockStatement.Operations[statementIndex - 1] is { IsImplicit: false, Syntax: var priorSyntax }
                        ? priorSyntax
                        : null;
                }
            }
        }

        // We couldn't find a reasonable location for the new initialization statement.
        // Just place ourselves after the last statement in the constructor.
        return TryGetLastStatement(blockStatement);
    }
}

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

    public static IOperation? TryFindFieldOrPropertyAssignmentStatement(
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

    public static bool IsFieldOrPropertyReference(IOperation operation, INamedTypeSymbol containingType)
        => IsFieldOrPropertyAssignment(operation, containingType, out _);

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
