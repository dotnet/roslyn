// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionExpression;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer;

internal abstract class AbstractUseCollectionInitializerCodeFixProvider<
    TSyntaxKind,
    TExpressionSyntax,
    TStatementSyntax,
    TObjectCreationExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TInvocationExpressionSyntax,
    TExpressionStatementSyntax,
    TLocalDeclarationStatementSyntax,
    TVariableDeclaratorSyntax,
    TAnalyzer>()
    : AbstractUseCollectionExpressionCodeFixProvider<TObjectCreationExpressionSyntax>(
        AnalyzersResources.Collection_initialization_can_be_simplified,
        nameof(AnalyzersResources.Collection_initialization_can_be_simplified))
    where TSyntaxKind : struct
    where TExpressionSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TObjectCreationExpressionSyntax : TExpressionSyntax
    where TMemberAccessExpressionSyntax : TExpressionSyntax
    where TInvocationExpressionSyntax : TExpressionSyntax
    where TExpressionStatementSyntax : TStatementSyntax
    where TLocalDeclarationStatementSyntax : TStatementSyntax
    where TVariableDeclaratorSyntax : SyntaxNode
    where TAnalyzer : AbstractUseCollectionInitializerAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TInvocationExpressionSyntax,
        TExpressionStatementSyntax,
        TLocalDeclarationStatementSyntax,
        TVariableDeclaratorSyntax,
        TAnalyzer>, new()
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseCollectionInitializerDiagnosticId];

    protected abstract TAnalyzer GetAnalyzer();

    protected abstract Task<(SyntaxNode oldNode, SyntaxNode newNode)> GetReplacementNodesAsync(
        Document document, TObjectCreationExpressionSyntax objectCreation, bool useCollectionExpression, ImmutableArray<Match> matches, CancellationToken cancellationToken);

    protected sealed override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        TObjectCreationExpressionSyntax objectCreation,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        // Fix-All for this feature is somewhat complicated.  As Collection-Initializers could be arbitrarily
        // nested, we have to make sure that any edits we make to one Collection-Initializer are seen by any higher
        // ones.  In order to do this we actually process each object-creation-node, one at a time, rewriting the
        // tree for each node.  In order to do this effectively, we use the '.TrackNodes' feature to keep track of
        // all the object creation nodes as we make edits to the tree.  If we didn't do this, then we wouldn't be
        // able to find the second object-creation-node after we make the edit for the first one.
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        using var analyzer = GetAnalyzer();

        var useCollectionExpression = properties.ContainsKey(UseCollectionInitializerHelpers.UseCollectionExpressionName) is true;
        var matches = analyzer.Analyze(
            semanticModel, syntaxFacts, objectCreation, useCollectionExpression, cancellationToken);

        if (matches.IsDefault)
            return;

        var (oldNode, newNode) = await GetReplacementNodesAsync(
            document, objectCreation, useCollectionExpression, matches, cancellationToken).ConfigureAwait(false);

        editor.ReplaceNode(oldNode, newNode);
        foreach (var match in matches)
        {
            if (match.StatementOrExpression is TStatementSyntax statement)
                editor.RemoveNode(statement, SyntaxRemoveOptions.KeepUnbalancedDirectives);
        }
    }
}
