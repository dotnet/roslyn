// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Calculates and caches results of changed documents analysis. 
    /// The work is triggered by an incremental analyzer on idle or explicitly when "continue" operation is executed.
    /// Contains analyses of the latest observed document versions.
    /// </summary>
    internal sealed class EditAndContinueDocumentAnalysesCache
    {
        private readonly object _guard = new();
        private readonly Dictionary<DocumentId, (AsyncLazy<DocumentAnalysisResults> results, Document document)> _analyses = new();
        private readonly AsyncLazy<ActiveStatementsMap> _baseActiveStatements;

        public EditAndContinueDocumentAnalysesCache(AsyncLazy<ActiveStatementsMap> baseActiveStatements)
        {
            _baseActiveStatements = baseActiveStatements;
        }

        public async ValueTask<ImmutableArray<ActiveStatement>> GetActiveStatementsAsync(Document? baseDocument, Document document, ImmutableArray<TextSpan> activeStatementSpans, CancellationToken cancellationToken)
        {
            try
            {
                var results = await GetDocumentAnalysisAsync(baseDocument, document, activeStatementSpans, cancellationToken).ConfigureAwait(false);
                return results.ActiveStatements;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public async ValueTask<ImmutableArray<DocumentAnalysisResults>> GetDocumentAnalysesAsync(IReadOnlyList<(Document? oldDocument, Document newDocument, ImmutableArray<TextSpan> newActiveStatementSpans)> documentInfos, CancellationToken cancellationToken)
        {
            try
            {
                if (documentInfos.IsEmpty())
                {
                    return ImmutableArray<DocumentAnalysisResults>.Empty;
                }

                var tasks = documentInfos.Select(info => Task.Run(() => GetDocumentAnalysisAsync(info.oldDocument, info.newDocument, info.newActiveStatementSpans, cancellationToken).AsTask(), cancellationToken));
                var allResults = await Task.WhenAll(tasks).ConfigureAwait(false);

                return allResults.ToImmutableArray();
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Returns a document analysis or kicks off a new one if one is not available for the specified document snapshot.
        /// </summary>
        /// <param name="baseDocument">Base document or null if the document did not exist in the baseline.</param>
        /// <param name="document">Document snapshot to analyze.</param>
        public async ValueTask<DocumentAnalysisResults> GetDocumentAnalysisAsync(Document? baseDocument, Document document, ImmutableArray<TextSpan> activeStatementSpans, CancellationToken cancellationToken)
        {
            try
            {
                AsyncLazy<DocumentAnalysisResults> lazyResults;

                lock (_guard)
                {
                    lazyResults = GetDocumentAnalysisNoLock(baseDocument, document, activeStatementSpans);
                }

                return (await lazyResults.GetValueAsync(cancellationToken).ConfigureAwait(false));
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private AsyncLazy<DocumentAnalysisResults> GetDocumentAnalysisNoLock(Document? baseDocument, Document document, ImmutableArray<TextSpan> activeStatementSpans)
        {
            // Do not reuse an analysis of the document unless its snasphot is exactly the same as was used to calculate the results.
            // Note that comparing document snapshots in effect compares the entire solution snapshots (when another document is changed a new solution snapshot is created
            // that creates new document snapshots for all queried documents).
            // 
            // It would be possible to reuse analysis results of documents whose content does not change in between two solution snapshots.
            // However, we'd need rather sophisticated caching logic. The smantic analysis gathers information from other documents when
            // calculating results for a specific document. In some cases it's easy to record the set of documents the analysis depends on.
            // For example, when analyzing a partial class we can record all documents its declaration spans. However, in other cases the analysis
            // checks for absence of a top-level type symbol. Adding a symbol to any document thus invalidates such analysis. It'd be possible
            // to keep track of which type symbols an analysis is conditional upon, if it was worth the extra complexity.
            if (_analyses.TryGetValue(document.Id, out var analysis) && analysis.document == document)
            {
                return analysis.results;
            }

            var lazyResults = new AsyncLazy<DocumentAnalysisResults>(
                asynchronousComputeFunction: async cancellationToken =>
                {
                    try
                    {
                        var analyzer = document.Project.LanguageServices.GetRequiredService<IEditAndContinueAnalyzer>();

                        var baseActiveStatements = await _baseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                        if (!baseActiveStatements.DocumentMap.TryGetValue(document.Id, out var documentBaseActiveStatements))
                        {
                            documentBaseActiveStatements = ImmutableArray<ActiveStatement>.Empty;
                        }

                        return await analyzer.AnalyzeDocumentAsync(baseDocument, documentBaseActiveStatements, document, activeStatementSpans, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                },
                cacheResult: true);

            _analyses[document.Id] = (lazyResults, document);

            return lazyResults;
        }
    }
}
