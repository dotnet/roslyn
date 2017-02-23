﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            var service = _workspace.Services.GetService<ISolutionSynchronizationService>();
            var snapshot = await service.CreatePinnedRemotableDataScopeAsync(solution, cancellationToken).ConfigureAwait(false);

            return await CreateServiceSessionAsync(serviceName, snapshot, callbackTarget, cancellationToken).ConfigureAwait(false);
        }

        protected abstract void OnConnected();

        protected abstract void OnDisconnected();

        protected abstract Task<Session> CreateServiceSessionAsync(string serviceName, PinnedRemotableDataScope snapshot, object callbackTarget, CancellationToken cancellationToken);

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
            protected readonly PinnedRemotableDataScope PinnedScope;
            protected readonly CancellationToken CancellationToken;

            private bool _disposed;

            protected Session(PinnedRemotableDataScope scope, CancellationToken cancellationToken)
            {
                _disposed = false;

                PinnedScope = scope;
                CancellationToken = cancellationToken;
            }

            public abstract Task InvokeAsync(string targetName, params object[] arguments);

            /// <summary>
            /// All caller must guard itself from it returning default(T) which can happen if OOP is killed
            /// unintentionally such as user killed OOP process.
            /// </summary>
            public abstract Task<T> InvokeAsync<T>(string targetName, params object[] arguments);

            public abstract Task InvokeAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync);

            /// <summary>
            /// All caller must guard itself from it returning default(T) which can happen if OOP is killed
            /// unintentionally such as user killed OOP process.
            /// </summary>
            public abstract Task<T> InvokeAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync);

            public void AddAdditionalAssets(CustomAsset asset)
            {
                PinnedScope.AddAdditionalAsset(asset, CancellationToken);
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

                PinnedScope.Dispose();
            }
        }

        public class NoOpClient : RemoteHostClient
        {
            public NoOpClient(Workspace workspace) :
                base(workspace)
            {
            }

            protected override Task<Session> CreateServiceSessionAsync(
                string serviceName, PinnedRemotableDataScope snapshot, object callbackTarget, CancellationToken cancellationToken)
            {
                return Task.FromResult<Session>(new NoOpSession(snapshot, cancellationToken));
            }

            protected override void OnConnected()
            {
                // do nothing
            }

            protected override void OnDisconnected()
            {
                // do nothing
            }

            private class NoOpSession : Session
            {
                public NoOpSession(PinnedRemotableDataScope scope, CancellationToken cancellationToken) :
                    base(scope, cancellationToken)
                {
                }

                public override Task InvokeAsync(string targetName, params object[] arguments)
                {
                    return SpecializedTasks.EmptyTask;
                }

                public override Task<T> InvokeAsync<T>(string targetName, params object[] arguments)
                {
                    return SpecializedTasks.Default<T>();
                }

                public override Task InvokeAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync)
                {
                    return SpecializedTasks.EmptyTask;
                }

                public override Task<T> InvokeAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync)
                {
                    return SpecializedTasks.Default<T>();
                }
            }
        }
    }
}
