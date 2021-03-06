// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Storage.CloudCache;
using Microsoft.VisualStudio.RpcContracts.Caching;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    [ExportWorkspaceService(typeof(ICloudCacheStorageServiceFactory), ServiceLayer.Host), Shared]
    internal class VisualStudioCloudCacheStorageServiceFactory : ICloudCacheStorageServiceFactory
    {
        private readonly IAsyncServiceProvider _serviceProvider;
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioCloudCacheStorageServiceFactory(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider)
        {
            _threadingContext = threadingContext;
            _serviceProvider = (IAsyncServiceProvider)serviceProvider;
        }

        public AbstractPersistentStorageService Create(IPersistentStorageLocationService locationService)
            => new VisualStudioCloudCacheStorageService(_serviceProvider, _threadingContext, locationService);

        private class VisualStudioCloudCacheStorageService : AbstractCloudCachePersistentStorageService
        {
            private readonly IAsyncServiceProvider _serviceProvider;
            private readonly IThreadingContext _threadingContext;

            public VisualStudioCloudCacheStorageService(IAsyncServiceProvider serviceProvider, IThreadingContext threadingContext, IPersistentStorageLocationService locationService)
                : base(locationService)
            {
                _serviceProvider = serviceProvider;
                _threadingContext = threadingContext;
            }

            protected sealed override void DisposeCacheService(ICacheService cacheService)
            {
                if (cacheService is IAsyncDisposable asyncDisposable)
                {
                    _threadingContext.JoinableTaskFactory.Run(
                        () => asyncDisposable.DisposeAsync().AsTask());
                }
                else if (cacheService is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            protected sealed override async ValueTask<ICacheService> CreateCacheServiceAsync(CancellationToken cancellationToken)
            {
                var serviceContainer = await _serviceProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>().ConfigureAwait(false);
                var serviceBroker = serviceContainer.GetFullAccessServiceBroker();

#pragma warning disable ISB001 // Dispose of proxies
                // cache service will be disposed inside VisualStudioCloudCachePersistentStorage.Dispose
                var cacheService = await serviceBroker.GetProxyAsync<ICacheService>(VisualStudioServices.VS2019_10.CacheService, cancellationToken: cancellationToken).ConfigureAwait(false);
#pragma warning restore ISB001 // Dispose of proxies

                Contract.ThrowIfNull(cacheService);
                return cacheService;
            }
        }
    }
}
