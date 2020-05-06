// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// This will let one to hold onto <see cref="RemoteHostClient.Connection"/> for a while.
    /// this helper will let you not care about remote host being gone while you hold onto the connection if that ever happen
    /// 
    /// when this is used, solution must be explicitly passed around between client (VS) and remote host (OOP)
    /// </summary>
    internal sealed class KeepAliveSession : IDisposable
    {
        private readonly IRemotableDataService _remotableDataService;
        private readonly RemoteHostClient.Connection _connection;

        public KeepAliveSession(RemoteHostClient.Connection connection, IRemotableDataService remotableDataService)
        {
            _remotableDataService = remotableDataService;
            _connection = connection;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public Task RunRemoteAsync(string targetName, Solution? solution, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
            => RemoteHostClient.RunRemoteAsync(_connection, _remotableDataService, targetName, solution, arguments, cancellationToken);

        public Task<T> RunRemoteAsync<T>(string targetName, Solution? solution, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
            => RemoteHostClient.RunRemoteAsync<T>(_connection, _remotableDataService, targetName, solution, arguments, dataReader: null, cancellationToken);
    }
}
