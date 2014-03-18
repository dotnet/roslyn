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
        public static IAsyncEnumerable<TResult> Select<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TResult> selector)
        {
            return new SelectAsyncEnumerable<TSource, TResult>(source, selector);
        }

        private class SelectAsyncEnumerable<TSource, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<TSource> source;
            private readonly Func<TSource, TResult> selector;

            public SelectAsyncEnumerable(
                IAsyncEnumerable<TSource> source,
                Func<TSource, TResult> selector)
            {
                this.source = source;
                this.selector = selector;
            }

            public IAsyncEnumerator<TResult> GetEnumerator()
            {
                return new SelectAsyncEnumerator<TSource, TResult>(source.GetEnumerator(), selector);
            }
        }

        private class SelectAsyncEnumerator<TSource, TResult> : IAsyncEnumerator<TResult>
        {
            private readonly IAsyncEnumerator<TSource> source;
            private readonly Func<TSource, TResult> func;
            private TResult current;

            public SelectAsyncEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, TResult> func)
            {
                this.source = source;
                this.func = func;
            }

            public TResult Current
            {
                get
                {
                    return current;
                }
            }

            public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
            {
                var result = await source.MoveNextAsync(cancellationToken).ConfigureAwait(false);
                this.current = result ? func(source.Current) : default(TResult);
                return result;
            }

            public void Dispose()
            {
                source.Dispose();
            }
        }
    }
}
