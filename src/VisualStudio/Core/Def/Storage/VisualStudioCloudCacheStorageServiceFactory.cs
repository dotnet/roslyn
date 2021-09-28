// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Storage.CloudCache;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    [ExportWorkspaceService(typeof(ICloudCacheStorageServiceFactory), ServiceLayer.Host), Shared]
    internal class VisualStudioCloudCacheStorageServiceFactory : ICloudCacheStorageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioCloudCacheStorageServiceFactory()
        {
        }

        public AbstractPersistentStorageService Create(IPersistentStorageConfiguration configuration)
            => new HubClientCloudCachePersistentStorageService(configuration);
    }
}
