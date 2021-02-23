// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.UnitTests.WorkspaceServices.Mocks;
using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Cache;
using Microsoft.VisualStudio.Cache.SQLite;
using Microsoft.VisualStudio.RpcContracts.Caching;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices
{
    internal class TestCloudCacheServiceProvider : ICloudCacheServiceProvider
    {
        private readonly string _relativePathBase;

        public TestCloudCacheServiceProvider(string relativePathBase)
        {
            Console.WriteLine($"Instantiated {nameof(TestCloudCacheServiceProvider)}");
            _relativePathBase = relativePathBase;
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

            var someContext = new CacheContext { RelativePathBase = _relativePathBase };
            var pool = new SqliteConnectionPool();
            var activeContext = await pool.ActivateContextAsync(someContext, default);
            var cacheService = new CacheService(activeContext, serviceBroker, authorizationServiceClient, pool);
            return new TestCloudCacheService(cacheService);
        }

        private class TestCloudCacheService : ICloudCacheService
        {
            private readonly ICacheService _cacheService;

            public TestCloudCacheService(ICacheService cacheService)
            {
                _cacheService = cacheService;
            }

            private static CacheItemKey Convert(CloudCacheItemKey key)
                => new(Convert(key.ContainerKey), key.ItemName) { Version = key.Version };

            private static CacheContainerKey Convert(CloudCacheContainerKey containerKey)
                => new(containerKey.Component, containerKey.Dimensions);

            public void Dispose()
            {
                if (_cacheService is IAsyncDisposable asyncDisposable)
                {
                    asyncDisposable.DisposeAsync().AsTask().Wait();
                }
                else if (_cacheService is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            public ValueTask DisposeAsync()
            {
                if (_cacheService is IAsyncDisposable asyncDisposable)
                {
                    return asyncDisposable.DisposeAsync();
                }
                else if (_cacheService is IDisposable disposable)
                {
                    disposable.Dispose();
                    return ValueTaskFactory.CompletedTask;
                }

                return ValueTaskFactory.CompletedTask;
            }

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
