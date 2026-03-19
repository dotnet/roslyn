// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SimplifyThisOrMe;

internal abstract partial class AbstractSimplifyThisOrMeCodeFixProvider<TMemberAccessExpressionSyntax>
    : SyntaxEditorBasedCodeFixProvider
    where TMemberAccessExpressionSyntax : SyntaxNode
{
    protected abstract string GetTitle();
    protected abstract SyntaxNode Rewrite(SyntaxNode root, ISet<TMemberAccessExpressionSyntax> memberAccessNodes);

    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [IDEDiagnosticIds.RemoveThisOrMeQualificationDiagnosticId];

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, GetTitle(), IDEDiagnosticIds.RemoveThisOrMeQualificationDiagnosticId);
        return Task.CompletedTask;
    }

    protected sealed override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
        var memberAccessNodes = diagnostics.Select(
            d => (TMemberAccessExpressionSyntax)d.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken)).ToSet();

        var newRoot = Rewrite(root, memberAccessNodes);
        editor.ReplaceNode(root, newRoot);
    }
}
