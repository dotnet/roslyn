// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal struct SymbolKeyResolution
    {
        private readonly ISymbol _symbol;
        private readonly ImmutableArray<ISymbol> _candidateSymbols;
        private readonly CandidateReason _candidateReason;

        internal SymbolKeyResolution(ISymbol symbol) : this()
        {
            _symbol = symbol;
            _candidateSymbols = ImmutableArray<ISymbol>.Empty;
            _candidateReason = CandidateReason.None;
        }

        internal SymbolKeyResolution(ImmutableArray<ISymbol> candidateSymbols, CandidateReason candidateReason)
        {
            _symbol = null;
            _candidateSymbols = candidateSymbols;
            _candidateReason = candidateReason;
        }

        public ISymbol Symbol
        {
            get { return _symbol; }
        }

        public ImmutableArray<ISymbol> CandidateSymbols
        {
            get { return _candidateSymbols.NullToEmpty(); }
        }

        public CandidateReason CandidateReason
        {
            get { return _candidateReason; }
        }
    }
}
