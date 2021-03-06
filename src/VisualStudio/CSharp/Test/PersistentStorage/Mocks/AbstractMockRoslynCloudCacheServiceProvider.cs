// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copy of https://devdiv.visualstudio.com/DevDiv/_git/VS.CloudCache?path=%2Ftest%2FMicrosoft.VisualStudio.Cache.Tests%2FMocks&_a=contents&version=GBmain
// Try to keep in sync and avoid unnecessary changes here.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Storage.CloudCache;
using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Cache;
using Microsoft.VisualStudio.Cache.SQLite;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices.Mocks
{
    internal abstract class AbstractMockCloudCacheStorageServiceFactory : ICloudCacheStorageServiceFactory
    {
        private readonly string _relativePathBase;

        protected AbstractMockRoslynCloudCacheServiceProvider(string relativePathBase)
        {
            _relativePathBase = relativePathBase;
        }

        protected abstract IRoslynCloudCacheService CreateService(CacheService cacheService);

        public async ValueTask<IRoslynCloudCacheService> CreateCacheAsync(CancellationToken cancellationToken)
        {
            // Directly access VS' CacheService through their library and not as a brokered service. Then create our
            // wrapper CloudCacheService directly on that instance.
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

            return CreateService(cacheService);
        }
    }
}
