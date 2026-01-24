// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis;

internal static class IAsyncEnumerableExtensions
{
    public static async Task<ImmutableArray<T>> ToImmutableArrayAsync<T>(this IAsyncEnumerable<T> values, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<T>.GetInstance(out var result);

        await foreach (var value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
            result.Add(value);

        return result.ToImmutableAndClear();
    }

    // Remove after .NET 10, https://github.com/dotnet/roslyn/issues/80198
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
    public static async IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(this IEnumerable<TSource> source)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        foreach (var item in source)
            yield return item;
    }
}
