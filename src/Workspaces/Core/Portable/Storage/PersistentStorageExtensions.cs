// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

#if !DOTNET_BUILD_FROM_SOURCE
using Microsoft.CodeAnalysis.SQLite.v2;
#endif

namespace Microsoft.CodeAnalysis.Storage;

internal static class PersistentStorageExtensions
{
    public static IChecksummedPersistentStorageService GetPersistentStorageService(this SolutionServices services)
    {
        var configuration = services.GetRequiredService<IPersistentStorageConfiguration>();

#if DOTNET_BUILD_FROM_SOURCE
        return NoOpPersistentStorageService.GetOrThrow(configuration);
#else
        return services.GetService<SQLitePersistentStorageService>() ?? NoOpPersistentStorageService.GetOrThrow(configuration);
#endif
    }
}
