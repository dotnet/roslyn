// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Recommendations
{
    internal interface IRecommendationService : ILanguageService
    {
        RecommendedSymbols GetRecommendedSymbolsInContext(
            SyntaxContext syntaxContext,
            RecommendationServiceOptions options,
            CancellationToken cancellationToken);
    }

    internal readonly struct RecommendedSymbols(
        ImmutableArray<ISymbol> namedSymbols,
        ImmutableArray<ISymbol> unnamedSymbols = default)
    {

        /// <summary>
        /// The named symbols to recommend.
        /// </summary>
        public ImmutableArray<ISymbol> NamedSymbols => namedSymbols.NullToEmpty();

        /// <summary>
        /// The unnamed symbols to recommend.  For example, operators, conversions and indexers.
        /// </summary>
        public ImmutableArray<ISymbol> UnnamedSymbols => unnamedSymbols.NullToEmpty();

        public RecommendedSymbols(ImmutableArray<ISymbol> namedSymbols)
            : this(namedSymbols, default)
        {
        }
    }
}
