// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

internal abstract partial class SyntaxEditorBasedCodeRefactoringProvider : CodeRefactoringProvider
{
    protected static readonly ImmutableArray<RefactorAllScope> DefaultRefactorAllScopes = [RefactorAllScope.Document, RefactorAllScope.Project, RefactorAllScope.Solution];
    protected static readonly ImmutableArray<RefactorAllScope> AllRefactorAllScopes = [RefactorAllScope.Document, RefactorAllScope.Project, RefactorAllScope.Solution, RefactorAllScope.ContainingType, RefactorAllScope.ContainingMember];

    protected abstract ImmutableArray<RefactorAllScope> SupportedRefactorAllScopes { get; }
    protected virtual CodeActionCleanup Cleanup => CodeActionCleanup.Default;

    public sealed override RefactorAllProvider? GetRefactorAllProvider()
    {
        if (SupportedRefactorAllScopes.IsEmpty)
            return null;

        return RefactorAllProvider.Create(
            async (refactorAllContext, document, refactorAllSpans) =>
                await this.RefactorAllAsync(document, refactorAllSpans, refactorAllContext.CodeActionEquivalenceKey, refactorAllContext.CancellationToken).ConfigureAwait(false),
            SupportedRefactorAllScopes,
            this.Cleanup);
    }

    protected Task<Document> RefactorAsync(
        Document document,
        TextSpan refactorAllSpan,
        string? equivalenceKey,
        CancellationToken cancellationToken)
    {
        return RefactorAllWithEditorAsync(document,
            editor => RefactorAllAsync(document, [refactorAllSpan], editor, equivalenceKey, cancellationToken),
            cancellationToken);
    }

    protected Task<Document> RefactorAllAsync(
        Document document,
        Optional<ImmutableArray<TextSpan>> refactorAllSpans,
        string? equivalenceKey,
        CancellationToken cancellationToken)
    {
        return RefactorAllWithEditorAsync(document, RefactorAllAsync, cancellationToken);

        // Local functions
        Task RefactorAllAsync(SyntaxEditor editor)
        {
            // Refactor the entire document if there are no sub-spans to refactor.
            var spans = refactorAllSpans.HasValue ? refactorAllSpans.Value : [editor.OriginalRoot.FullSpan];
            return this.RefactorAllAsync(document, spans, editor, equivalenceKey, cancellationToken);
        }
    }

    internal static async Task<Document> RefactorAllWithEditorAsync(
        Document document,
        Func<SyntaxEditor, Task> editAsync,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var editor = new SyntaxEditor(root, document.Project.Solution.Services);

        await editAsync(editor).ConfigureAwait(false);

        var newRoot = editor.GetChangedRoot();
        return document.WithSyntaxRoot(newRoot);
    }

    protected abstract Task RefactorAllAsync(
        Document document,
        ImmutableArray<TextSpan> refactorAllSpans,
        SyntaxEditor editor,
        string? equivalenceKey,
        CancellationToken cancellationToken);
}
