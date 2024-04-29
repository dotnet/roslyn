// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

#pragma warning disable CA1068 // CancellationToken parameters must come last

internal static class RoslynParallel
{
    public static Task ForEachAsync<TSource>(
        IEnumerable<TSource> source,
        CancellationToken cancellationToken,
        Func<TSource, CancellationToken, ValueTask> body)
    {
        return ForEachAsync(source, GetParallelOptions(cancellationToken), body);
    }

    private static ParallelOptions GetParallelOptions(CancellationToken cancellationToken)
        => new() { TaskScheduler = TaskScheduler.Default, CancellationToken = cancellationToken };

    public static async Task ForEachAsync<TSource>(
        IEnumerable<TSource> source,
        ParallelOptions parallelOptions,
        Func<TSource, CancellationToken, ValueTask> body)
    {
        var cancellationToken = parallelOptions.CancellationToken;
        if (cancellationToken.IsCancellationRequested)
            return;

#if NET
        await Parallel.ForEachAsync(source, parallelOptions, body).ConfigureAwait(false);
#else
        using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

        foreach (var item in source)
        {
            tasks.Add(CreateWorkAsync(
                parallelOptions.TaskScheduler,
                async () => await body(item, cancellationToken).ConfigureAwait(false),
                cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
#endif
    }

    public static Task ForEachAsync<TSource>(
        IAsyncEnumerable<TSource> source,
        CancellationToken cancellationToken,
        Func<TSource, CancellationToken, ValueTask> body)
    {
        return ForEachAsync(source, GetParallelOptions(cancellationToken), body);
    }

    public static async Task ForEachAsync<TSource>(
        IAsyncEnumerable<TSource> source,
        ParallelOptions parallelOptions,
        Func<TSource, CancellationToken, ValueTask> body)
    {
        var cancellationToken = parallelOptions.CancellationToken;
        if (cancellationToken.IsCancellationRequested)
            return;

#if NET
        await Parallel.ForEachAsync(source, parallelOptions, body).ConfigureAwait(false);
#else
        using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

        await foreach (var item in source)
        {
            tasks.Add(CreateWorkAsync(
                parallelOptions.TaskScheduler,
                async () => await body(item, cancellationToken).ConfigureAwait(false),
                cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
#endif
    }

    public static Task CreateWorkAsync(TaskScheduler scheduler, Func<Task> createWorkAsync, CancellationToken cancellationToken)
        => Task.Factory.StartNew(createWorkAsync, cancellationToken, TaskCreationOptions.None, scheduler).Unwrap();
}
