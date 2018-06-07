// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal struct SymbolKeyResolution
    {
        private readonly ImmutableArray<ISymbol> _candidateSymbols;

        public ISymbol Symbol { get; }
        public ImmutableArray<ISymbol> CandidateSymbols => _candidateSymbols.NullToEmpty();
        public CandidateReason CandidateReason { get; }

        internal SymbolKeyResolution(ISymbol symbol)
        {
            Symbol = symbol;
            _candidateSymbols = ImmutableArray<ISymbol>.Empty;
            CandidateReason = CandidateReason.None;
        }

        internal SymbolKeyResolution(ImmutableArray<ISymbol> candidateSymbols, CandidateReason candidateReason)
        {
            Symbol = null;
            _candidateSymbols = candidateSymbols;
            CandidateReason = candidateReason;
        }

        internal static SymbolKeyResolution Create(IEnumerable<ISymbol> symbols)
        {
            return symbols == null
                ? default
                : Create(symbols.WhereNotNull().ToArray());
        }

        internal static SymbolKeyResolution Create(ISymbol[] symbols)
        {
            return symbols.Length == 0
                ? default
                : symbols.Length == 1
                    ? new SymbolKeyResolution(symbols[0])
                    : new SymbolKeyResolution(ImmutableArray.Create(symbols), CandidateReason.Ambiguous);
        }
    }
}
