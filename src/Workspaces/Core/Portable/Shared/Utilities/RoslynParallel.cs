// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal static class RoslynParallel
{
#pragma warning disable CA1068 // CancellationToken parameters must come last
    public static async Task ForEachAsync<TSource>(
#pragma warning restore CA1068 // CancellationToken parameters must come last
        IEnumerable<TSource> source,
        CancellationToken cancellationToken,
        Func<TSource, CancellationToken, ValueTask> body)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

#if NET
        await Parallel.ForEachAsync(source, cancellationToken, body).ConfigureAwait(false);
#else
        using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

        foreach (var item in source)
        {
            tasks.Add(Task.Run(async () =>
            {
                await body(item, cancellationToken).ConfigureAwait(false);
            }, cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
#endif
    }

#pragma warning disable CA1068 // CancellationToken parameters must come last
    public static async Task ForEachAsync<TSource>(
#pragma warning restore CA1068 // CancellationToken parameters must come last
        IAsyncEnumerable<TSource> source,
        CancellationToken cancellationToken,
        Func<TSource, CancellationToken, ValueTask> body)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

#if NET
        await Parallel.ForEachAsync(source, cancellationToken, body).ConfigureAwait(false);
#else
        using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

        await foreach (var item in source)
        {
            tasks.Add(Task.Run(async () =>
            {
                await body(item, cancellationToken).ConfigureAwait(false);
            }, cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
#endif
    }
}
