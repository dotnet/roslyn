// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
{
    /// <summary>
    /// SearchScope is used to control where the <see cref="AbstractAddImportFeatureService{TSimpleNameSyntax}"/>
    /// searches.  We search different scopes in different ways.  For example we use 
    /// SymbolTreeInfos to search unreferenced projects and metadata dlls.  However,
    /// for the current project we're editing we defer to the compiler to do the 
    /// search.
    /// </summary>
    private abstract class SearchScope(
        AbstractAddImportFeatureService<TSimpleNameSyntax> provider, bool exact)
    {
        public readonly bool Exact = exact;
        protected readonly AbstractAddImportFeatureService<TSimpleNameSyntax> Provider = provider;

        protected abstract Task<ImmutableArray<ISymbol>> FindDeclarationsAsync(SymbolFilter filter, SearchQuery query, CancellationToken cancellationToken);

        public abstract SymbolReference CreateReference<T>(SymbolResult<T> symbol) where T : INamespaceOrTypeSymbol;

        public async Task<ImmutableArray<SymbolResult<ISymbol>>> FindDeclarationsAsync(
            string name, TSimpleNameSyntax nameNode, SymbolFilter filter, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (name != null && string.IsNullOrWhiteSpace(name))
                return [];

            if (Exact)
            {
                // Try finding exact matches first.  This provides better results for the common case of
                // people typing the right name, and it also allows for searching specialized indices that
                // contain those names quickly.
                {
                    using var query = SearchQuery.Create(name, ignoreCase: false);
                    var symbols = await FindDeclarationsAsync(filter, query, cancellationToken).ConfigureAwait(false);

                    if (symbols.Length > 0)
                        return symbols.SelectAsArray(static (s, nameNode) => SymbolResult.Create(s.Name, nameNode, s, weight: 0), nameNode);
                }

                // If no exact matches were found, fallback to a the weaker case insensitive search.  This
                // uses heuristics that can find some additional results, but with less accuracy (so not 
                // everything will necessarily be found).
                {
                    using var query = SearchQuery.Create(name, ignoreCase: true);
                    var symbols = await FindDeclarationsAsync(filter, query, cancellationToken).ConfigureAwait(false);

                    // Use a weight of '1' to indicate that these were case insensitive matches, and any other
                    // results from other search scoped should beat it.
                    return symbols.SelectAsArray(static (s, nameNode) => SymbolResult.Create(s.Name, nameNode, s, weight: 1), nameNode);
                }
            }
            else
            {
                using var query = SearchQuery.CreateFuzzy(name);
                var symbols = await FindDeclarationsAsync(filter, query, cancellationToken).ConfigureAwait(false);

                // TODO(cyrusn): It's a shame we have to compute this twice.  However, there's no
                // great way to store the original value we compute because it happens deep in the 
                // compiler bowels when we call FindDeclarations.
                using var similarityChecker = new WordSimilarityChecker(name, substringsAreSimilar: false);

                var results = new FixedSizeArrayBuilder<SymbolResult<ISymbol>>(symbols.Length);
                foreach (var symbol in symbols)
                {
                    var areSimilar = similarityChecker.AreSimilar(symbol.Name, out var matchCost);

                    Debug.Assert(areSimilar);
                    results.Add(SymbolResult.Create(symbol.Name, nameNode, symbol, matchCost));
                }

                return results.MoveToImmutable();
            }
        }
    }
}
