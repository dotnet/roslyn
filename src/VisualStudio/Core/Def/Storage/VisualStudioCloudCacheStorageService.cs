// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage.CloudCache;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.RpcContracts.Caching;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    internal class VisualStudioCloudCacheStorageService : AbstractCloudCachePersistentStorageService
    {
        [ExportWorkspaceServiceFactory(typeof(ICloudCacheStorageService), ServiceLayer.Host), Shared]
        internal class ServiceFactory : IWorkspaceServiceFactory
        {
            private readonly IAsyncServiceProvider _serviceProvider;
            private readonly IThreadingContext _threadingContext;

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public ServiceFactory(SVsServiceProvider serviceProvider, IThreadingContext threadingContext)
            {
                _serviceProvider = (IAsyncServiceProvider)serviceProvider;
                _threadingContext = threadingContext;
            }

            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                => new VisualStudioCloudCacheStorageService(_serviceProvider, _threadingContext, workspaceServices.GetRequiredService<IPersistentStorageConfiguration>());
        }

        private readonly IAsyncServiceProvider _serviceProvider;
        private readonly IThreadingContext _threadingContext;

        public VisualStudioCloudCacheStorageService(IAsyncServiceProvider serviceProvider, IThreadingContext threadingContext, IPersistentStorageConfiguration configuration)
            : base(configuration)
        {
            _serviceProvider = serviceProvider;
            _threadingContext = threadingContext;
        }

        protected sealed override async ValueTask<ICacheService> CreateCacheServiceAsync(string solutionFolder, CancellationToken cancellationToken)
        {
            var serviceContainer = await _serviceProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>(_threadingContext.JoinableTaskFactory).ConfigureAwait(false);
            var serviceBroker = serviceContainer.GetFullAccessServiceBroker();

#pragma warning disable ISB001 // Dispose of proxies
            // cache service will be disposed inside VisualStudioCloudCachePersistentStorage.Dispose
            var cacheService = await serviceBroker.GetProxyAsync<ICacheService>(
                VisualStudioServices.VS2019_10.CacheService,
                // replace with CacheService.RelativePathBaseActivationArgKey once available.
                new ServiceActivationOptions { ActivationArguments = ImmutableDictionary<string, string>.Empty.Add("RelativePathBase", solutionFolder) },
                cancellationToken).ConfigureAwait(false);
#pragma warning restore ISB001 // Dispose of proxies

            Contract.ThrowIfNull(cacheService);
            return cacheService;
        }
    }
}
