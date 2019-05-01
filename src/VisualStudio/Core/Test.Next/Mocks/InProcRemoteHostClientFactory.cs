// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InProcRemoteHostClientFactory()
        {
        }

        public Task<RemoteHostClient> CreateAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            return InProcRemoteHostClient.CreateAsync(workspace, runCacheCleanup: false, cancellationToken: cancellationToken);
        }
    }
}
