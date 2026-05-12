// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.Threading;

internal static class AsyncLazy
{
    public static AsyncLazy<T> Create<T, TArg>(Func<TArg, CancellationToken, Task<T>> asynchronousComputeFunction, TArg arg)
        => AsyncLazy<T>.Create(asynchronousComputeFunction, arg);

    public static AsyncLazy<T> Create<T>(Func<CancellationToken, Task<T>> asynchronousComputeFunction)
        => Create(
            asynchronousComputeFunction: static (asynchronousComputeFunction, cancellationToken) => asynchronousComputeFunction(cancellationToken),
            arg: asynchronousComputeFunction);

    public static AsyncLazy<T> Create<T>(T value)
        => AsyncLazy<T>.Create<T>(value);
}
