// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Storage.CloudCache;

namespace Microsoft.CodeAnalysis.Storage
{
    internal static class PersistentStorageExtensions
    {
        public static IChecksummedPersistentStorageService GetPersistentStorageService(this HostWorkspaceServices services, OptionSet options)
            => GetPersistentStorageService(services, GetPersistentStorageDatabase(options));

        public static StorageDatabase GetPersistentStorageDatabase(this OptionSet options)
            => options.GetOption(StorageOptions.CloudCacheFeatureFlag) ? StorageDatabase.CloudCache : options.GetOption(StorageOptions.Database);

        public static IChecksummedPersistentStorageService GetPersistentStorageService(this HostWorkspaceServices services, StorageDatabase database)
        {
            var configuration = services.GetRequiredService<IPersistentStorageConfiguration>();

            switch (database)
            {
                case StorageDatabase.SQLite:
                    return services.GetService<ISQLiteStorageServiceFactory>()?.Create() ??
                           NoOpPersistentStorageService.GetOrThrow(configuration);

                case StorageDatabase.CloudCache:
                    return services.GetService<ICloudCacheStorageServiceFactory>()?.Create() ??
                           NoOpPersistentStorageService.GetOrThrow(configuration);

                default:
                    return NoOpPersistentStorageService.GetOrThrow(configuration);
            }
        }

        public static ValueTask<IChecksummedPersistentStorage> GetPersistentStorageAsync(
            this Solution solution, bool checkBranchId, CancellationToken cancellationToken)
        {
            return GetPersistentStorageAsync(solution.Workspace.Services, solution.Options, SolutionKey.ToSolutionKey(solution), checkBranchId, cancellationToken);
        }

        public static ValueTask<IChecksummedPersistentStorage> GetPersistentStorageAsync(
            this HostWorkspaceServices services, OptionSet options, SolutionKey solutionKey, bool checkBranchId, CancellationToken cancellationToken)
        {
            return GetPersistentStorageAsync(services, GetPersistentStorageDatabase(options), solutionKey, checkBranchId, cancellationToken);
        }

        public static ValueTask<IChecksummedPersistentStorage> GetPersistentStorageAsync(
            this HostWorkspaceServices services, StorageDatabase database, SolutionKey solutionKey, bool checkBranchId, CancellationToken cancellationToken)
        {
            var persistenceStorageService = services.GetPersistentStorageService(database);

            var configuration = services.GetRequiredService<IPersistentStorageConfiguration>();
            return persistenceStorageService.GetStorageAsync(configuration, solutionKey, checkBranchId, cancellationToken);
        }
    }
}
