// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.Formatting;

internal partial class TokenStream
{
    // gain of having hand written iterator seems about 50-100ms over auto generated one.
    // not sure whether it is worth it. but I already wrote it to test, so going to just keep it.
    public readonly struct Iterator(SegmentedList<SyntaxToken> tokensIncludingZeroWidth)
    {
        public Enumerator GetEnumerator()
            => new(tokensIncludingZeroWidth);

        public struct Enumerator
        {
            private readonly SegmentedList<SyntaxToken> _tokensIncludingZeroWidth;
            private readonly int _maxCount;

            private (int index, SyntaxToken currentToken, SyntaxToken nextToken) _current;
            private int _index;

            public Enumerator(SegmentedList<SyntaxToken> tokensIncludingZeroWidth)
            {
                _tokensIncludingZeroWidth = tokensIncludingZeroWidth;
                _maxCount = _tokensIncludingZeroWidth.Count - 1;

                _index = 0;
                _current = default;
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

            public readonly (int index, SyntaxToken currentToken, SyntaxToken nextToken) Current => _current;
        }
    }
}
