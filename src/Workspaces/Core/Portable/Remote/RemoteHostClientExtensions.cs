// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteHostClientExtensions
    {
        /// <summary>
        /// Creates <see cref="SessionWithSolution"/> for the <paramref name="serviceName"/> if possible, otherwise returns <see langword="null"/>.
        /// </summary>
        public static async Task<SessionWithSolution?> TryCreateSessionAsync(
            this RemoteHostClient client, string serviceName, Solution solution, CancellationToken cancellationToken, object? callbackTarget = null)
        {
            var connection = await client.TryCreateConnectionAsync(serviceName, callbackTarget, cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return null;
            }

            return await SessionWithSolution.CreateAsync(connection, solution, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates <see cref="KeepAliveSession"/> for the <paramref name="serviceName"/>, otherwise returns <see langword="null"/>.
        /// </summary>
        public static async Task<KeepAliveSession?> TryCreateKeepAliveSessionAsync(
            this RemoteHostClient client, string serviceName, CancellationToken cancellationToken, object? callbackTarget = null)
        {
            var connection = await client.TryCreateConnectionAsync(serviceName, callbackTarget, cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return null;
            }

            return new KeepAliveSession(client, connection, serviceName, callbackTarget);
        }

        public static async Task<bool> TryRunRemoteAsync(
            this RemoteHostClient client, string serviceName, string targetName, Solution solution,
            IReadOnlyList<object> arguments, CancellationToken cancellationToken, object? callbackTarget = null)
        {
            using var session = await client.TryCreateSessionAsync(serviceName, solution, cancellationToken, callbackTarget).ConfigureAwait(false);
            if (session == null)
            {
                return false;
            }

            await session.InvokeAsync(targetName, arguments, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public static async Task<bool> TryRunRemoteAsync(
            this RemoteHostClient client, string serviceName, string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken, object? callbackTarget = null)
        {
            using var connection = await client.TryCreateConnectionAsync(serviceName, callbackTarget, cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return false;
            }

            await connection.InvokeAsync(targetName, arguments, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public static async Task<Optional<T>> TryRunRemoteAsync<T>(
            this RemoteHostClient client, string serviceName, string targetName, Solution solution, IReadOnlyList<object> arguments, CancellationToken cancellationToken, object? callbackTarget = null)
        {
            using var session = await client.TryCreateSessionAsync(serviceName, solution, cancellationToken, callbackTarget).ConfigureAwait(false);
            if (session == null)
            {
                return default;
            }

            return await session.InvokeAsync<T>(targetName, arguments, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronize given solution as primary workspace solution in remote host
        /// </summary>
        public static async Task SynchronizePrimaryWorkspaceAsync(this Workspace workspace, Solution solution, CancellationToken cancellationToken)
        {
            if (solution.BranchId != solution.Workspace.PrimaryBranchId)
            {
                return;
            }

            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return;
            }

            using (Logger.LogBlock(FunctionId.SolutionChecksumUpdater_SynchronizePrimaryWorkspace, cancellationToken))
            {
                var checksum = await solution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);

                _ = await client.TryRunRemoteAsync(
                    WellKnownRemoteHostServices.RemoteHostService,
                    nameof(IRemoteHostService.SynchronizePrimaryWorkspaceAsync),
                    solution,
                    new object[] { checksum, solution.WorkspaceVersion },
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
