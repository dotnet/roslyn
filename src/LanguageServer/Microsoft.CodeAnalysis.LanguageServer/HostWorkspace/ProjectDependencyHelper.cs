// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal static class ProjectDependencyHelper
{
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

        var packageReferences = projectFileInfo.PackageReferences;
        var resolvedReferences = ArrayPool<bool>.Shared.Rent(packageReferences.Length);
        Array.Clear(resolvedReferences, 0, packageReferences.Length);
        int? assetsFileVersion = null;
        try
        {
            ProjectAssetsReader.FindResolvedPackageReferences(
                projectAssetsPath, packageReferences, resolvedReferences.AsSpan(0, packageReferences.Length), ref assetsFileVersion);

            using var _ = PooledHashSet<PackageReferenceItem>.GetInstance(out var unresolved);
            for (var i = 0; i < packageReferences.Length; i++)
            {
                if (!resolvedReferences[i])
                    unresolved.Add(packageReferences[i]);
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
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            // The file could not be read, so nothing is known about which packages are resolved. Report a
            // restore, which rewrites the file and recovers from a corrupt or partially written one.
            logger.LogError(e, string.Format(
                LanguageServerResources.Failed_to_read_project_assets_file_0_version_1_2,
                projectAssetsPath,
                assetsFileVersion?.ToString() ?? "<unknown>",
                e.Message));
            return true;
        }
        finally
        {
            ArrayPool<bool>.Shared.Return(resolvedReferences);
        }
    }

    internal static class TestAccessor
    {
        public static bool CheckProjectAssetsForUnresolvedDependencies(
            string projectAssetsPath,
            (string Name, string VersionRange)[] packageReferences,
            ILogger logger)
        {
            var packageReferenceItems = new PackageReferenceItem[packageReferences.Length];
            for (var i = 0; i < packageReferences.Length; i++)
                packageReferenceItems[i] = new(packageReferences[i].Name, packageReferences[i].VersionRange);

            var projectFileInfo = ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, "TestProject.csproj") with
            {
                ProjectAssetsFilePath = projectAssetsPath,
                PackageReferences = packageReferenceItems,
            };

            return ProjectDependencyHelper.CheckProjectAssetsForUnresolvedDependencies(projectFileInfo, logger);
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
