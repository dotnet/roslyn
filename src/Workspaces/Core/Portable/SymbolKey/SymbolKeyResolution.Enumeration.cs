// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKeyResolution
    {
        public struct Enumerable<TSymbol> where TSymbol : ISymbol
        {
            private readonly SymbolKeyResolution _resolution;

            internal Enumerable(SymbolKeyResolution resolution)
                => _resolution = resolution;

            public Enumerator<TSymbol> GetEnumerator()
                => new Enumerator<TSymbol>(_resolution);
        }

        public struct Enumerator<TSymbol> where TSymbol : ISymbol
        {
            private readonly SymbolKeyResolution _symbolKeyResolution;
            private int _index;

            internal Enumerator(SymbolKeyResolution symbolKeyResolution)
            {
                _symbolKeyResolution = symbolKeyResolution;
                _index = -1;
            }

            public bool MoveNext()
            {
                if (_symbolKeyResolution.Symbol != null)
                {
                    return ++_index == 0 && _symbolKeyResolution.Symbol is TSymbol;
                }

                while (++_index < _symbolKeyResolution.CandidateSymbols.Length)
                {
                    if (_symbolKeyResolution.CandidateSymbols[_index] is TSymbol)
                    {
                        return true;
                    }
                }

                return false;
            }

            public TSymbol Current
            {
                get
                {
                    if (_symbolKeyResolution.Symbol != null)
                    {
                        return (TSymbol)_symbolKeyResolution.Symbol;
                    }

                    return (TSymbol)_symbolKeyResolution.CandidateSymbols[_index];
                }
            }
        }
    }
}
