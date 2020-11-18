// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// This represents client in client/server model.
    /// 
    /// user can create a connection to communicate with the server (remote host) through this client
    /// </summary>
    internal abstract class RemoteHostClient : IDisposable
    {
        public event EventHandler<bool>? StatusChanged;

        protected void Started()
        {
            OnStatusChanged(started: true);
        }

        public virtual void Dispose()
            => OnStatusChanged(started: false);

        private void OnStatusChanged(bool started)
            => StatusChanged?.Invoke(this, started);

        public static Task<RemoteHostClient?> TryGetClientAsync(Project project, CancellationToken cancellationToken)
        {
            if (!RemoteSupportedLanguages.IsSupported(project.Language))
            {
                return SpecializedTasks.Null<RemoteHostClient>();
            }

            return TryGetClientAsync(project.Solution.Workspace, cancellationToken);
        }

        public static Task<RemoteHostClient?> TryGetClientAsync(Workspace workspace, CancellationToken cancellationToken)
            => TryGetClientAsync(workspace.Services, cancellationToken);

        public static Task<RemoteHostClient?> TryGetClientAsync(HostWorkspaceServices services, CancellationToken cancellationToken)
        {
            var service = services.GetService<IRemoteHostClientProvider>();
            if (service == null)
            {
                return SpecializedTasks.Null<RemoteHostClient>();
            }

            return service.TryGetRemoteHostClientAsync(cancellationToken);
        }

        public abstract Task<RemoteServiceConnection> CreateConnectionAsync(RemoteServiceName serviceName, object? callbackTarget, CancellationToken cancellationToken);

        public async Task RunRemoteAsync(RemoteServiceName serviceName, string targetName, Solution? solution, IReadOnlyList<object?> arguments, object? callbackTarget, CancellationToken cancellationToken)
        {
            using var connection = await CreateConnectionAsync(serviceName, callbackTarget, cancellationToken).ConfigureAwait(false);
            await connection.RunRemoteAsync(targetName, solution, arguments, cancellationToken).ConfigureAwait(false);
        }

        public Task<T> RunRemoteAsync<T>(RemoteServiceName serviceName, string targetName, Solution? solution, IReadOnlyList<object?> arguments, object? callbackTarget, CancellationToken cancellationToken)
            => RunRemoteAsync<T>(serviceName, targetName, solution, arguments, callbackTarget, dataReader: null, cancellationToken);

        public async Task<T> RunRemoteAsync<T>(RemoteServiceName serviceName, string targetName, Solution? solution, IReadOnlyList<object?> arguments, object? callbackTarget, Func<Stream, CancellationToken, Task<T>>? dataReader, CancellationToken cancellationToken)
        {
            using var connection = await CreateConnectionAsync(serviceName, callbackTarget, cancellationToken).ConfigureAwait(false);
            return await connection.RunRemoteAsync(targetName, solution, arguments, dataReader, cancellationToken).ConfigureAwait(false);
        }
    }
}
