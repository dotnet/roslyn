// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        /// Return an unique string per client.
        /// 
        /// one can use this to distinguish different clients that are connected to different RemoteHosts including
        /// cases where 2 external process finding each others
        /// </summary>
        public abstract string ClientId { get; }

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

        public static string CreateClientId(string prefix)
        {
            return $"VS ({prefix}) ({Guid.NewGuid().ToString()})";
        }

        /// <summary>
        /// NoOpClient is used if a user killed our remote host process. Basically this client never
        /// create a session
        /// </summary>
        public class NoOpClient : RemoteHostClient
        {
            public NoOpClient(Workspace workspace)
                : base(workspace)
            {
            }

            public override string ClientId => nameof(NoOpClient);

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
            private bool _disposed;

            protected Connection()
            {
#if DEBUG
                _creationCallStack = Environment.StackTrace;
#endif
                _disposed = false;
            }

            public abstract Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken);
            public abstract Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken);
            public abstract Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken);
            public abstract Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken);

            protected virtual void Dispose(bool disposing)
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

                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

#if DEBUG
            private readonly string _creationCallStack;

            ~Connection()
            {
                // this can happen if someone kills OOP. 
                // when that happen, we don't want to crash VS, so this is debug only check
                if (!Environment.HasShutdownStarted)
                {
                    Debug.Assert(false,
                        $"Unless OOP process (RoslynCodeAnalysisService) is explicitly killed, this should have been disposed!\r\n {_creationCallStack}");
                }
            }
#endif
        }
    }
}
