// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.InitializeParameter;

using static InitializeParameterHelpersCore;

internal abstract class AbstractInitializerParameterService<TStatementSyntax>
    : IInitializeParameterService
    where TStatementSyntax : SyntaxNode
{
    public abstract SyntaxNode GetBody(SyntaxNode methodNode);
    protected abstract SyntaxNode? TryGetLastStatement(IBlockOperation? blockStatement);
    protected abstract bool TryUpdateTupleAssignment(IBlockOperation? blockStatement, IParameterSymbol parameter, ISymbol fieldOrProperty, SyntaxEditor editor);

    protected abstract void InsertStatement(
        SyntaxEditor editor, SyntaxNode functionDeclaration, bool returnsVoid, SyntaxNode? statementToAddAfter, TStatementSyntax statement);

    public void InsertStatement(SyntaxEditor editor, SyntaxNode functionDeclaration, bool returnsVoid, SyntaxNode? statementToAddAfter, SyntaxNode statement)
        => InsertStatement(editor, functionDeclaration, returnsVoid, statementToAddAfter, (TStatementSyntax)statement);

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
