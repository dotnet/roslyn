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
#endif

namespace Microsoft.CodeAnalysis.Storage
{
    [ExportWorkspaceServiceFactory(typeof(IPersistentStorageService), ServiceLayer.Desktop), Shared]
    internal class PersistenceStorageServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PersistenceStorageServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
#if !DOTNET_BUILD_FROM_SOURCE
            var optionService = workspaceServices.GetRequiredService<IOptionService>();
            var database = optionService.GetOption(StorageOptions.Database);
            switch (database)
            {
                case StorageDatabase.SQLite:
                    var locationService = workspaceServices.GetService<IPersistentStorageLocationService>();
                    if (locationService != null)
                    {
                        if (UseInMemoryWriteCache(workspaceServices))
                        {
                            return new SQLite.v2.SQLitePersistentStorageService(locationService);
                        }
                        else
                        {
                            return new SQLite.v1.SQLitePersistentStorageService(locationService);
                        }
                    }

                    break;
            }
#endif

            return NoOpPersistentStorageService.Instance;
        }

        private static bool UseInMemoryWriteCache(HostWorkspaceServices workspaceServices)
            => workspaceServices.Workspace.Options.GetOption(StorageOptions.SQLiteInMemoryWriteCache) ||
               workspaceServices.GetService<IExperimentationService>()?.IsExperimentEnabled(WellKnownExperimentNames.SQLiteInMemoryWriteCache) == true;
    }
}
