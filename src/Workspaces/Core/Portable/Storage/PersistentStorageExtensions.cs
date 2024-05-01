// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

#if !DOTNET_BUILD_FROM_SOURCE
using Microsoft.CodeAnalysis.SQLite.v2;
#endif

namespace Microsoft.CodeAnalysis.Storage;

internal static class PersistentStorageExtensions
{
    public static async Task<IChecksummedPersistentStorage> GetPersistentStorageAsync(this Solution solution, CancellationToken cancellationToken)
    {
        var storageService = solution.Services.GetPersistentStorageService();
        return await storageService.GetStorageAsync(SolutionKey.ToSolutionKey(solution), cancellationToken).ConfigureAwait(false);
    }

    public static Task<IChecksummedPersistentStorage> GetPersistentStorageAsync(this Project project, CancellationToken cancellationToken)
        => GetPersistentStorageAsync(project.Solution, cancellationToken);

    public static Task<IChecksummedPersistentStorage> GetPersistentStorageAsync(this Document document, CancellationToken cancellationToken)
        => GetPersistentStorageAsync(document.Project.Solution, cancellationToken);

    public static IChecksummedPersistentStorageService GetPersistentStorageService(this SolutionServices services)
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
            _ => NoOpPersistentStorageService.GetOrThrow(configuration),
        };
    }
}
