// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.VisualStudio.RpcContracts.Caching;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    [ExportWorkspaceService(typeof(ICloudCacheServiceProvider), ServiceLayer.Host), Shared]
    internal class VisualStudioCloudCacheServiceProvider : ICloudCacheServiceProvider
    {
        private readonly IAsyncServiceProvider _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioCloudCacheServiceProvider(
            SVsServiceProvider serviceProvider)
        {
            _serviceProvider = (IAsyncServiceProvider)serviceProvider;
        }

        public async ValueTask<ICloudCacheService> CreateCacheAsync(CancellationToken cancellationToken)
        {
            var serviceContainer = await _serviceProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>().ConfigureAwait(false);
            var serviceBroker = serviceContainer.GetFullAccessServiceBroker();

#pragma warning disable ISB001 // Dispose of proxies
            // cache service will be disposed inside VisualStudioCloudCachePersistentStorage.Dispose
            var cacheService = await serviceBroker.GetProxyAsync<ICacheService>(VisualStudioServices.VS2019_10.CacheService, cancellationToken: cancellationToken).ConfigureAwait(false);
#pragma warning restore ISB001 // Dispose of proxies

            Contract.ThrowIfNull(cacheService);
            return new VisualStudioCloudCacheService(cacheService);
        }
    }
}
