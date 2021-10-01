// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage.CloudCache;
using Microsoft.CodeAnalysis.UnitTests.WorkspaceServices.Mocks;

namespace CloudCache
{
    [ExportWorkspaceService(typeof(ICloudCacheStorageServiceProvider), ServiceLayer.Host), Shared]
    internal class IdeCoreBenchmarksCloudCacheServiceProvider : ICloudCacheStorageServiceProvider
    {
        public IChecksummedPersistentStorageService Service { get; }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IdeCoreBenchmarksCloudCacheServiceProvider()
        {
            Console.WriteLine($"Instantiated {nameof(IdeCoreBenchmarksCloudCacheServiceProvider)}");
            Service = new MockCloudCachePersistentStorageService(@"C:\github\roslyn");
        }
    }
}
