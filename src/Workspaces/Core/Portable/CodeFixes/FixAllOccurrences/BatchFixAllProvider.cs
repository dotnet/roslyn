// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Helper class for "Fix all occurrences" code fix providers.
    /// </summary>
    internal sealed class BatchFixAllProvider : AbstractDefaultFixAllProvider
    {
        public static readonly FixAllProvider Instance = new BatchFixAllProvider();

        private BatchFixAllProvider()
        {
        }

        protected sealed override string GetFixAllTitle(FixAllContext fixAllContext)
            => FixAllContextHelper.GetDefaultFixAllTitle(fixAllContext);

        /// <summary>
        /// All fix-alls funnel into this method.  For doc-fix-all or project-fix-all call into this with just their
        /// single <see cref="FixAllContext"/> in <paramref name="fixAllContexts"/>.  For solution-fix-all, <paramref
        /// name="fixAllContexts"/> will contain a context for each project in the solution.
        /// </summary>
        protected sealed override async Task<Solution?> FixAllContextsAsync(
            FixAllContext originalFixAllContext,
            ImmutableArray<FixAllContext> fixAllContexts)
        {
            var cancellationToken = originalFixAllContext.CancellationToken;
            var progressTracker = originalFixAllContext.GetProgressTracker();
            progressTracker.Description = FixAllContextHelper.GetDefaultFixAllTitle(originalFixAllContext);

            // We have 2*P + 1 pieces of work.  Computing diagnostics and fixes/changes per context, and then one pass
            // applying fixes.
            progressTracker.AddItems(fixAllContexts.Length * 2 + 1);

            // Mapping from document to the cumulative text changes created for that document.
            var docIdToTextMerger = new Dictionary<DocumentId, TextChangeMerger>();

            // Process each context one at a time, allowing us to dump most of the information we computed for each once
            // done with it.  The only information we need to preserve is the data we store in docIdToTextMerger
            foreach (var fixAllContext in fixAllContexts)
            {
                Contract.ThrowIfFalse(fixAllContext.Scope is FixAllScope.Document or FixAllScope.Project);
                await FixSingleContextAsync(docIdToTextMerger, fixAllContext).ConfigureAwait(false);
            }

            // Finally, merge in all text changes into the solution.  We can't do this per-project as we have to have
            // process *all* diagnostics in the solution to find the changes made to all documents.
            progressTracker.Description = WorkspaceExtensionsResources.Applying_fix_all;
            using (progressTracker.ItemCompletedScope())
            {
                if (docIdToTextMerger.Count == 0)
                    return null;

                var currentSolution = originalFixAllContext.Solution;
                foreach (var group in docIdToTextMerger.GroupBy(kvp => kvp.Key.ProjectId))
                    currentSolution = await ApplyChangesAsync(currentSolution, group.SelectAsArray(kvp => (kvp.Key, kvp.Value)), cancellationToken).ConfigureAwait(false);

                return currentSolution;
            }
        }

        private static async Task FixSingleContextAsync(Dictionary<DocumentId, TextChangeMerger> docIdToTextMerger, FixAllContext fixAllContext)
        {
            // First, determine the diagnostics to fix for that context.
            var documentToDiagnostics = await DetermineDiagnosticsAsync(fixAllContext).ConfigureAwait(false);

            // Second, process all those diagnostics, merging the cumulative set of text changes per document into docIdToTextMerger.
            await AddDocumentChangesAsync(fixAllContext, docIdToTextMerger, documentToDiagnostics).ConfigureAwait(false);
        }

        private static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> DetermineDiagnosticsAsync(FixAllContext fixAllContext)
        {
            var progressTracker = fixAllContext.GetProgressTracker();
            using var _ = progressTracker.ItemCompletedScope();

            var project = fixAllContext.Project;
            progressTracker.Description = string.Format(WorkspaceExtensionsResources._0_Computing_diagnostics, project.Name);

            var documentToDiagnostics = await fixAllContext.GetDocumentDiagnosticsToFixAsync().ConfigureAwait(false);

            var filtered = documentToDiagnostics.Where(kvp =>
            {
                if (kvp.Key.Project != project)
                    return false;

                if (fixAllContext.Document != null && fixAllContext.Document != kvp.Key)
                    return false;

                return true;
            });

            return filtered.ToImmutableDictionary();
        }

        private static async Task AddDocumentChangesAsync(
            FixAllContext fixAllContext,
            Dictionary<DocumentId, TextChangeMerger> docIdToTextMerger,
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentToDiagnostics)
        {
            // First, order the diagnostics so we process them in a consistent manner and get the same results given the
            // same input solution.
            var orderedDiagnostics = documentToDiagnostics.SelectMany(kvp => kvp.Value)
                                                          .Where(d => d.Location.IsInSource)
                                                          .OrderBy(d => d.Location.SourceTree!.FilePath)
                                                          .ThenBy(d => d.Location.SourceSpan.Start)
                                                          .ToImmutableArray();

            // Now determine all the document changes caused from these diagnostics.
            var allChangedDocumentsInDiagnosticsOrder =
                await GetAllChangedDocumentsInDiagnosticsOrderAsync(fixAllContext, orderedDiagnostics).ConfigureAwait(false);

            // Finally, take all the changes made to each document and merge them together into docIdToTextMerger to
            // keep track of the total set of changes to any particular document.
            await MergeTextChangesAsync(fixAllContext, allChangedDocumentsInDiagnosticsOrder, docIdToTextMerger).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns all the changed documents produced by fixing the list of provided <paramref
        /// name="orderedDiagnostics"/>.  The documents will be returned such that fixed documents for a later
        /// diagnostic will appear later than those for an earlier diagnostic.
        /// </summary>
        private static async Task<ImmutableArray<Document>> GetAllChangedDocumentsInDiagnosticsOrderAsync(
            FixAllContext fixAllContext, ImmutableArray<Diagnostic> orderedDiagnostics)
        {
            var solution = fixAllContext.Solution;
            var cancellationToken = fixAllContext.CancellationToken;

            // Next, process each diagnostic, determine the code actions to fix it, then figure out the document 
            // changes produced by that code action.
            using var _1 = ArrayBuilder<Task<ImmutableArray<Document>>>.GetInstance(out var fixedDocumentsArray);
            foreach (var diagnostic in orderedDiagnostics)
            {
                var document = solution.GetRequiredDocument(diagnostic.Location.SourceTree!);

                cancellationToken.ThrowIfCancellationRequested();
                fixedDocumentsArray.Add(Task.Run(async () =>
                {
                    // Create a context that will add the reported code actions into this
                    using var _2 = ArrayBuilder<CodeAction>.GetInstance(out var codeActions);
                    var context = new CodeFixContext(document, diagnostic, GetRegisterCodeFixAction(fixAllContext.CodeActionEquivalenceKey, codeActions), cancellationToken);

                    // Wait for the all the code actions to be reported for this diagnostic.
                    await (fixAllContext.CodeFixProvider.RegisterCodeFixesAsync(context) ?? Task.CompletedTask).ConfigureAwait(false);

                    // Now, process each code action and find out all the document changes caused by it.
                    using var _3 = ArrayBuilder<Document>.GetInstance(out var changedDocuments);

                    foreach (var codeAction in codeActions)
                    {
                        var changedSolution = await codeAction.GetChangedSolutionInternalAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                        if (changedSolution is null)
                        {
                            // No changed documents
                            continue;
                        }

                        var solutionChanges = new SolutionChanges(changedSolution, solution);

                        // TODO: Handle added/removed documents
                        // TODO: Handle changed/added/removed additional documents

                        var documentIdsWithChanges = solutionChanges.GetProjectChanges().SelectMany(p => p.GetChangedDocuments());
                        changedDocuments.AddRange(documentIdsWithChanges.Select(id => changedSolution.GetRequiredDocument(id)));
                    }

                    return changedDocuments.ToImmutable();
                }, cancellationToken));
            }

            // Wait for all that work to finish.
            await Task.WhenAll(fixedDocumentsArray).ConfigureAwait(false);

            // Flatten the set of changed documents.  These will naturally still be ordered by the diagnostic that
            // caused the change.
            using var _4 = ArrayBuilder<Document>.GetInstance(out var allFixedDocuments);
            foreach (var task in fixedDocumentsArray)
            {
                var fixedDocuments = await task.ConfigureAwait(false);
                allFixedDocuments.AddRange(fixedDocuments);
            }

            return allFixedDocuments.ToImmutable();
        }

        /// <summary>
        /// Take all the changes made to a particular document and determine the text changes caused by each one.  Take
        /// those individual text changes and attempt to merge them together in order into <paramref
        /// name="docIdToTextMerger"/>.
        /// </summary>
        private static async Task MergeTextChangesAsync(
            FixAllContext fixAllContext,
            ImmutableArray<Document> allChangedDocumentsInDiagnosticsOrder,
            Dictionary<DocumentId, TextChangeMerger> docIdToTextMerger)
        {
            var solution = fixAllContext.Solution;
            var cancellationToken = fixAllContext.CancellationToken;

            var differenceService = solution.Workspace.Services.GetRequiredService<IDocumentTextDifferencingService>();

            // Now for each document that is changed, grab all the documents it was changed to (remember, many code
            // actions might have touched that document).  Figure out the actual change, and then add that to the
            // interval tree of changes we're keeping track of for that document.
            using var _ = ArrayBuilder<Task>.GetInstance(out var mergeDocumentChangesTasks);
            foreach (var group in allChangedDocumentsInDiagnosticsOrder.GroupBy(d => d.Id))
            {
                var docId = group.Key;
                var allDocChanges = group.ToImmutableArray();

                // If we don't have an text merger for this doc yet, create one to keep track of all the changes.
                if (!docIdToTextMerger.TryGetValue(docId, out var textMerger))
                {
                    var originalDocument = solution.GetRequiredDocument(docId);
                    textMerger = new TextChangeMerger(originalDocument);
                    docIdToTextMerger.Add(docId, textMerger);
                }

                // Process all document groups in parallel.  For each group, merge all the doc changes into an
                // aggregated set of changes in the TextChangeMerger type.
                mergeDocumentChangesTasks.Add(Task.Run(
                    async () => await textMerger.TryMergeChangesAsync(allDocChanges, cancellationToken).ConfigureAwait(false), cancellationToken));
            }

            await Task.WhenAll(mergeDocumentChangesTasks).ConfigureAwait(false);
        }

        private static Action<CodeAction, ImmutableArray<Diagnostic>> GetRegisterCodeFixAction(
            string? codeActionEquivalenceKey, ArrayBuilder<CodeAction> codeActions)
        {
            return (action, diagnostics) =>
            {
                using var _ = ArrayBuilder<CodeAction>.GetInstance(out var builder);
                builder.Push(action);
                while (builder.Count > 0)
                {
                    var currentAction = builder.Pop();
                    if (currentAction is { EquivalenceKey: var equivalenceKey }
                        && codeActionEquivalenceKey == equivalenceKey)
                    {
                        lock (codeActions)
                            codeActions.Add(currentAction);
                    }

                    foreach (var nestedAction in currentAction.NestedCodeActions)
                        builder.Push(nestedAction);
                }
            };
        }

        private static async Task<Solution> ApplyChangesAsync(
            Solution currentSolution,
            ImmutableArray<(DocumentId, TextChangeMerger)> docIdsAndMerger,
            CancellationToken cancellationToken)
        {
            foreach (var (documentId, textMerger) in docIdsAndMerger)
            {
                var newText = await textMerger.GetFinalMergedTextAsync(cancellationToken).ConfigureAwait(false);
                currentSolution = currentSolution.WithDocumentText(documentId, newText);
            }

            return currentSolution;
        }
    }
}
