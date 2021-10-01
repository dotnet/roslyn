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
    [ExportWorkspaceService(typeof(ICloudCacheStorageServiceFactory), ServiceLayer.Host), Shared]
    internal class IdeCoreBenchmarksCloudCacheServiceProvider : ICloudCacheStorageServiceFactory
    {
        private readonly IChecksummedPersistentStorageService _instance;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IdeCoreBenchmarksCloudCacheServiceProvider()
        {
            Console.WriteLine($"Instantiated {nameof(IdeCoreBenchmarksCloudCacheServiceProvider)}");
            _instance = new MockCloudCachePersistentStorageService(@"C:\github\roslyn");
        }

        public IChecksummedPersistentStorageService Create()
            => _instance;
    }
}
