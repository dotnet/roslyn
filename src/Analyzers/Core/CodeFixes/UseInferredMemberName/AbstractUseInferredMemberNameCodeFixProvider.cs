// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.UseInferredMemberName;

internal abstract class AbstractUseInferredMemberNameCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    protected abstract void LanguageSpecificRemoveSuggestedNode(SyntaxEditor editor, SyntaxNode node);

    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.UseInferredMemberNameDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, AnalyzersResources.Use_inferred_member_name, nameof(AnalyzersResources.Use_inferred_member_name));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var root = editor.OriginalRoot;

        foreach (var diagnostic in diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            LanguageSpecificRemoveSuggestedNode(editor, node);
        }

        return Task.CompletedTask;
    }
}
