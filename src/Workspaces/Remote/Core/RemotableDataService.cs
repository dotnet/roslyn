// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemotableDataService : IRemotableDataService
    {
        [ExportWorkspaceServiceFactory(typeof(IRemotableDataService)), Shared]
        internal sealed class Factory : IWorkspaceServiceFactory
        {
            private readonly SolutionAssetStorage _assetStorages = new SolutionAssetStorage();

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                => new RemotableDataService(_assetStorages);
        }

        public SolutionAssetStorage AssetStorage { get; private set; }

        private RemotableDataService(SolutionAssetStorage storages)
        {
            AssetStorage = storages;
        }
    }
}
