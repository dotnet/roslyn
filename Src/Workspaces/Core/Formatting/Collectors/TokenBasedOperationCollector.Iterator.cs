using System;
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    internal partial class TokenBasedOperationCollector
    {
        // gain of having hand written iterator seems about 50-100ms over auto generated one.
        // not sure whether it is worth it. but I already wrote it to test, so going to just keep it.
        private class Iterator : IEnumerable<ValueTuple<int, CommonSyntaxToken, CommonSyntaxToken>>
        {
            private readonly List<CommonSyntaxToken> tokensIncludingZeroWidth;

            public Iterator(List<CommonSyntaxToken> tokensIncludingZeroWidth)
            {
                this.tokensIncludingZeroWidth = tokensIncludingZeroWidth;
            }

            public IEnumerator<ValueTuple<int, CommonSyntaxToken, CommonSyntaxToken>> GetEnumerator()
            {
                return new Enumerator(this.tokensIncludingZeroWidth);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private struct Enumerator : IEnumerator<ValueTuple<int, CommonSyntaxToken, CommonSyntaxToken>>
            {
                private readonly List<CommonSyntaxToken> tokensIncludingZeroWidth;
                private readonly int MaxCount;

                private ValueTuple<int, CommonSyntaxToken, CommonSyntaxToken> current;
                private int index;

                public Enumerator(List<CommonSyntaxToken> tokensIncludingZeroWidth)
                {
                    this.tokensIncludingZeroWidth = tokensIncludingZeroWidth;
                    this.MaxCount = this.tokensIncludingZeroWidth.Count - 1;

                    this.index = 0;
                    this.current = new ValueTuple<int, CommonSyntaxToken, CommonSyntaxToken>();
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (index < this.MaxCount)
                    {
                        current = ValueTuple.Create(index, this.tokensIncludingZeroWidth[index], this.tokensIncludingZeroWidth[index + 1]);
                        index++;
                        return true;
                    }

                    return MoveNextRare();
                }

                private bool MoveNextRare()
                {
                    index = this.MaxCount + 1;
                    current = new ValueTuple<int, CommonSyntaxToken, CommonSyntaxToken>();
                    return false;
                }

                public ValueTuple<int, CommonSyntaxToken, CommonSyntaxToken> Current
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
                        if (index == 0 || index == this.MaxCount + 1)
                        {
                            throw new InvalidOperationException();
                        }

                        return Current;
                    }
                }

                void System.Collections.IEnumerator.Reset()
                {
                    index = 0;
                    current = new ValueTuple<int, CommonSyntaxToken, CommonSyntaxToken>();
                }
            }
        }
    }
}
