// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Storage.CloudCache;
using Microsoft.ServiceHub.Client;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.Storage;
using Microsoft.VisualStudio.RpcContracts.Caching;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Storage
{
    [ExportWorkspaceService(typeof(ICloudCacheStorageServiceFactory), WorkspaceKind.RemoteWorkspace), Shared]
    internal class RemoteCloudCacheStorageServiceFactory : ICloudCacheStorageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteCloudCacheStorageServiceFactory()
        {
        }

        public AbstractPersistentStorageService Create(IPersistentStorageConfiguration configuration)
            => new RemoteCloudCachePersistentStorageService(configuration);

        private class RemoteCloudCachePersistentStorageService : AbstractCloudCachePersistentStorageService
        {
            public RemoteCloudCachePersistentStorageService(IPersistentStorageConfiguration configuration)
                : base(configuration)
            {
            }

            private void DisposeCacheService(ICacheService cacheService)
            {
                if (cacheService is IAsyncDisposable asyncDisposable)
                {
                    asyncDisposable.DisposeAsync().AsTask().Wait();
                }
                else if (cacheService is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            protected override async ValueTask<WrappedCacheService> CreateCacheServiceAsync(string solutionFolder, CancellationToken cancellationToken)
            {
                using var hubClient = new HubClient();

#pragma warning disable ISB001 // Dispose of proxies
                // cache service will be disposed inside RemoteCloudCacheService.Dispose
                var cacheService = await hubClient.GetProxyAsync<ICacheService>(
                    VisualStudioServices.VS2019_10.CacheService,
                    new ServiceActivationOptions { ActivationArguments = new Dictionary<string, string> { { "foo", solutionFolder } } },
                    cancellationToken).ConfigureAwait(false);
#pragma warning restore ISB001 // Dispose of proxies

                Contract.ThrowIfNull(cacheService);
                return new WrappedCacheService(hubClient, cacheService, this.DisposeCacheService);
            }
        }
    }
}
