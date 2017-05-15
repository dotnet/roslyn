// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// This lets users create a session to communicate with remote host (i.e. ServiceHub)
    /// </summary>
    internal abstract class RemoteHostClient
    {
        private readonly Workspace _workspace;

        protected RemoteHostClient(Workspace workspace)
        {
            _workspace = workspace;
        }

        public event EventHandler<bool> ConnectionChanged;

        /// <summary>
        /// Create <see cref="RemoteHostClient.Session"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating session could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public Task<Session> TryCreateServiceSessionAsync(string serviceName, CancellationToken cancellationToken)
        {
            return TryCreateServiceSessionAsync(serviceName, callbackTarget: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Create <see cref="RemoteHostClient.Session"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating session could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public Task<Session> TryCreateServiceSessionAsync(string serviceName, object callbackTarget, CancellationToken cancellationToken)
        {
            return TryCreateServiceSessionAsync(serviceName, getSnapshotAsync: null, callbackTarget: callbackTarget, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Create <see cref="RemoteHostClient.Session"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating session could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public Task<Session> TryCreateServiceSessionAsync(string serviceName, Solution solution, CancellationToken cancellationToken)
        {
            return TryCreateServiceSessionAsync(serviceName, solution, callbackTarget: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Create <see cref="RemoteHostClient.Session"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating session could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public async Task<Session> TryCreateServiceSessionAsync(string serviceName, Solution solution, object callbackTarget, CancellationToken cancellationToken)
        {
            Func<CancellationToken, Task<PinnedRemotableDataScope>> getSnapshotAsync = ct => GetPinnedScopeAsync(solution, ct);
            return await TryCreateServiceSessionAsync(serviceName, getSnapshotAsync, callbackTarget, cancellationToken).ConfigureAwait(false);
        }

        protected abstract void OnConnected();

        protected abstract void OnDisconnected();

        protected abstract Task<Session> TryCreateServiceSessionAsync(string serviceName, Optional<Func<CancellationToken, Task<PinnedRemotableDataScope>>> getSnapshotAsync, object callbackTarget, CancellationToken cancellationToken);

        internal void Shutdown()
        {
            // this should be only used by RemoteHostService to shutdown this remote host
            Disconnected();
        }

        protected void Connected()
        {
            OnConnected();

            OnConnectionChanged(true);
        }

        protected void Disconnected()
        {
            OnDisconnected();

            OnConnectionChanged(false);
        }

        private void OnConnectionChanged(bool connected)
        {
            ConnectionChanged?.Invoke(this, connected);
        }

        private async Task<PinnedRemotableDataScope> GetPinnedScopeAsync(Solution solution, CancellationToken cancellationToken)
        {
            if (solution == null)
            {
                return null;
            }

            Contract.ThrowIfFalse(solution.Workspace == _workspace);

            var service = _workspace.Services.GetService<ISolutionSynchronizationService>();
            return await service.CreatePinnedRemotableDataScopeAsync(solution, cancellationToken).ConfigureAwait(false);
        }

        // TODO: make this to not exposed to caller. abstract all of these under Request and Response mechanism
        public abstract class Session : IDisposable
        {
            protected readonly PinnedRemotableDataScope PinnedScopeOpt;
            protected readonly CancellationToken CancellationToken;

            private bool _disposed;

            protected Session(PinnedRemotableDataScope scope, CancellationToken cancellationToken)
            {
                _disposed = false;

                PinnedScopeOpt = scope;
                CancellationToken = cancellationToken;
            }

            public abstract Task InvokeAsync(string targetName, params object[] arguments);

            public abstract Task<T> InvokeAsync<T>(string targetName, params object[] arguments);

            public abstract Task InvokeAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync);

            public abstract Task<T> InvokeAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync);

            public void AddAdditionalAssets(CustomAsset asset)
            {
                Contract.ThrowIfNull(PinnedScopeOpt);
                PinnedScopeOpt.AddAdditionalAsset(asset, CancellationToken);
            }

            protected virtual void OnDisposed()
            {
                // do nothing
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                OnDisposed();

                PinnedScopeOpt?.Dispose();
            }
        }

        public class NoOpClient : RemoteHostClient
        {
            public NoOpClient(Workspace workspace) :
                base(workspace)
            {
            }

            protected override Task<Session> TryCreateServiceSessionAsync(
                string serviceName, Optional<Func<CancellationToken, Task<PinnedRemotableDataScope>>> getSnapshotAsync, object callbackTarget, CancellationToken cancellationToken)
            {
                return SpecializedTasks.Default<Session>();
            }

            protected override void OnConnected()
            {
                // do nothing
            }

            protected override void OnDisconnected()
            {
                // do nothing
            }
        }
    }
}
