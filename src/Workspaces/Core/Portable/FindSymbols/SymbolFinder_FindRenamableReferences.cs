// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            ISymbol symbol,
            Solution solution,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference_Rename, cancellationToken))
            {
                var streamingProgress = new StreamingProgressCollector();

                IImmutableSet<Document> documents = null;
                var engine = new FindReferencesSearchEngine(
                    solution,
                    documents,
                    ReferenceFinders.DefaultRenameReferenceFinders,
                    streamingProgress,
                    FindReferencesSearchOptions.Default,
                    cancellationToken);

                await engine.FindReferencesAsync(symbol).ConfigureAwait(false);
                return streamingProgress.GetReferencedSymbols();
            }
        }
    }
}
