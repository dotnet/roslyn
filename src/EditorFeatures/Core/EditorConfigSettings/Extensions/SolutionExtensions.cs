// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Extensions
{
    internal static class SolutionExtensions
    {
        public static ImmutableArray<Project> GetProjectsUnderEditorConfigFile(this Solution solution, string pathToEditorConfigFile)
        {
            var directoryPathToCheck = Path.GetDirectoryName(pathToEditorConfigFile);
            if (directoryPathToCheck is null)
            {
                // we have been given an invalid file path
                return ImmutableArray<Project>.Empty;
            }

            var directoryInfoToCheck = new DirectoryInfo(directoryPathToCheck);
            var builder = ArrayBuilder<Project>.GetInstance();
            foreach (var project in solution.Projects)
            {
                if (!TryGetFolderContainingProject(project, out var projectDirectory))
                {
                    // Certain ASP.NET scenarios will create artificial projects that do not exist on disk
                    continue;
                }

                if (ContainsPath(directoryInfoToCheck, projectDirectory))
                {
                    builder.Add(project);
                }
            }

            return builder.ToImmutableAndFree();

            static bool TryGetFolderContainingProject(Project project, [NotNullWhen(true)] out DirectoryInfo? directoryInfo)
            {
                directoryInfo = null;
                if (project.FilePath is null)
                {
                    return false;
                }

                var fileDirectoryInfo = new DirectoryInfo(project.FilePath);
                if (fileDirectoryInfo.Parent is null)
                {
                    return false;
                }

                directoryInfo = fileDirectoryInfo.Parent;
                return true;
            }

            static bool ContainsPath(DirectoryInfo directoryContainingEditorConfig, DirectoryInfo projectDirectory)
            {
                if (directoryContainingEditorConfig.FullName == projectDirectory.FullName)
                {
                    return true;
                }

                // walk up each folder for the project and see if it matches
                // example match:
                // C:\source\roslyn\.editorconfig
                // C:\source\roslyn\src\project.csproj

                while (projectDirectory.Parent is not null)
                {
                    if (projectDirectory.Parent.FullName == directoryContainingEditorConfig.FullName)
                    {
                        return true;
                    }

                    projectDirectory = projectDirectory.Parent;
                }

                return false;
            }
        }
    }
}
