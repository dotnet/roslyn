﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.Extensions.Logging;
using NuGet.ProjectModel;
using NuGet.Versioning;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
internal static class ProjectDependencyHelper
{
    private const string UnresolvedProjectDependenciesName = "workspace/_roslyn_projectHasUnresolvedDependencies";

    internal static bool HasUnresolvedDependencies(ProjectFileInfo newProjectFileInfo, ProjectFileInfo? previousProjectFileInfo, ILogger logger)
    {
        if (previousProjectFileInfo is null)
        {
            // This means we're likely opening the project for the first time.
            // We need to check the assets on disk to see if we need to restore.
            return CheckProjectAssetsForUnresolvedDependencies(newProjectFileInfo, logger);
        }

        if (newProjectFileInfo.TargetFramework != previousProjectFileInfo.TargetFramework)
        {
            // If the target framework has changed then we need to run a restore.
            return true;
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

        if (projectFileInfo.PackageReferences.IsEmpty)
        {
            // If there are no package references then there are no unresolved dependencies.
            return false;
        }

        // Iterate the project's package references and check if there is a package with the same name
        // and acceptable version in the lock file.

        var lockFileFormat = new LockFileFormat();
        var lockFile = lockFileFormat.Read(projectAssetsPath);
        var projectAssetsMap = CreateProjectAssetsMap(lockFile);

        using var _ = PooledHashSet<PackageReference>.GetInstance(out var unresolved);

        foreach (var reference in projectFileInfo.PackageReferences)
        {
            if (!projectAssetsMap.TryGetValue(reference.Name, out var projectAssetsVersions))
            {
                // If the package name isn't in the lock file then it's unresolved.
                unresolved.Add(reference);
                continue;
            }

            var requestedVersionRange = VersionRange.TryParse(reference.VersionRange, out var versionRange)
                ? versionRange
                : VersionRange.All;

            var projectAssetsHasVersion = projectAssetsVersions.Any(projectAssetsVersion => SatisfiesVersion(requestedVersionRange, projectAssetsVersion));
            if (!projectAssetsHasVersion)
            {
                // If the package name is in the lock file but none of the versions satisfy the requested version range then it's unresolved.
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

        static bool SatisfiesVersion(VersionRange requestedVersionRange, NuGetVersion projectAssetsVersion)
        {
            return requestedVersionRange.Satisfies(projectAssetsVersion);
        }
    }

    internal static async Task RestoreProjectsAsync(ImmutableHashSet<string> projectPaths, CancellationToken cancellationToken)
    {
        if (projectPaths.IsEmpty)
        {
            return;
        }

        Contract.ThrowIfNull(LanguageServerHost.Instance, "We don't have an LSP channel yet to send this request through.");
        var languageServerManager = LanguageServerHost.Instance.GetRequiredLspService<IClientLanguageServerManager>();
        var unresolvedParams = new UnresolvedDependenciesParams(projectPaths.ToArray());
        await languageServerManager.SendRequestAsync(UnresolvedProjectDependenciesName, unresolvedParams, cancellationToken);
    }

    [DataContract]
    private record UnresolvedDependenciesParams(
        [property: DataMember(Name = "projectFilePaths")] string[] ProjectFilePaths);
}
