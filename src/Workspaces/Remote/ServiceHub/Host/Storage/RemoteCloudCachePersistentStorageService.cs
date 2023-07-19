// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote.Host;
using Microsoft.CodeAnalysis.Storage.CloudCache;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.Storage;
using Microsoft.VisualStudio.RpcContracts.Caching;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Storage
{
    internal class RemoteCloudCachePersistentStorageService : AbstractCloudCachePersistentStorageService
    {
        [ExportWorkspaceServiceFactory(typeof(ICloudCacheStorageService), WorkspaceKind.RemoteWorkspace), Shared]
        internal class ServiceFactory : IWorkspaceServiceFactory
        {
            private readonly IGlobalServiceBroker _globalServiceBroker;

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public ServiceFactory(IGlobalServiceBroker globalServiceBroker)
            {
                _globalServiceBroker = globalServiceBroker;
            }

            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                => new RemoteCloudCachePersistentStorageService(_globalServiceBroker, workspaceServices.GetRequiredService<IPersistentStorageConfiguration>());
        }

        private readonly IGlobalServiceBroker _globalServiceBroker;

        public RemoteCloudCachePersistentStorageService(
            IGlobalServiceBroker globalServiceBroker,
            IPersistentStorageConfiguration configuration)
            : base(configuration)
        {
            _globalServiceBroker = globalServiceBroker;
        }

        protected override async ValueTask<ICacheService> CreateCacheServiceAsync(string solutionFolder, CancellationToken cancellationToken)
        {
            var serviceBroker = _globalServiceBroker.Instance;

#pragma warning disable ISB001 // Dispose of proxies
            // cache service will be disposed inside RemoteCloudCacheService.Dispose
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
