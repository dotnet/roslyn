// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    // This file contains the legacy FindReferences APIs.  The APIs are legacy because they
    // do not contain enough information for us to effectively remote them over to the OOP
    // process to do the work.  Specifically, they lack the "current project context" necessary
    // to be able to effectively serialize symbols to/from the remote process.

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
            CancellationToken cancellationToken = default)
        {
            var progressCollector = new StreamingProgressCollector(StreamingFindReferencesProgress.Instance);
            await FindReferencesAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, progress: progressCollector, documents: null,
                options: FindReferencesSearchOptions.Default,
                cancellationToken: cancellationToken).ConfigureAwait(false);
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
            CancellationToken cancellationToken = default)
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
        public static Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(
            ISymbol symbol,
            Solution solution,
            IFindReferencesProgress progress,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken = default)
        {
            return FindReferencesAsync(
                symbol, solution, progress, documents,
                FindReferencesSearchOptions.Default, cancellationToken);
        }

        private static async Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(
            ISymbol symbol,
            Solution solution,
            IFindReferencesProgress progress,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            progress ??= FindReferencesProgress.Instance;
            var streamingProgress = new StreamingProgressCollector(
                new StreamingFindReferencesProgressAdapter(progress));
            await FindReferencesAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, streamingProgress, documents,
                options, cancellationToken).ConfigureAwait(false);
            return streamingProgress.GetReferencedSymbols();
        }

        internal static class TestAccessor
        {
            internal static Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(
                ISymbol symbol,
                Solution solution,
                IFindReferencesProgress progress,
                IImmutableSet<Document> documents,
                FindReferencesSearchOptions options,
                CancellationToken cancellationToken)
            {
                return SymbolFinder.FindReferencesAsync(symbol, solution, progress, documents, options, cancellationToken);
            }
        }
    }
}
