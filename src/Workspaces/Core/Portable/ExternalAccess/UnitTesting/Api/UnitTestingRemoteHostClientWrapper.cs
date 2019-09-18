// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public async Task<UnitTestingKeepAliveSessionWrapper> TryCreateKeepAliveSessionAsync(string serviceName, CancellationToken cancellationToken)
        {
            var keepAliveSession = await UnderlyingObject.TryCreateKeepAliveSessionAsync(serviceName, cancellationToken).ConfigureAwait(false);
            return new UnitTestingKeepAliveSessionWrapper(keepAliveSession);
        }
    }
}
