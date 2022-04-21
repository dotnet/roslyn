// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Storage.CloudCache;

#if !DOTNET_BUILD_FROM_SOURCE
using Microsoft.CodeAnalysis.SQLite.v2;
#endif

namespace Microsoft.CodeAnalysis.Storage
{
    internal static class PersistentStorageExtensions
    {
        public static IChecksummedPersistentStorageService GetPersistentStorageService(this HostWorkspaceServices services)
        {
            var workspaceConfiguration = services.GetService<IWorkspaceConfigurationService>();
            var configuration = services.GetRequiredService<IPersistentStorageConfiguration>();

            var cacheStorage = workspaceConfiguration?.Options.CacheStorage;
            return cacheStorage switch
            {
#if !DOTNET_BUILD_FROM_SOURCE
                StorageDatabase.SQLite
                    => services.GetService<SQLitePersistentStorageService>() ??
                       NoOpPersistentStorageService.GetOrThrow(configuration),
#endif
                StorageDatabase.CloudCache
                    => services.GetService<ICloudCacheStorageService>() ??
                       NoOpPersistentStorageService.GetOrThrow(configuration),
                _ => NoOpPersistentStorageService.GetOrThrow(configuration),
            };
        }
    }
}
