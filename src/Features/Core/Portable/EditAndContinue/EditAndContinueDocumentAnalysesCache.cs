// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
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
        private readonly Dictionary<DocumentId, (Document document, AsyncLazy<DocumentAnalysisResults> results)> _analyses = new();
        private readonly AsyncLazy<ActiveStatementsMap> _baseActiveStatements;

        public EditAndContinueDocumentAnalysesCache(AsyncLazy<ActiveStatementsMap> baseActiveStatements)
        {
            _baseActiveStatements = baseActiveStatements;
        }

        public AsyncLazy<DocumentAnalysisResults> GetDocumentAnalysis(Document? baseDocument, Document document, ImmutableArray<TextSpan> activeStatementSpans)
        {
            lock (_guard)
            {
                return GetDocumentAnalysisNoLock(baseDocument, document, activeStatementSpans);
            }
        }

        public ImmutableArray<(Document, AsyncLazy<DocumentAnalysisResults>)> GetDocumentAnalyses(ArrayBuilder<(Document? oldDocument, Document newDocument, ImmutableArray<TextSpan> newActiveStatementSpans)> builder)
        {
            if (builder.IsEmpty())
            {
                return ImmutableArray<(Document, AsyncLazy<DocumentAnalysisResults>)>.Empty;
            }

            lock (_guard)
            {
                return builder.SelectAsArray(change => (change.newDocument, GetDocumentAnalysisNoLock(change.oldDocument, change.newDocument, change.newActiveStatementSpans)));
            }
        }

        /// <summary>
        /// Returns a document analysis or kicks off a new one if one is not available for the specified document snapshot.
        /// </summary>
        /// <param name="baseDocument">Base document or null if the document did not exist in the baseline.</param>
        /// <param name="document">Document snapshot to analyze.</param>
        private AsyncLazy<DocumentAnalysisResults> GetDocumentAnalysisNoLock(Document? baseDocument, Document document, ImmutableArray<TextSpan> activeStatementSpans)
        {
            if (_analyses.TryGetValue(document.Id, out var analysis) && analysis.document == document)
            {
                return analysis.results;
            }

            var analyzer = document.Project.LanguageServices.GetRequiredService<IEditAndContinueAnalyzer>();

            var lazyResults = new AsyncLazy<DocumentAnalysisResults>(
                asynchronousComputeFunction: async cancellationToken =>
                {
                    try
                    {
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

            // TODO: this will replace potentially running analysis with another one.
            // Consider cancelling the replaced one.
            _analyses[document.Id] = (document, lazyResults);
            return lazyResults;
        }
    }
}
