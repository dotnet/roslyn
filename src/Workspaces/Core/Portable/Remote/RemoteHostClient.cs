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
    /// This represents client in client/servier model.
    /// 
    /// user can create a connection to communicate with the server (remote host) through this client
    /// </summary>
    internal abstract partial class RemoteHostClient
    {
        public readonly Workspace Workspace;

        protected RemoteHostClient(Workspace workspace)
        {
            Workspace = workspace;
        }

        public event EventHandler<bool> StatusChanged;

        /// <summary>
        /// Create <see cref="RemoteHostClient.Connection"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating session could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public abstract Task<Connection> TryCreateConnectionAsync(string serviceName, object callbackTarget, CancellationToken cancellationToken);

        protected abstract void OnStarted();

        protected abstract void OnStopped();

        internal void Shutdown()
        {
            // this should be only used by RemoteHostService to shutdown this remote host
            Stopped();
        }

        protected void Started()
        {
            OnStarted();

            OnStatusChanged(true);
        }

        protected void Stopped()
        {
            OnStopped();

            OnStatusChanged(false);
        }

        private void OnStatusChanged(bool started)
        {
            StatusChanged?.Invoke(this, started);
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

            public override Task<Connection> TryCreateConnectionAsync(string serviceName, object callbackTarget, CancellationToken cancellationToken)
            {
                return SpecializedTasks.Default<Connection>();
            }

            protected override void OnStarted()
            {
                // do nothing
            }

            protected override void OnStopped()
            {
                // do nothing
            }
        }

        /// <summary>
        /// This is a connection between client and server. user can use this to communicate with remote host.
        /// 
        /// This doesn't know anything specific to Roslyn. this is general pure connection between client and server.
        /// </summary>
        public abstract class Connection : IDisposable
        {
            // this will be removed soon.
            protected readonly CancellationToken CancellationToken;

            // scope will be also removed from the connection
            private readonly object _gate;
            private PinnedRemotableDataScope _scopeDoNotAccessDirectly;

            private bool _disposed;

            protected Connection(CancellationToken cancellationToken)
            {
                _gate = new object();

                _disposed = false;
                _scopeDoNotAccessDirectly = null;

                CancellationToken = cancellationToken;
            }

            protected abstract Task OnRegisterPinnedRemotableDataScopeAsync(PinnedRemotableDataScope scope);

            public PinnedRemotableDataScope PinnedRemotableDataScope
            {
                get
                {
                    lock (_gate)
                    {
                        return _scopeDoNotAccessDirectly;
                    }
                }
                set
                {
                    lock (_gate)
                    {
                        _scopeDoNotAccessDirectly = value;
                    }
                }
            }

            public virtual Task RegisterPinnedRemotableDataScopeAsync(PinnedRemotableDataScope scope)
            {
                // make sure all thread can read the info
                PinnedRemotableDataScope = scope;

                return OnRegisterPinnedRemotableDataScopeAsync(scope);
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
