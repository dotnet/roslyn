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
            if (documentsAndDiagnosticsToFixMap != null && documentsAndDiagnosticsToFixMap.Any())
            {
                FixAllLogger.LogDiagnosticsStats(documentsAndDiagnosticsToFixMap);

                var fixesBag = new ConcurrentBag<CodeAction>();

                using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation_Fixes, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var documents = documentsAndDiagnosticsToFixMap.Keys;
                    var tasks = documents.Select(d => AddDocumentFixesAsync(d, documentsAndDiagnosticsToFixMap[d], fixesBag.Add, fixAllState, cancellationToken))
                                         .ToArray();
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }

                if (fixesBag.Any())
                {
                    using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation_Merge, cancellationToken))
                    {
                        FixAllLogger.LogFixesToMergeStats(fixesBag);
                        return await TryGetMergedFixAsync(fixesBag, fixAllState, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return null;
        }

        public async virtual Task AddDocumentFixesAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, Action<CodeAction> addFix, 
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            Debug.Assert(!diagnostics.IsDefault);
            cancellationToken.ThrowIfCancellationRequested();

            var fixerTasks = new Task[diagnostics.Length];

            for (var i = 0; i < diagnostics.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var diagnostic = diagnostics[i];
                fixerTasks[i] = Task.Run(async () =>
                {
                    var fixes = new List<CodeAction>();
                    var context = new CodeFixContext(document, diagnostic,

                        // TODO: Can we share code between similar lambdas that we pass to this API in BatchFixAllProvider.cs, CodeFixService.cs and CodeRefactoringService.cs?
                        (a, d) =>
                        {
                            // Serialize access for thread safety - we don't know what thread the fix provider will call this delegate from.
                            lock (fixes)
                            {
                                fixes.Add(a);
                            }
                        },
                        cancellationToken);

                    // TODO: Wrap call to ComputeFixesAsync() below in IExtensionManager.PerformFunctionAsync() so that
                    // a buggy extension that throws can't bring down the host?
                    var task = fixAllState.CodeFixProvider.RegisterCodeFixesAsync(context) ?? SpecializedTasks.EmptyTask;
                    await task.ConfigureAwait(false);

                    foreach (var fix in fixes)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (fix != null && fix.EquivalenceKey == fixAllState.CodeActionEquivalenceKey)
                        {
                            addFix(fix);
                        }
                    }
                });
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

                var fixesBag = new ConcurrentBag<CodeAction>();

                using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation_Fixes, cancellationToken))
                {
                    var projects = projectsAndDiagnosticsToFixMap.Keys;
                    var tasks = projects.Select(p => AddProjectFixesAsync(p, projectsAndDiagnosticsToFixMap[p], fixesBag.Add, fixAllState, cancellationToken))
                                        .ToArray();
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }

                if (fixesBag.Any())
                {
                    using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation_Merge, cancellationToken))
                    {
                        FixAllLogger.LogFixesToMergeStats(fixesBag);
                        return await TryGetMergedFixAsync(fixesBag, fixAllState, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return null;
        }

        public virtual async Task AddProjectFixesAsync(
            Project project, ImmutableArray<Diagnostic> diagnostics, Action<CodeAction> addFix, 
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            Debug.Assert(!diagnostics.IsDefault);
            cancellationToken.ThrowIfCancellationRequested();

            var fixes = new List<CodeAction>();
            var context = new CodeFixContext(project, diagnostics,

                // TODO: Can we share code between similar lambdas that we pass to this API in BatchFixAllProvider.cs, CodeFixService.cs and CodeRefactoringService.cs?
                (a, d) =>
                {
                    // Serialize access for thread safety - we don't know what thread the fix provider will call this delegate from.
                    lock (fixes)
                    {
                        fixes.Add(a);
                    }
                },
                cancellationToken);

            // TODO: Wrap call to ComputeFixesAsync() below in IExtensionManager.PerformFunctionAsync() so that
            // a buggy extension that throws can't bring down the host?
            var task = fixAllState.CodeFixProvider.RegisterCodeFixesAsync(context) ?? SpecializedTasks.EmptyTask;
            await task.ConfigureAwait(false);

            foreach (var fix in fixes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (fix != null && fix.EquivalenceKey == fixAllState.CodeActionEquivalenceKey)
                {
                    addFix(fix);
                }
            }
        }

        public virtual async Task<CodeAction> TryGetMergedFixAsync(
            IEnumerable<CodeAction> batchOfFixes, FixAllState fixAllState, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(batchOfFixes);
            Contract.ThrowIfFalse(batchOfFixes.Any());

            var solution = fixAllState.Solution;
            var newSolution = await TryMergeFixesAsync(solution, batchOfFixes, fixAllState, cancellationToken).ConfigureAwait(false);
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
            Solution oldSolution, IEnumerable<CodeAction> codeActions,
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            var changedDocumentsMap = new Dictionary<DocumentId, Document>();
            Dictionary<DocumentId, List<Document>> documentsToMergeMap = null;

            foreach (var codeAction in codeActions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // TODO: Parallelize GetChangedSolutionInternalAsync for codeActions
                var changedSolution = await codeAction.GetChangedSolutionInternalAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                var solutionChanges = new SolutionChanges(changedSolution, oldSolution);

                // TODO: Handle added/removed documents
                // TODO: Handle changed/added/removed additional documents

                var documentIdsWithChanges = solutionChanges
                    .GetProjectChanges()
                    .SelectMany(p => p.GetChangedDocuments());

                foreach (var documentId in documentIdsWithChanges)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var document = changedSolution.GetDocument(documentId);

                    Document existingDocument;
                    if (changedDocumentsMap.TryGetValue(documentId, out existingDocument))
                    {
                        if (existingDocument != null)
                        {
                            changedDocumentsMap[documentId] = null;
                            var documentsToMerge = new List<Document>();
                            documentsToMerge.Add(existingDocument);
                            documentsToMerge.Add(document);
                            documentsToMergeMap = documentsToMergeMap ?? new Dictionary<DocumentId, List<Document>>();
                            documentsToMergeMap[documentId] = documentsToMerge;
                        }
                        else
                        {
                            documentsToMergeMap[documentId].Add(document);
                        }
                    }
                    else
                    {
                        changedDocumentsMap[documentId] = document;
                    }
                }
            }

            var currentSolution = oldSolution;
            foreach (var kvp in changedDocumentsMap)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var document = kvp.Value;
                if (document != null)
                {
                    var documentText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    currentSolution = currentSolution.WithDocumentText(kvp.Key, documentText);
                }
            }

            if (documentsToMergeMap != null)
            {
                var mergedDocuments = new ConcurrentDictionary<DocumentId, SourceText>();
                var documentsToMergeArray = documentsToMergeMap.ToImmutableArray();
                var mergeTasks = new Task[documentsToMergeArray.Length];
                for (int i = 0; i < documentsToMergeArray.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var kvp = documentsToMergeArray[i];
                    var documentId = kvp.Key;
                    var documentsToMerge = kvp.Value;
                    var oldDocument = oldSolution.GetDocument(documentId);

                    mergeTasks[i] = Task.Run(async () =>
                    {
                        var appliedChanges = (await documentsToMerge[0].GetTextChangesAsync(oldDocument, cancellationToken).ConfigureAwait(false)).ToList();

                        foreach (var document in documentsToMerge.Skip(1))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            appliedChanges = await TryAddDocumentMergeChangesAsync(
                                oldDocument,
                                document,
                                appliedChanges,
                                cancellationToken).ConfigureAwait(false);
                        }

                        var oldText = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        var newText = oldText.WithChanges(appliedChanges);
                        mergedDocuments.TryAdd(documentId, newText);
                    });
                }

                await Task.WhenAll(mergeTasks).ConfigureAwait(false);

                foreach (var kvp in mergedDocuments)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentSolution = currentSolution.WithDocumentText(kvp.Key, kvp.Value);
                }
            }

            return currentSolution;
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
