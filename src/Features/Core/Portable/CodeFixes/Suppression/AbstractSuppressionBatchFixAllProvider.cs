// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression;

/// <summary>
/// Helper class for "Fix all occurrences" code fix providers.
/// </summary>
internal abstract class AbstractSuppressionBatchFixAllProvider : FixAllProvider
{
    public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
    {
        if (fixAllContext.Document != null)
        {
            var documentsAndDiagnosticsToFixMap = await fixAllContext.GetDocumentDiagnosticsToFixAsync().ConfigureAwait(false);
            return await GetFixAsync(documentsAndDiagnosticsToFixMap, fixAllContext).ConfigureAwait(false);
        }
        else
        {
            var projectsAndDiagnosticsToFixMap = await fixAllContext.GetProjectDiagnosticsToFixAsync().ConfigureAwait(false);
            return await GetFixAsync(projectsAndDiagnosticsToFixMap, fixAllContext).ConfigureAwait(false);
        }
    }

    private async Task<CodeAction?> GetFixAsync(
        ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentsAndDiagnosticsToFixMap,
        FixAllContext fixAllContext)
    {
        var cancellationToken = fixAllContext.CancellationToken;
        if (documentsAndDiagnosticsToFixMap?.Any() == true)
        {
            var progressTracker = fixAllContext.Progress;
            progressTracker.Report(CodeAnalysisProgress.Description(fixAllContext.GetDefaultFixAllTitle()));

            var fixAllState = fixAllContext.State;
            FixAllLogger.LogDiagnosticsStats(fixAllState.CorrelationId, documentsAndDiagnosticsToFixMap);

            var diagnosticsAndCodeActions = await GetDiagnosticsAndCodeActionsAsync(documentsAndDiagnosticsToFixMap, fixAllContext).ConfigureAwait(false);

            if (diagnosticsAndCodeActions.Length > 0)
            {
                var functionId = FunctionId.CodeFixes_FixAllOccurrencesComputation_Document_Merge;
                using (Logger.LogBlock(functionId, FixAllLogger.CreateCorrelationLogMessage(fixAllState.CorrelationId), cancellationToken))
                {
                    FixAllLogger.LogFixesToMergeStats(functionId, fixAllState.CorrelationId, diagnosticsAndCodeActions.Length);
                    return await TryGetMergedFixAsync(
                        diagnosticsAndCodeActions, fixAllState, progressTracker, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return null;
    }

    private async Task<ImmutableArray<(Diagnostic diagnostic, CodeAction action)>> GetDiagnosticsAndCodeActionsAsync(
        ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentsAndDiagnosticsToFixMap,
        FixAllContext fixAllContext)
    {
        var cancellationToken = fixAllContext.CancellationToken;
        var fixAllState = fixAllContext.State;

        using (Logger.LogBlock(
            FunctionId.CodeFixes_FixAllOccurrencesComputation_Document_Fixes,
            FixAllLogger.CreateCorrelationLogMessage(fixAllState.CorrelationId),
            cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var progressTracker = fixAllContext.Progress;

            // Determine the set of documents to actually fix.  We can also use this to update the progress bar with
            // the amount of remaining work to perform.  We'll update the progress bar as we compute each fix in
            // AddDocumentFixesAsync.
            var source = documentsAndDiagnosticsToFixMap.WhereAsArray(static (kvp, _) => !kvp.Value.IsDefaultOrEmpty, state: false);
            progressTracker.AddItems(source.Length);

            return await ProducerConsumer<(Diagnostic diagnostic, CodeAction action)>.RunParallelAsync(
                source,
                produceItems: static async (tuple, callback, args, cancellationToken) =>
                {
                    var (@this, fixAllState, progressTracker) = args;
                    using var _ = progressTracker.ItemCompletedScope();

                    var (document, diagnosticsToFix) = tuple;
                    await @this.AddDocumentFixesAsync(
                        document, diagnosticsToFix, callback, fixAllState, cancellationToken).ConfigureAwait(false);
                },
                args: (@this: this, fixAllState, progressTracker),
                cancellationToken).ConfigureAwait(false);
        }
    }

    protected virtual async Task AddDocumentFixesAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        Action<(Diagnostic diagnostic, CodeAction action)> onItemFound,
        FixAllState fixAllState, CancellationToken cancellationToken)
    {
        Debug.Assert(!diagnostics.IsDefault);
        cancellationToken.ThrowIfCancellationRequested();

        var registerCodeFix = GetRegisterCodeFixAction(fixAllState, onItemFound);
        await RoslynParallel.ForEachAsync(
            source: diagnostics,
            cancellationToken,
            async (diagnostic, cancellationToken) =>
            {
                var context = new CodeFixContext(document, diagnostic, registerCodeFix, cancellationToken);

                // TODO: Wrap call to RegisterCodeFixesAsync() below in IExtensionManager.PerformFunctionAsync() so that
                // a buggy extension that throws can't bring down the host?
                if (fixAllState.Provider.RegisterCodeFixesAsync(context) is Task task)
                    await task.ConfigureAwait(false);
            }).ConfigureAwait(false);
    }

    private async Task<CodeAction?> GetFixAsync(
        ImmutableDictionary<Project, ImmutableArray<Diagnostic>> projectsAndDiagnosticsToFixMap,
        FixAllContext fixAllContext)
    {
        var cancellationToken = fixAllContext.CancellationToken;
        var fixAllState = fixAllContext.State;
        var progressTracker = fixAllContext.Progress;

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
                        result, fixAllState, progressTracker, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return null;
    }

    private static Action<CodeAction, ImmutableArray<Diagnostic>> GetRegisterCodeFixAction(
        FixAllState fixAllState,
        Action<(Diagnostic diagnostic, CodeAction action)> onItemFound)
    {
        return (action, diagnostics) =>
        {
            using var _ = ArrayBuilder<CodeAction>.GetInstance(out var stack);
            stack.Push(action);
            while (stack.TryPop(out var currentAction))
            {
                if (currentAction is { EquivalenceKey: var equivalenceKey }
                    && equivalenceKey == fixAllState.CodeActionEquivalenceKey)
                {
                    onItemFound((diagnostics.First(), currentAction));
                }

                foreach (var nestedAction in currentAction.NestedActions)
                {
                    stack.Push(nestedAction);
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
        FixAllState fixAllState, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(batchOfFixes.Any());

        var solution = fixAllState.Solution;
        var newSolution = await TryMergeFixesAsync(
            solution, batchOfFixes, progressTracker, cancellationToken).ConfigureAwait(false);
        if (newSolution != null && newSolution != solution)
        {
            var title = FixAllHelper.GetDefaultFixAllTitle(fixAllState.Scope, title: fixAllState.DiagnosticIds.First(), fixAllState.Document!, fixAllState.Project);
            return CodeAction.SolutionChangeAction.Create(title, _ => Task.FromResult(newSolution), title);
        }

        return null;
    }

    private static async Task<Solution> TryMergeFixesAsync(
        Solution oldSolution,
        ImmutableArray<(Diagnostic diagnostic, CodeAction action)> diagnosticsAndCodeActions,
        IProgress<CodeAnalysisProgress> progressTracker,
        CancellationToken cancellationToken)
    {
        var documentIdToChangedDocuments = await GetDocumentIdToChangedDocumentsAsync(
            oldSolution, diagnosticsAndCodeActions, progressTracker, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        // Now, in parallel, process all the changes to any individual document, producing
        // the final source text for any given document.
        var documentIdToFinalText = await GetDocumentIdToFinalTextAsync(
            oldSolution, documentIdToChangedDocuments,
            diagnosticsAndCodeActions, cancellationToken).ConfigureAwait(false);

        // Finally, apply the changes to each document to the solution, producing the
        // new solution.
        var finalSolution = oldSolution.WithDocumentTexts(documentIdToFinalText);
        return finalSolution;
    }

    private static async Task<IReadOnlyDictionary<DocumentId, ConcurrentBag<(CodeAction, Document)>>> GetDocumentIdToChangedDocumentsAsync(
        Solution oldSolution,
        ImmutableArray<(Diagnostic diagnostic, CodeAction action)> diagnosticsAndCodeActions,
        IProgress<CodeAnalysisProgress> progressTracker,
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
                oldSolution, documentIdToChangedDocuments, action, progressTracker, cancellationToken));
        }

        await Task.WhenAll(getChangedDocumentsTasks).ConfigureAwait(false);
        return documentIdToChangedDocuments;
    }

    private static async Task<ImmutableArray<(DocumentId documentId, SourceText newText)>> GetDocumentIdToFinalTextAsync(
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
        foreach (var (_, changedDocuments) in documentIdToChangedDocuments)
        {
            getFinalDocumentTasks.Add(GetFinalDocumentTextAsync(
                oldSolution, codeActionToDiagnosticLocation, documentIdToFinalText, changedDocuments, cancellationToken));
        }

        await Task.WhenAll(getFinalDocumentTasks).ConfigureAwait(false);
        return documentIdToFinalText.SelectAsArray(kvp => (kvp.Key, kvp.Value));
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
            var finalText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            documentIdToFinalText.TryAdd(document.Id, finalText);
            return;
        }

        Debug.Assert(orderedDocuments.Length > 1);

        // More complex case.  We have multiple changes to the document.  Apply them in order
        // to get the final document.

        var oldDocument = oldSolution.GetRequiredDocument(orderedDocuments[0].document.Id);
        var merger = new TextChangeMerger(oldDocument);

        foreach (var (_, currentDocument) in orderedDocuments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Debug.Assert(currentDocument.Id == oldDocument.Id);

            await merger.TryMergeChangesAsync(currentDocument, cancellationToken).ConfigureAwait(false);
        }

        // WithChanges requires a ordered list of TextChanges without any overlap.
        var newText = await merger.GetFinalMergedTextAsync(cancellationToken).ConfigureAwait(false);
        documentIdToFinalText.TryAdd(oldDocument.Id, newText);
    }

    private static readonly Func<DocumentId, ConcurrentBag<(CodeAction, Document)>> s_getValue =
        _ => [];

    private static async Task GetChangedDocumentsAsync(
        Solution oldSolution,
        ConcurrentDictionary<DocumentId, ConcurrentBag<(CodeAction, Document)>> documentIdToChangedDocuments,
        CodeAction codeAction,
        IProgress<CodeAnalysisProgress> progressTracker,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var changedSolution = await codeAction.GetChangedSolutionInternalAsync(
            oldSolution, progressTracker, cancellationToken: cancellationToken).ConfigureAwait(false);
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
}
