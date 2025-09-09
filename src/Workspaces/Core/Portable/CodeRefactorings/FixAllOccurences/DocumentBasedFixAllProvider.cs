// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using RefactorAllScope = Microsoft.CodeAnalysis.CodeFixes.RefactorAllScope;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

/// <summary>
/// Provides a base class to write a <see cref="RefactorAllProvider"/> for refactorings that fixes documents independently.
/// This type should be used in the case where the code refactoring(s) only affect individual <see cref="Document"/>s.
/// </summary>
/// <remarks>
/// This type provides suitable logic for fixing large solutions in an efficient manner.  Projects are serially
/// processed, with all the documents in the project being processed in parallel. 
/// <see cref="FixAllAsync(RefactorAllContext, Document, Optional{ImmutableArray{TextSpan}})"/> is invoked for each document for implementors to process.
///
/// TODO: Make public, tracked with https://github.com/dotnet/roslyn/issues/60703
/// </remarks>
internal abstract class DocumentBasedRefactorAllProvider(ImmutableArray<RefactorAllScope> supportedFixAllScopes) : RefactorAllProvider
{
    private readonly ImmutableArray<RefactorAllScope> _supportedFixAllScopes = supportedFixAllScopes;

    protected DocumentBasedRefactorAllProvider()
        : this(DefaultSupportedRefactorAllScopes)
    {
    }

    /// <summary>
    /// Produce a suitable title for the fix-all <see cref="CodeAction"/> this type creates in <see
    /// cref="GetRefactoringAsync(RefactorAllContext)"/>.  Override this if customizing that title is desired.
    /// </summary>
    protected virtual string GetFixAllTitle(RefactorAllContext fixAllContext)
        => fixAllContext.GetDefaultFixAllTitle();

    /// <summary>
    /// Apply fix all operation for the code refactoring in the <see cref="RefactorAllContext.Document"/>
    /// for the given <paramref name="fixAllContext"/>.  The document returned will only be examined for its content
    /// (e.g. it's <see cref="SyntaxTree"/> or <see cref="SourceText"/>.  No other aspects of document (like it's properties),
    /// or changes to the <see cref="Project"/> or <see cref="Solution"/> it points at will be considered.
    /// </summary>
    /// <param name="fixAllContext">The context for the Fix All operation.</param>
    /// <param name="document">The document to fix.</param>
    /// <param name="fixAllSpans">The spans to fix in the document. If not specified, entire document needs to be fixedd.</param>
    /// <returns>
    /// <para>The new <see cref="Document"/> representing the content fixed document.</para>
    /// <para>-or-</para>
    /// <para><see langword="null"/>, if no changes were made to the document.</para>
    /// </returns>
    protected abstract Task<Document?> FixAllAsync(RefactorAllContext fixAllContext, Document document, Optional<ImmutableArray<TextSpan>> fixAllSpans);

    public sealed override IEnumerable<RefactorAllScope> GetSupportedRefactorAllScopes()
        => _supportedFixAllScopes;

    public sealed override Task<CodeAction?> GetRefactoringAsync(RefactorAllContext fixAllContext)
        => DefaultFixAllProviderHelpers.GetFixAsync(
            fixAllContext.GetDefaultFixAllTitle(), fixAllContext, FixAllContextsHelperAsync);

    private Task<Solution?> FixAllContextsHelperAsync(RefactorAllContext originalFixAllContext, ImmutableArray<RefactorAllContext> fixAllContexts)
        => DocumentBasedFixAllProviderHelpers.FixAllContextsAsync(
            originalFixAllContext,
            fixAllContexts,
            originalFixAllContext.Progress,
            this.GetFixAllTitle(originalFixAllContext),
            GetFixedDocumentsAsync);

    /// <summary>
    /// Attempts to apply fix all operations returning, for each updated document, either the new syntax root for that
    /// document or its new text.  Syntax roots are returned for documents that support them, and are used to perform a
    /// final cleanup pass for formatting/simplification/etc.  Text is returned for documents that don't support syntax.
    /// </summary>
    private async Task GetFixedDocumentsAsync(
        RefactorAllContext fixAllContext, Func<Document, Document?, ValueTask> onDocumentFixed)
    {
        Contract.ThrowIfFalse(fixAllContext.Scope is RefactorAllScope.Document or RefactorAllScope.Project
            or RefactorAllScope.ContainingMember or RefactorAllScope.ContainingType);

        var cancellationToken = fixAllContext.CancellationToken;

        // Process all documents in parallel to get the change for each doc.
        var documentsAndSpansToFix = await fixAllContext.GetRefactorAllSpansAsync(cancellationToken).ConfigureAwait(false);

        await Parallel.ForEachAsync(
            source: documentsAndSpansToFix,
            cancellationToken,
            async (tuple, cancellationToken) =>
            {
                var (document, spans) = tuple;
                var newDocument = await this.FixAllAsync(fixAllContext, document, spans).ConfigureAwait(false);
                await onDocumentFixed(document, newDocument).ConfigureAwait(false);
            }).ConfigureAwait(false);
    }
}
