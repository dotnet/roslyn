// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

/// <summary>
/// Caches <see cref="ProjectFileInfo"/> results from design-time builds to disk,
/// so that file-based app projects can be loaded more quickly on subsequent opens.
/// </summary>
internal static class ProjectFileInfoCache
{
    private const string CacheFileName = "projectfileinfo.cache";

    private static string GetCacheFilePath(string entryPointFilePath)
    {
        var artifactsPath = VirtualProjectXmlProvider.GetArtifactsPath(entryPointFilePath);
        return Path.Combine(artifactsPath, CacheFileName);
    }

    /// <summary>
    /// Tries to read cached <see cref="ProjectFileInfo"/> entries from disk for the given entry point file.
    /// Returns <c>default</c> if no cache exists or the cache cannot be read.
    /// </summary>
    public static ImmutableArray<ProjectFileInfo> TryReadFromCache(string entryPointFilePath, ILogger logger)
    {
        var cacheFilePath = GetCacheFilePath(entryPointFilePath);
        if (!File.Exists(cacheFilePath))
            return default;

        var result = IOUtilities.PerformIO(() =>
        {
            using var fileStream = File.OpenRead(cacheFilePath);
            return JsonSerializer.Deserialize<ImmutableArray<ProjectFileInfo>>(fileStream, JsonSettings.SingleLineSerializerOptions);
        });

        // Sanity check: the cached info should be non-empty and contain the entry point file.
        if (result.IsDefault || result.IsEmpty)
        {
            logger.LogDebug("ProjectFileInfo cache for '{entryPointFilePath}' was empty or could not be read.", entryPointFilePath);
            return default;
        }

        logger.LogInformation("Successfully read ProjectFileInfo cache for '{entryPointFilePath}'.", entryPointFilePath);
        return result;
    }

    /// <summary>
    /// Writes <see cref="ProjectFileInfo"/> entries to disk for the given entry point file.
    /// </summary>
    public static void WriteToCache(string entryPointFilePath, ImmutableArray<ProjectFileInfo> projectFileInfos, ILogger logger)
    {
        if (projectFileInfos.IsDefaultOrEmpty)
            return;

        var cacheFilePath = GetCacheFilePath(entryPointFilePath);

        IOUtilities.PerformIO(() =>
        {
            var directory = Path.GetDirectoryName(cacheFilePath);
            if (directory != null)
                Directory.CreateDirectory(directory);

            using var fileStream = File.Create(cacheFilePath);
            JsonSerializer.Serialize(fileStream, projectFileInfos, JsonSettings.SingleLineSerializerOptions);
        });

        logger.LogInformation("Wrote ProjectFileInfo cache for '{entryPointFilePath}'.", entryPointFilePath);
    }
}
