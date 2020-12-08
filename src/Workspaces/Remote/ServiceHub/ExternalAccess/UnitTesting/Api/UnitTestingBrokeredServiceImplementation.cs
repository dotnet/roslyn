// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingBrokeredServiceImplementation
    {
        public static ValueTask<T> RunServiceAsync<T>(Func<CancellationToken, ValueTask<T>> implementation, CancellationToken cancellationToken)
            => BrokeredServiceBase.RunServiceImplAsync<T>(implementation, cancellationToken);

        public static ValueTask RunServiceAsync(Func<CancellationToken, ValueTask> implementation, CancellationToken cancellationToken)
            => BrokeredServiceBase.RunServiceImplAsync(implementation, cancellationToken);

        public static async ValueTask<UnitTestingIncrementalAnalyzerProvider?> TryRegisterAnalyzerProviderAsync(
            ServiceBrokerClient client,
            string analyzerName,
            IUnitTestingIncrementalAnalyzerProviderImplementation provider,
            CancellationToken cancellationToken)
        {
            using var rental = await client.GetProxyAsync<IRemoteWorkspaceSolutionProviderService>(RemoteWorkspaceSolutionProviderService.ServiceDescriptor, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(rental.Proxy);
            var workspace = await rental.Proxy.GetWorkspaceAsync(WorkspaceKind.RemoteWorkspace, cancellationToken).ConfigureAwait(false);
            return UnitTestingIncrementalAnalyzerProvider.TryRegister(workspace, analyzerName, provider);
        }

        public static async ValueTask<Solution> GetSolutionAsync(ServiceBrokerClient client, object solutionInfo, CancellationToken cancellationToken)
        {
            using var rental = await client.GetProxyAsync<IRemoteWorkspaceSolutionProviderService>(RemoteWorkspaceSolutionProviderService.ServiceDescriptor, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(rental.Proxy);
            return await rental.Proxy.GetSolutionAsync((PinnedSolutionInfo)solutionInfo, cancellationToken).ConfigureAwait(false);
        }
    }
}
