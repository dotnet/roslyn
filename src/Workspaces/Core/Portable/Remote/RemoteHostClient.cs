﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// This represents client in client/server model.
    /// 
    /// user can create a connection to communicate with the server (remote host) through this client
    /// </summary>
    internal abstract class RemoteHostClient : IDisposable
    {
        public event EventHandler<bool>? StatusChanged;

        protected void Started()
        {
            OnStatusChanged(started: true);
        }

        public virtual void Dispose()
            => OnStatusChanged(started: false);

        private void OnStatusChanged(bool started)
            => StatusChanged?.Invoke(this, started);

        public static Task<RemoteHostClient?> TryGetClientAsync(Project project, CancellationToken cancellationToken)
        {
            if (!RemoteSupportedLanguages.IsSupported(project.Language))
            {
                return SpecializedTasks.Null<RemoteHostClient>();
            }

            return TryGetClientAsync(project.Solution.Workspace, cancellationToken);
        }

        public static Task<RemoteHostClient?> TryGetClientAsync(Workspace workspace, CancellationToken cancellationToken)
            => TryGetClientAsync(workspace.Services, cancellationToken);

        public static Task<RemoteHostClient?> TryGetClientAsync(HostWorkspaceServices services, CancellationToken cancellationToken)
        {
            var service = services.GetService<IRemoteHostClientProvider>();
            if (service == null)
            {
                return SpecializedTasks.Null<RemoteHostClient>();
            }

            return service.TryGetRemoteHostClientAsync(cancellationToken);
        }

        public abstract RemoteServiceConnection<T> CreateConnection<T>(object? callbackTarget)
            where T : class;

        // no solution, no callback:

        public async ValueTask<bool> TryInvokeAsync<TService>(
            Func<TService, CancellationToken, ValueTask> invocation,
            CancellationToken cancellationToken)
            where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget: null);
            return await connection.TryInvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(
            Func<TService, CancellationToken, ValueTask<TResult>> invocation,
            CancellationToken cancellationToken)
            where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget: null);
            return await connection.TryInvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
        }

        // no solution, callback:

        public async ValueTask<bool> TryInvokeAsync<TService>(
            Func<TService, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation,
            object callbackTarget,
            CancellationToken cancellationToken)
            where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget);
            return await connection.TryInvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(
            Func<TService, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation,
            object callbackTarget,
            CancellationToken cancellationToken)
            where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget);
            return await connection.TryInvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
        }

        // solution, no callback:

        public async ValueTask<bool> TryInvokeAsync<TService>(
            Solution solution,
            Func<TService, PinnedSolutionInfo, CancellationToken, ValueTask> invocation,
            CancellationToken cancellationToken)
            where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget: null);
            return await connection.TryInvokeAsync(solution, invocation, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(
            Solution solution,
            Func<TService, PinnedSolutionInfo, CancellationToken, ValueTask<TResult>> invocation,
            CancellationToken cancellationToken)
            where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget: null);
            return await connection.TryInvokeAsync(solution, invocation, cancellationToken).ConfigureAwait(false);
        }

        // project, no callback.

        /// <summary>
        /// Equivalent to <see cref="TryInvokeAsync{TService}(Solution, Func{TService, PinnedSolutionInfo, CancellationToken, ValueTask}, CancellationToken)"/>
        /// except that only the project (and its dependent projects) will be sync'ed to the remote host before executing.
        /// This is useful for operations that don't every do any work outside of that project-cone and do not want to pay
        /// the high potential cost of a full sync.
        /// </summary>
        public async ValueTask<bool> TryInvokeAsync<TService>(
            Project project,
            Func<TService, PinnedSolutionInfo, CancellationToken, ValueTask> invocation,
            CancellationToken cancellationToken)
            where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget: null);
            return await connection.TryInvokeAsync(project, invocation, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Equivalent to <see cref="TryInvokeAsync{TService}(Solution, Func{TService, PinnedSolutionInfo, CancellationToken, ValueTask}, CancellationToken)"/>
        /// except that only the project (and its dependent projects) will be sync'ed to the remote host before executing.
        /// This is useful for operations that don't every do any work outside of that project-cone and do not want to pay
        /// the high potential cost of a full sync.
        /// </summary>
        public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(
            Project project,
            Func<TService, PinnedSolutionInfo, CancellationToken, ValueTask<TResult>> invocation,
            CancellationToken cancellationToken)
            where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget: null);
            return await connection.TryInvokeAsync(project, invocation, cancellationToken).ConfigureAwait(false);
        }

        // solution, callback:

        public async ValueTask<bool> TryInvokeAsync<TService>(
            Solution solution,
            Func<TService, PinnedSolutionInfo, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation,
            object callbackTarget,
            CancellationToken cancellationToken)
            where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget);
            return await connection.TryInvokeAsync(solution, invocation, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(
            Solution solution,
            Func<TService, PinnedSolutionInfo, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation,
            object callbackTarget,
            CancellationToken cancellationToken)
            where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget);
            return await connection.TryInvokeAsync(solution, invocation, cancellationToken).ConfigureAwait(false);
        }

        // project, callback:

        /// <summary>
        /// Equivalent to <see cref="TryInvokeAsync{TService}(Solution, Func{TService, PinnedSolutionInfo, RemoteServiceCallbackId, CancellationToken, ValueTask}, object, CancellationToken)"/>
        /// except that only the project (and its dependent projects) will be sync'ed to the remote host before executing.
        /// This is useful for operations that don't every do any work outside of that project-cone and do not want to pay
        /// the high potential cost of a full sync.
        /// </summary>
        public async ValueTask<bool> TryInvokeAsync<TService>(
            Project project,
            Func<TService, PinnedSolutionInfo, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation,
            object callbackTarget,
            CancellationToken cancellationToken)
            where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget);
            return await connection.TryInvokeAsync(project, invocation, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Equivalent to <see cref="TryInvokeAsync{TService}(Solution, Func{TService, PinnedSolutionInfo, RemoteServiceCallbackId, CancellationToken, ValueTask}, object, CancellationToken)"/>
        /// except that only the project (and its dependent projects) will be sync'ed to the remote host before executing.
        /// This is useful for operations that don't every do any work outside of that project-cone and do not want to pay
        /// the high potential cost of a full sync.
        /// </summary>
        public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(
            Project project,
            Func<TService, PinnedSolutionInfo, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation,
            object callbackTarget,
            CancellationToken cancellationToken)
            where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget);
            return await connection.TryInvokeAsync(project, invocation, cancellationToken).ConfigureAwait(false);
        }

        // streaming

        /// <summary>
        /// Invokes a remote API that streams data back to the caller via a pipe.
        /// </summary>
        public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(
            Solution solution,
            Func<TService, PinnedSolutionInfo, PipeWriter, CancellationToken, ValueTask> invocation,
            Func<PipeReader, CancellationToken, ValueTask<TResult>> reader,
            CancellationToken cancellationToken)
            where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget: null);
            return await connection.TryInvokeAsync(solution, invocation, reader, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Equivalent to <see cref="TryInvokeAsync{TService, TResult}(Project, Func{TService, PinnedSolutionInfo, PipeWriter, CancellationToken, ValueTask}, Func{PipeReader, CancellationToken, ValueTask{TResult}}, CancellationToken)"/>
        /// except that only the project (and its dependent projects) will be sync'ed to the remote host before executing.
        /// This is useful for operations that don't every do any work outside of that project-cone and do not want to pay
        /// the high potential cost of a full sync.
        /// </summary>
        public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(
            Project project,
            Func<TService, PinnedSolutionInfo, PipeWriter, CancellationToken, ValueTask> invocation,
            Func<PipeReader, CancellationToken, ValueTask<TResult>> reader,
            CancellationToken cancellationToken)
            where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget: null);
            return await connection.TryInvokeAsync(project, invocation, reader, cancellationToken).ConfigureAwait(false);
        }
    }
}
