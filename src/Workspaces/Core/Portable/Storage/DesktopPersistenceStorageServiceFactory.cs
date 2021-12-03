// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

// When building for source-build, there is no sqlite dependency
#if !DOTNET_BUILD_FROM_SOURCE
using Microsoft.CodeAnalysis.SQLite.v2;
using Microsoft.CodeAnalysis.Storage.CloudCache;
#endif

namespace Microsoft.CodeAnalysis.Storage
{
    [ExportWorkspaceServiceFactory(typeof(IPersistentStorageService), ServiceLayer.Desktop), Shared]
    internal class DesktopPersistenceStorageServiceFactory : IWorkspaceServiceFactory
    {
#if DOTNET_BUILD_FROM_SOURCE

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DesktopPersistenceStorageServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return NoOpPersistentStorageService.Instance;
        }

#else

        private readonly SQLiteConnectionPoolService _connectionPoolService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DesktopPersistenceStorageServiceFactory(SQLiteConnectionPoolService connectionPoolService)
        {
            _connectionPoolService = connectionPoolService;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var options = workspaceServices.Workspace.Options;
            var locationService = workspaceServices.GetService<IPersistentStorageLocationService>();

            if (locationService != null)
            {
                var database = GetDatabase(workspaceServices);
                switch (database)
                {
                    case StorageDatabase.SQLite:
                        return new SQLitePersistentStorageService(options, _connectionPoolService, locationService);

                    case StorageDatabase.CloudCache:
                        var factory = workspaceServices.GetService<ICloudCacheStorageServiceFactory>();

                        return factory == null
                            ? NoOpPersistentStorageService.GetOrThrow(options)
                            : factory.Create(locationService);
                }
            }

            return NoOpPersistentStorageService.GetOrThrow(options);
        }

        private static StorageDatabase GetDatabase(HostWorkspaceServices workspaceServices)
        {
            var experimentationService = workspaceServices.GetService<IExperimentationService>();
            if (experimentationService?.IsExperimentEnabled(WellKnownExperimentNames.CloudCache) == true)
                return StorageDatabase.CloudCache;

            var optionService = workspaceServices.GetRequiredService<IOptionService>();
            return optionService.GetOption(StorageOptions.Database);
        }

#endif
    }
}
