// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SQLite.v2;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.UnitTests.WorkspaceServices.Mocks;
using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Cache;
using Microsoft.VisualStudio.RpcContracts.Caching;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices
{
    public class CloudCachePersistentStorageTests : AbstractPersistentStorageTests
    {
        internal override AbstractPersistentStorageService GetStorageService(
            IMefHostExportProvider exportProvider, IPersistentStorageLocationService locationService, IPersistentStorageFaultInjector? faultInjector, string relativePathBase)
        {
            var provider = new TestCloudCacheServiceProvider(relativePathBase);
            return new CloudCachePersistentStorageService(provider, locationService, mustSucceed: false);
        }
    }

    internal class TestCloudCacheServiceProvider : ICloudCacheServiceProvider
    {
        private readonly string _relativePathBase;

        public TestCloudCacheServiceProvider(string relativePathBase)
        {
            Console.WriteLine($"Instantiated {nameof(TestCloudCacheServiceProvider)}");
            _relativePathBase = relativePathBase;
        }

        public ValueTask<ICloudCacheService> CreateCacheAsync(CancellationToken cancellationToken)
        {
            var authorizationServiceClient = new AuthorizationServiceClient(new AuthorizationServiceMock());
            var solutionService = new SolutionServiceMock();
            var fileSystem = new FileSystemServiceMock();
            var serviceBroker = new ServiceBrokerMock()
            {
                BrokeredServices =
                {
                    { VisualStudioServices.VS2019_9.SolutionService.Moniker, solutionService },
                    { VisualStudioServices.VS2019_9.FileSystem.Moniker, fileSystem },
                    { FrameworkServices.Authorization.Moniker, new AuthorizationServiceMock() },
                },
            };

            var someContext = new CacheContext { RelativePathBase = _relativePathBase };
            var cacheService = new CacheService(someContext, serviceBroker, authorizationServiceClient);
            return new(new IdeCoreBenchmarksCloudCacheService(cacheService));
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
