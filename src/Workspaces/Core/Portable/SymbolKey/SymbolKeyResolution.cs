// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal struct SymbolKeyResolution : IEquatable<SymbolKeyResolution>
    {
        private readonly ImmutableArray<ISymbol> _candidateSymbols;

        /// <summary>
        /// The symbol that was resolved from the <see cref="SymbolKey"/>, if any. Returns null
        /// if the <see cref="SymbolKey"/> could not be successfully resolved to a single symbol.
        /// If null is returned, it may still be that case that we have one or more "best guesses"
        /// as to what symbol was intended. These best guesses are available via the
        /// <see cref="CandidateSymbols"/> property.
        /// </summary>
        public ISymbol Symbol { get; }

        /// <summary>
        /// If the <see cref="SymbolKey"/> did not successfully resolve to a symbol, but there were
        /// one or more symbols that may have been considered but discarded, this property returns those
        /// symbols. The reason that the symbols did not successfully resolve to a symbol are
        /// available in the CandidateReason property. For example, if the symbol was ambiguous.
        /// </summary>
        public ImmutableArray<ISymbol> CandidateSymbols => _candidateSymbols.NullToEmpty();

        ///<summary>
        /// If the <see cref="SymbolKey"/> did not successfully resolve to a symbol, but there were one or more
        /// symbols that may have been considered but discarded, this property describes why those
        /// symbol or symbols were not considered suitable.
        /// </summary>
        public CandidateReason CandidateReason { get; }

        internal SymbolKeyResolution(ISymbol symbol)
            : this(symbol, ImmutableArray<ISymbol>.Empty, CandidateReason.None)
        {
        }

        internal SymbolKeyResolution(ImmutableArray<ISymbol> candidateSymbols, CandidateReason candidateReason)
            : this(symbol: null, candidateSymbols, candidateReason)
        {
        }

        private SymbolKeyResolution(ISymbol symbol, ImmutableArray<ISymbol> candidateSymbols, CandidateReason candidateReason)
            : this()
        {
            Symbol = symbol;
            _candidateSymbols = candidateSymbols;
            CandidateReason = candidateReason;
        }

        internal ImmutableArray<ISymbol> GetAllSymbols()
            => Symbol != null
                ? ImmutableArray.Create(this.Symbol)
                : _candidateSymbols.NullToEmpty();

        internal static SymbolKeyResolution Create(IEnumerable<ISymbol> symbols)
        {
            if (symbols == null)
            {
                return default;
            }

            var symbolArray = symbols.WhereNotNull().ToArray();

            if (symbolArray.Length == 0)
            {
                return default;
            }

            return symbolArray.Length == 1
                ? new SymbolKeyResolution(symbolArray[0])
                : new SymbolKeyResolution(ImmutableArray.Create(symbolArray), CandidateReason.Ambiguous);
        }

        internal static SymbolKeyResolution Create(ImmutableArray<ISymbol> symbols)
        {
            if (symbols.IsDefaultOrEmpty)
            {
                return default;
            }

            return symbols.Length == 1
                ? new SymbolKeyResolution(symbols[0])
                : new SymbolKeyResolution(symbols, CandidateReason.Ambiguous);
        }

        public override bool Equals(object obj)
            => obj is SymbolKeyResolution && Equals((SymbolKeyResolution)obj);

        public bool Equals(SymbolKeyResolution other)
            => object.Equals(this.Symbol, other.Symbol)
                && ((_candidateSymbols.IsDefault && other._candidateSymbols.IsDefault) || _candidateSymbols.SequenceEqual(other._candidateSymbols))
                && this.CandidateReason == other.CandidateReason;

        public override int GetHashCode()
            => Hash.Combine(this.Symbol, Hash.Combine(Hash.CombineValues(_candidateSymbols, 4), (int)this.CandidateReason));

        internal bool IsEmpty
            => this.Symbol == null && this._candidateSymbols.IsDefaultOrEmpty;
    }
}
