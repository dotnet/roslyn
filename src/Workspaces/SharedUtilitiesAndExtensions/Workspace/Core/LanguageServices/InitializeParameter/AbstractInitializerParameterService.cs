// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InitializeParameter;

using static InitializeParameterHelpersCore;

internal abstract class AbstractInitializerParameterService<TStatementSyntax>
    : IInitializeParameterService
    where TStatementSyntax : SyntaxNode
{
    protected abstract bool IsFunctionDeclaration(SyntaxNode node);

    protected abstract SyntaxNode? GetAccessorBody(IMethodSymbol accessor, CancellationToken cancellationToken);

    protected abstract SyntaxNode GetBody(SyntaxNode methodNode);
    protected abstract SyntaxNode? TryGetLastStatement(IBlockOperation? blockStatement);

    protected abstract bool TryUpdateTupleAssignment(IBlockOperation? blockStatement, IParameterSymbol parameter, ISymbol fieldOrProperty, SyntaxEditor editor);
    protected abstract Task<Solution> TryAddAssignmentForPrimaryConstructorAsync(
        Document document, IParameterSymbol parameter, ISymbol fieldOrProperty, CancellationToken cancellationToken);

    protected abstract void InsertStatement(
        SyntaxEditor editor, SyntaxNode functionDeclaration, bool returnsVoid, SyntaxNode? statementToAddAfter, TStatementSyntax statement);

    public bool TryGetBlockForSingleParameterInitialization(
        SyntaxNode functionDeclaration,
        SemanticModel semanticModel,
        ISyntaxFactsService syntaxFacts,
        CancellationToken cancellationToken,
        out IBlockOperation? blockStatement)
    {
        blockStatement = null;

        var functionBody = GetBody(functionDeclaration);
        if (functionBody == null)
        {
            // We support initializing parameters, even when the containing member doesn't have a
            // body. This is useful for when the user is typing a new constructor and hasn't written
            // the body yet.
            return true;
        }

        // In order to get the block operation for the body of an anonymous function, we need to
        // get it via `IAnonymousFunctionOperation.Body` instead of getting it directly from the body syntax.

        var operation = semanticModel.GetOperation(
            syntaxFacts.IsAnonymousFunctionExpression(functionDeclaration) ? functionDeclaration : functionBody,
            cancellationToken);

        if (operation == null)
            return false;

        switch (operation.Kind)
        {
            case OperationKind.AnonymousFunction:
                blockStatement = ((IAnonymousFunctionOperation)operation).Body;
                break;
            case OperationKind.Block:
                blockStatement = (IBlockOperation)operation;
                break;
            default:
                return false;
        }

        return true;
    }

    public void InsertStatement(SyntaxEditor editor, SyntaxNode functionDeclaration, bool returnsVoid, SyntaxNode? statementToAddAfter, SyntaxNode statement)
        => InsertStatement(editor, functionDeclaration, returnsVoid, statementToAddAfter, (TStatementSyntax)statement);

    public async Task<Solution> AddAssignmentAsync(
        Document document,
        IParameterSymbol parameter,
        ISymbol fieldOrProperty,
        CancellationToken cancellationToken)
    {
        if (parameter is { DeclaringSyntaxReferences: [var parameterReference] })
        {
            var parameterDeclaration = parameterReference.GetSyntax(cancellationToken);

            var functionDeclaration = parameterDeclaration.FirstAncestorOrSelf<SyntaxNode>(IsFunctionDeclaration);
            if (functionDeclaration is not null)
            {
                // try to handle the case where the parameter is within something function-like (like a constructor)

                return await TryAddAssignmentForFunctionLikeDeclarationAsync(
                    document, parameter, fieldOrProperty, functionDeclaration, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // try to handle primary constructor case.
                return await TryAddAssignmentForPrimaryConstructorAsync(
                    document, parameter, fieldOrProperty, cancellationToken).ConfigureAwait(false);
            }
        }

        return document.Project.Solution;
    }

    private async Task<Solution> TryAddAssignmentForFunctionLikeDeclarationAsync(
        Document document,
        IParameterSymbol parameter,
        ISymbol fieldOrProperty,
        SyntaxNode functionDeclaration,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        if (TryGetBlockForSingleParameterInitialization(functionDeclaration, semanticModel, syntaxFacts, cancellationToken, out var blockStatementOpt))
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, document.Project.Solution.Services);
            AddAssignment(functionDeclaration, blockStatementOpt, parameter, fieldOrProperty, editor);

            var newDocument = document.WithSyntaxRoot(editor.GetChangedRoot());
            return newDocument.Project.Solution;
        }

        return document.Project.Solution;
    }

    private void AddAssignment(
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

    public bool IsThrowNotImplementedProperty(Compilation compilation, IPropertySymbol property, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var accessors);

        if (property.GetMethod != null)
            accessors.AddIfNotNull(GetAccessorBody(property.GetMethod, cancellationToken));

        if (property.SetMethod != null)
            accessors.AddIfNotNull(GetAccessorBody(property.SetMethod, cancellationToken));

        if (accessors.Count == 0)
            return false;

        foreach (var group in accessors.GroupBy(node => node.SyntaxTree))
        {
            var semanticModel = compilation.GetSemanticModel(group.Key);
            foreach (var accessorBody in accessors)
            {
                var operation = semanticModel.GetOperation(accessorBody, cancellationToken);
                if (operation is null)
                    return false;

                if (!operation.IsSingleThrowNotImplementedOperation())
                    return false;
            }
        }

        return true;
    }
}
