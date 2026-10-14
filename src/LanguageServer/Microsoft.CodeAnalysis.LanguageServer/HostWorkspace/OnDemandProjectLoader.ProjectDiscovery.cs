// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal sealed partial class OnDemandProjectLoader
{
    internal sealed class ProjectDiscovery(
        ImmutableArray<string> supportedProjectFileExtensions,
        ILoggerFactory loggerFactory)
    {
        private static readonly StringComparison s_pathComparison = PathUtilities.IsUnixLikePlatform ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        private readonly ImmutableHashSet<string> _supportedProjectFileExtensions = supportedProjectFileExtensions.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger _logger = loggerFactory.CreateLogger<ProjectDiscovery>();

        internal ImmutableArray<string> DiscoverProjects(
            string directory,
            ImmutableHashSet<string> workspaceFolders,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!PathUtilities.IsAbsolute(directory))
                return [];

            var workspaceFolder = GetDeepestContainingWorkspaceFolder(directory, workspaceFolders, cancellationToken);
            if (workspaceFolder is null)
                return [];

            var currentDirectory = directory;
            while (currentDirectory is not null && PathUtilities.IsSameDirectoryOrChildOf(currentDirectory, workspaceFolder, s_pathComparison))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var projects = GetProjectsInDirectory(currentDirectory, cancellationToken);
                if (!projects.IsEmpty)
                    return projects;

                if (PathUtilities.Comparer.Equals(currentDirectory, workspaceFolder))
                    break;

                currentDirectory = Path.GetDirectoryName(currentDirectory);
            }

            return [];
        }

        private static string? GetDeepestContainingWorkspaceFolder(
            string directory,
            ImmutableHashSet<string> workspaceFolders,
            CancellationToken cancellationToken)
        {
            string? deepestWorkspaceFolder = null;
            foreach (var workspaceFolder in workspaceFolders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (PathUtilities.IsSameDirectoryOrChildOf(directory, workspaceFolder, s_pathComparison) &&
                    (deepestWorkspaceFolder is null || workspaceFolder.Length > deepestWorkspaceFolder.Length))
                {
                    deepestWorkspaceFolder = workspaceFolder;
                }
            }

            return deepestWorkspaceFolder;
        }

        private ImmutableArray<string> GetProjectsInDirectory(string directory, CancellationToken cancellationToken)
        {
            try
            {
                var builder = ImmutableArray.CreateBuilder<string>();
                foreach (var filePath in Directory.EnumerateFiles(directory))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (IsSupportedProject(filePath))
                        builder.Add(filePath);
                }

                return builder.ToImmutable();
            }
            catch (Exception exception) when (IOUtilities.IsNormalIOException(exception))
            {
                _logger.LogWarning(
                    exception,
                    "Could not enumerate project files in '{Directory}' while loading a project on demand.",
                    directory);
                return [];
            }
        }

        internal bool IsSupportedProject(string filePath)
            => _supportedProjectFileExtensions.Contains(Path.GetExtension(filePath));
    }
}
