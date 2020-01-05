// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The result of <see cref="SymbolKey.Resolve(Compilation, bool, bool, CancellationToken)"/>.
    /// If the <see cref="SymbolKey"/> could be uniquely mapped to a single <see cref="ISymbol"/>
    /// then that will be returned in <see cref="Symbol"/>.  Otherwise, if 
    /// the key resolves to multiple symbols (which can happen in error scenarios), then 
    /// <see cref="CandidateSymbols"/> and <see cref="CandidateReason"/> will be returned.
    /// 
    /// If no symbol can be found <see cref="Symbol"/> will be <c>null</c> and <see cref="CandidateSymbols"/>
    /// will be empty.
    /// </summary>
    internal partial struct SymbolKeyResolution
    {
        private readonly ImmutableArray<ISymbol> _candidateSymbols;

        internal SymbolKeyResolution(ISymbol symbol)
        {
            Symbol = symbol;
            _candidateSymbols = default;
            CandidateReason = CandidateReason.None;
        }

        internal SymbolKeyResolution(ImmutableArray<ISymbol> candidateSymbols, CandidateReason candidateReason)
        {
            Symbol = null;
            _candidateSymbols = candidateSymbols;
            CandidateReason = candidateReason;

#if DEBUG
            foreach (var symbol in CandidateSymbols)
            {
                Debug.Assert(symbol != null);
            }
#endif
        }

        internal int SymbolCount => Symbol != null ? 1 : CandidateSymbols.Length;

        public ISymbol Symbol { get; }
        public CandidateReason CandidateReason { get; }
        public ImmutableArray<ISymbol> CandidateSymbols => _candidateSymbols.NullToEmpty();

        public Enumerator<ISymbol> GetEnumerator()
            => new Enumerator<ISymbol>(this);

        internal Enumerable<TSymbol> OfType<TSymbol>() where TSymbol : ISymbol
            => new Enumerable<TSymbol>(this);
    }
}
