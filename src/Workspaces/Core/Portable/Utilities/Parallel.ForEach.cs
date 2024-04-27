// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET

#pragma warning disable CA1068 // CancellationToken parameters must come last

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities;

internal static partial class ParallelUtilities
{
    public static async Task ForEachAsync<TSource>(
        IEnumerable<TSource> source,
        CancellationToken cancellationToken,
        Func<TSource, CancellationToken, ValueTask> body)
    {
        using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

        foreach (var item in source)
        {
            tasks.Add(Task.Run(async () =>
            {
                await body(item, cancellationToken).ConfigureAwait(false);
            }, cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}

#endif
