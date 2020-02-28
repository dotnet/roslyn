// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
#pragma warning disable CS0618 // Type or member is obsolete

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingRemoteHostClientWrapper
    {
        internal UnitTestingRemoteHostClientWrapper(RemoteHostClient underlyingObject)
            => UnderlyingObject = underlyingObject ?? throw new ArgumentNullException(nameof(underlyingObject));

        internal RemoteHostClient UnderlyingObject { get; }

        public async Task<UnitTestingKeepAliveSessionWrapper> TryCreateUnitTestingKeepAliveSessionWrapperAsync(string serviceName, CancellationToken cancellationToken)
        {
            var keepAliveSession = await UnderlyingObject.TryCreateKeepAliveSessionAsync(serviceName, callbackTarget: null, cancellationToken).ConfigureAwait(false);
            return new UnitTestingKeepAliveSessionWrapper(keepAliveSession);
        }

        public async Task<UnitTestingSessionWithSolutionWrapper> TryCreateUnitingSessionWithSolutionWrapperAsync(string serviceName, Solution solution, CancellationToken cancellationToken)
        {
            var connection = await UnderlyingObject.TryCreateConnectionAsync(serviceName, callbackTarget: null, cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return default;
            }

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

        public event EventHandler<bool> StatusChanged
        {
            add => UnderlyingObject.StatusChanged += value;
            remove => UnderlyingObject.StatusChanged -= value;
        }
    }
}
