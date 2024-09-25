// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

#pragma warning disable CA1068 // CancellationToken parameters must come last

internal static partial class RoslynParallel
{
    // For all these helpers, we defer to the native .net core version if we're on .net core.  Otherwise, we defer to
    // our ported version of that code when on .net framework.

    public static Task ForEachAsync<TSource>(
        IEnumerable<TSource> source,
        CancellationToken cancellationToken,
        Func<TSource, CancellationToken, ValueTask> body)
    {
#if NET
        return Parallel.ForEachAsync(source, cancellationToken, body);
#else
        return NetFramework.ForEachAsync(source, cancellationToken, body);
#endif
    }

    public static Task ForEachAsync<TSource>(
        IEnumerable<TSource> source,
        ParallelOptions parallelOptions,
        Func<TSource, CancellationToken, ValueTask> body)
    {
#if NET
        return Parallel.ForEachAsync(source, parallelOptions, body);
#else
        return NetFramework.ForEachAsync(source, parallelOptions, body);
#endif
    }

    public static Task ForEachAsync<TSource>(
        IAsyncEnumerable<TSource> source,
        CancellationToken cancellationToken,
        Func<TSource, CancellationToken, ValueTask> body)
    {
#if NET
        return Parallel.ForEachAsync(source, cancellationToken, body);
#else
        return NetFramework.ForEachAsync(source, cancellationToken, body);
#endif
    }

    public static Task ForEachAsync<TSource>(
        IAsyncEnumerable<TSource> source,
        ParallelOptions parallelOptions,
        Func<TSource, CancellationToken, ValueTask> body)
    {
#if NET
        return Parallel.ForEachAsync(source, parallelOptions, body);
#else
        return NetFramework.ForEachAsync(source, parallelOptions, body);
#endif
    }
}
