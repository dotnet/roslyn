// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public override async Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
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

        internal override async Task<CodeAction> GetFixAsync(
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentsAndDiagnosticsToFixMap,
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            if (documentsAndDiagnosticsToFixMap?.Any() == true)
            {
                FixAllLogger.LogDiagnosticsStats(documentsAndDiagnosticsToFixMap);

                var diagnosticsAndCodeActions = await GetDiagnosticsAndCodeActions(
                    documentsAndDiagnosticsToFixMap, fixAllState, cancellationToken).ConfigureAwait(false);

                if (diagnosticsAndCodeActions.Length > 0)
                {
                    using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation_Merge, cancellationToken))
                    {
                        FixAllLogger.LogFixesToMergeStats(diagnosticsAndCodeActions.Length);
                        return await TryGetMergedFixAsync(
                            diagnosticsAndCodeActions, fixAllState, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return null;
        }

        private async Task<ImmutableArray<(Diagnostic diagnostic, CodeAction action)>> GetDiagnosticsAndCodeActions(
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentsAndDiagnosticsToFixMap,
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            var fixesBag = new ConcurrentBag<(Diagnostic diagnostic, CodeAction action)>();
            using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation_Fixes, cancellationToken))
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

        protected async virtual Task AddDocumentFixesAsync(
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
                    return fixAllState.CodeFixProvider.RegisterCodeFixesAsync(context) ?? SpecializedTasks.EmptyTask;
                }));
            }

            await Task.WhenAll(fixerTasks).ConfigureAwait(false);
        }

        internal override async Task<CodeAction> GetFixAsync(
            ImmutableDictionary<Project, ImmutableArray<Diagnostic>> projectsAndDiagnosticsToFixMap,
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            if (projectsAndDiagnosticsToFixMap != null && projectsAndDiagnosticsToFixMap.Any())
            {
                FixAllLogger.LogDiagnosticsStats(projectsAndDiagnosticsToFixMap);

                var bag = new ConcurrentBag<(Diagnostic diagnostic, CodeAction action)>();
                using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation_Fixes, cancellationToken))
                {
                    var projects = projectsAndDiagnosticsToFixMap.Keys;
                    var tasks = projects.Select(p => AddProjectFixesAsync(
                        p, projectsAndDiagnosticsToFixMap[p], bag, fixAllState, cancellationToken)).ToArray();

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }

                var result = bag.ToImmutableArray();
                if (result.Length > 0)
                {
                    using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation_Merge, cancellationToken))
                    {
                        FixAllLogger.LogFixesToMergeStats(result.Length);
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
            => (action, diagnostics) =>
               {
                   if (action != null && action.EquivalenceKey == fixAllState.CodeActionEquivalenceKey)
                   {
                       result.Add((diagnostics.First(), action));
                   }
               };


        protected virtual Task AddProjectFixesAsync(
            Project project, ImmutableArray<Diagnostic> diagnostics, 
            ConcurrentBag<(Diagnostic diagnostic, CodeAction action)> fixes, 
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            Debug.Assert(!diagnostics.IsDefault);
            cancellationToken.ThrowIfCancellationRequested();

            var registerCodeFix = GetRegisterCodeFixAction(fixAllState, fixes);
            var context = new CodeFixContext(
                project, diagnostics, registerCodeFix, cancellationToken);

            // TODO: Wrap call to ComputeFixesAsync() below in IExtensionManager.PerformFunctionAsync() so that
            // a buggy extension that throws can't bring down the host?
            return fixAllState.CodeFixProvider.RegisterCodeFixesAsync(context) ?? SpecializedTasks.EmptyTask;
        }

        public virtual async Task<CodeAction> TryGetMergedFixAsync(
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
        {
            return fixAllState.GetDefaultFixAllTitle();
        }

        public virtual async Task<Solution> TryMergeFixesAsync(
            Solution oldSolution, 
            ImmutableArray<(Diagnostic diagnostic, CodeAction action)> diagnosticsAndCodeActions,
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            var documentIdToChangedDocuments = await GetDocumentIdToChangedDocuments(
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

        private async Task<IReadOnlyDictionary<DocumentId, ConcurrentBag<(CodeAction, Document)>>> GetDocumentIdToChangedDocuments(
            Solution oldSolution, 
            ImmutableArray<(Diagnostic diagnostic, CodeAction action)> diagnosticsAndCodeActions, 
            CancellationToken cancellationToken)
        {
            var documentIdToChangedDocuments = new ConcurrentDictionary<DocumentId, ConcurrentBag<(CodeAction, Document)>>();

            // Process all code actions in parallel to find all the documents that are changed.
            // For each changed document, also keep track of the associated code action that
            // produced it.
            var getChangedDocumentsTasks = new List<Task>();
            foreach (var diagnosticAndCodeAction in diagnosticsAndCodeActions)
            {
                getChangedDocumentsTasks.Add(GetChangedDocumentsAsync(
                    oldSolution, documentIdToChangedDocuments,
                    diagnosticAndCodeAction.action, cancellationToken));
            }

            await Task.WhenAll(getChangedDocumentsTasks).ConfigureAwait(false);
            return documentIdToChangedDocuments;
        }

        private async Task<IReadOnlyDictionary<DocumentId, SourceText>> GetDocumentIdToFinalTextAsync(
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

        private async Task GetFinalDocumentTextAsync(
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
            var firstChangedDocument = orderedDocuments[0].document;
            var documentId = firstChangedDocument.Id;

            var oldDocument = oldSolution.GetDocument(documentId);
            var appliedChanges = (await firstChangedDocument.GetTextChangesAsync(oldDocument, cancellationToken).ConfigureAwait(false)).ToList();

            for (var i = 1; i < orderedDocuments.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentDocument = orderedDocuments[i].document;
                Debug.Assert(currentDocument.Id == documentId);

                appliedChanges = await TryAddDocumentMergeChangesAsync(
                    oldDocument,
                    currentDocument,
                    appliedChanges,
                    cancellationToken).ConfigureAwait(false);
            }

            var oldText = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = oldText.WithChanges(appliedChanges);

            documentIdToFinalText.TryAdd(documentId, newText);
        }

        private static Func<DocumentId, ConcurrentBag<(CodeAction, Document)>> s_getValue = 
            _ => new ConcurrentBag<(CodeAction, Document)>();

        private async Task GetChangedDocumentsAsync(
            Solution oldSolution,
            ConcurrentDictionary<DocumentId, ConcurrentBag<(CodeAction, Document)>> documentIdToChangedDocuments,
            CodeAction codeAction,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var changedSolution = await codeAction.GetChangedSolutionInternalAsync(
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var solutionChanges = new SolutionChanges(changedSolution, oldSolution);

            // TODO: Handle added/removed documents
            // TODO: Handle changed/added/removed additional documents

            var documentIdsWithChanges = solutionChanges
                .GetProjectChanges()
                .SelectMany(p => p.GetChangedDocuments());

            foreach (var documentId in documentIdsWithChanges)
            {
                var changedDocument = changedSolution.GetDocument(documentId);

                documentIdToChangedDocuments.GetOrAdd(documentId, s_getValue).Add(
                    (codeAction, changedDocument));
            }
        }

        /// <summary>
        /// Try to merge the changes between <paramref name="newDocument"/> and <paramref name="oldDocument"/> into <paramref name="cumulativeChanges"/>.
        /// If there is any conflicting change in <paramref name="newDocument"/> with existing <paramref name="cumulativeChanges"/>, then the original <paramref name="cumulativeChanges"/> are returned.
        /// Otherwise, the newly merged changes are returned.
        /// </summary>
        /// <param name="oldDocument">Base document on which FixAll was invoked.</param>
        /// <param name="newDocument">New document with a code fix that is being merged.</param>
        /// <param name="cumulativeChanges">Existing merged changes from other batch fixes into which newDocument changes are being merged.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private static async Task<List<TextChange>> TryAddDocumentMergeChangesAsync(
            Document oldDocument,
            Document newDocument,
            List<TextChange> cumulativeChanges,
            CancellationToken cancellationToken)
        {
            var successfullyMergedChanges = new List<TextChange>();

            int cumulativeChangeIndex = 0;
            foreach (var change in await newDocument.GetTextChangesAsync(oldDocument, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (cumulativeChangeIndex < cumulativeChanges.Count && cumulativeChanges[cumulativeChangeIndex].Span.End < change.Span.Start)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Existing change that does not overlap with the current change in consideration
                    successfullyMergedChanges.Add(cumulativeChanges[cumulativeChangeIndex]);
                    cumulativeChangeIndex++;
                }

                if (cumulativeChangeIndex < cumulativeChanges.Count)
                {
                    var cumulativeChange = cumulativeChanges[cumulativeChangeIndex];
                    if (!cumulativeChange.Span.IntersectsWith(change.Span))
                    {
                        // The current change in consideration does not intersect with any existing change
                        successfullyMergedChanges.Add(change);
                    }
                    else
                    {
                        if (change.Span != cumulativeChange.Span || change.NewText != cumulativeChange.NewText)
                        {
                            // The current change in consideration overlaps an existing change but
                            // the changes are not identical. 
                            // Bail out merge efforts and return the original 'cumulativeChanges'.
                            return cumulativeChanges;
                        }
                        else
                        {
                            // The current change in consideration is identical to an existing change
                            successfullyMergedChanges.Add(change);
                            cumulativeChangeIndex++;
                        }
                    }
                }
                else
                {
                    // The current change in consideration does not intersect with any existing change
                    successfullyMergedChanges.Add(change);
                }
            }

            while (cumulativeChangeIndex < cumulativeChanges.Count)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Existing change that does not overlap with the current change in consideration
                successfullyMergedChanges.Add(cumulativeChanges[cumulativeChangeIndex]);
                cumulativeChangeIndex++;
            }

            return successfullyMergedChanges;
        }
    }
}
