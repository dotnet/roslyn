// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities;

internal sealed class AsyncLazyWithoutArg<T> : AsyncLazy<
    T,
    (Func<CancellationToken, Task<T>> asynchronousComputeFunction, Func<CancellationToken, T>? synchronousComputeFunction)>
{
    public AsyncLazyWithoutArg(T value) : base(value)
    {
    }

    public AsyncLazyWithoutArg(Func<CancellationToken, Task<T>> asynchronousComputeFunction)
        : this(asynchronousComputeFunction, synchronousComputeFunction: null)
    {
    }

    public AsyncLazyWithoutArg(
        Func<CancellationToken, Task<T>> asynchronousComputeFunction,
        Func<CancellationToken, T>? synchronousComputeFunction)
        : base(
            asynchronousComputeFunction: static (t, c) => t.asynchronousComputeFunction(c),
            synchronousComputeFunction: synchronousComputeFunction is null ? null : static (t, c) => t.synchronousComputeFunction!(c),
            (asynchronousComputeFunction, synchronousComputeFunction))
    {
    }
}
