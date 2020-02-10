// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed partial class ServiceHubRemoteHostClient
    {
        private partial class ConnectionManager
        {
            private class PooledConnection : Connection
            {
                private readonly ConnectionManager _connectionManager;
                private readonly string _serviceName;
                private readonly JsonRpcConnection _connection;

                public PooledConnection(ConnectionManager pools, string serviceName, JsonRpcConnection connection)
                {
                    _connectionManager = pools;
                    _serviceName = serviceName;
                    _connection = connection;
                }

                public override Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
                    => _connection.InvokeAsync(targetName, arguments, cancellationToken);

                public override Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
                    => _connection.InvokeAsync<T>(targetName, arguments, cancellationToken);

                public override Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task<T>> dataReader, CancellationToken cancellationToken)
                    => _connection.InvokeAsync(targetName, arguments, dataReader, cancellationToken);

                protected override void DisposeImpl()
                {
                    _connectionManager.Free(_serviceName, _connection);
                    base.DisposeImpl();
                }
            }
        }
    }
}
