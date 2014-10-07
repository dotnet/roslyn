// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal struct SymbolKeyResolution
    {
        private readonly ISymbol symbol;
        private readonly ImmutableArray<ISymbol> candidateSymbols;
        private readonly CandidateReason candidateReason;

        internal SymbolKeyResolution(ISymbol symbol) : this()
        {
            this.symbol = symbol;
            this.candidateSymbols = ImmutableArray<ISymbol>.Empty;
            this.candidateReason = CandidateReason.None;
        }

        internal SymbolKeyResolution(ImmutableArray<ISymbol> candidateSymbols, CandidateReason candidateReason)
        {
            this.symbol = null;
            this.candidateSymbols = candidateSymbols;
            this.candidateReason = candidateReason;
        }

        public ISymbol Symbol
        {
            get { return this.symbol; }
        }

        public ImmutableArray<ISymbol> CandidateSymbols
        {
            get { return this.candidateSymbols.NullToEmpty(); }
        }

        public CandidateReason CandidateReason
        {
            get { return this.candidateReason; }
        }
    }
}
