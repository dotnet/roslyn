// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly IAsyncEnumerable<TSource> _source;
            private readonly Func<TSource, TResult> _selector;

            public SelectAsyncEnumerable(
                IAsyncEnumerable<TSource> source,
                Func<TSource, TResult> selector)
            {
                _source = source;
                _selector = selector;
            }

            public IAsyncEnumerator<TResult> GetEnumerator()
            {
                return new SelectAsyncEnumerator<TSource, TResult>(_source.GetEnumerator(), _selector);
            }
        }

        private class SelectAsyncEnumerator<TSource, TResult> : IAsyncEnumerator<TResult>
        {
            private readonly IAsyncEnumerator<TSource> _source;
            private readonly Func<TSource, TResult> _func;
            private TResult _current;

            public SelectAsyncEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, TResult> func)
            {
                _source = source;
                _func = func;
            }

            public TResult Current
            {
                get
                {
                    return _current;
                }
            }

            public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
            {
                var result = await _source.MoveNextAsync(cancellationToken).ConfigureAwait(false);
                _current = result ? _func(_source.Current) : default(TResult);
                return result;
            }

            public void Dispose()
            {
                _source.Dispose();
            }
        }
    }
}
