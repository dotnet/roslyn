// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            var session = await UnderlyingObject.TryCreateSessionAsync(serviceName, solution, callbackTarget: null, cancellationToken).ConfigureAwait(false);
            return new UnitTestingSessionWithSolutionWrapper(session);
        }

        public event EventHandler<bool> StatusChanged
        {
            add => UnderlyingObject.StatusChanged += value;
            remove => UnderlyingObject.StatusChanged -= value;
        }
    }
}
