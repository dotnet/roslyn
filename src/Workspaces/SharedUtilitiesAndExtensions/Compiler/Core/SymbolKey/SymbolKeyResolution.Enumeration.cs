// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    internal readonly partial struct SymbolKeyResolution
    {
        public readonly struct Enumerable<TSymbol> where TSymbol : ISymbol
        {
            private readonly SymbolKeyResolution _resolution;

            internal Enumerable(SymbolKeyResolution resolution)
                => _resolution = resolution;

            public Enumerator<TSymbol> GetEnumerator()
                => new(_resolution);
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

            public readonly TSymbol Current
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
