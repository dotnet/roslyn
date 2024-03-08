// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities;

/// <summary>
/// Represents an AsyncLazy instance holding compute functions
/// </summary>
/// <typeparam name="T">Return type of compute functions</typeparam>
internal sealed class AsyncLazyWithoutArg<T> : AsyncLazy<T>
{
    /// <summary>
    /// The underlying function that starts an asynchronous computation of the resulting value.
    /// Null'ed out once we've computed the result and we've been asked to cache it.  Otherwise,
    /// it is kept around in case the value needs to be computed again.
    /// </summary>
    private Func<CancellationToken, Task<T>>? _asynchronousComputeFunction;

    /// <summary>
    /// The underlying function that starts a synchronous computation of the resulting value.
    /// Null'ed out once we've computed the result and we've been asked to cache it, or if we
    /// didn't get any synchronous function given to us in the first place.
    /// </summary>
    private Func<CancellationToken, T>? _synchronousComputeFunction;

    /// <summary>
    /// Creates an AsyncLazy that always returns the value, analogous to <see cref="Task.FromResult{T}" />.
    /// </summary>
    public AsyncLazyWithoutArg(T value)
        : base(value)
    {
    }

    public AsyncLazyWithoutArg(Func<CancellationToken, Task<T>> asynchronousComputeFunction)
        : this(asynchronousComputeFunction, synchronousComputeFunction: null)
    {
    }

    public AsyncLazyWithoutArg(Func<CancellationToken, Task<T>> asynchronousComputeFunction, Func<CancellationToken, T>? synchronousComputeFunction)
    {
        Contract.ThrowIfNull(asynchronousComputeFunction);
        _asynchronousComputeFunction = asynchronousComputeFunction;
        _synchronousComputeFunction = synchronousComputeFunction;
    }

    protected override bool HasAsynchronousComputeFunction => _asynchronousComputeFunction is not null;
    protected override bool HasSynchronousComputeFunction => _synchronousComputeFunction is not null;

    protected override T InvokeComputeFunction(CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(_synchronousComputeFunction);

        return _synchronousComputeFunction(cancellationToken);
    }

    protected override Task<T> InvokeComputeFunctionAsync(CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(_asynchronousComputeFunction);

        return _asynchronousComputeFunction(cancellationToken);
    }

    protected override void ClearComputeFunctions()
    {
        _asynchronousComputeFunction = null;
        _synchronousComputeFunction = null;
    }
}
