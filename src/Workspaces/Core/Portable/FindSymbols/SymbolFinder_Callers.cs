﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;

#if !NETCOREAPP
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        /// <summary>
        /// Finds all the callers of a specified symbol.
        /// </summary>
        public static Task<IEnumerable<SymbolCallerInfo>> FindCallersAsync(
            ISymbol symbol, Solution solution, CancellationToken cancellationToken = default)
        {
            return FindCallersAsync(symbol, solution, documents: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Finds all the callers of a specified symbol.
        /// </summary>
        public static async Task<IEnumerable<SymbolCallerInfo>> FindCallersAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Document> documents, CancellationToken cancellationToken = default)
        {
            symbol = symbol.OriginalDefinition;
            var foundSymbol = await FindSourceDefinitionAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
            symbol = foundSymbol ?? symbol;

            var references = await FindCallReferencesAsync(solution, symbol, documents, cancellationToken).ConfigureAwait(false);

            var directReference = references.Where(
                r => SymbolEquivalenceComparer.Instance.Equals(symbol, r.Definition)).FirstOrDefault();

            var indirectReferences = references.WhereAsArray(r => r != directReference);

            var results = new List<SymbolCallerInfo>();

            if (directReference != null)
            {
                await AddReferencingSymbols(directReference, isDirect: true).ConfigureAwait(false);
            }

            foreach (var indirectReference in indirectReferences)
            {
                await AddReferencingSymbols(indirectReference, isDirect: false).ConfigureAwait(false);
            }

            return results;

            async Task AddReferencingSymbols(ReferencedSymbol reference, bool isDirect)
            {
                var result = await reference.Locations.FindReferencingSymbolsAsync(cancellationToken).ConfigureAwait(false);
                foreach (var (callingSymbol, locations) in result)
                {
                    results.Add(new SymbolCallerInfo(callingSymbol, reference.Definition, locations, isDirect));
                }
            }
        }

        private static async Task<ImmutableArray<ReferencedSymbol>> FindCallReferencesAsync(
            Solution solution,
            ISymbol symbol,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken = default)
        {
            if (symbol != null)
            {
                if (symbol.Kind == SymbolKind.Event ||
                    symbol.Kind == SymbolKind.Method ||
                    symbol.Kind == SymbolKind.Property)
                {
                    var collector = new StreamingProgressCollector();
                    await FindReferencesAsync(
                        symbol, solution, collector, documents,
                        FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);
                    return collector.GetReferencedSymbols();
                }
            }

            return ImmutableArray<ReferencedSymbol>.Empty;
        }
    }
}
