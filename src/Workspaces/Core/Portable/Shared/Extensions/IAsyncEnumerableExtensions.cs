// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class IAsyncEnumerableExtensions
    {
        public static async Task<ImmutableArray<T>> ToImmutableArrayAsync<T>(this IAsyncEnumerable<T> values, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<T>.GetInstance(out var result);
            await foreach (var value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
                result.Add(value);

            return result.ToImmutable();
        }
    }
}
