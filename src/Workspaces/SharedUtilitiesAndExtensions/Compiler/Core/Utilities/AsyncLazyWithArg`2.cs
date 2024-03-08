// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities;

/// <summary>
/// Represents an AsyncLazy instance holding compute functions and additional 
/// argument data to pass to those functions
/// </summary>
/// <typeparam name="T">Return type of compute functions</typeparam>
/// <typeparam name="TArg">Argument type to compute functions</typeparam>
internal sealed class AsyncLazyWithArg<T, TArg> : AsyncLazy<T>
{
    private PooledDelegates.Releaser _asyncComputeFunctionReleaser;
    private PooledDelegates.Releaser _syncComputeFunctionReleaser;
    private bool _isAsyncComputeFunctionReleaserDisposed;
    private bool _isSyncComputeFunctionReleaserDisposed;

    public AsyncLazyWithArg(Func<TArg, CancellationToken, Task<T>> asynchronousComputeFunction, TArg arg)
        : this(asynchronousComputeFunction, synchronousComputeFunction: null, arg)
    {
    }

    public AsyncLazyWithArg(Func<TArg, CancellationToken, Task<T>> asynchronousComputeFunction, Func<TArg, CancellationToken, T>? synchronousComputeFunction, TArg arg)
    {
        Contract.ThrowIfNull(asynchronousComputeFunction);

        _asyncComputeFunctionReleaser = PooledDelegates.GetPooledFunction(
            (ct, arg) => arg.asynchronousComputeFunction(arg.arg, ct),
            (asynchronousComputeFunction, arg),
            out Func<CancellationToken, Task<T>> translatedAsynchronousComputeFunction);

        Func<CancellationToken, T>? translatedSynchronousComputeFunction = null;
        if (synchronousComputeFunction is not null)
        {
            _syncComputeFunctionReleaser = PooledDelegates.GetPooledFunction(
                (ct, arg) => arg.synchronousComputeFunction(arg.arg, ct),
                (synchronousComputeFunction, arg),
                out translatedSynchronousComputeFunction);
        }
        else
        {
            // No need to dispose the synchronous releaser
            _isSyncComputeFunctionReleaserDisposed = true;
        }

        InitializeComputeFunctions(translatedAsynchronousComputeFunction, translatedSynchronousComputeFunction);
    }

    protected override void ClearComputeFunctions()
    {
        if (!_isAsyncComputeFunctionReleaserDisposed)
        {
            _isAsyncComputeFunctionReleaserDisposed = true;

            _asyncComputeFunctionReleaser.Dispose();
            _asyncComputeFunctionReleaser = default;
        }

        if (!_isSyncComputeFunctionReleaserDisposed)
        {
            _isSyncComputeFunctionReleaserDisposed = true;

            _syncComputeFunctionReleaser.Dispose();
            _syncComputeFunctionReleaser = default;
        }

        base.ClearComputeFunctions();
    }
}
