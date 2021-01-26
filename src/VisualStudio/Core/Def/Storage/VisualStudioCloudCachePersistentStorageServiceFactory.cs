// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    [ExportWorkspaceServiceFactory(typeof(ICloudCacheStorageService), ServiceLayer.Host), Shared]
    internal partial class VisualStudioCloudCachePersistentStorageServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IAsyncServiceProvider _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioCloudCachePersistentStorageServiceFactory(
            [Import("Microsoft.VisualStudio.Shell.SVsServiceProvider")] Shell.IAsyncServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new VisualStudioCloudCachePersistentStorageService(
                _serviceProvider, workspaceServices.GetRequiredService<IPersistentStorageLocationService>());
        }
    }
}
