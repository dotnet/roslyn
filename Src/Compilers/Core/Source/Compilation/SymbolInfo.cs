// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    public struct SymbolInfo : IEquatable<SymbolInfo>
    {
        internal static readonly SymbolInfo None = new SymbolInfo(null, ImmutableArray<ISymbol>.Empty, CandidateReason.None);

        private ImmutableArray<ISymbol> candidateSymbols;

        /// <summary>
        /// The symbol that was referred to by the syntax node, if any. Returns null if the given
        /// expression did not bind successfully to a single symbol. If null is returned, it may
        /// still be that case that we have one or more "best guesses" as to what symbol was
        /// intended. These best guesses are available via the CandidateSymbols property.
        /// </summary>
        public ISymbol Symbol { get; private set; }

        /// <summary>
        /// If the expression did not successfully resolve to a symbol, but there were one or more
        /// symbols that may have been considered but discarded, this property returns those
        /// symbols. The reason that the symbols did not successfully resolve to a symbol are
        /// available in the CandidateReason property. For example, if the symbol was inaccessible,
        /// ambiguous, or used in the wrong context.
        /// </summary>
        public ImmutableArray<ISymbol> CandidateSymbols
        {
            get
            {
                return this.candidateSymbols.NullToEmpty();
            }
        }

        internal ImmutableArray<ISymbol> GetAllSymbols()
        {
            if (this.Symbol != null)
            {
                return ImmutableArray.Create<ISymbol>(this.Symbol);
            }
            else
            {
                return this.candidateSymbols;
            }
        }

        ///<summary>
        /// If the expression did not successfully resolve to a symbol, but there were one or more
        /// symbols that may have been considered but discarded, this property describes why those
        /// symbol or symbols were not considered suitable.
        /// </summary>
        public CandidateReason CandidateReason { get; private set; }

        internal SymbolInfo(ISymbol symbol)
            : this(symbol, ImmutableArray<ISymbol>.Empty, CandidateReason.None)
        {
        }

        internal SymbolInfo(ISymbol symbol, CandidateReason reason)
            : this(symbol, ImmutableArray<ISymbol>.Empty, reason)
        {
        }

        internal SymbolInfo(ImmutableArray<ISymbol> candidateSymbols, CandidateReason candidateReason)
            : this(null, candidateSymbols, candidateReason)
        {
        }

        internal SymbolInfo(ISymbol symbol, ImmutableArray<ISymbol> candidateSymbols, CandidateReason candidateReason)
            : this()
        {
            this.Symbol = symbol;
            this.candidateSymbols = candidateSymbols.IsDefault ? ImmutableArray.Create<ISymbol>() : candidateSymbols;

#if DEBUG
            const NamespaceKind NamespaceKindNamespaceGroup = (NamespaceKind)0;
            Debug.Assert((object)symbol == null || symbol.Kind != SymbolKind.Namespace || ((INamespaceSymbol)symbol).NamespaceKind != NamespaceKindNamespaceGroup);
            foreach (var item in this.candidateSymbols)
            {
                Debug.Assert(item.Kind != SymbolKind.Namespace || ((INamespaceSymbol)item).NamespaceKind != NamespaceKindNamespaceGroup);
            }
#endif

            this.CandidateReason = candidateReason;
        }

        public override bool Equals(object obj)
        {
            return obj is SymbolInfo && Equals((SymbolInfo)obj);
        }

        public bool Equals(SymbolInfo other)
        {
            return object.Equals(this.Symbol, other.Symbol)
                && ((this.candidateSymbols.IsDefault && other.candidateSymbols.IsDefault) || this.candidateSymbols.SequenceEqual(other.candidateSymbols))
                && this.CandidateReason == other.CandidateReason;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Symbol, Hash.Combine(Hash.CombineValues(this.candidateSymbols, 4), (int)this.CandidateReason));
        }

        internal bool IsEmpty
        {
            get
            {
                return this.Symbol == null
                    && this.CandidateSymbols.Length == 0;
            }
        }
    }
}