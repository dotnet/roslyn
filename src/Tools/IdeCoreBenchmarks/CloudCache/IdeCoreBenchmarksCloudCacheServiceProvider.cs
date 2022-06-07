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
    [ExportWorkspaceService(typeof(ICloudCacheStorageServiceFactory), ServiceLayer.Host), Shared]
    internal class IdeCoreBenchmarksCloudCacheServiceProvider : ICloudCacheStorageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IdeCoreBenchmarksCloudCacheServiceProvider()
        {
            Console.WriteLine($"Instantiated {nameof(IdeCoreBenchmarksCloudCacheServiceProvider)}");
        }

        public AbstractPersistentStorageService Create(IPersistentStorageConfiguration configuration)
        {
            return new MockCloudCachePersistentStorageService(
                configuration, @"C:\github\roslyn", cs =>
                {
                    if (cs is IAsyncDisposable asyncDisposable)
                    {
                        asyncDisposable.DisposeAsync().AsTask().Wait();
                    }
                    else if (cs is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                });
        }
    }
}
