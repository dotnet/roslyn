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
        public static IAsyncEnumerable<T> Empty<T>()
        {
            return EmptyAsyncEnumerable<T>.Instance;
        }

        public static async Task<IEnumerable<T>> ToTask<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken)
        {
            using (var enumerator = source.GetEnumerator())
            {
                var result = new List<T>();

                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    result.Add(enumerator.Current);
                }

                return result;
            }
        }
    }
}