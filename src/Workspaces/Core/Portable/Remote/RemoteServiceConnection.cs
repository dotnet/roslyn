// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Abstracts a connection to a legacy remote service.
    /// </summary>
    internal abstract class RemoteServiceConnection : IDisposable
    {
        public abstract void Dispose();

        public abstract Task RunRemoteAsync(string targetName, Solution? solution, IReadOnlyList<object?> arguments, CancellationToken cancellationToken);
        public abstract Task<T> RunRemoteAsync<T>(string targetName, Solution? solution, IReadOnlyList<object?> arguments, Func<Stream, CancellationToken, Task<T>>? dataReader, CancellationToken cancellationToken);

        public Task<T> RunRemoteAsync<T>(string targetName, Solution? solution, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
            => RunRemoteAsync<T>(targetName, solution, arguments, dataReader: null, cancellationToken);
    }

    /// <summary>
    /// Abstracts a connection to a service implementing type <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">Remote interface type of the service.</typeparam>
    internal abstract class RemoteServiceConnection<TService> : IDisposable
        where TService : class
    {
        public abstract void Dispose();

        public abstract ValueTask<bool> TryInvokeAsync(
            Func<TService, CancellationToken, ValueTask> invocation,
            CancellationToken cancellationToken);

        public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
            Func<TService, CancellationToken, ValueTask<TResult>> invocation,
            CancellationToken cancellationToken);

        public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
            Func<TService, PipeWriter, CancellationToken, ValueTask> invocation,
            Func<PipeReader, CancellationToken, ValueTask<TResult>> reader,
            CancellationToken cancellationToken);

        public abstract ValueTask<bool> TryInvokeAsync(
            Solution solution,
            Func<TService, PinnedSolutionInfo, CancellationToken, ValueTask> invocation,
            CancellationToken cancellationToken);

        public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
            Solution solution,
            Func<TService, PinnedSolutionInfo, CancellationToken, ValueTask<TResult>> invocation,
            CancellationToken cancellationToken);

        public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
            Solution solution,
            Func<TService, PinnedSolutionInfo, PipeWriter, CancellationToken, ValueTask> invocation,
            Func<PipeReader, CancellationToken, ValueTask<TResult>> reader,
            CancellationToken cancellationToken);
    }
}
