// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Storage.CloudCache;
using Microsoft.CodeAnalysis.UnitTests.WorkspaceServices.Mocks;

namespace CloudCache
{
    [ExportWorkspaceServiceFactory(typeof(ICloudCacheStorageServiceProvider), ServiceLayer.Host), Shared]
    internal class IdeCoreBenchmarksCloudCacheServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IdeCoreBenchmarksCloudCacheServiceFactory()
        {
            Console.WriteLine($"Instantiated {nameof(IdeCoreBenchmarksCloudCacheServiceFactory)}");
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new ServiceProvider(workspaceServices.GetRequiredService<IPersistentStorageConfiguration>());
        }

        private class ServiceProvider : ICloudCacheStorageServiceProvider
        {
            private readonly MockCloudCachePersistentStorageService _service;

            public ServiceProvider(IPersistentStorageConfiguration configuration)
            {
                _service = new MockCloudCachePersistentStorageService(configuration, @"C:\github\roslyn");
            }

            public AbstractPersistentStorageService GetService()
                => _service;
        }
    }
}
