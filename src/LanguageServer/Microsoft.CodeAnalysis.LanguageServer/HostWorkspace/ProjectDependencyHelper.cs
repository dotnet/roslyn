// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.Extensions.Logging;
using NuGet.ProjectModel;
using NuGet.Versioning;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal static class ProjectDependencyHelper
{
    private const string NuGetCacheFileName = "project.nuget.cache";

    internal static bool NeedsRestore(ProjectFileInfo newProjectFileInfo, ProjectFileInfo? previousProjectFileInfo, ILogger logger)
    {
        if (previousProjectFileInfo is null)
        {
            // This means we're likely opening the project for the first time.
            // We need to check the assets on disk to see if we need to restore.
            return CheckProjectAssetsForUnresolvedDependencies(newProjectFileInfo, logger);
        }

        var newPackageReferences = newProjectFileInfo.PackageReferences;
        var previousPackageReferences = previousProjectFileInfo.PackageReferences;

        if (newPackageReferences.Length != previousPackageReferences.Length)
        {
            // If the number of package references has changed then we need to run a restore.
            // We need to run a restore even in the removal case to ensure the items get removed from the compilation.
            return true;
        }

        if (!newPackageReferences.SetEquals(previousPackageReferences))
        {
            // The set of package references have different values.  We need to run a restore.
            return true;
        }

        // We have the same set of package references.  We still need to verify that the assets
        // exist on disk (they could have been deleted by a git clean for example).
        return CheckProjectAssetsForUnresolvedDependencies(newProjectFileInfo, logger);
    }

    private static bool CheckProjectAssetsForUnresolvedDependencies(ProjectFileInfo projectFileInfo, ILogger logger)
    {
        var projectAssetsPath = projectFileInfo.ProjectAssetsFilePath;
        if (!File.Exists(projectAssetsPath))
        {
            // If the file doesn't exist then all package references are unresolved.
            logger.LogWarning(string.Format(LanguageServerResources.Project_0_has_unresolved_dependencies, projectFileInfo.FilePath));
            return true;
        }

        if (projectFileInfo.PackageReferences.Length == 0)
        {
            // If there are no package references then there are no unresolved dependencies.
            return false;
        }

        // Fast path: consult the 'project.nuget.cache' file NuGet writes next to the assets file. If it
        // confirms the last restore still accounts for every package reference the project declares, we can
        // skip parsing the (potentially large) project.assets.json entirely.
        if (TryConfirmRestoreUpToDateFromNuGetCache(projectFileInfo, projectAssetsPath))
        {
            return false;
        }

        return CheckAssetsFileForUnresolvedReferences(projectFileInfo, projectAssetsPath, logger);
    }

    /// <summary>
    /// Uses the <c>project.nuget.cache</c> file NuGet writes next to <paramref name="projectAssetsPath"/> to
    /// confirm—without parsing the (potentially large) <c>project.assets.json</c>—that the last restore is still
    /// applicable to the project we're loading. The cache must be valid (<see cref="CacheFile.IsValid"/> verifies
    /// the format version matches the NuGet library we build against, the last restore succeeded, and a dependency
    /// graph hash was recorded) and the packages that restore produced
    /// (<see cref="CacheFile.ExpectedPackageFilePaths"/>) must satisfy every package reference the project currently
    /// declares. The latter check catches a project whose package set changed since the last restore—for example a
    /// package added directly to the project or via a shared <c>Directory.Packages.props</c>—in which case the
    /// previously restored assets no longer reflect the project and we can't claim it's up to date.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> only when the cache proves the restore is current for every package reference the
    /// project declares; otherwise <see langword="false"/> (the cache is missing, invalid, or doesn't account for
    /// all of the project's package references), in which case the caller should inspect the assets file directly.
    /// </returns>
    /// <remarks>
    /// This intentionally does not reproduce NuGet's dependency-graph-hash comparison, which needs the current
    /// restore dependency graph that is not available here. Instead it confirms the restored package set covers the
    /// project's declared references and otherwise defers to the precise assets-file check, so it can only ever
    /// accelerate the up-to-date case and never report a stale restore as current.
    /// </remarks>
    private static bool TryConfirmRestoreUpToDateFromNuGetCache(ProjectFileInfo projectFileInfo, string projectAssetsPath)
    {
        var cachePath = Path.Combine(Path.GetDirectoryName(projectAssetsPath)!, NuGetCacheFileName);
        if (!File.Exists(cachePath))
        {
            return false;
        }

        CacheFile cacheFile;
        try
        {
            using var stream = File.OpenRead(cachePath);

            // CacheFileFormat.Read handles malformed content internally (returning an invalid cache), so we
            // only need to guard against failures opening the file itself.
            cacheFile = CacheFileFormat.Read(stream, NuGet.Common.NullLogger.Instance, cachePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }

        // IsValid checks Version == CacheFile.CurrentVersion (the format version this NuGet build understands),
        // Success, and that a dependency graph hash was recorded.
        if (!cacheFile.IsValid || cacheFile.ExpectedPackageFilePaths is not { Count: > 0 })
        {
            return false;
        }

        // Reconstruct the set of packages the last restore produced and confirm every package reference the
        // project currently declares is present with a satisfying version. If a reference isn't accounted for,
        // the project changed since the restore (or the cache is otherwise incomplete), so we can't confirm it's
        // up to date and defer to the assets-file check.
        var restoredPackages = TryCreateRestoredPackagesMap(cacheFile.ExpectedPackageFilePaths);
        if (restoredPackages is null)
        {
            return false;
        }

        foreach (var reference in projectFileInfo.PackageReferences)
        {
            if (!IsPackageReferenceResolved(reference, restoredPackages))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Builds a map of package id to the versions present in the restore, derived from the
    /// <see cref="CacheFile.ExpectedPackageFilePaths"/>. Each entry looks like
    /// <c>{packagesFolder}/{id}/{version}/{id}.{version}.nupkg.sha512</c>, so the package id and resolved version
    /// are the two directories that contain the file. Returns <see langword="null"/> if no package could be parsed.
    /// </summary>
    private static Dictionary<string, ImmutableArray<NuGetVersion>>? TryCreateRestoredPackagesMap(IList<string> expectedPackageFilePaths)
    {
        var versionsById = new Dictionary<string, List<NuGetVersion>>(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in expectedPackageFilePaths)
        {
            var versionDirectory = Path.GetDirectoryName(filePath);
            var idDirectory = Path.GetDirectoryName(versionDirectory);
            if (string.IsNullOrEmpty(versionDirectory) || string.IsNullOrEmpty(idDirectory))
            {
                continue;
            }

            var id = Path.GetFileName(idDirectory);
            if (string.IsNullOrEmpty(id) || !NuGetVersion.TryParse(Path.GetFileName(versionDirectory), out var version))
            {
                continue;
            }

            if (!versionsById.TryGetValue(id, out var versions))
            {
                versions = new List<NuGetVersion>(capacity: 1);
                versionsById.Add(id, versions);
            }

            if (!versions.Contains(version))
            {
                versions.Add(version);
            }
        }

        if (versionsById.Count == 0)
        {
            return null;
        }

        var map = new Dictionary<string, ImmutableArray<NuGetVersion>>(versionsById.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (id, versions) in versionsById)
        {
            map.Add(id, versions.ToImmutableArray());
        }

        return map;
    }

    /// <summary>
    /// Determines whether <paramref name="reference"/> is satisfied by a restored package in
    /// <paramref name="restoredPackages"/> (a map of package id to the versions present in the restore). A
    /// reference is resolved when its name is present and at least one restored version satisfies the requested
    /// version range.
    /// </summary>
    private static bool IsPackageReferenceResolved(PackageReferenceItem reference, IReadOnlyDictionary<string, ImmutableArray<NuGetVersion>> restoredPackages)
    {
        if (!restoredPackages.TryGetValue(reference.Name, out var versions))
        {
            // The package name isn't in the restore at all.
            return false;
        }

        var requestedVersionRange = VersionRange.TryParse(reference.VersionRange, out var versionRange)
            ? versionRange
            : VersionRange.All;

        foreach (var version in versions)
        {
            if (requestedVersionRange.Satisfies(version))
            {
                return true;
            }
        }

        return false;
    }

    private static bool CheckAssetsFileForUnresolvedReferences(ProjectFileInfo projectFileInfo, string projectAssetsPath, ILogger logger)
    {
        // Iterate the project's package references and check if there is a package with the same name
        // and acceptable version in the lock file.

        var lockFileFormat = new LockFileFormat();
        var lockFile = lockFileFormat.Read(projectAssetsPath);
        var projectAssetsMap = CreateProjectAssetsMap(lockFile);

        using var _ = PooledHashSet<PackageReferenceItem>.GetInstance(out var unresolved);

        foreach (var reference in projectFileInfo.PackageReferences)
        {
            if (!IsPackageReferenceResolved(reference, projectAssetsMap))
            {
                unresolved.Add(reference);
            }
        }

        if (unresolved.Any())
        {
            var message = string.Format(LanguageServerResources.Project_0_has_unresolved_dependencies, projectFileInfo.FilePath)
                + Environment.NewLine
                + string.Join(Environment.NewLine, unresolved.Select(r => $"    {r.Name}-{r.VersionRange}"));
            logger.LogWarning(message);
            return true;
        }

        return false;

        static ImmutableDictionary<string, ImmutableArray<NuGetVersion>> CreateProjectAssetsMap(LockFile lockFile)
        {
            // Create a map of package names to all versions in the lock file.
            var map = lockFile.Libraries
                .GroupBy(l => l.Name, l => l.Version, StringComparer.OrdinalIgnoreCase)
                .ToImmutableDictionary(g => g.Key, g => g.ToImmutableArray(), StringComparer.OrdinalIgnoreCase);

            return map;
        }
    }

    internal static async Task RestoreProjectsAsync(WorkDoneProgressManager workDoneProgressManager, ImmutableArray<string> projectPaths, bool enableProgressReporting, DotnetCliHelper dotnetCliHelper, ILogger logger, CancellationToken cancellationToken)
    {
        if (projectPaths.IsEmpty)
            return;

        try
        {
            await RestoreHandler.RestoreAsync(projectPaths, workDoneProgressManager, dotnetCliHelper, logger, enableProgressReporting, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Restore was cancelled.  This is not a failure, it just leaves the project unrestored or partially restored (same as if the user cancelled a CLI restore).
            // We don't want this exception to bubble up to the project load queue however as it may need to additional work after this call.
            logger.LogWarning("Project restore was canceled.");
        }
    }
}
