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
    /// Abstracts a connection to a service implementing type <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">Remote interface type of the service.</typeparam>
    internal abstract class RemoteServiceConnection<TService> : IDisposable
        where TService : class
    {
        public abstract void Dispose();

        // no solution, no callback

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

        // no solution, callback

        public abstract ValueTask<bool> TryInvokeAsync(
            Func<TService, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation,
            CancellationToken cancellationToken);

        public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
            Func<TService, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation,
            CancellationToken cancellationToken);

        // solution, no callback

        public abstract ValueTask<bool> TryInvokeAsync(
            Solution solution,
            Func<TService, PinnedSolutionInfo, CancellationToken, ValueTask> invocation,
            CancellationToken cancellationToken);

        public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
            Solution solution,
            Func<TService, PinnedSolutionInfo, CancellationToken, ValueTask<TResult>> invocation,
            CancellationToken cancellationToken);

        // project, no callback

        public abstract ValueTask<bool> TryInvokeAsync(
            Project project,
            Func<TService, PinnedSolutionInfo, CancellationToken, ValueTask> invocation,
            CancellationToken cancellationToken);

        public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
            Project project,
            Func<TService, PinnedSolutionInfo, CancellationToken, ValueTask<TResult>> invocation,
            CancellationToken cancellationToken);

        // solution, callback

        public abstract ValueTask<bool> TryInvokeAsync(
            Solution solution,
            Func<TService, PinnedSolutionInfo, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation,
            CancellationToken cancellationToken);

        public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
            Solution solution,
            Func<TService, PinnedSolutionInfo, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation,
            CancellationToken cancellationToken);

        // project, callback

        public abstract ValueTask<bool> TryInvokeAsync(
            Project project,
            Func<TService, PinnedSolutionInfo, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation,
            CancellationToken cancellationToken);

        public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
            Project project,
            Func<TService, PinnedSolutionInfo, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation,
            CancellationToken cancellationToken);

        // streaming

        public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
            Solution solution,
            Func<TService, PinnedSolutionInfo, PipeWriter, CancellationToken, ValueTask> invocation,
            Func<PipeReader, CancellationToken, ValueTask<TResult>> reader,
            CancellationToken cancellationToken);

        public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
            Project project,
            Func<TService, PinnedSolutionInfo, PipeWriter, CancellationToken, ValueTask> invocation,
            Func<PipeReader, CancellationToken, ValueTask<TResult>> reader,
            CancellationToken cancellationToken);
    }
}
