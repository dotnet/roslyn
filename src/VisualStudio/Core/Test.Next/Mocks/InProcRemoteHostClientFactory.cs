// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Test.Utilities.Remote;

namespace Roslyn.VisualStudio.Next.UnitTests.Mocks
{
    [ExportWorkspaceService(typeof(IRemoteHostClientFactory), layer: ServiceLayer.Host), Shared]
    internal class InProcRemoteHostClientFactory : IRemoteHostClientFactory
    {
        [ImportingConstructor]
        public InProcRemoteHostClientFactory()
        {
        }

        public Task<RemoteHostClient> CreateAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            return InProcRemoteHostClient.CreateAsync(workspace, runCacheCleanup: false);
        }
    }
}
