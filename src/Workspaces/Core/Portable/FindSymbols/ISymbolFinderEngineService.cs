// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
            ISymbol symbol, Solution solution, IFindReferencesProgress progress,
            IImmutableSet<Document> documents, CancellationToken cancellationToken);
    }

    [ExportWorkspaceService(typeof(ISymbolFinderEngineService)), Shared]
    internal class DefaultSymbolFinderEngineService : ISymbolFinderEngineService
    {
        public async Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(ISymbol symbol, Solution solution, IFindReferencesProgress progress, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference, cancellationToken))
            {
                var finders = ReferenceFinders.DefaultReferenceFinders;
                progress = progress ?? FindReferencesProgress.Instance;
                var engine = new FindReferencesSearchEngine(solution, documents, finders, progress, cancellationToken);
                return await engine.FindReferencesAsync(symbol).ConfigureAwait(false);
            }
        }
    }
}