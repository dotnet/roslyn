// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        internal static async Task<ImmutableArray<ReferencedSymbol>> FindRenamableReferencesAsync(
            SymbolAndProjectId symbolAndProjectId,
            Solution solution,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference_Rename, cancellationToken))
            {
                var streamingProgress = new StreamingProgressCollector(
                    StreamingFindReferencesProgress.Instance);

                IImmutableSet<Document> documents = null;
                var engine = new FindReferencesSearchEngine(
                    solution,
                    documents,
                    ReferenceFinders.DefaultRenameReferenceFinders,
                    streamingProgress,
                    FindReferencesSearchOptions.Default.WithIgnoreUnchangeableDocuments(true),
                    cancellationToken);

                await engine.FindReferencesAsync(symbolAndProjectId).ConfigureAwait(false);
                return streamingProgress.GetReferencedSymbols();
            }
        }
    }
}
