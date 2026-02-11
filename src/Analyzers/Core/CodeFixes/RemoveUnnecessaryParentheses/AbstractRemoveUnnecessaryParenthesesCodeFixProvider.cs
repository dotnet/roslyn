// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

internal abstract class AbstractRemoveUnnecessaryParenthesesCodeFixProvider<TParenthesizedExpressionSyntax>
    : SyntaxEditorBasedCodeFixProvider
    where TParenthesizedExpressionSyntax : SyntaxNode
{
    public override ImmutableArray<string> FixableDiagnosticIds
       => [IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId];

    protected abstract bool CanRemoveParentheses(
        TParenthesizedExpressionSyntax current, SemanticModel semanticModel, CancellationToken cancellationToken);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, AnalyzersResources.Remove_unnecessary_parentheses, nameof(AnalyzersResources.Remove_unnecessary_parentheses));
    }

    protected override Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var originalNodes = diagnostics.SelectAsArray(
            d => (TParenthesizedExpressionSyntax)d.AdditionalLocations[0].FindNode(
                findInsideTrivia: true, getInnermostNodeForTie: true, cancellationToken));

        return editor.ApplyExpressionLevelSemanticEditsAsync(
            document, originalNodes,
            (semanticModel, current) => current != null && CanRemoveParentheses(current, semanticModel, cancellationToken),
            (_, currentRoot, current) => currentRoot.ReplaceNode(current, syntaxFacts.Unparenthesize(current)),
            cancellationToken);
    }
}
