// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal partial class TokenStream
    {
        // gain of having hand written iterator seems about 50-100ms over auto generated one.
        // not sure whether it is worth it. but I already wrote it to test, so going to just keep it.
        private class Iterator : IEnumerable<ValueTuple<int, SyntaxToken, SyntaxToken>>
        {
            private readonly List<SyntaxToken> _tokensIncludingZeroWidth;

            public Iterator(List<SyntaxToken> tokensIncludingZeroWidth)
            {
                _tokensIncludingZeroWidth = tokensIncludingZeroWidth;
            }

            public IEnumerator<ValueTuple<int, SyntaxToken, SyntaxToken>> GetEnumerator()
            {
                return new Enumerator(_tokensIncludingZeroWidth);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private struct Enumerator : IEnumerator<ValueTuple<int, SyntaxToken, SyntaxToken>>
            {
                private readonly List<SyntaxToken> _tokensIncludingZeroWidth;
                private readonly int _maxCount;

                private ValueTuple<int, SyntaxToken, SyntaxToken> _current;
                private int _index;

                public Enumerator(List<SyntaxToken> tokensIncludingZeroWidth)
                {
                    _tokensIncludingZeroWidth = tokensIncludingZeroWidth;
                    _maxCount = _tokensIncludingZeroWidth.Count - 1;

                    _index = 0;
                    _current = new ValueTuple<int, SyntaxToken, SyntaxToken>();
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (_index < _maxCount)
                    {
                        _current = ValueTuple.Create(_index, _tokensIncludingZeroWidth[_index], _tokensIncludingZeroWidth[_index + 1]);
                        _index++;
                        return true;
                    }

                    return MoveNextRare();
                }

                private bool MoveNextRare()
                {
                    _index = _maxCount + 1;
                    _current = new ValueTuple<int, SyntaxToken, SyntaxToken>();
                    return false;
                }

                public ValueTuple<int, SyntaxToken, SyntaxToken> Current => _current;

                object System.Collections.IEnumerator.Current
                {
                    get
                    {
                        if (_index == 0 || _index == _maxCount + 1)
                        {
                            throw new InvalidOperationException();
                        }

                        return Current;
                    }
                }

                void System.Collections.IEnumerator.Reset()
                {
                    _index = 0;
                    _current = new ValueTuple<int, SyntaxToken, SyntaxToken>();
                }
            }
        }
    }
}
