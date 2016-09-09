// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal interface ISymbolFinderEngineService : IWorkspaceService
    {
        Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution, 
            IStreamingFindReferencesProgress progress, 
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken);
    }

    [ExportWorkspaceService(typeof(ISymbolFinderEngineService)), Shared]
    internal class DefaultSymbolFinderEngineService : ISymbolFinderEngineService
    {
        public async Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution, 
            IStreamingFindReferencesProgress progress, IImmutableSet<Document> documents, 
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference, cancellationToken))
            {
                return await FindReferencesInCurrentProcessAsync(
                    symbolAndProjectId, solution, progress, 
                    documents, cancellationToken).ConfigureAwait(false);
            }
        }

        public static Task<IEnumerable<ReferencedSymbol>> FindReferencesInCurrentProcessAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution, 
            IStreamingFindReferencesProgress progress, IImmutableSet<Document> documents, 
            CancellationToken cancellationToken)
        {
            var finders = ReferenceFinders.DefaultReferenceFinders;
            progress = progress ?? StreamingFindReferencesProgress.Instance;
            var engine = new FindReferencesSearchEngine(
                solution, documents, finders, progress, cancellationToken);
            return engine.FindReferencesAsync(symbolAndProjectId);
        }
    }
}