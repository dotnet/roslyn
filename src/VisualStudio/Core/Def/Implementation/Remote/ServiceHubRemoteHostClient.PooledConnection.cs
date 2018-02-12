// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed partial class ServiceHubRemoteHostClient : RemoteHostClient
    {
        private partial class ConnectionManager
        {
            private class PooledConnection : Connection
            {
                private readonly ConnectionManager _pools;
                private readonly string _serviceName;
                private readonly JsonRpcConnection _connection;

                public PooledConnection(ConnectionManager pools, string serviceName, JsonRpcConnection connection)
                {
                    _pools = pools;
                    _serviceName = serviceName;
                    _connection = connection;
                }

                public override Task SetConnectionStateAsync(PinnedRemotableDataScope scope) =>
                    _connection.SetConnectionStateAsync(scope);

                public override Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken) =>
                    _connection.InvokeAsync(targetName, arguments, cancellationToken);

                public override Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken) =>
                    _connection.InvokeAsync<T>(targetName, arguments, cancellationToken);

                public override Task InvokeAsync(
                    string targetName, IReadOnlyList<object> arguments,
                    Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken) =>
                    _connection.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync, cancellationToken);

                public override Task<T> InvokeAsync<T>(
                    string targetName, IReadOnlyList<object> arguments,
                    Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken) =>
                    _connection.InvokeAsync<T>(targetName, arguments, funcWithDirectStreamAsync, cancellationToken);

                protected override void OnDisposed() =>
                    _pools.Free(_serviceName, _connection);

            }
        }
    }
}
