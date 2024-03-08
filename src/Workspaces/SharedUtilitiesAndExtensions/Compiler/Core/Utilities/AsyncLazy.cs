// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities;

internal static class AsyncLazy
{
    public static AsyncLazyWithArg<T, TArg> Create<T, TArg>(Func<TArg, CancellationToken, Task<T>> asynchronousComputeFunction, TArg arg)
        => new AsyncLazyWithArg<T, TArg>(asynchronousComputeFunction, arg);

    public static AsyncLazyWithArg<T, TArg> Create<T, TArg>(Func<TArg, CancellationToken, T> synchronousComputeFunction, TArg arg)
        => new AsyncLazyWithArg<T, TArg>((arg, cancellationToken) => Task.FromResult(synchronousComputeFunction(arg, cancellationToken)), synchronousComputeFunction, arg);

    public static AsyncLazyWithArg<T, TArg> Create<T, TArg>(Func<TArg, CancellationToken, Task<T>> asynchronousComputeFunction, Func<TArg, CancellationToken, T> synchronousComputeFunction, TArg arg)
        => new AsyncLazyWithArg<T, TArg>(asynchronousComputeFunction, synchronousComputeFunction, arg);

    public static AsyncLazy<T> Create<T>(Func<CancellationToken, Task<T>> asynchronousComputeFunction)
        => new AsyncLazy<T>(asynchronousComputeFunction);

    public static AsyncLazy<T> Create<T>(Func<CancellationToken, T> synchronousComputeFunction)
        => new AsyncLazy<T>(cancellationToken => Task.FromResult(synchronousComputeFunction(cancellationToken)), synchronousComputeFunction);

    public static AsyncLazy<T> Create<T>(Func<CancellationToken, Task<T>> asynchronousComputeFunction, Func<CancellationToken, T> synchronousComputeFunction)
        => new AsyncLazy<T>(asynchronousComputeFunction, synchronousComputeFunction);

    public static AsyncLazy<T> Create<T>(T value)
        => new AsyncLazy<T>(value);
}
