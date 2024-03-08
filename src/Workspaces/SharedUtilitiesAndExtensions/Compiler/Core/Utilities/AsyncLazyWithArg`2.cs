// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities;

/// <summary>
/// Represents an AsyncLazy instance holding compute functions and additional 
/// argument data to pass to those functions
/// </summary>
/// <typeparam name="T">Return type of compute functions</typeparam>
/// <typeparam name="TArg">Argument type to compute functions</typeparam>
internal sealed class AsyncLazyWithArg<T, TArg> : AsyncLazy<T>
{
    /// <summary>
    /// The underlying function that starts an asynchronous computation of the resulting value.
    /// Null'ed out once we've computed the result and we've been asked to cache it.  Otherwise,
    /// it is kept around in case the value needs to be computed again.
    /// </summary>
    private Func<TArg, CancellationToken, Task<T>>? _asynchronousComputeFunction;

    /// <summary>
    /// The underlying function that starts a synchronous computation of the resulting value.
    /// Null'ed out once we've computed the result and we've been asked to cache it, or if we
    /// didn't get any synchronous function given to us in the first place.
    /// </summary>
    private Func<TArg, CancellationToken, T>? _synchronousComputeFunction;

    /// <summary>
    /// Data passed to the compute functions. Typically allowed to prevent closures in calls to 
    /// the <see cref="AsyncLazy"/> Create methods.
    /// </summary>
    private TArg? _arg;

    public AsyncLazyWithArg(Func<TArg, CancellationToken, Task<T>> asynchronousComputeFunction, TArg arg)
        : this(asynchronousComputeFunction, synchronousComputeFunction: null, arg)
    {
    }

    public AsyncLazyWithArg(Func<TArg, CancellationToken, Task<T>> asynchronousComputeFunction, Func<TArg, CancellationToken, T>? synchronousComputeFunction, TArg arg)
    {
        Contract.ThrowIfNull(asynchronousComputeFunction);
        _asynchronousComputeFunction = asynchronousComputeFunction;
        _synchronousComputeFunction = synchronousComputeFunction;
        _arg = arg;
    }

    protected override bool HasAsynchronousComputeFunction => _asynchronousComputeFunction is not null;
    protected override bool HasSynchronousComputeFunction => _synchronousComputeFunction is not null;

    protected override T InvokeComputeFunction(CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(_synchronousComputeFunction);

        return _synchronousComputeFunction(_arg!, cancellationToken);
    }

    protected override Task<T> InvokeComputeFunctionAsync(CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(_asynchronousComputeFunction);

        return _asynchronousComputeFunction(_arg!, cancellationToken);
    }

    protected override void ClearComputeFunctions()
    {
        _asynchronousComputeFunction = null;
        _synchronousComputeFunction = null;
        _arg = default;
    }
}
