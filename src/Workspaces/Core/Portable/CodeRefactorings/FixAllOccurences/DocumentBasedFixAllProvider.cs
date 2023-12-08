// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using FixAllScope = Microsoft.CodeAnalysis.CodeFixes.FixAllScope;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Provides a base class to write a <see cref="FixAllProvider"/> for refactorings that fixes documents independently.
    /// This type should be used in the case where the code refactoring(s) only affect individual <see cref="Document"/>s.
    /// </summary>
    /// <remarks>
    /// This type provides suitable logic for fixing large solutions in an efficient manner.  Projects are serially
    /// processed, with all the documents in the project being processed in parallel. 
    /// <see cref="FixAllAsync(FixAllContext, Document, Optional{ImmutableArray{TextSpan}})"/> is invoked for each document for implementors to process.
    ///
    /// TODO: Make public, tracked with https://github.com/dotnet/roslyn/issues/60703
    /// </remarks>
    internal abstract class DocumentBasedFixAllProvider : FixAllProvider
    {
        private readonly ImmutableArray<FixAllScope> _supportedFixAllScopes;

        protected DocumentBasedFixAllProvider()
            : this(DefaultSupportedFixAllScopes)
        {
        }

        protected DocumentBasedFixAllProvider(ImmutableArray<FixAllScope> supportedFixAllScopes)
        {
            _supportedFixAllScopes = supportedFixAllScopes;
        }

        /// <summary>
        /// Produce a suitable title for the fix-all <see cref="CodeAction"/> this type creates in <see
        /// cref="GetFixAsync(FixAllContext)"/>.  Override this if customizing that title is desired.
        /// </summary>
        protected virtual string GetFixAllTitle(FixAllContext fixAllContext)
            => fixAllContext.GetDefaultFixAllTitle();

        /// <summary>
        /// Apply fix all operation for the code refactoring in the <see cref="FixAllContext.Document"/>
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
        protected abstract Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document, Optional<ImmutableArray<TextSpan>> fixAllSpans);

        public sealed override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
            => _supportedFixAllScopes;

        public sealed override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            => DefaultFixAllProviderHelpers.GetFixAsync(
                fixAllContext.GetDefaultFixAllTitle(), fixAllContext, FixAllContextsHelperAsync);

        private Task<Solution?> FixAllContextsHelperAsync(FixAllContext originalFixAllContext, ImmutableArray<FixAllContext> fixAllContexts)
            => DocumentBasedFixAllProviderHelpers.FixAllContextsAsync(originalFixAllContext, fixAllContexts,
                    originalFixAllContext.Progress,
                    this.GetFixAllTitle(originalFixAllContext),
                    GetFixedDocumentsAsync);

        /// <summary>
        /// Attempts to apply fix all operations returning, for each updated document, either
        /// the new syntax root for that document or its new text.  Syntax roots are returned for documents that support
        /// them, and are used to perform a final cleanup pass for formatting/simplication/etc.  Text is returned for
        /// documents that don't support syntax.
        /// </summary>
        private async Task<Dictionary<DocumentId, (SyntaxNode? node, SourceText? text)>> GetFixedDocumentsAsync(
            FixAllContext fixAllContext, IProgress<CodeAnalysisProgress> progressTracker)
        {
            Contract.ThrowIfFalse(fixAllContext.Scope is FixAllScope.Document or FixAllScope.Project
                or FixAllScope.ContainingMember or FixAllScope.ContainingType);

            var cancellationToken = fixAllContext.CancellationToken;

            using var _1 = progressTracker.ItemCompletedScope();
            using var _2 = ArrayBuilder<Task<(DocumentId, (SyntaxNode? node, SourceText? text))>>.GetInstance(out var tasks);

            var docIdToNewRootOrText = new Dictionary<DocumentId, (SyntaxNode? node, SourceText? text)>();

            // Process all documents in parallel to get the change for each doc.
            var documentsAndSpansToFix = await fixAllContext.GetFixAllSpansAsync(cancellationToken).ConfigureAwait(false);

            foreach (var (document, spans) in documentsAndSpansToFix)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var newDocument = await this.FixAllAsync(fixAllContext, document, spans).ConfigureAwait(false);
                    if (newDocument == null || newDocument == document)
                        return default;

                    // For documents that support syntax, grab the tree so that we can clean it up later.  If it's a
                    // language that doesn't support that, then just grab the text.
                    var node = newDocument.SupportsSyntaxTree ? await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false) : null;
                    var text = newDocument.SupportsSyntaxTree ? null : await newDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

                    return (document.Id, (node, text));
                }, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var task in tasks)
            {
                var (docId, nodeOrText) = await task.ConfigureAwait(false);
                if (docId != null)
                    docIdToNewRootOrText[docId] = nodeOrText;
            }

            return docIdToNewRootOrText;
        }
    }
}
