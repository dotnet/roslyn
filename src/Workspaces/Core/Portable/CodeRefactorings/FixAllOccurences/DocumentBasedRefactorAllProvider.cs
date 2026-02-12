// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

/// <summary>
/// Provides a base class to write a <see cref="RefactorAllProvider"/> for refactorings that refactor documents
/// independently. This type should be used in the case where the code refactoring(s) only affect individual <see
/// cref="Document"/>s.
/// </summary>
/// <remarks>
/// This type provides suitable logic for refactorings large solutions in an efficient manner.  Projects are serially
/// processed, with all the documents in the project being processed in parallel. <see cref="RefactorAllAsync"/> is
/// invoked for each document for implementors to process.
///
/// TODO: Make public, tracked with https://github.com/dotnet/roslyn/issues/60703
/// </remarks>
internal abstract class DocumentBasedRefactorAllProvider(ImmutableArray<RefactorAllScope> supportedRefactorAllScopes)
    : RefactorAllProvider
{
    private readonly ImmutableArray<RefactorAllScope> _supportedRefactorAllScopes = supportedRefactorAllScopes;

    protected DocumentBasedRefactorAllProvider()
        : this(DefaultSupportedRefactorAllScopes)
    {
    }

    /// <summary>
    /// Produce a suitable title for the refactor-all <see cref="CodeAction"/> this type creates in <see
    /// cref="GetRefactoringAsync(RefactorAllContext)"/>.  Override this if customizing that title is desired.
    /// </summary>
    protected virtual string GetRefactorAllTitle(RefactorAllContext refactorAllContext)
        => refactorAllContext.GetDefaultRefactorAllTitle();

    /// <summary>
    /// Apply refactor all operation for the code refactoring in the <see cref="RefactorAllContext.Document"/> for the
    /// given <paramref name="refactorAllContext"/>.  The document returned will only be examined for its content (e.g.
    /// it's <see cref="SyntaxTree"/> or <see cref="SourceText"/>.  No other aspects of document (like it's properties),
    /// or changes to the <see cref="Project"/> or <see cref="Solution"/> it points at will be considered.
    /// </summary>
    /// <param name="refactorAllContext">The context for the Refactor All operation.</param>
    /// <param name="document">The document to refactor.</param>
    /// <param name="refactorAllSpans">The spans to refactor in the document. If not specified, entire document needs to be refactored.</param>
    /// <returns>
    /// <para>The new <see cref="Document"/> representing the content refactored document.</para>
    /// <para>-or-</para>
    /// <para><see langword="null"/>, if no changes were made to the document.</para>
    /// </returns>
    protected abstract Task<Document?> RefactorAllAsync(
        RefactorAllContext refactorAllContext, Document document, Optional<ImmutableArray<TextSpan>> refactorAllSpans);

    public sealed override IEnumerable<RefactorAllScope> GetSupportedRefactorAllScopes()
        => _supportedRefactorAllScopes;

    public sealed override Task<CodeAction?> GetRefactoringAsync(RefactorAllContext refactorAllContext)
        => DefaultFixAllProviderHelpers.GetFixAsync(
            refactorAllContext.GetDefaultRefactorAllTitle(), refactorAllContext, RefactorAllContextsHelperAsync);

    private Task<Solution?> RefactorAllContextsHelperAsync(RefactorAllContext originalRefactorAllContext, ImmutableArray<RefactorAllContext> refactorAllContexts)
        => DocumentBasedFixAllProviderHelpers.FixAllContextsAsync(
            originalRefactorAllContext,
            refactorAllContexts,
            originalRefactorAllContext.Progress,
            this.GetRefactorAllTitle(originalRefactorAllContext),
            GetRefactoredDocumentsAsync);

    /// <summary>
    /// Attempts to apply refactor all operations. Returning, for each updated document, either the new syntax root for
    /// that document or its new text.  Syntax roots are returned for documents that support them, and are used to
    /// perform a final cleanup pass for formatting/simplification/etc.  Text is returned for documents that don't
    /// support syntax.
    /// </summary>
    private async Task GetRefactoredDocumentsAsync(
        RefactorAllContext refactorAllContext, Func<Document, Document?, ValueTask> onDocumentRefactored)
    {
        Contract.ThrowIfFalse(refactorAllContext.Scope is RefactorAllScope.Document or RefactorAllScope.Project
            or RefactorAllScope.ContainingMember or RefactorAllScope.ContainingType);

        var cancellationToken = refactorAllContext.CancellationToken;

        // Process all documents in parallel to get the change for each doc.
        var documentsAndSpansToRefactor = await refactorAllContext.GetRefactorAllSpansAsync(cancellationToken).ConfigureAwait(false);

        await Parallel.ForEachAsync(
            source: documentsAndSpansToRefactor,
            cancellationToken,
            async (tuple, cancellationToken) =>
            {
                var (document, spans) = tuple;

                Document? newDocument;
                try
                {
                    newDocument = await this.RefactorAllAsync(refactorAllContext, document, spans).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    throw new RefactorOrFixAllDocumentException(document, ex);
                }

                await onDocumentRefactored(document, newDocument).ConfigureAwait(false);
            }).ConfigureAwait(false);
    }
}
