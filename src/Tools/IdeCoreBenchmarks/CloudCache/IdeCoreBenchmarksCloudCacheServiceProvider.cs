// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.UnitTests.WorkspaceServices.Mocks;
using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Cache;
using Microsoft.VisualStudio.Cache.SQLite;
using Microsoft.VisualStudio.LanguageServices.Storage;
using Microsoft.VisualStudio.RpcContracts.Caching;

namespace CloudCache
{
    [ExportWorkspaceService(typeof(IRoslynCloudCacheServiceProvider), ServiceLayer.Host), Shared]
    internal class IdeCoreBenchmarksCloudCacheServiceProvider : IRoslynCloudCacheServiceProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IdeCoreBenchmarksCloudCacheServiceProvider()
        {
            Console.WriteLine($"Instantiated {nameof(IdeCoreBenchmarksCloudCacheServiceProvider)}");
        }

        public async ValueTask<IRoslynCloudCacheService> CreateCacheAsync(CancellationToken cancellationToken)
        {
            var authorizationServiceClient = new AuthorizationServiceClient(new AuthorizationServiceMock());
            var solutionService = new SolutionServiceMock();
            var fileSystem = new FileSystemServiceMock();
            var serviceBroker = new ServiceBrokerMock()
            {
                BrokeredServices =
                {
                    { VisualStudioServices.VS2019_10.SolutionService.Moniker, solutionService },
                    { VisualStudioServices.VS2019_10.FileSystem.Moniker, fileSystem },
                    { FrameworkServices.Authorization.Moniker, new AuthorizationServiceMock() },
                },
            };

            var someContext = new CacheContext { RelativePathBase = @"C:\github\roslyn" };
            var pool = new SqliteConnectionPool();
            var activeContext = await pool.ActivateContextAsync(someContext, default);
            var cacheService = new CacheService(activeContext, serviceBroker, authorizationServiceClient, pool);
            return new IdeCoreBenchmarksCloudCacheService(cacheService);
        }

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
