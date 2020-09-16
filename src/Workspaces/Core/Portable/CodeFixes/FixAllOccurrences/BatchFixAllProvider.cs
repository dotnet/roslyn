// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Helper class for "Fix all occurrences" code fix providers.
    /// </summary>
    internal partial class BatchFixAllProvider : FixAllProvider
    {
        public static readonly FixAllProvider Instance = new BatchFixAllProvider();

        protected BatchFixAllProvider() { }

        #region "AbstractFixAllProvider methods"

        public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            if (fixAllContext.Document != null)
            {
                var documentsAndDiagnosticsToFixMap = await fixAllContext.GetDocumentDiagnosticsToFixAsync().ConfigureAwait(false);
                return await GetFixAsync(documentsAndDiagnosticsToFixMap, fixAllContext.State, fixAllContext.CancellationToken).ConfigureAwait(false);
            }
            else
            {
                var projectsAndDiagnosticsToFixMap = await fixAllContext.GetProjectDiagnosticsToFixAsync().ConfigureAwait(false);
                return await GetFixAsync(projectsAndDiagnosticsToFixMap, fixAllContext.State, fixAllContext.CancellationToken).ConfigureAwait(false);
            }
        }

        #endregion

        private async Task<CodeAction?> GetFixAsync(
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentsAndDiagnosticsToFixMap,
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            if (documentsAndDiagnosticsToFixMap?.Any() == true)
            {
                FixAllLogger.LogDiagnosticsStats(fixAllState.CorrelationId, documentsAndDiagnosticsToFixMap);

                var diagnosticsAndCodeActions = await GetDiagnosticsAndCodeActionsAsync(
                    documentsAndDiagnosticsToFixMap, fixAllState, cancellationToken).ConfigureAwait(false);

                if (diagnosticsAndCodeActions.Length > 0)
                {
                    var functionId = FunctionId.CodeFixes_FixAllOccurrencesComputation_Document_Merge;
                    using (Logger.LogBlock(functionId, FixAllLogger.CreateCorrelationLogMessage(fixAllState.CorrelationId), cancellationToken))
                    {
                        FixAllLogger.LogFixesToMergeStats(functionId, fixAllState.CorrelationId, diagnosticsAndCodeActions.Length);
                        return await TryGetMergedFixAsync(
                            diagnosticsAndCodeActions, fixAllState, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return null;
        }

        private async Task<ImmutableArray<(Diagnostic diagnostic, CodeAction action)>> GetDiagnosticsAndCodeActionsAsync(
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentsAndDiagnosticsToFixMap,
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            var fixesBag = new ConcurrentBag<(Diagnostic diagnostic, CodeAction action)>();
            using (Logger.LogBlock(
                FunctionId.CodeFixes_FixAllOccurrencesComputation_Document_Fixes,
                FixAllLogger.CreateCorrelationLogMessage(fixAllState.CorrelationId),
                cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tasks = new List<Task>();

                foreach (var kvp in documentsAndDiagnosticsToFixMap)
                {
                    var document = kvp.Key;
                    var diagnosticsToFix = kvp.Value;
                    Debug.Assert(!diagnosticsToFix.IsDefaultOrEmpty);
                    if (!diagnosticsToFix.IsDefaultOrEmpty)
                    {
                        tasks.Add(AddDocumentFixesAsync(
                            document, diagnosticsToFix, fixesBag, fixAllState, cancellationToken));
                    }
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            return fixesBag.ToImmutableArray();
        }

        protected virtual async Task AddDocumentFixesAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            ConcurrentBag<(Diagnostic diagnostic, CodeAction action)> fixes,
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            Debug.Assert(!diagnostics.IsDefault);
            cancellationToken.ThrowIfCancellationRequested();

            var registerCodeFix = GetRegisterCodeFixAction(fixAllState, fixes);

            var fixerTasks = new List<Task>();
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                fixerTasks.Add(Task.Run(() =>
                {
                    var context = new CodeFixContext(document, diagnostic, registerCodeFix, cancellationToken);

                    // TODO: Wrap call to ComputeFixesAsync() below in IExtensionManager.PerformFunctionAsync() so that
                    // a buggy extension that throws can't bring down the host?
                    return fixAllState.CodeFixProvider.RegisterCodeFixesAsync(context) ?? Task.CompletedTask;
                }, cancellationToken));
            }

            await Task.WhenAll(fixerTasks).ConfigureAwait(false);
        }

        private async Task<CodeAction?> GetFixAsync(
            ImmutableDictionary<Project, ImmutableArray<Diagnostic>> projectsAndDiagnosticsToFixMap,
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            if (projectsAndDiagnosticsToFixMap != null && projectsAndDiagnosticsToFixMap.Any())
            {
                FixAllLogger.LogDiagnosticsStats(fixAllState.CorrelationId, projectsAndDiagnosticsToFixMap);

                var bag = new ConcurrentBag<(Diagnostic diagnostic, CodeAction action)>();
                using (Logger.LogBlock(
                    FunctionId.CodeFixes_FixAllOccurrencesComputation_Project_Fixes,
                    FixAllLogger.CreateCorrelationLogMessage(fixAllState.CorrelationId),
                    cancellationToken))
                {
                    var projects = projectsAndDiagnosticsToFixMap.Keys;
                    var tasks = projects.Select(p => AddProjectFixesAsync(
                        p, projectsAndDiagnosticsToFixMap[p], bag, fixAllState, cancellationToken)).ToArray();

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }

                var result = bag.ToImmutableArray();
                if (result.Length > 0)
                {
                    var functionId = FunctionId.CodeFixes_FixAllOccurrencesComputation_Project_Merge;
                    using (Logger.LogBlock(functionId, cancellationToken))
                    {
                        FixAllLogger.LogFixesToMergeStats(functionId, fixAllState.CorrelationId, result.Length);
                        return await TryGetMergedFixAsync(
                            result, fixAllState, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return null;
        }

        private static Action<CodeAction, ImmutableArray<Diagnostic>> GetRegisterCodeFixAction(
            FixAllState fixAllState,
            ConcurrentBag<(Diagnostic diagnostic, CodeAction action)> result)
        {
            return (action, diagnostics) =>
            {
                using var _ = ArrayBuilder<CodeAction>.GetInstance(out var builder);
                builder.Push(action);
                while (builder.Count > 0)
                {
                    var currentAction = builder.Pop();
                    if (currentAction is { EquivalenceKey: var equivalenceKey }
                        && equivalenceKey == fixAllState.CodeActionEquivalenceKey)
                    {
                        result.Add((diagnostics.First(), currentAction));
                    }

                    foreach (var nestedAction in currentAction.NestedCodeActions)
                    {
                        builder.Push(nestedAction);
                    }
                }
            };
        }

        protected virtual Task AddProjectFixesAsync(
            Project project, ImmutableArray<Diagnostic> diagnostics,
            ConcurrentBag<(Diagnostic diagnostic, CodeAction action)> fixes,
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual async Task<CodeAction?> TryGetMergedFixAsync(
            ImmutableArray<(Diagnostic diagnostic, CodeAction action)> batchOfFixes,
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(batchOfFixes.Any());

            var solution = fixAllState.Solution;
            var newSolution = await TryMergeFixesAsync(
                solution, batchOfFixes, fixAllState, cancellationToken).ConfigureAwait(false);
            if (newSolution != null && newSolution != solution)
            {
                var title = GetFixAllTitle(fixAllState);
                return new CodeAction.SolutionChangeAction(title, _ => Task.FromResult(newSolution));
            }

            return null;
        }

        public virtual string GetFixAllTitle(FixAllState fixAllState)
            => FixAllContextHelper.GetDefaultFixAllTitle(fixAllState.Scope, fixAllState.DiagnosticIds, fixAllState.Document, fixAllState.Project);

        public virtual async Task<Solution> TryMergeFixesAsync(
            Solution oldSolution,
            ImmutableArray<(Diagnostic diagnostic, CodeAction action)> diagnosticsAndCodeActions,
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            var documentIdToChangedDocuments = await GetDocumentIdToChangedDocumentsAsync(
                oldSolution, diagnosticsAndCodeActions, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // Now, in parallel, process all the changes to any individual document, producing
            // the final source text for any given document.
            var documentIdToFinalText = await GetDocumentIdToFinalTextAsync(
                oldSolution, documentIdToChangedDocuments,
                diagnosticsAndCodeActions, cancellationToken).ConfigureAwait(false);

            // Finally, apply the changes to each document to the solution, producing the
            // new solution.
            var currentSolution = oldSolution;
            foreach (var kvp in documentIdToFinalText)
            {
                currentSolution = currentSolution.WithDocumentText(kvp.Key, kvp.Value);
            }

            return currentSolution;
        }

        private static async Task<IReadOnlyDictionary<DocumentId, ConcurrentBag<(CodeAction, Document)>>> GetDocumentIdToChangedDocumentsAsync(
            Solution oldSolution,
            ImmutableArray<(Diagnostic diagnostic, CodeAction action)> diagnosticsAndCodeActions,
            CancellationToken cancellationToken)
        {
            var documentIdToChangedDocuments = new ConcurrentDictionary<DocumentId, ConcurrentBag<(CodeAction, Document)>>();

            // Process all code actions in parallel to find all the documents that are changed.
            // For each changed document, also keep track of the associated code action that
            // produced it.
            var getChangedDocumentsTasks = new List<Task>();
            foreach (var (_, action) in diagnosticsAndCodeActions)
            {
                getChangedDocumentsTasks.Add(GetChangedDocumentsAsync(
                    oldSolution, documentIdToChangedDocuments,
                    action, cancellationToken));
            }

            await Task.WhenAll(getChangedDocumentsTasks).ConfigureAwait(false);
            return documentIdToChangedDocuments;
        }

        private static async Task<IReadOnlyDictionary<DocumentId, SourceText>> GetDocumentIdToFinalTextAsync(
            Solution oldSolution,
            IReadOnlyDictionary<DocumentId, ConcurrentBag<(CodeAction, Document)>> documentIdToChangedDocuments,
            ImmutableArray<(Diagnostic diagnostic, CodeAction action)> diagnosticsAndCodeActions,
            CancellationToken cancellationToken)
        {
            // We process changes to a document in 'Diagnostic' order.  i.e. we apply the change
            // created for an earlier diagnostic before the change applied to a later diagnostic.
            // It's as if we processed the diagnostics in the document, in order, finding the code
            // action for it and applying it right then.
            var codeActionToDiagnosticLocation = diagnosticsAndCodeActions.ToDictionary(
                tuple => tuple.action, tuple => tuple.diagnostic?.Location.SourceSpan.Start ?? 0);

            var documentIdToFinalText = new ConcurrentDictionary<DocumentId, SourceText>();
            var getFinalDocumentTasks = new List<Task>();
            foreach (var kvp in documentIdToChangedDocuments)
            {
                getFinalDocumentTasks.Add(GetFinalDocumentTextAsync(
                    oldSolution, codeActionToDiagnosticLocation, documentIdToFinalText,
                    kvp.Value, cancellationToken));
            }

            await Task.WhenAll(getFinalDocumentTasks).ConfigureAwait(false);
            return documentIdToFinalText;
        }

        private static async Task GetFinalDocumentTextAsync(
            Solution oldSolution,
            Dictionary<CodeAction, int> codeActionToDiagnosticLocation,
            ConcurrentDictionary<DocumentId, SourceText> documentIdToFinalText,
            IEnumerable<(CodeAction action, Document document)> changedDocuments,
            CancellationToken cancellationToken)
        {
            // Merges all the text changes made to a single document by many code actions
            // into the final text for that document.

            var orderedDocuments = changedDocuments.OrderBy(t => codeActionToDiagnosticLocation[t.action])
                                                   .ThenBy(t => t.action.Title)
                                                   .ToImmutableArray();

            if (orderedDocuments.Length == 1)
            {
                // Super simple case.  Only one code action changed this document.  Just use
                // its final result.
                var document = orderedDocuments[0].document;
                var finalText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                documentIdToFinalText.TryAdd(document.Id, finalText);
                return;
            }

            Debug.Assert(orderedDocuments.Length > 1);

            // More complex case.  We have multiple changes to the document.  Apply them in order
            // to get the final document.

            var totalChangesIntervalTree = SimpleIntervalTree.Create(new TextChangeIntervalIntrospector(), Array.Empty<TextChange>());

            var oldDocument = oldSolution.GetRequiredDocument(orderedDocuments[0].document.Id);
            var differenceService = oldSolution.Workspace.Services.GetRequiredService<IDocumentTextDifferencingService>();

            foreach (var (_, currentDocument) in orderedDocuments)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Debug.Assert(currentDocument.Id == oldDocument.Id);

                await TryAddDocumentMergeChangesAsync(
                    differenceService,
                    oldDocument,
                    currentDocument,
                    totalChangesIntervalTree,
                    cancellationToken).ConfigureAwait(false);
            }

            // WithChanges requires a ordered list of TextChanges without any overlap.
            var changesToApply = totalChangesIntervalTree.Distinct().OrderBy(tc => tc.Span.Start);

            var oldText = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = oldText.WithChanges(changesToApply);

            documentIdToFinalText.TryAdd(oldDocument.Id, newText);
        }

        private readonly struct TextChangeIntervalIntrospector : IIntervalIntrospector<TextChange>
        {
            int IIntervalIntrospector<TextChange>.GetStart(TextChange value) => value.Span.Start;
            int IIntervalIntrospector<TextChange>.GetLength(TextChange value) => value.Span.Length;
        }

        private static readonly Func<DocumentId, ConcurrentBag<(CodeAction, Document)>> s_getValue =
            _ => new ConcurrentBag<(CodeAction, Document)>();

        private static async Task GetChangedDocumentsAsync(
            Solution oldSolution,
            ConcurrentDictionary<DocumentId, ConcurrentBag<(CodeAction, Document)>> documentIdToChangedDocuments,
            CodeAction codeAction,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var changedSolution = await codeAction.GetChangedSolutionInternalAsync(
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (changedSolution is null)
            {
                // No changed documents
                return;
            }

            var solutionChanges = new SolutionChanges(changedSolution, oldSolution);

            // TODO: Handle added/removed documents
            // TODO: Handle changed/added/removed additional documents

            var documentIdsWithChanges = solutionChanges
                .GetProjectChanges()
                .SelectMany(p => p.GetChangedDocuments());

            foreach (var documentId in documentIdsWithChanges)
            {
                var changedDocument = changedSolution.GetRequiredDocument(documentId);

                documentIdToChangedDocuments.GetOrAdd(documentId, s_getValue).Add(
                    (codeAction, changedDocument));
            }
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
            using var _1 = ArrayBuilder<TextChange>.GetInstance(out var overlappingSpans);
            using var _2 = ArrayBuilder<TextChange>.GetInstance(out var intersectingSpans);

            return AllChangesCanBeApplied(
                cumulativeChanges, currentChanges,
                overlappingSpans: overlappingSpans,
                intersectingSpans: intersectingSpans);
        }

        private static bool AllChangesCanBeApplied(
            SimpleIntervalTree<TextChange, TextChangeIntervalIntrospector> cumulativeChanges,
            ImmutableArray<TextChange> currentChanges,
            ArrayBuilder<TextChange> overlappingSpans,
            ArrayBuilder<TextChange> intersectingSpans)
        {
            foreach (var change in currentChanges)
            {
                overlappingSpans.Clear();
                intersectingSpans.Clear();

                cumulativeChanges.FillWithIntervalsThatOverlapWith(
                    change.Span.Start, change.Span.Length, overlappingSpans);

                cumulativeChanges.FillWithIntervalsThatIntersectWith(
                   change.Span.Start, change.Span.Length, intersectingSpans);

                var value = ChangeCanBeApplied(change,
                    overlappingSpans: overlappingSpans,
                    intersectingSpans: intersectingSpans);
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
            ArrayBuilder<TextChange> overlappingSpans,
            ArrayBuilder<TextChange> intersectingSpans)
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
                ? PureInsertionChangeCanBeApplied(change, overlappingSpans, intersectingSpans)
                : OverwriteChangeCanBeApplied(change, overlappingSpans, intersectingSpans);
        }

        private static bool IsPureInsertion(TextChange change)
            => change.Span.IsEmpty;

        private static bool PureInsertionChangeCanBeApplied(
            TextChange change,
            ArrayBuilder<TextChange> overlappingSpans,
            ArrayBuilder<TextChange> intersectingSpans)
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
            ArrayBuilder<TextChange> overlappingSpans,
            ArrayBuilder<TextChange> intersectingSpans)
        {
            Debug.Assert(!IsPureInsertion(change));

            return !OverwriteChangeConflictsWithOverlappingSpans(change, overlappingSpans) &&
                   !OverwriteChangeConflictsWithIntersectingSpans(change, intersectingSpans);
        }

        private static bool OverwriteChangeConflictsWithOverlappingSpans(
            TextChange change,
            ArrayBuilder<TextChange> overlappingSpans)
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
            ArrayBuilder<TextChange> intersectingSpans)
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
            foreach (var otherSpan in intersectingSpans)
            {
                if (IsPureInsertion(otherSpan))
                {
                    // Intersecting with a pure-insertion is too ambiguous, so we just consider
                    // this a conflict.
                    return true;
                }
            }

            return false;
        }
    }
}
