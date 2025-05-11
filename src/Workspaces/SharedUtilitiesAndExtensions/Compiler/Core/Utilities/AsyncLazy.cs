// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities;

internal static class AsyncLazy
{
    public static AsyncLazy<T> Create<T, TArg>(Func<TArg, CancellationToken, Task<T>> asynchronousComputeFunction, Func<TArg, CancellationToken, T>? synchronousComputeFunction, TArg arg)
        => AsyncLazy<T>.Create(asynchronousComputeFunction, synchronousComputeFunction, arg);

    public static AsyncLazy<T> Create<T, TArg>(Func<TArg, CancellationToken, Task<T>> asynchronousComputeFunction, TArg arg)
        => Create(
            asynchronousComputeFunction,
            synchronousComputeFunction: null,
            arg);

    public static AsyncLazy<T> Create<T, TArg>(Func<TArg, CancellationToken, T> synchronousComputeFunction, TArg arg)
        => Create(
            asynchronousComputeFunction: static (outerArg, cancellationToken) => Task.FromResult(outerArg.synchronousComputeFunction(outerArg.arg, cancellationToken)),
            synchronousComputeFunction: static (outerArg, cancellationToken) => outerArg.synchronousComputeFunction(outerArg.arg, cancellationToken),
            (synchronousComputeFunction, arg));

    public static AsyncLazy<T> Create<T>(Func<CancellationToken, Task<T>> asynchronousComputeFunction)
        => Create(
            asynchronousComputeFunction: static (asynchronousComputeFunction, cancellationToken) => asynchronousComputeFunction(cancellationToken),
            arg: asynchronousComputeFunction);

    public static AsyncLazy<T> Create<T>(Func<CancellationToken, T> synchronousComputeFunction)
        => Create(
            synchronousComputeFunction: static (synchronousComputeFunction, cancellationToken) => synchronousComputeFunction(cancellationToken),
            arg: synchronousComputeFunction);

    public static AsyncLazy<T> Create<T>(Func<CancellationToken, Task<T>> asynchronousComputeFunction, Func<CancellationToken, T> synchronousComputeFunction)
        => Create(
            asynchronousComputeFunction: static (arg, cancellationToken) => arg.asynchronousComputeFunction(cancellationToken),
            synchronousComputeFunction: static (arg, cancellationToken) => arg.synchronousComputeFunction(cancellationToken),
            arg: (asynchronousComputeFunction, synchronousComputeFunction));

    public static AsyncLazy<T> Create<T>(T value)
        => AsyncLazy<T>.Create<T>(value);
}
