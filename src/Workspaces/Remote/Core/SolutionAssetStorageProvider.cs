// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class SolutionAssetStorageProvider : ISolutionAssetStorageProvider
{
    [ExportWorkspaceServiceFactory(typeof(ISolutionAssetStorageProvider)), Shared]
    internal sealed class Factory : IWorkspaceServiceFactory
    {
        private readonly SolutionAssetStorage _storage = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public Factory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new SolutionAssetStorageProvider(_storage);
    }

    public SolutionAssetStorage AssetStorage { get; private set; }

    private SolutionAssetStorageProvider(SolutionAssetStorage storage)
    {
        AssetStorage = storage;
    }
}
