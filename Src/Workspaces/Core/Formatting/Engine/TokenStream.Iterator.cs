// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal partial class TokenStream
    {
        // gain of having hand written iterator seems about 50-100ms over auto generated one.
        // not sure whether it is worth it. but I already wrote it to test, so going to just keep it.
        private class Iterator : IEnumerable<ValueTuple<int, SyntaxToken, SyntaxToken>>
        {
            private readonly List<SyntaxToken> tokensIncludingZeroWidth;

            public Iterator(List<SyntaxToken> tokensIncludingZeroWidth)
            {
                this.tokensIncludingZeroWidth = tokensIncludingZeroWidth;
            }

            public IEnumerator<ValueTuple<int, SyntaxToken, SyntaxToken>> GetEnumerator()
            {
                return new Enumerator(this.tokensIncludingZeroWidth);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private struct Enumerator : IEnumerator<ValueTuple<int, SyntaxToken, SyntaxToken>>
            {
                private readonly List<SyntaxToken> tokensIncludingZeroWidth;
                private readonly int maxCount;

                private ValueTuple<int, SyntaxToken, SyntaxToken> current;
                private int index;

                public Enumerator(List<SyntaxToken> tokensIncludingZeroWidth)
                {
                    this.tokensIncludingZeroWidth = tokensIncludingZeroWidth;
                    this.maxCount = this.tokensIncludingZeroWidth.Count - 1;

                    this.index = 0;
                    this.current = new ValueTuple<int, SyntaxToken, SyntaxToken>();
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (index < this.maxCount)
                    {
                        current = ValueTuple.Create(index, this.tokensIncludingZeroWidth[index], this.tokensIncludingZeroWidth[index + 1]);
                        index++;
                        return true;
                    }

                    return MoveNextRare();
                }

                private bool MoveNextRare()
                {
                    index = this.maxCount + 1;
                    current = new ValueTuple<int, SyntaxToken, SyntaxToken>();
                    return false;
                }

                public ValueTuple<int, SyntaxToken, SyntaxToken> Current
                {
                    get
                    {
                        return current;
                    }
                }

                object System.Collections.IEnumerator.Current
                {
                    get
                    {
                        if (index == 0 || index == this.maxCount + 1)
                        {
                            throw new InvalidOperationException();
                        }

                        return Current;
                    }
                }

                void System.Collections.IEnumerator.Reset()
                {
                    index = 0;
                    current = new ValueTuple<int, SyntaxToken, SyntaxToken>();
                }
            }
        }
    }
}
