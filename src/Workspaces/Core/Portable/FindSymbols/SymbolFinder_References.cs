// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
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
        public static Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(
            ISymbol symbol,
            Solution solution,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindReferencesAsync(symbol, solution, progress: null, documents: null, cancellationToken: cancellationToken);
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
            using (Logger.LogBlock(FunctionId.FindReference, cancellationToken))
            {
                var finders = ReferenceFinders.DefaultReferenceFinders;
                progress = progress ?? FindReferencesProgress.Instance;
                var engine = new FindReferencesSearchEngine(solution, documents, finders, progress, cancellationToken);
                return await engine.FindReferencesAsync(symbol).ConfigureAwait(false);
            }
        }

        internal static Task<IEnumerable<ReferencedSymbol>> FindRenamableReferencesAsync(
            ISymbol symbol,
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
                    FindReferencesProgress.Instance,
                    cancellationToken);

                return engine.FindReferencesAsync(symbol);
            }
        }
    }
}
