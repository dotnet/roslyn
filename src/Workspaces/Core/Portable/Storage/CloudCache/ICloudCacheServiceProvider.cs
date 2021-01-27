// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Storage
{
    internal interface ICloudCacheServiceProvider : IWorkspaceService
    {
        ValueTask<ICloudCacheService> CreateCacheAsync(CancellationToken cancellationToken);
    }

#if false

    [ExportWorkspaceServiceFactory(typeof(ICloudCacheServiceProvider)), Shared]
    internal class DefaultCloudCacheServiceProviderFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultCloudCacheServiceProviderFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new DefaultCloudCacheServiceProvider(workspaceServices);

        private class DefaultCloudCacheServiceProvider : ICloudCacheServiceProvider
        {
            private readonly HostWorkspaceServices _workspaceServices;

            public DefaultCloudCacheServiceProvider(HostWorkspaceServices workspaceServices)
                => _workspaceServices = workspaceServices;

            public ValueTask<ICloudCacheService?> CreateCacheAsync()
            {
                if (_workspaceServices.Workspace.Options.GetOption(StorageOptions.DatabaseMustSucceed))
                    throw new InvalidOperationException("Could not find host exported cloud cache");

                return new((ICloudCacheService?)null);
            }
        }
    }

#endif
}
