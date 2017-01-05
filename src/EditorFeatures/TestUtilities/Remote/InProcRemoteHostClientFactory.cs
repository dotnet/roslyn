// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;

namespace Roslyn.Test.Utilities.Remote
{
    [ExportWorkspaceService(typeof(IRemoteHostClientFactory), layer: ServiceLayer.Host), Shared]
    internal class InProcRemoteHostClientFactory : IRemoteHostClientFactory
    {
        public Task<RemoteHostClient> CreateAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            return InProcRemoteHostClient.CreateAsync(workspace, runCacheCleanup: false, cancellationToken: cancellationToken);
        }
    }
}
