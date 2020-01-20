﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
