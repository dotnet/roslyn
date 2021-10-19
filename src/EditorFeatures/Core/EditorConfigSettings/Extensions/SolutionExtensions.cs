// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Extensions
{
    internal static class SolutionExtensions
    {
        public static ImmutableArray<Project> GetProjectsForPath(this Solution solution, string givenPath)
        {
            if (Path.GetDirectoryName(givenPath) is not string givenFolderPath ||
                solution.FilePath is not string solutionFilePath ||
                new DirectoryInfo(solutionFilePath).Parent is not DirectoryInfo solutionParentDirectory ||
                new DirectoryInfo(givenFolderPath).FullName == solutionParentDirectory.FullName)
            {
                return solution.Projects.ToImmutableArray();
            }

            var builder = ArrayBuilder<Project>.GetInstance();
            foreach (var project in solution.Projects)
            {
                if (project.FilePath is not string projectFilePath ||
                    new DirectoryInfo(projectFilePath).Parent is not DirectoryInfo projectDirectoryPath)
                {
                    continue;
                }

                if (ContainsPath(new DirectoryInfo(givenFolderPath), projectDirectoryPath))
                {
                    builder.Add(project);
                }
            }

            return builder.ToImmutableAndFree();

            static bool ContainsPath(DirectoryInfo givenPath, DirectoryInfo projectPath)
            {
                if (projectPath.FullName == givenPath.FullName)
                {
                    return true;
                }

                while (projectPath.Parent is not null)
                {
                    if (projectPath.Parent.FullName == givenPath.FullName)
                    {
                        return true;
                    }

                    projectPath = projectPath.Parent;
                }

                return false;
            }
        }
    }
}
