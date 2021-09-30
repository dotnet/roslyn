﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Api
{
    internal static class RazorBrokeredServiceImplementation
    {
        public static ValueTask<T> RunServiceAsync<T>(Func<CancellationToken, ValueTask<T>> implementation, CancellationToken cancellationToken)
            => BrokeredServiceBase.RunServiceImplAsync<T>(implementation, cancellationToken);

        public static ValueTask RunServiceAsync(Func<CancellationToken, ValueTask> implementation, CancellationToken cancellationToken)
            => BrokeredServiceBase.RunServiceImplAsync(implementation, cancellationToken);

        public static ValueTask<Solution> GetSolutionAsync(this RazorPinnedSolutionInfoWrapper solutionInfo, ServiceBrokerClient client, CancellationToken cancellationToken)
            => RemoteWorkspaceManager.Default.GetSolutionAsync(client, solutionInfo.UnderlyingObject, cancellationToken);
    }
}
