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

        public Task<Session> CreateServiceSessionAsync(string serviceName, Solution solution, CancellationToken cancellationToken)
        {
            return CreateServiceSessionAsync(serviceName, solution, callbackTarget: null, cancellationToken: cancellationToken);
        }

        public async Task<Session> CreateServiceSessionAsync(string serviceName, Solution solution, object callbackTarget, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(solution.Workspace == _workspace);

            var service = _workspace.Services.GetService<ISolutionChecksumService>();
            var snapshot = await service.CreateChecksumAsync(solution, cancellationToken).ConfigureAwait(false);

            return await CreateServiceSessionAsync(serviceName, snapshot, callbackTarget, cancellationToken).ConfigureAwait(false);
        }

        protected abstract void OnConnected();

        protected abstract void OnDisconnected();

        protected abstract Task<Session> CreateServiceSessionAsync(string serviceName, ChecksumScope snapshot, object callbackTarget, CancellationToken cancellationToken);

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

        // TODO: make this to not exposed to caller. abstract all of these under Request and Response mechanism
        public abstract class Session : IDisposable
        {
            protected readonly ChecksumScope ChecksumScope;
            protected readonly CancellationToken CancellationToken;

            private bool _disposed;

            protected Session(ChecksumScope scope, CancellationToken cancellationToken)
            {
                _disposed = false;

                ChecksumScope = scope;
                CancellationToken = cancellationToken;
            }

            public abstract Task InvokeAsync(string targetName, params object[] arguments);

            public abstract Task<T> InvokeAsync<T>(string targetName, params object[] arguments);

            public abstract Task InvokeAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync);

            public abstract Task<T> InvokeAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync);

            public void AddAdditionalAssets(Asset asset)
            {
                ChecksumScope.AddAdditionalAsset(asset, CancellationToken);
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

                ChecksumScope.Dispose();
            }
        }
    }
}
