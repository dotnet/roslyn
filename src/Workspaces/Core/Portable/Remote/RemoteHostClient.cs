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
    internal abstract partial class RemoteHostClient
    {
        public readonly Workspace Workspace;

        protected RemoteHostClient(Workspace workspace)
        {
            Workspace = workspace;
        }

        public event EventHandler<bool> ConnectionChanged;

        /// <summary>
        /// Create <see cref="RemoteHostClient.Session"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating session could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public abstract Task<Session> TryCreateSessionAsync(string serviceName, object callbackTarget, CancellationToken cancellationToken);

        protected abstract void OnConnected();

        protected abstract void OnDisconnected();

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

        /// <summary>
        /// NoOpClient is used if a user killed our remote host process. Basically this client never
        /// create a session
        /// </summary>
        public class NoOpClient : RemoteHostClient
        {
            public NoOpClient(Workspace workspace) :
                base(workspace)
            {
            }

            public override Task<Session> TryCreateSessionAsync(string serviceName, object callbackTarget, CancellationToken cancellationToken)
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

        public abstract class Session : IDisposable
        {
            protected readonly CancellationToken CancellationToken;
            private PinnedRemotableDataScope _scope;

            private bool _disposed;

            protected Session(CancellationToken cancellationToken)
            {
                _disposed = false;
                _scope = null;

                CancellationToken = cancellationToken;
            }

            public PinnedRemotableDataScope PinnedRemotableDataScope => _scope;

            public virtual Task RegisterPinnedRemotableDataScopeAsync(PinnedRemotableDataScope scope)
            {
                // make sure all thread can read the info
                Interlocked.Exchange(ref _scope, scope);
                return SpecializedTasks.EmptyTask;
            }

            public abstract Task InvokeAsync(string targetName, params object[] arguments);

            public abstract Task<T> InvokeAsync<T>(string targetName, params object[] arguments);

            public abstract Task InvokeAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync);

            public abstract Task<T> InvokeAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync);

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
            }
        }
    }
}
