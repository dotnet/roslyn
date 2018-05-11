// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed partial class ServiceHubRemoteHostClient : RemoteHostClient
    {
        private partial class ConnectionManager
        {
            private class PooledConnection : Connection
            {
                private readonly ConnectionManager _connectionManager;
                private readonly string _serviceName;
                private OwnedDisposable<Connection> _connection;

                public PooledConnection(ConnectionManager pools, string serviceName, ref OwnedDisposable<Connection> connection)
                {
                    _connectionManager = pools;
                    _serviceName = serviceName;
                    _connection = connection;
                }

                public override Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken) =>
                    _connection.Target.InvokeAsync(targetName, arguments, cancellationToken);

                public override Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken) =>
                    _connection.Target.InvokeAsync<T>(targetName, arguments, cancellationToken);

                public override Task InvokeAsync(
                    string targetName, IReadOnlyList<object> arguments,
                    Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken) =>
                    _connection.Target.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync, cancellationToken);

                public override Task<T> InvokeAsync<T>(
                    string targetName, IReadOnlyList<object> arguments,
                    Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken) =>
                    _connection.Target.InvokeAsync<T>(targetName, arguments, funcWithDirectStreamAsync, cancellationToken);

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        _connectionManager.Free(_serviceName, ref _connection);
                    }

                    base.Dispose(disposing);
                }
            }
        }
    }
}
