// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Storage.CloudCache;
using Microsoft.VisualStudio.LanguageServices.Storage;

namespace Microsoft.CodeAnalysis.Remote.Storage
{
    [ExportWorkspaceService(typeof(ICloudCacheStorageServiceFactory), WorkspaceKind.RemoteWorkspace), Shared]
    internal class RemoteCloudCacheStorageServiceFactory : ICloudCacheStorageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteCloudCacheStorageServiceFactory()
        {
        }

        public AbstractPersistentStorageService Create(IPersistentStorageConfiguration configuration)
            => new HubClientCloudCachePersistentStorageService(configuration);
    }
}
