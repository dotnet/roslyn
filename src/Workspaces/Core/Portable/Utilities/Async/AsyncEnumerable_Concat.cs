// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal static partial class AsyncEnumerable
    {
        public static IAsyncEnumerable<T> Concat<T>(
            this IAsyncEnumerable<T> first,
            IAsyncEnumerable<T> second)
        {
            return new ConcatAsyncEnumerable<T>(first, second);
        }

        private class ConcatAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IAsyncEnumerable<T> first;
            private readonly IAsyncEnumerable<T> second;

            public ConcatAsyncEnumerable(IAsyncEnumerable<T> first, IAsyncEnumerable<T> second)
            {
                this.first = first;
                this.second = second;
            }

            public IAsyncEnumerator<T> GetEnumerator()
            {
                return new ConcatAsyncEnumerator<T>(first.GetEnumerator(), second.GetEnumerator());
            }
        }

        private class ConcatAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> first;
            private readonly IAsyncEnumerator<T> second;

            private IAsyncEnumerator<T> currentEnumerator;

            public ConcatAsyncEnumerator(IAsyncEnumerator<T> first, IAsyncEnumerator<T> second)
            {
                this.first = first;
                this.second = second;

                this.currentEnumerator = first;
            }

            public T Current { get; private set; }

            public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
            {
                while (true)
                {
                    var currentEnumeratorMoveNext = await currentEnumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false);

                    // The current enumerator moved forward successfully.  Get it's current
                    // value and store it, and return true to let the caller know we moved.
                    if (currentEnumeratorMoveNext)
                    {
                        this.Current = currentEnumerator.Current;
                        return true;
                    }

                    // Current enumerator didn't move forward.  If it's the second enumerator
                    // then we're done.  
                    if (currentEnumerator == this.second)
                    {
                        this.Current = default(T);
                        return false;
                    }

                    // The first enumerator finished.  Set our current enumerator to the
                    // second enumerator and then recurse.
                    this.currentEnumerator = second;
                }
            }

            public void Dispose()
            {
                first.Dispose();
                second.Dispose();
            }
        }
    }
}