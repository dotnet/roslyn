// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        /// <summary>
        /// SearchScope is used to control where the <see cref="AbstractAddImportFeatureService{TSimpleNameSyntax}"/>
        /// searches.  We search different scopes in different ways.  For example we use 
        /// SymbolTreeInfos to search unreferenced projects and metadata dlls.  However,
        /// for the current project we're editing we defer to the compiler to do the 
        /// search.
        /// </summary>
        private abstract class SearchScope
        {
            public readonly bool Exact;
            protected readonly AbstractAddImportFeatureService<TSimpleNameSyntax> provider;

            protected SearchScope(AbstractAddImportFeatureService<TSimpleNameSyntax> provider, bool exact)
            {
                this.provider = provider;
                Exact = exact;
            }

            protected abstract Task<ImmutableArray<ISymbol>> FindDeclarationsAsync(SymbolFilter filter, SearchQuery query, CancellationToken cancellationToken);

            public abstract SymbolReference CreateReference<T>(SymbolResult<T> symbol) where T : INamespaceOrTypeSymbol;

            public async Task<ImmutableArray<SymbolResult<ISymbol>>> FindDeclarationsAsync(
                string name, TSimpleNameSyntax nameNode, SymbolFilter filter, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (name != null && string.IsNullOrWhiteSpace(name))
                {
                    return ImmutableArray<SymbolResult<ISymbol>>.Empty;
                }

                using var query = Exact ? SearchQuery.Create(name, ignoreCase: true) : SearchQuery.CreateFuzzy(name);
                var symbols = await FindDeclarationsAsync(filter, query, cancellationToken).ConfigureAwait(false);

                if (Exact)
                {
                    // We did an exact, case insensitive, search.  Case sensitive matches should
                    // be preferred though over insensitive ones.
                    return symbols.SelectAsArray(s =>
                        SymbolResult.Create(s.Name, nameNode, s, weight: s.Name == name ? 0 : 1));
                }

                // TODO(cyrusn): It's a shame we have to compute this twice.  However, there's no
                // great way to store the original value we compute because it happens deep in the 
                // compiler bowels when we call FindDeclarations.
                var similarityChecker = WordSimilarityChecker.Allocate(name, substringsAreSimilar: false);

                var result = symbols.SelectAsArray(s =>
                {
                    var areSimilar = similarityChecker.AreSimilar(s.Name, out var matchCost);

                    Debug.Assert(areSimilar);
                    return SymbolResult.Create(s.Name, nameNode, s, matchCost);
                });

                similarityChecker.Free();

                return result;
            }
        }
    }
}
