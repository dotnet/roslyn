// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Recommendations
{
    internal interface IRecommendationService : ILanguageService
    {
        RecommendedSymbols GetRecommendedSymbolsAtPosition(
            Workspace workspace,
            SemanticModel semanticModel,
            int position,
            OptionSet options,
            CancellationToken cancellationToken);
    }

    internal readonly struct RecommendedSymbols
    {
        private readonly ImmutableArray<ISymbol> _namedSymbols;
        private readonly ImmutableArray<ISymbol> _unnamedSymbols;

        /// <summary>
        /// The named symbols to recommend.
        /// </summary>
        public ImmutableArray<ISymbol> NamedSymbols => _namedSymbols.NullToEmpty();

        /// <summary>
        /// The unnamed symbols to recommend.  For example, operators, conversions and indexers.
        /// </summary>
        public ImmutableArray<ISymbol> UnnamedSymbols => _unnamedSymbols.NullToEmpty();

        ///// <summary>
        ///// The container the recommended symbols were found within if this we are recommending items after dotting into
        ///// something.
        ///// </summary>
        //public readonly INamespaceOrTypeSymbol? Container;

        ///// <summary>
        ///// Whether the container represents an instance of a type or the type itself.  For example
        ///// <c>Int32.TryParse</c> vs <c>0.ToString()</c>.  In both cases the container will be <see
        ///// cref="System.Int32"/>. In the former case the container will not be an instance, but in the latter case it
        ///// will be.
        ///// </summary>
        //public readonly bool IsInstance;

        public RecommendedSymbols(ImmutableArray<ISymbol> namedSymbols)
            : this(namedSymbols, default/*, null, false*/)
        {
        }

        public RecommendedSymbols(
            ImmutableArray<ISymbol> namedSymbols,
            ImmutableArray<ISymbol> unnamedSymbols = default)
            //,
            //INamespaceOrTypeSymbol? container,
            //bool isInstance)
        {
            _namedSymbols = namedSymbols;
            _unnamedSymbols = unnamedSymbols;
            //Container = container;
            //IsInstance = isInstance;
        }

        public RecommendedSymbols WithNamedSymbols(ImmutableArray<ISymbol> namedSymbols)
            => new(namedSymbols, UnnamedSymbols/*, Container, IsInstance*/);
    }
}
