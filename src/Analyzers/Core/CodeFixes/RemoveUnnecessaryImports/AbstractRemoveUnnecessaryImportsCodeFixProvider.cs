// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports;

internal abstract class AbstractRemoveUnnecessaryImportsCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    protected abstract string GetTitle();
    protected abstract ISyntaxFormatting GetSyntaxFormatting();

    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [RemoveUnnecessaryImportsConstants.DiagnosticFixableId];

#if !CODE_STYLE
    // We don't need to perform any cleanup for this code fix.  It simply removes code, leaving
    // things in the state it already needs to be in.
    protected override CodeActionCleanup Cleanup => CodeActionCleanup.None;
#endif

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var title = GetTitle();
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                cancellationToken => RemoveUnnecessaryImportsAsync(context.Document, cancellationToken),
                title,
                // If the user is on a using/import that is marked as unnecessary, then we want to make sure that this
                // code action is preferred over virtually any others that could be located at that position.
                priority: CodeActionPriority.High),
            context.Diagnostics);
    }

    private static Task<Document> RemoveUnnecessaryImportsAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        var service = document.GetRequiredLanguageService<IRemoveUnnecessaryImportsService>();
        return service.RemoveUnnecessaryImportsAsync(document, cancellationToken);
    }

    protected sealed override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var newDocument = await RemoveUnnecessaryImportsAsync(document, cancellationToken).ConfigureAwait(false);
        var newRoot = await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        editor.ReplaceNode(editor.OriginalRoot, newRoot);
    }
}
