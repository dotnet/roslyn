// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        /// <summary>
        /// Finds all references to a symbol throughout a solution
        /// </summary>
        /// <param name="symbol">The symbol to find references to.</param>
        /// <param name="solution">The solution to find references within.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        public static async Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(
            ISymbol symbol,
            Solution solution,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var progressCollector = new StreamingProgressCollector(StreamingFindReferencesProgress.Instance);
            await FindReferencesAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, progress: progressCollector,
                documents: null, cancellationToken: cancellationToken).ConfigureAwait(false);
            return progressCollector.GetReferencedSymbols();
        }

        /// <summary>
        /// Finds all references to a symbol throughout a solution
        /// </summary>
        /// <param name="symbol">The symbol to find references to.</param>
        /// <param name="solution">The solution to find references within.</param>
        /// <param name="documents">A set of documents to be searched. If documents is null, then that means "all documents".</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        public static Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(
            ISymbol symbol,
            Solution solution,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindReferencesAsync(symbol, solution, progress: null, documents: documents, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Finds all references to a symbol throughout a solution
        /// </summary>
        /// <param name="symbol">The symbol to find references to.</param>
        /// <param name="solution">The solution to find references within.</param>
        /// <param name="progress">An optional progress object that will receive progress
        /// information as the search is undertaken.</param>
        /// <param name="documents">An optional set of documents to be searched. If documents is null, then that means "all documents".</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        public static async Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(
            ISymbol symbol,
            Solution solution,
            IFindReferencesProgress progress,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            progress = progress ?? FindReferencesProgress.Instance;
            var streamingProgress = new StreamingProgressCollector(
                new StreamingFindReferencesProgressAdapter(progress));
            await FindReferencesAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, streamingProgress, documents, cancellationToken).ConfigureAwait(false);
            return streamingProgress.GetReferencedSymbols();
        }

        internal static async Task FindReferencesAsync(
            SymbolAndProjectId symbolAndProjectId,
            Solution solution,
            IStreamingFindReferencesProgress progress,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (Logger.LogBlock(FunctionId.FindReference, cancellationToken))
            {
                var finders = ReferenceFinders.DefaultReferenceFinders;
                progress = progress ?? StreamingFindReferencesProgress.Instance;
                var engine = new FindReferencesSearchEngine(
                    solution, documents, finders, progress, cancellationToken);
                await engine.FindReferencesAsync(symbolAndProjectId).ConfigureAwait(false);
            }
        }

        internal static async Task<ImmutableArray<ReferencedSymbol>> FindRenamableReferencesAsync(
            SymbolAndProjectId symbolAndProjectId,
            Solution solution,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference_Rename, cancellationToken))
            {
                IImmutableSet<Document> documents = null;
                var engine = new FindReferencesSearchEngine(
                    solution,
                    documents,
                    ReferenceFinders.DefaultRenameReferenceFinders,
                    StreamingFindReferencesProgress.Instance,
                    cancellationToken);

                return await engine.FindReferencesAsync(symbolAndProjectId).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Collects all the <see cref="ISymbol"/> definitions and <see cref="ReferenceLocation"/> 
        /// references that are reported independently and packages them up into the final list
        /// of <see cref="ReferencedSymbol" />.  This is used by the old non-streaming Find-References
        /// APIs to return all the results at the end of the operation, as opposed to broadcasting
        /// the results as they are found.
        /// </summary>
        private class StreamingProgressCollector : IStreamingFindReferencesProgress
        {
            private readonly object _gate = new object();
            private readonly IStreamingFindReferencesProgress _underlyingProgress;

            private readonly Dictionary<SymbolAndProjectId, List<ReferenceLocation>> _symbolToLocations =
                new Dictionary<SymbolAndProjectId, List<ReferenceLocation>>();

            public StreamingProgressCollector(
                IStreamingFindReferencesProgress underlyingProgress)
            {
                _underlyingProgress = underlyingProgress;
            }

            public IEnumerable<ReferencedSymbol> GetReferencedSymbols()
            {
                lock (_gate)
                {
                    var result = new List<ReferencedSymbol>();
                    foreach (var kvp in _symbolToLocations)
                    {
                        result.Add(new ReferencedSymbol(kvp.Key, kvp.Value.ToList()));
                    }

                    return result;
                }
            }

            public Task OnStartedAsync() => _underlyingProgress.OnStartedAsync();
            public Task OnCompletedAsync() => _underlyingProgress.OnCompletedAsync();
            public Task ReportProgressAsync(int current, int maximum) => _underlyingProgress.ReportProgressAsync(current, maximum);

            public Task OnFindInDocumentCompletedAsync(Document document) => _underlyingProgress.OnFindInDocumentCompletedAsync(document);
            public Task OnFindInDocumentStartedAsync(Document document) => _underlyingProgress.OnFindInDocumentStartedAsync(document);

            public Task OnDefinitionFoundAsync(SymbolAndProjectId definition)
            {
                lock (_gate)
                {
                    _symbolToLocations[definition] = new List<ReferenceLocation>();
                }

                return _underlyingProgress.OnDefinitionFoundAsync(definition);
            }

            public Task OnReferenceFoundAsync(SymbolAndProjectId definition, ReferenceLocation location)
            {
                lock (_gate)
                {
                    _symbolToLocations[definition].Add(location);
                }

                return _underlyingProgress.OnReferenceFoundAsync(definition, location);
            }
        }
    }
}