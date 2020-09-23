// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingRemoteHostClientWrapper
    {
        internal UnitTestingRemoteHostClientWrapper(RemoteHostClient underlyingObject)
            => UnderlyingObject = underlyingObject;

        internal RemoteHostClient? UnderlyingObject { get; }

        [MemberNotNullWhen(false, nameof(UnderlyingObject))]
        public bool IsDefault => UnderlyingObject == null;

        public static async Task<UnitTestingRemoteHostClientWrapper?> TryGetClientAsync(HostWorkspaceServices services, CancellationToken cancellationToken = default)
        {
            var client = await RemoteHostClient.TryGetClientAsync(services, cancellationToken).ConfigureAwait(false);
            if (client is null)
                return null;

            return new UnitTestingRemoteHostClientWrapper(client);
        }

        public async Task<bool> TryRunRemoteAsync(UnitTestingServiceHubService service, string targetName, Solution? solution, IReadOnlyList<object?> arguments, object? callbackTarget, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(IsDefault);
            await UnderlyingObject.RunRemoteAsync((WellKnownServiceHubService)service, targetName, solution, arguments, callbackTarget, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public async Task<Optional<T>> TryRunRemoteAsync<T>(UnitTestingServiceHubService service, string targetName, Solution? solution, IReadOnlyList<object?> arguments, object? callbackTarget, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(IsDefault);
            return await UnderlyingObject.RunRemoteAsync<T>((WellKnownServiceHubService)service, targetName, solution, arguments, callbackTarget, cancellationToken).ConfigureAwait(false);
        }

        public async Task<UnitTestingRemoteServiceConnectionWrapper> CreateConnectionAsync(UnitTestingServiceHubService service, object? callbackTarget, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(IsDefault);
            return new UnitTestingRemoteServiceConnectionWrapper(await UnderlyingObject.CreateConnectionAsync((WellKnownServiceHubService)service, callbackTarget, cancellationToken).ConfigureAwait(false));
        }

        [Obsolete]
        public event EventHandler<bool> StatusChanged
        {
            add
            {
                Contract.ThrowIfTrue(IsDefault);
                UnderlyingObject.StatusChanged += value;
            }

            remove
            {
                Contract.ThrowIfTrue(IsDefault);
                UnderlyingObject.StatusChanged -= value;
            }
        }
    }
}
