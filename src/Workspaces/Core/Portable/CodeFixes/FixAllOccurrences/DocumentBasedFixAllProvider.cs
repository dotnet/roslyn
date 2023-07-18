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

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Provides a base class to write a <see cref="FixAllProvider"/> that fixes documents independently. This type
    /// should be used instead of <see cref="WellKnownFixAllProviders.BatchFixer"/> in the case where fixes for a <see
    /// cref="Diagnostic"/> only affect the <see cref="Document"/> the diagnostic was produced in.
    /// </summary>
    /// <remarks>
    /// This type provides suitable logic for fixing large solutions in an efficient manner.  Projects are serially
    /// processed, with all the documents in the project being processed in parallel.  Diagnostics are computed for the
    /// project and then appropriately bucketed by document.  These are then passed to <see
    /// cref="FixAllAsync(FixAllContext, Document, ImmutableArray{Diagnostic})"/> for implementors to process.
    /// </remarks>
    public abstract class DocumentBasedFixAllProvider : FixAllProvider
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
        /// Fix all the <paramref name="diagnostics"/> present in <paramref name="document"/>.  The document returned
        /// will only be examined for its content (e.g. it's <see cref="SyntaxTree"/> or <see cref="SourceText"/>.  No
        /// other aspects of (like it's properties), or changes to the <see cref="Project"/> or <see cref="Solution"/>
        /// it points at will be considered.
        /// </summary>
        /// <param name="fixAllContext">The context for the Fix All operation.</param>
        /// <param name="document">The document to fix.</param>
        /// <param name="diagnostics">The diagnostics to fix in the document.</param>
        /// <returns>
        /// <para>The new <see cref="Document"/> representing the content fixed document.</para>
        /// <para>-or-</para>
        /// <para><see langword="null"/>, if no changes were made to the document.</para>
        /// </returns>
        protected abstract Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics);

        public sealed override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
            => _supportedFixAllScopes;

        public sealed override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            => DefaultFixAllProviderHelpers.GetFixAsync(
                fixAllContext.GetDefaultFixAllTitle(), fixAllContext, FixAllContextsHelperAsync);

        private Task<Solution?> FixAllContextsHelperAsync(FixAllContext originalFixAllContext, ImmutableArray<FixAllContext> fixAllContexts)
            => DocumentBasedFixAllProviderHelpers.FixAllContextsAsync(originalFixAllContext, fixAllContexts,
                    originalFixAllContext.GetProgressTracker(),
                    this.GetFixAllTitle(originalFixAllContext),
                    DetermineDiagnosticsAndGetFixedDocumentsAsync);

        private async Task<Dictionary<DocumentId, (SyntaxNode? node, SourceText? text)>> DetermineDiagnosticsAndGetFixedDocumentsAsync(
            FixAllContext fixAllContext,
            IProgressTracker progressTracker)
        {
            // First, determine the diagnostics to fix.
            var diagnostics = await DetermineDiagnosticsAsync(fixAllContext, progressTracker).ConfigureAwait(false);

            // Second, get the fixes for all the diagnostics, and apply them to determine the new root/text for each doc.
            return await GetFixedDocumentsAsync(fixAllContext, progressTracker, diagnostics).ConfigureAwait(false);
        }

        /// <summary>
        /// Determines all the diagnostics we should be fixing for the given <paramref name="fixAllContext"/>.
        /// </summary>
        private static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> DetermineDiagnosticsAsync(FixAllContext fixAllContext, IProgressTracker progressTracker)
        {
            using var _ = progressTracker.ItemCompletedScope();
            return await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);
        }

        /// <summary>
        /// Attempts to fix all the provided <paramref name="diagnostics"/> returning, for each updated document, either
        /// the new syntax root for that document or its new text.  Syntax roots are returned for documents that support
        /// them, and are used to perform a final cleanup pass for formatting/simplication/etc.  Text is returned for
        /// documents that don't support syntax.
        /// </summary>
        private async Task<Dictionary<DocumentId, (SyntaxNode? node, SourceText? text)>> GetFixedDocumentsAsync(
            FixAllContext fixAllContext, IProgressTracker progressTracker, ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnostics)
        {
            var cancellationToken = fixAllContext.CancellationToken;

            using var _1 = progressTracker.ItemCompletedScope();
            using var _2 = ArrayBuilder<Task<(DocumentId, (SyntaxNode? node, SourceText? text))>>.GetInstance(out var tasks);

            var docIdToNewRootOrText = new Dictionary<DocumentId, (SyntaxNode? node, SourceText? text)>();
            if (!diagnostics.IsEmpty)
            {
                // Then, process all documents in parallel to get the change for each doc.
                foreach (var (document, documentDiagnostics) in diagnostics)
                {
                    if (documentDiagnostics.IsDefaultOrEmpty)
                        continue;

                    tasks.Add(Task.Run(async () =>
                    {
                        var newDocument = await this.FixAllAsync(fixAllContext, document, documentDiagnostics).ConfigureAwait(false);
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
            }

            return docIdToNewRootOrText;
        }
    }
}
