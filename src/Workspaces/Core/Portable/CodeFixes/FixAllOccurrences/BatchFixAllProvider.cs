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
    internal sealed class BatchFixAllProvider : FixAllProvider
    {
        public static readonly FixAllProvider Instance = new BatchFixAllProvider();

        private BatchFixAllProvider() { }

#pragma warning disable RS0005 // Do not use generic 'CodeAction.Create' to create 'CodeAction'

        public sealed override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            Contract.ThrowIfNull(fixAllContext.Document);
            var title = FixAllContextHelper.GetDefaultFixAllTitle(fixAllContext);

            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    Contract.ThrowIfNull(fixAllContext.Document);
                    return Task.FromResult<CodeAction?>(CodeAction.Create(
                        title,
                        c => GetDocumentFixesAsync(fixAllContext.WithCancellationToken(c)),
                        nameof(BatchFixAllProvider)));

                case FixAllScope.Project:
                    return Task.FromResult<CodeAction?>(CodeAction.Create(
                        title,
                        c => GetProjectFixesAsync(fixAllContext.WithCancellationToken(c)),
                        nameof(BatchFixAllProvider)));

                case FixAllScope.Solution:
                    return Task.FromResult<CodeAction?>(CodeAction.Create(
                        title,
                        c => GetSolutionFixesAsync(fixAllContext.WithCancellationToken(c)),
                        nameof(BatchFixAllProvider)));

                case FixAllScope.Custom:
                default:
                    return Task.FromResult<CodeAction?>(null);
            }
        }

#pragma warning restore RS0005 // Do not use generic 'CodeAction.Create' to create 'CodeAction'

        private static Task<Solution> GetDocumentFixesAsync(FixAllContext fixAllContext)
            => FixAllContextsAsync(
                fixAllContext,
                ImmutableArray.Create(fixAllContext));

        private static Task<Solution> GetProjectFixesAsync(FixAllContext fixAllContext)
            => FixAllContextsAsync(
                fixAllContext,
                ImmutableArray.Create(fixAllContext.WithDocument(null)));

        private static Task<Solution> GetSolutionFixesAsync(FixAllContext fixAllContext)
        {
            var solution = fixAllContext.Solution;
            var dependencyGraph = solution.GetProjectDependencyGraph();

            // Walk through each project in topological order, determining and applying the diagnostics for each
            // project.  We do this in topological order so that the compilations for successive projects are readily
            // available as we just computed them for dependent projects.  If we were to do it out of order, we might
            // start with a project that has a ton of dependencies, and we'd spend an inordinate amount of time just
            // building the compilations for it before we could proceed.
            //
            // By processing one project at a time, we can also let go of a project once done with it, allowing us to
            // reclaim lots of the memory so we don't overload the system while processing a large solution.
            var sortedProjectIds = dependencyGraph.GetTopologicallySortedProjects().ToImmutableArray();
            return FixAllContextsAsync(
                fixAllContext,
                sortedProjectIds.SelectAsArray(
                    id => fixAllContext.WithScope(FixAllScope.Project).WithProject(solution.GetRequiredProject(id)).WithDocument(null)));
        }

        /// <summary>
        /// All fix-alls funnel into this method.  For doc-fix-all or project-fix-all call into this with just their
        /// single <see cref="FixAllContext"/> in <paramref name="fixAllContexts"/>.  For solution-fix-all, <paramref
        /// name="fixAllContexts"/> will contain a context for each project in the solution.
        /// </summary>
        private static async Task<Solution> FixAllContextsAsync(
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
            var docIdToIntervalTree = new Dictionary<DocumentId, SimpleIntervalTree<TextChange, TextChangeIntervalIntrospector>>();

            // Process each context one at a time, allowing us to dump most of the information we computed for each once
            // done with it.  The only information we need to preserve is the data we store in docIdToIntervalTree
            foreach (var fixAllContext in fixAllContexts)
            {
                Contract.ThrowIfFalse(fixAllContext.Scope is FixAllScope.Document or FixAllScope.Project);
                await FixSingleContextAsync(docIdToIntervalTree, fixAllContext).ConfigureAwait(false);
            }

            // Finally, merge in all text changes into the solution.  We can't do this per-project as we have to have
            // process *all* diagnostics in the solution to find the changes made to all documents.
            progressTracker.Description = WorkspaceExtensionsResources.Applying_fix_all;
            using (progressTracker.ItemCompletedScope())
            {
                var currentSolution = originalFixAllContext.Solution;
                foreach (var group in docIdToIntervalTree.GroupBy(kvp => kvp.Key.ProjectId))
                    currentSolution = await ApplyChangesAsync(currentSolution, group.SelectAsArray(kvp => (kvp.Key, kvp.Value)), cancellationToken).ConfigureAwait(false);

                return currentSolution;
            }
        }

        private static async Task FixSingleContextAsync(Dictionary<DocumentId, SimpleIntervalTree<TextChange, TextChangeIntervalIntrospector>> docIdToIntervalTree, FixAllContext fixAllContext)
        {
            // First, determine the diagnostics to fix for that context.
            var documentToDiagnostics = await DetermineDiagnosticsAsync(fixAllContext).ConfigureAwait(false);

            // Second, process all those diagnostics, merging the cumulative set of text changes per document into docIdToIntervalTree.
            await AddDocumentChangesAsync(fixAllContext, docIdToIntervalTree, documentToDiagnostics).ConfigureAwait(false);
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
            Dictionary<DocumentId, SimpleIntervalTree<TextChange, TextChangeIntervalIntrospector>> docIdToIntervalTree,
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

            // Finally, take all the changes made to each document and merge them together into docIdToIntervalTree to
            // keep track of the total set of changes to any particular document.
            await MergeTextChangesAsync(fixAllContext, allChangedDocumentsInDiagnosticsOrder, docIdToIntervalTree).ConfigureAwait(false);
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
        /// name="docIdToIntervalTree"/>.
        /// </summary>
        private static async Task MergeTextChangesAsync(
            FixAllContext fixAllContext,
            ImmutableArray<Document> allChangedDocumentsInDiagnosticsOrder,
            Dictionary<DocumentId, SimpleIntervalTree<TextChange, TextChangeIntervalIntrospector>> docIdToIntervalTree)
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
                var currentDocId = group.Key;
                var allDocChanges = group.ToImmutableArray();
                var originalDocument = solution.GetRequiredDocument(currentDocId);

                // If we don't have an interval tree for this doc yet, create one to keep track of all the changes.
                if (!docIdToIntervalTree.TryGetValue(currentDocId, out var totalChangesIntervalTree))
                {
                    totalChangesIntervalTree = SimpleIntervalTree.Create(new TextChangeIntervalIntrospector(), Array.Empty<TextChange>());
                    docIdToIntervalTree.Add(currentDocId, totalChangesIntervalTree);
                }

                // Process all document groups in parallel.
                mergeDocumentChangesTasks.Add(Task.Run(async () =>
                {
                    // For each change produced for this document (ordered by the diagnostic that created it), merge in
                    // the changes to get the latest cumulative changes.
                    foreach (var changedDocument in allDocChanges)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Debug.Assert(changedDocument.Id == originalDocument.Id);

                        await TryAddDocumentMergeChangesAsync(
                            differenceService,
                            originalDocument,
                            changedDocument,
                            totalChangesIntervalTree,
                            cancellationToken).ConfigureAwait(false);
                    }
                }, cancellationToken));
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
            ImmutableArray<(DocumentId, SimpleIntervalTree<TextChange, TextChangeIntervalIntrospector>)> docIdsAndIntervalTrees,
            CancellationToken cancellationToken)
        {
            foreach (var (documentId, totalChangesIntervalTree) in docIdsAndIntervalTrees)
            {
                // WithChanges requires a ordered list of TextChanges without any overlap.
                var changesToApply = totalChangesIntervalTree.Distinct().OrderBy(tc => tc.Span.Start);

                var oldDocument = currentSolution.GetRequiredDocument(documentId);
                var oldText = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var newText = oldText.WithChanges(changesToApply);
                currentSolution = currentSolution.WithDocumentText(documentId, newText);
            }

            return currentSolution;
        }

        private readonly struct TextChangeIntervalIntrospector : IIntervalIntrospector<TextChange>
        {
            int IIntervalIntrospector<TextChange>.GetStart(TextChange value) => value.Span.Start;
            int IIntervalIntrospector<TextChange>.GetLength(TextChange value) => value.Span.Length;
        }

        /// <summary>
        /// Try to merge the changes between <paramref name="newDocument"/> and <paramref name="oldDocument"/>
        /// into <paramref name="cumulativeChanges"/>. If there is any conflicting change in 
        /// <paramref name="newDocument"/> with existing <paramref name="cumulativeChanges"/>, then no
        /// changes are added
        /// </summary>
        private static async Task TryAddDocumentMergeChangesAsync(
            IDocumentTextDifferencingService differenceService,
            Document oldDocument,
            Document newDocument,
            SimpleIntervalTree<TextChange, TextChangeIntervalIntrospector> cumulativeChanges,
            CancellationToken cancellationToken)
        {
            var currentChanges = await differenceService.GetTextChangesAsync(
                oldDocument, newDocument, cancellationToken).ConfigureAwait(false);

            if (AllChangesCanBeApplied(cumulativeChanges, currentChanges))
            {
                foreach (var change in currentChanges)
                {
                    cumulativeChanges.AddIntervalInPlace(change);
                }
            }
        }

        private static bool AllChangesCanBeApplied(
            SimpleIntervalTree<TextChange, TextChangeIntervalIntrospector> cumulativeChanges,
            ImmutableArray<TextChange> currentChanges)
        {
            using var overlappingSpans = TemporaryArray<TextChange>.Empty;
            using var intersectingSpans = TemporaryArray<TextChange>.Empty;

            return AllChangesCanBeApplied(
                cumulativeChanges, currentChanges,
                overlappingSpans: ref overlappingSpans.AsRef(),
                intersectingSpans: ref intersectingSpans.AsRef());
        }

        private static bool AllChangesCanBeApplied(
            SimpleIntervalTree<TextChange, TextChangeIntervalIntrospector> cumulativeChanges,
            ImmutableArray<TextChange> currentChanges,
            ref TemporaryArray<TextChange> overlappingSpans,
            ref TemporaryArray<TextChange> intersectingSpans)
        {
            foreach (var change in currentChanges)
            {
                overlappingSpans.Clear();
                intersectingSpans.Clear();

                cumulativeChanges.FillWithIntervalsThatOverlapWith(
                    change.Span.Start, change.Span.Length, ref overlappingSpans);

                cumulativeChanges.FillWithIntervalsThatIntersectWith(
                   change.Span.Start, change.Span.Length, ref intersectingSpans);

                var value = ChangeCanBeApplied(change,
                    overlappingSpans: in overlappingSpans,
                    intersectingSpans: in intersectingSpans);
                if (!value)
                {
                    return false;
                }
            }

            // All the changes would merge in fine.  We can absorb this.
            return true;
        }

        private static bool ChangeCanBeApplied(
            TextChange change,
            in TemporaryArray<TextChange> overlappingSpans,
            in TemporaryArray<TextChange> intersectingSpans)
        {
            // We distinguish two types of changes that can happen.  'Pure Insertions' 
            // and 'Overwrites'.  Pure-Insertions are those that are just inserting 
            // text into a specific *position*.  They do not replace any existing text.
            // 'Overwrites' end up replacing existing text with some other piece of 
            // (possibly-empty) text.
            //
            // Overwrites of text tend to be easy to understand and merge.  It is very
            // clear what code is being overwritten and how it should interact with
            // other changes.  Pure-insertions are more ambiguous to deal with.  For
            // example, say there are two pure-insertions at some position.  There is
            // no way for us to know what to do with this.  For example, we could take
            // one insertion then the other, or vice versa.  Because of this ambiguity
            // we conservatively disallow cases like this.

            return IsPureInsertion(change)
                ? PureInsertionChangeCanBeApplied(change, in overlappingSpans, in intersectingSpans)
                : OverwriteChangeCanBeApplied(change, in overlappingSpans, in intersectingSpans);
        }

        private static bool IsPureInsertion(TextChange change)
            => change.Span.IsEmpty;

        private static bool PureInsertionChangeCanBeApplied(
            TextChange change,
            in TemporaryArray<TextChange> overlappingSpans,
            in TemporaryArray<TextChange> intersectingSpans)
        {
            // Pure insertions can't ever overlap anything.  (They're just an insertion at a 
            // single position, and overlaps can't occur with single-positions).
            Debug.Assert(IsPureInsertion(change));
            Debug.Assert(overlappingSpans.Count == 0);
            if (intersectingSpans.Count == 0)
            {
                // Our pure-insertion didn't hit any other changes.  This is safe to apply.
                return true;
            }

            if (intersectingSpans.Count == 1)
            {
                // Our pure-insertion hit another change.  Thats safe when:
                //  1) if both changes are the same.
                //  2) the change we're hitting is an overwrite-change and we're at the end of it.

                // Specifically, it is not safe for us to insert somewhere in start-to-middle of an 
                // existing overwrite-change.  And if we have another pure-insertion change, then it's 
                // not safe for both of us to be inserting at the same point (except when the 
                // change is identical).

                // Note: you may wonder why we don't support hitting an overwriting change at the
                // start of the overwrite.  This is because it's now ambiguous as to which of these
                // changes should be applied first.

                var otherChange = intersectingSpans[0];
                if (otherChange == change)
                {
                    // We're both pure-inserting the same text at the same position.  
                    // We assume this is a case of some provider making the same changes and
                    // we allow this.
                    return true;
                }

                return !IsPureInsertion(otherChange) &&
                       otherChange.Span.End == change.Span.Start;
            }

            // We're intersecting multiple changes.  That's never OK.
            return false;
        }

        private static bool OverwriteChangeCanBeApplied(
            TextChange change,
            in TemporaryArray<TextChange> overlappingSpans,
            in TemporaryArray<TextChange> intersectingSpans)
        {
            Debug.Assert(!IsPureInsertion(change));

            return !OverwriteChangeConflictsWithOverlappingSpans(change, in overlappingSpans) &&
                   !OverwriteChangeConflictsWithIntersectingSpans(change, in intersectingSpans);
        }

        private static bool OverwriteChangeConflictsWithOverlappingSpans(
            TextChange change,
            in TemporaryArray<TextChange> overlappingSpans)
        {
            Debug.Assert(!IsPureInsertion(change));

            if (overlappingSpans.Count == 0)
            {
                // This overwrite didn't overlap with any other changes.  This change is safe to make.
                return false;
            }

            // The change we want to make overlapped an existing change we're making.  Only allow
            // this if there was a single overlap and we are exactly the same change as it.
            // Otherwise, this is a conflict.
            var isSafe = overlappingSpans.Count == 1 && overlappingSpans[0] == change;

            return !isSafe;
        }

        private static bool OverwriteChangeConflictsWithIntersectingSpans(
            TextChange change,
            in TemporaryArray<TextChange> intersectingSpans)
        {
            Debug.Assert(!IsPureInsertion(change));

            // We care about our intersections with pure-insertion changes.  Overwrite-changes that
            // we overlap are already handled in OverwriteChangeConflictsWithOverlappingSpans.
            // And overwrite spans that we abut (i.e. which we're adjacent to) are totally safe 
            // for both to be applied.
            //
            // However, pure-insertion changes are extremely ambiguous. It is not possible to tell which
            // change should be applied first.  So if we get any pure-insertions we have to bail
            // on applying this span.
            return intersectingSpans.Any(static otherSpan => IsPureInsertion(otherSpan));
        }
    }
}
