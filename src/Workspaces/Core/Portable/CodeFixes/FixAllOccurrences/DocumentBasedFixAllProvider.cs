// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
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
        {
            _supportedFixAllScopes = base.GetSupportedFixAllScopes().ToImmutableArray();
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
            => FixAllContextHelper.GetDefaultFixAllTitle(fixAllContext);

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
                FixAllContextHelper.GetDefaultFixAllTitle(fixAllContext), fixAllContext, FixAllContextsAsync);

        private async Task<Solution?> FixAllContextsAsync(FixAllContext originalFixAllContext, ImmutableArray<FixAllContext> fixAllContexts)
        {
            var progressTracker = originalFixAllContext.GetProgressTracker();
            progressTracker.Description = this.GetFixAllTitle(originalFixAllContext);

            var solution = originalFixAllContext.Solution;

            // We have 3 pieces of work per project.  Computing diagnostics, computing fixes, and applying fixes.
            progressTracker.AddItems(fixAllContexts.Length * 3);

            // Process each context one at a time, allowing us to dump any information we computed for each once done with it.
            var currentSolution = solution;
            foreach (var fixAllContext in fixAllContexts)
            {
                Contract.ThrowIfFalse(fixAllContext.Scope is FixAllScope.Document or FixAllScope.Project
                    or FixAllScope.ContainingMember or FixAllScope.ContainingType);
                currentSolution = await FixSingleContextAsync(currentSolution, fixAllContext, progressTracker).ConfigureAwait(false);
            }

            return currentSolution;
        }

        private async Task<Solution> FixSingleContextAsync(Solution currentSolution, FixAllContext fixAllContext, IProgressTracker progressTracker)
        {
            // First, determine the diagnostics to fix.
            var diagnostics = await DetermineDiagnosticsAsync(fixAllContext, progressTracker).ConfigureAwait(false);

            // Second, get the fixes for all the diagnostics, and apply them to determine the new root/text for each doc.
            var docIdToNewRootOrText = await GetFixedDocumentsAsync(fixAllContext, progressTracker, diagnostics).ConfigureAwait(false);

            // Finally, cleanup the new doc roots, and apply the results to the solution.
            currentSolution = await CleanupAndApplyChangesAsync(fixAllContext, progressTracker, currentSolution, docIdToNewRootOrText).ConfigureAwait(false);

            return currentSolution;
        }

        /// <summary>
        /// Determines all the diagnostics we should be fixing for the given <paramref name="fixAllContext"/>.
        /// </summary>
        private static async Task<ImmutableArray<Diagnostic>> DetermineDiagnosticsAsync(FixAllContext fixAllContext, IProgressTracker progressTracker)
        {
            using var _ = progressTracker.ItemCompletedScope();

            if (fixAllContext.Document != null)
            {
                if (fixAllContext.State.FixAllSpans.IsEmpty)
                {
                    return await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document).ConfigureAwait(false);
                }
                else
                {
                    var diagnostics = ImmutableArray<Diagnostic>.Empty;
                    foreach (var fixAllSpan in fixAllContext.State.FixAllSpans)
                    {
                        diagnostics = diagnostics.AddRange(await fixAllContext.GetDocumentSpanDiagnosticsAsync(
                            fixAllContext.Document, fixAllSpan).ConfigureAwait(false));
                    }

                    return diagnostics;
                }
            }

            return await fixAllContext.GetAllDiagnosticsAsync(fixAllContext.Project).ConfigureAwait(false);
        }

        /// <summary>
        /// Attempts to fix all the provided <paramref name="diagnostics"/> returning, for each updated document, either
        /// the new syntax root for that document or its new text.  Syntax roots are returned for documents that support
        /// them, and are used to perform a final cleanup pass for formatting/simplication/etc.  Text is returned for
        /// documents that don't support syntax.
        /// </summary>
        private async Task<Dictionary<DocumentId, (SyntaxNode? node, SourceText? text)>> GetFixedDocumentsAsync(
            FixAllContext fixAllContext, IProgressTracker progressTracker, ImmutableArray<Diagnostic> diagnostics)
        {
            var cancellationToken = fixAllContext.CancellationToken;

            using var _1 = progressTracker.ItemCompletedScope();
            using var _2 = ArrayBuilder<Task<(DocumentId, (SyntaxNode? node, SourceText? text))>>.GetInstance(out var tasks);

            var docIdToNewRootOrText = new Dictionary<DocumentId, (SyntaxNode? node, SourceText? text)>();
            if (!diagnostics.IsEmpty)
            {
                // Then, once we've got the diagnostics, bucket them by document and the process all documents in
                // parallel to get the change for each doc.
                foreach (var group in diagnostics.Where(d => d.Location.IsInSource).GroupBy(d => d.Location.SourceTree))
                {
                    var tree = group.Key;
                    Contract.ThrowIfNull(tree);
                    var document = fixAllContext.Solution.GetRequiredDocument(tree);
                    var documentDiagnostics = group.ToImmutableArray();
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
                        var text = newDocument.SupportsSyntaxTree ? null : await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

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

        /// <summary>
        /// Take all the fixed documents and format/simplify/clean them up (if the language supports that), and take the
        /// resultant text and apply it to the solution.  If the language doesn't support cleanup, then just take the
        /// given text and apply that instead.
        /// </summary>
        private static async Task<Solution> CleanupAndApplyChangesAsync(
            FixAllContext fixAllContext,
            IProgressTracker progressTracker,
            Solution currentSolution,
            Dictionary<DocumentId, (SyntaxNode? node, SourceText? text)> docIdToNewRootOrText)
        {
            var cancellationToken = fixAllContext.CancellationToken;
            using var _1 = progressTracker.ItemCompletedScope();

            if (docIdToNewRootOrText.Count > 0)
            {
                // Next, go and insert those all into the solution so all the docs in this particular project point at
                // the new trees (or text).  At this point though, the trees have not been cleaned up.  We don't cleanup
                // the documents as they are created, or one at a time as we add them, as that would cause us to run
                // cleanup on N different solution forks (which would be very expensive).  Instead, by adding all the
                // changed documents to one solution, and hten cleaning *those* we only perform cleanup semantics on one
                // forked solution.
                foreach (var (docId, (newRoot, newText)) in docIdToNewRootOrText)
                {
                    currentSolution = newRoot != null
                        ? currentSolution.WithDocumentSyntaxRoot(docId, newRoot)
                        : currentSolution.WithDocumentText(docId, newText!);
                }

                // Next, go and cleanup any trees we inserted. Once we clean the document, we get the text of it and
                // insert that back into the final solution.  This way we can release both the original fixed tree, and
                // the cleaned tree (both of which can be much more expensive than just text).
                //
                // Do this in parallel across all the documents that were fixed.
                using var _2 = ArrayBuilder<Task<(DocumentId docId, SourceText sourceText)>>.GetInstance(out var tasks);

                foreach (var (docId, (newRoot, _)) in docIdToNewRootOrText)
                {
                    if (newRoot != null)
                    {
                        var dirtyDocument = currentSolution.GetRequiredDocument(docId);
                        tasks.Add(Task.Run(async () =>
                        {
                            var cleanedDocument = await PostProcessCodeAction.Instance.PostProcessChangesAsync(dirtyDocument, cancellationToken).ConfigureAwait(false);
                            var cleanedText = await cleanedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                            return (dirtyDocument.Id, cleanedText);
                        }, cancellationToken));
                    }
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                // Finally, apply the cleaned documents to the solution.
                foreach (var task in tasks)
                {
                    var (docId, cleanedText) = await task.ConfigureAwait(false);
                    currentSolution = currentSolution.WithDocumentText(docId, cleanedText);
                }
            }

            return currentSolution;
        }

        /// <summary>
        /// Dummy class just to get access to <see cref="CodeAction.PostProcessChangesAsync(Document, CancellationToken)"/>
        /// </summary>
        private class PostProcessCodeAction : CodeAction
        {
            public static readonly PostProcessCodeAction Instance = new();

            public override string Title => "";

            public new Task<Document> PostProcessChangesAsync(Document document, CancellationToken cancellationToken)
                => base.PostProcessChangesAsync(document, cancellationToken);
        }
    }
}
