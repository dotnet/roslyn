// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.UnitTests.WorkspaceServices.Mocks;
using Microsoft.VisualStudio.Cache;
using Microsoft.VisualStudio.LanguageServices.Storage;
using Microsoft.VisualStudio.RpcContracts.Caching;

namespace CloudCache
{
    [ExportWorkspaceService(typeof(IRoslynCloudCacheServiceProvider), ServiceLayer.Host), Shared]
    internal class IdeCoreBenchmarksCloudCacheServiceProvider : AbstractMockRoslynCloudCacheServiceProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IdeCoreBenchmarksCloudCacheServiceProvider()
            : base(@"C:\github\roslyn")
        {
            Console.WriteLine($"Instantiated {nameof(IdeCoreBenchmarksCloudCacheServiceProvider)}");
        }

        protected override IRoslynCloudCacheService CreateService(CacheService cacheService)
            => new IdeCoreBenchmarksCloudCacheService(cacheService);

        private class IdeCoreBenchmarksCloudCacheService : AbstractCloudCacheService
        {
            public IdeCoreBenchmarksCloudCacheService(ICacheService cacheService) : base(cacheService)
            {
            }

            public override void Dispose()
            {
                if (this.CacheService is IAsyncDisposable asyncDisposable)
                {
                    asyncDisposable.DisposeAsync().AsTask().Wait();
                }
                else if (this.CacheService is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
