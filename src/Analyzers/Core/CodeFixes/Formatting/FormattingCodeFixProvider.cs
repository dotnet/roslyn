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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal abstract class AbstractFormattingCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    protected AbstractFormattingCodeFixProvider()
    {
#if !CODE_STYLE
        // Backdoor that allows this provider to use the high-priority bucket.
        this.CustomTags = this.CustomTags.Add(CodeAction.CanBeHighPriorityTag);
#endif
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.FormattingDiagnosticId];

    protected abstract ISyntaxFormatting SyntaxFormatting { get; }

    /// <summary>
    /// Fixing formatting is high priority.  It's something the user wants to be able to fix quickly, is driven by
    /// them acting on an error reported in code, and can be computed fast as it only uses syntax not semantics.
    /// It's also the 8th most common fix that people use, and is picked almost all the times it is shown.
    /// </summary>
    protected override CodeActionRequestPriority ComputeRequestPriority()
        => CodeActionRequestPriority.High;

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var codeAction = CodeAction.Create(
                AnalyzersResources.Fix_formatting,
                c => FixOneAsync(context, diagnostic, c),
                nameof(AbstractFormattingCodeFixProvider),
                CodeActionPriority.High);

#if !CODE_STYLE
            // Backdoor that allows this provider to use the high-priority bucket.
            codeAction.CustomTags = codeAction.CustomTags.Add(CodeAction.CanBeHighPriorityTag);
#endif

            context.RegisterCodeFix(codeAction, diagnostic);
        }

        return Task.CompletedTask;
    }

    private async Task<Document> FixOneAsync(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var root = await context.Document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = root.GetText();

        // The span to format is the full line(s) containing the diagnostic
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var diagnosticLinePositionSpan = text.Lines.GetLinePositionSpan(diagnosticSpan);
        var spanToFormat = TextSpan.FromBounds(
            text.Lines[diagnosticLinePositionSpan.Start.Line].Start,
            text.Lines[diagnosticLinePositionSpan.End.Line].End);

        var formattingOptions = await context.Document.GetSyntaxFormattingOptionsAsync(SyntaxFormatting, cancellationToken).ConfigureAwait(false);
        var formattedRoot = SyntaxFormatting.GetFormattingResult(root, [spanToFormat], formattingOptions, rules: default, cancellationToken: cancellationToken).GetFormattedRoot(cancellationToken);

        return context.Document.WithSyntaxRoot(formattedRoot);
    }

    protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(SyntaxFormatting, cancellationToken).ConfigureAwait(false);
        var updatedRoot = SyntaxFormatting.GetFormattingResult(editor.OriginalRoot, [editor.OriginalRoot.FullSpan], formattingOptions, rules: default, cancellationToken).GetFormattedRoot(cancellationToken);
        editor.ReplaceNode(editor.OriginalRoot, updatedRoot);
    }
}
