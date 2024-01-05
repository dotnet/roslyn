// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseObjectInitializer;

internal abstract class AbstractUseObjectInitializerCodeFixProvider<
    TSyntaxKind,
    TExpressionSyntax,
    TStatementSyntax,
    TObjectCreationExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TAssignmentStatementSyntax,
    TLocalDeclarationStatementSyntax,
    TVariableDeclaratorSyntax,
    TAnalyzer>
    : ForkingSyntaxEditorBasedCodeFixProvider<TObjectCreationExpressionSyntax>
    where TSyntaxKind : struct
    where TExpressionSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TObjectCreationExpressionSyntax : TExpressionSyntax
    where TMemberAccessExpressionSyntax : TExpressionSyntax
    where TAssignmentStatementSyntax : TStatementSyntax
    where TLocalDeclarationStatementSyntax : TStatementSyntax
    where TVariableDeclaratorSyntax : SyntaxNode
    where TAnalyzer : AbstractUseNamedMemberInitializerAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TAssignmentStatementSyntax,
        TLocalDeclarationStatementSyntax,
        TVariableDeclaratorSyntax,
        TAnalyzer>, new()
{
    protected override (string title, string equivalenceKey) GetTitleAndEquivalenceKey(CodeFixContext context)
        => (AnalyzersResources.Object_initialization_can_be_simplified, nameof(AnalyzersResources.Object_initialization_can_be_simplified));

    protected abstract TAnalyzer GetAnalyzer();

    protected abstract TStatementSyntax GetNewStatement(
        TStatementSyntax statement, TObjectCreationExpressionSyntax objectCreation,
        ImmutableArray<Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>> matches);

    public override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(IDEDiagnosticIds.UseObjectInitializerDiagnosticId);

    protected override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        TObjectCreationExpressionSyntax objectCreation,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var currentRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        using var analyzer = GetAnalyzer();

        var matches = analyzer.Analyze(semanticModel, syntaxFacts, objectCreation, cancellationToken);
        if (matches.IsDefaultOrEmpty)
            return;

        var statement = objectCreation.FirstAncestorOrSelf<TStatementSyntax>();
        Contract.ThrowIfNull(statement);

        var newStatement = GetNewStatement(statement, objectCreation, matches)
            .WithAdditionalAnnotations(Formatter.Annotation);

        editor.ReplaceNode(statement, newStatement);
        foreach (var match in matches)
            editor.RemoveNode(match.Statement, SyntaxRemoveOptions.KeepUnbalancedDirectives);
    }
}
