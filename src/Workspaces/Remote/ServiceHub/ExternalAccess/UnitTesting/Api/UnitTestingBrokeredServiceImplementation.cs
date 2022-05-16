// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingBrokeredServiceImplementation
    {
        public static ValueTask<T> RunServiceAsync<T>(Func<CancellationToken, ValueTask<T>> implementation, CancellationToken cancellationToken)
            => BrokeredServiceBase.RunServiceImplAsync(implementation, cancellationToken);

        public static ValueTask RunServiceAsync(Func<CancellationToken, ValueTask> implementation, CancellationToken cancellationToken)
            => BrokeredServiceBase.RunServiceImplAsync(implementation, cancellationToken);

        public static UnitTestingIncrementalAnalyzerProvider? TryRegisterAnalyzerProvider(string analyzerName, IUnitTestingIncrementalAnalyzerProviderImplementation provider)
            => UnitTestingIncrementalAnalyzerProvider.TryRegister(RemoteWorkspaceManager.Default.GetWorkspace(), analyzerName, provider);

        [Obsolete("Use RunServiceAsync (that is passsed a Solution) instead", error: false)]
        public static ValueTask<Solution> GetSolutionAsync(this UnitTestingPinnedSolutionInfoWrapper solutionInfo, ServiceBrokerClient client, CancellationToken cancellationToken)
            => RemoteWorkspaceManager.Default.GetSolutionAsync(client, solutionInfo.UnderlyingObject, cancellationToken);

        public static ValueTask<T> RunServiceAsync<T>(this UnitTestingPinnedSolutionInfoWrapper solutionInfo, ServiceBrokerClient client, Func<Solution, ValueTask<T>> implementation, CancellationToken cancellationToken)
            => RemoteWorkspaceManager.Default.RunServiceAsync(client, solutionInfo.UnderlyingObject, implementation, cancellationToken);
    }
}
