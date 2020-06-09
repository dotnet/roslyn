// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingRemoteHostClientWrapper
    {
        internal UnitTestingRemoteHostClientWrapper(RemoteHostClient? underlyingObject)
            => UnderlyingObject = underlyingObject;

        internal RemoteHostClient? UnderlyingObject { get; }

        public bool IsDefault => UnderlyingObject == null;

        public static async Task<UnitTestingRemoteHostClientWrapper?> TryGetClientAsync(HostWorkspaceServices services, CancellationToken cancellationToken = default)
            => new UnitTestingRemoteHostClientWrapper(await RemoteHostClient.TryGetClientAsync(services, cancellationToken).ConfigureAwait(false));

        public async Task<bool> TryRunRemoteAsync(UnitTestingServiceHubService service, string targetName, Solution? solution, IReadOnlyList<object?> arguments, object? callbackTarget, CancellationToken cancellationToken)
        {
            await UnderlyingObject!.RunRemoteAsync((WellKnownServiceHubService)service, targetName, solution, arguments, callbackTarget, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public async Task<Optional<T>> TryRunRemoteAsync<T>(UnitTestingServiceHubService service, string targetName, Solution? solution, IReadOnlyList<object?> arguments, object? callbackTarget, CancellationToken cancellationToken)
            => await UnderlyingObject!.RunRemoteAsync<T>((WellKnownServiceHubService)service, targetName, solution, arguments, callbackTarget, cancellationToken).ConfigureAwait(false);

        public async Task<UnitTestingRemoteServiceConnectionWrapper> CreateConnectionAsync(UnitTestingServiceHubService service, object? callbackTarget, CancellationToken cancellationToken)
            => new UnitTestingRemoteServiceConnectionWrapper(await UnderlyingObject!.CreateConnectionAsync((WellKnownServiceHubService)service, callbackTarget, cancellationToken).ConfigureAwait(false));

        [Obsolete]
        public async Task<UnitTestingKeepAliveSessionWrapper> TryCreateUnitTestingKeepAliveSessionWrapperAsync(string serviceName, CancellationToken cancellationToken)
        {
            var connection = await UnderlyingObject!.CreateConnectionAsync(new RemoteServiceName(serviceName), callbackTarget: null, cancellationToken).ConfigureAwait(false);
            return new UnitTestingKeepAliveSessionWrapper(connection);
        }

        [Obsolete]
        public async Task<UnitTestingSessionWithSolutionWrapper> TryCreateUnitingSessionWithSolutionWrapperAsync(string serviceName, Solution solution, CancellationToken cancellationToken)
        {
            var connection = await UnderlyingObject!.CreateConnectionAsync(new RemoteServiceName(serviceName), callbackTarget: null, cancellationToken).ConfigureAwait(false);

            SessionWithSolution? session = null;
            try
            {
                // transfer ownership of the connection to the session object:
                session = await SessionWithSolution.CreateAsync(connection, solution, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (session == null)
                {
                    connection.Dispose();
                }
            }

            return new UnitTestingSessionWithSolutionWrapper(session);
        }

        [Obsolete]
        public event EventHandler<bool> StatusChanged
        {
            add => UnderlyingObject!.StatusChanged += value;
            remove => UnderlyingObject!.StatusChanged -= value;
        }
    }
}
