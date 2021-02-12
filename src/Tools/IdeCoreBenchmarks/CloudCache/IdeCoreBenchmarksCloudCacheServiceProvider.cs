// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Cache;
using Microsoft.VisualStudio.Cache.SQLite;
using Microsoft.VisualStudio.RpcContracts.Caching;

namespace CloudCache
{
    [ExportWorkspaceService(typeof(ICloudCacheServiceProvider), ServiceLayer.Host), Shared]
    internal class IdeCoreBenchmarksCloudCacheServiceProvider : ICloudCacheServiceProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IdeCoreBenchmarksCloudCacheServiceProvider()
        {
            Console.WriteLine($"Instantiated {nameof(IdeCoreBenchmarksCloudCacheServiceProvider)}");
        }

        public async ValueTask<ICloudCacheService> CreateCacheAsync(CancellationToken cancellationToken)
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

        private class IdeCoreBenchmarksCloudCacheService : ICloudCacheService
        {
            private readonly ICacheService _cacheService;

            public IdeCoreBenchmarksCloudCacheService(ICacheService cacheService)
            {
                _cacheService = cacheService;
            }

            private static CacheItemKey Convert(CloudCacheItemKey key)
                => new(Convert(key.ContainerKey), key.ItemName) { Version = key.Version };

            private static CacheContainerKey Convert(CloudCacheContainerKey containerKey)
                => new(containerKey.Component, containerKey.Dimensions);

            public void Dispose()
                => (_cacheService as IDisposable)?.Dispose();

            public Task<bool> CheckExistsAsync(CloudCacheItemKey key, CancellationToken cancellationToken)
                => _cacheService.CheckExistsAsync(Convert(key), cancellationToken);

            public ValueTask<string> GetRelativePathBaseAsync(CancellationToken cancellationToken)
                => _cacheService.GetRelativePathBaseAsync(cancellationToken);

            public Task SetItemAsync(CloudCacheItemKey key, PipeReader reader, bool shareable, CancellationToken cancellationToken)
                => _cacheService.SetItemAsync(Convert(key), reader, shareable, cancellationToken);

            public Task<bool> TryGetItemAsync(CloudCacheItemKey key, PipeWriter writer, CancellationToken cancellationToken)
                => _cacheService.TryGetItemAsync(Convert(key), writer, cancellationToken);
        }
    }
}
