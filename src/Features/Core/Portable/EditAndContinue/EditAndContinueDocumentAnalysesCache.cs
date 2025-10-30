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
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Calculates and caches results of changed documents analysis. 
/// The work is triggered by an incremental analyzer on idle or explicitly when "continue" operation is executed.
/// Contains analyses of the latest observed document versions.
/// </summary>
internal sealed class EditAndContinueDocumentAnalysesCache(AsyncLazy<ActiveStatementsMap> baseActiveStatements, AsyncLazy<EditAndContinueCapabilities> capabilities, TraceLog log)
{
    private readonly object _guard = new();
    private readonly Dictionary<DocumentId, (AsyncLazy<DocumentAnalysisResults> results, Project oldProject, Document? newDocument, ImmutableArray<ActiveStatementLineSpan> activeStatementSpans)> _analyses = [];
    private readonly AsyncLazy<ActiveStatementsMap> _baseActiveStatements = baseActiveStatements;
    private readonly AsyncLazy<EditAndContinueCapabilities> _capabilities = capabilities;
    private readonly TraceLog _log = log;

    public async ValueTask<ImmutableArray<DocumentAnalysisResults>> GetDocumentAnalysesAsync(
        CommittedSolution oldSolution,
        Solution newSolution,
        IReadOnlyList<(Document? oldDocument, Document? newDocument)> documents,
        ActiveStatementSpanProvider activeStatementSpanProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            if (documents.IsEmpty())
            {
                return [];
            }

            var tasks = documents.Select(document => Task.Run(() => GetDocumentAnalysisAsync(oldSolution, newSolution, document.oldDocument, document.newDocument, activeStatementSpanProvider, cancellationToken).AsTask(), cancellationToken));
            var allResults = await Task.WhenAll(tasks).ConfigureAwait(false);

            return [.. allResults];
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    /// <summary>
    /// Returns a document analysis or kicks off a new one if one is not available for the specified document snapshot.
    /// </summary>
    /// <param name="oldSolution">Committed solution.</param>
    /// <param name="newDocument">Document snapshot to analyze.</param>
    /// <param name="activeStatementSpanProvider">Provider of active statement spans tracked by the editor for the solution snapshot of the <paramref name="newDocument"/>.</param>
    public async ValueTask<DocumentAnalysisResults> GetDocumentAnalysisAsync(
        CommittedSolution oldSolution,
        Solution newSolution,
        Document? oldDocument,
        Document? newDocument,
        ActiveStatementSpanProvider activeStatementSpanProvider,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(oldDocument != null || newDocument != null);

        try
        {
            var unmappedActiveStatementSpans = await GetLatestUnmappedActiveStatementSpansAsync(oldDocument, newDocument, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);

            // The base project may have been updated as documents were brought up-to-date in the committed solution.
            // Get the latest available snapshot of the base project from the committed solution and use it for analyses of all documents,
            // so that we use a single compilation for the base project (for efficiency).
            // Note that some other request might be updating documents in the committed solution that were not changed (not in changedOrAddedDocuments)
            // but are not up-to-date. These documents do not have impact on the analysis unless we read semantic information
            // from the project compilation. When reading such information we need to be aware of its potential incompleteness
            // and consult the compiler output binary (see https://github.com/dotnet/roslyn/issues/51261).
            var oldProject = oldDocument?.Project ?? oldSolution.GetRequiredProject(newDocument!.Project.Id);
            var newProject = newDocument?.Project ?? newSolution.GetRequiredProject(oldProject.Id);

            AsyncLazy<DocumentAnalysisResults> lazyResults;

            lock (_guard)
            {
                lazyResults = GetDocumentAnalysisNoLock(oldProject, newProject, oldDocument, newDocument, unmappedActiveStatementSpans);
            }

            return await lazyResults.GetValueAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    /// <summary>
    /// Calculates unmapped active statement spans in the <paramref name="newDocument"/> from spans provided by <paramref name="newActiveStatementSpanProvider"/>.
    /// </summary>
    private async Task<ImmutableArray<ActiveStatementLineSpan>> GetLatestUnmappedActiveStatementSpansAsync(Document? oldDocument, Document? newDocument, ActiveStatementSpanProvider newActiveStatementSpanProvider, CancellationToken cancellationToken)
    {
        if (newDocument == null)
        {
            // document has been deleted - all active statements have been deleted (rude edits will be reported)
            return [];
        }

        if (oldDocument == null)
        {
            // document has been added - it won't have active statements
            return [];
        }

        if (oldDocument.FilePath == null || newDocument.FilePath == null)
        {
            // document doesn't have a file path - we do not have tracking spans for it
            return [];
        }

        var newTree = await newDocument.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var newLineMappings = newTree.GetLineMappings(cancellationToken);

        // No #line directives -- retrieve the current location of tracking spans directly for this document:
        if (!newLineMappings.Any())
        {
            var newMappedDocumentSpans = await newActiveStatementSpanProvider(newDocument.Id, newDocument.FilePath, cancellationToken).ConfigureAwait(false);
            return newMappedDocumentSpans.SelectAsArray(static s => new ActiveStatementLineSpan(s.Id, s.LineSpan));
        }

        // The document has #line directives. In order to determine all active statement spans in the document
        // we need to find all documents that #line directives in this document map to.
        // We retrieve the tracking spans for all such documents and then map them back to this document.

        using var _1 = PooledDictionary<string, ImmutableArray<ActiveStatementSpan>>.GetInstance(out var mappedSpansByDocumentPath);
        using var _2 = ArrayBuilder<ActiveStatementLineSpan>.GetInstance(out var activeStatementSpansBuilder);

        var baseActiveStatements = await _baseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
        var analyzer = newDocument.Project.Services.GetRequiredService<IEditAndContinueAnalyzer>();
        var oldActiveStatements = await baseActiveStatements.GetOldActiveStatementsAsync(analyzer, oldDocument, cancellationToken).ConfigureAwait(false);

        foreach (var oldActiveStatement in oldActiveStatements)
        {
            var mappedFilePath = oldActiveStatement.Statement.FileSpan.Path;
            if (!mappedSpansByDocumentPath.TryGetValue(mappedFilePath, out var newMappedDocumentSpans))
            {
                newMappedDocumentSpans = await newActiveStatementSpanProvider((newDocument.FilePath == mappedFilePath) ? newDocument.Id : null, mappedFilePath, cancellationToken).ConfigureAwait(false);
                mappedSpansByDocumentPath.Add(mappedFilePath, newMappedDocumentSpans);
            }

            // Spans not tracked in the document (e.g. the document has been closed):
            if (newMappedDocumentSpans.IsEmpty)
            {
                continue;
            }

            // all baseline spans are being tracked in their corresponding mapped documents (if a span is deleted it's still tracked as empty):
            var newMappedDocumentActiveSpan = newMappedDocumentSpans.Single(static (s, id) => s.Id == id, oldActiveStatement.Statement.Id);
            Debug.Assert(newMappedDocumentActiveSpan.UnmappedDocumentId == null || newMappedDocumentActiveSpan.UnmappedDocumentId == newDocument.Id);

            // TODO: optimize
            var newLineMappingContainingActiveSpan = newLineMappings.FirstOrDefault(mapping => mapping.MappedSpan.Span.Contains(newMappedDocumentActiveSpan.LineSpan));

            var unmappedSpan = newLineMappingContainingActiveSpan.MappedSpan.IsValid ? newLineMappingContainingActiveSpan.Span : default;
            activeStatementSpansBuilder.Add(new ActiveStatementLineSpan(newMappedDocumentActiveSpan.Id, unmappedSpan));
        }

        return activeStatementSpansBuilder.ToImmutableAndClear();
    }

    private AsyncLazy<DocumentAnalysisResults> GetDocumentAnalysisNoLock(Project oldProject, Project newProject, Document? oldDocument, Document? newDocument, ImmutableArray<ActiveStatementLineSpan> activeStatementSpans)
    {
        Debug.Assert(oldDocument == null || oldDocument.Project == oldProject);
        Debug.Assert(newDocument == null || newDocument.Project == newProject);

        var documentId = oldDocument?.Id ?? newDocument!.Id;

        // Do not reuse an analysis of the document unless its snasphot is exactly the same as was used to calculate the results.
        // Note that comparing document snapshots in effect compares the entire solution snapshots (when another document is changed a new solution snapshot is created
        // that creates new document snapshots for all queried documents).
        // Also check the base project snapshot since the analysis uses semantic information from the base project as well.
        // 
        // It would be possible to reuse analysis results of documents whose content does not change in between two solution snapshots.
        // However, we'd need rather sophisticated caching logic. The semantic analysis gathers information from other documents when
        // calculating results for a specific document. In some cases it's easy to record the set of documents the analysis depends on.
        // For example, when analyzing a partial class we can record all documents its declaration spans. However, in other cases the analysis
        // checks for absence of a top-level type symbol. Adding a symbol to any document thus invalidates such analysis. It'd be possible
        // to keep track of which type symbols an analysis is conditional upon, if it was worth the extra complexity.
        if (_analyses.TryGetValue(documentId, out var analysis) &&
            analysis.oldProject == oldProject &&
            analysis.newDocument == newDocument &&
            analysis.activeStatementSpans.SequenceEqual(activeStatementSpans))
        {
            return analysis.results;
        }

        var lazyResults = AsyncLazy.Create(
            static async (arg, cancellationToken) =>
            {
                try
                {
                    var analyzer = arg.oldProject.Services.GetRequiredService<IEditAndContinueAnalyzer>();
                    return await analyzer.AnalyzeDocumentAsync(
                        arg.documentId,
                        arg.oldProject,
                        arg.newProject,
                        arg.self._baseActiveStatements,
                        arg.activeStatementSpans,
                        arg.self._capabilities,
                        arg.self._log,
                        cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            },
            arg: (self: this, documentId, newDocument, oldProject, newProject, activeStatementSpans));

        // Previous results for this document id are discarded as they are no longer relevant.
        // The only relevant analysis is for the latest base and document snapshots.
        // Note that the base snapshot may evolve if documents are dicovered that were previously
        // out-of-sync with the compiled outputs and are now up-to-date.
        _analyses[documentId] = (lazyResults, oldProject, newDocument, activeStatementSpans);

        return lazyResults;
    }
}
