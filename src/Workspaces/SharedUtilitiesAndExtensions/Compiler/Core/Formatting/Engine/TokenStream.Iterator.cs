// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal partial class TokenStream
    {
        // gain of having hand written iterator seems about 50-100ms over auto generated one.
        // not sure whether it is worth it. but I already wrote it to test, so going to just keep it.
        private class Iterator : IEnumerable<(int index, SyntaxToken currentToken, SyntaxToken nextToken)>
        {
            private readonly List<SyntaxToken> _tokensIncludingZeroWidth;

            public Iterator(List<SyntaxToken> tokensIncludingZeroWidth)
                => _tokensIncludingZeroWidth = tokensIncludingZeroWidth;

            public IEnumerator<(int index, SyntaxToken currentToken, SyntaxToken nextToken)> GetEnumerator()
                => new Enumerator(_tokensIncludingZeroWidth);

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
                => GetEnumerator();

            private struct Enumerator : IEnumerator<(int index, SyntaxToken currentToken, SyntaxToken nextToken)>
            {
                private readonly List<SyntaxToken> _tokensIncludingZeroWidth;
                private readonly int _maxCount;

                private (int index, SyntaxToken currentToken, SyntaxToken nextToken) _current;
                private int _index;

                public Enumerator(List<SyntaxToken> tokensIncludingZeroWidth)
                {
                    _tokensIncludingZeroWidth = tokensIncludingZeroWidth;
                    _maxCount = _tokensIncludingZeroWidth.Count - 1;

                    _index = 0;
                    _current = default;
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (_index < _maxCount)
                    {
                        _current = (_index, _tokensIncludingZeroWidth[_index], _tokensIncludingZeroWidth[_index + 1]);
                        _index++;
                        return true;
                    }

                    return MoveNextRare();
                }

                private bool MoveNextRare()
                {
                    _index = _maxCount + 1;
                    _current = default;
                    return false;
                }

                public (int index, SyntaxToken currentToken, SyntaxToken nextToken) Current => _current;

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
