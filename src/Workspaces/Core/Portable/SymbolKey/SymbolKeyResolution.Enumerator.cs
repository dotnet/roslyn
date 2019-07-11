// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKeyResolution
    {
        public struct Enumerator
        {
            private readonly SymbolKeyResolution _symbolKeyResolution;
            private int _index;

            public Enumerator(SymbolKeyResolution symbolKeyResolution)
            {
                _symbolKeyResolution = symbolKeyResolution;
                _index = -1;
            }

            public bool MoveNext()
            {
                _index++;
                if (_symbolKeyResolution.Symbol != null)
                {
                    return _index == 0;
                }

                return _index < _symbolKeyResolution.CandidateSymbols.Length;
            }

            public ISymbol Current
            {
                get
                {
                    if (_symbolKeyResolution.Symbol != null)
                    {
                        return _symbolKeyResolution.Symbol;
                    }

                    return _symbolKeyResolution.CandidateSymbols[_index];
                }
            }
        }
    }
}
