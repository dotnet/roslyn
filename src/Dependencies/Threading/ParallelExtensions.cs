// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

#pragma warning disable CA1068 // CancellationToken parameters must come last

using System.Collections.Generic;

namespace System.Threading.Tasks;

internal static partial class RoslynParallelExtensions
{
#if NET // binary compatibility
    public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
        => Parallel.ForEachAsync(source, cancellationToken, body);

    public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TSource, CancellationToken, ValueTask> body)
        => Parallel.ForEachAsync(source, parallelOptions, body);

    public static Task ForEachAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
        => Parallel.ForEachAsync(source, cancellationToken, body);

    public static Task ForEachAsync<TSource>(IAsyncEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TSource, CancellationToken, ValueTask> body)
        => Parallel.ForEachAsync(source, parallelOptions, body);
#else
    extension(Parallel)
    {
        public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
            => NetFramework.ForEachAsync(source, cancellationToken, body);

        public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TSource, CancellationToken, ValueTask> body)
            => NetFramework.ForEachAsync(source, parallelOptions, body);

        public static Task ForEachAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
            => NetFramework.ForEachAsync(source, cancellationToken, body);

        public static Task ForEachAsync<TSource>(IAsyncEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TSource, CancellationToken, ValueTask> body)
            => NetFramework.ForEachAsync(source, parallelOptions, body);
    }
#endif
}
