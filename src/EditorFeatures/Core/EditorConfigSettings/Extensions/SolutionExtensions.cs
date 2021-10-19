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
            if (Path.GetDirectoryName(givenPath) is not string givenFolderPath)
            {
                // we have been given an invalid file path
                return ImmutableArray<Project>.Empty;
            }

            if (SolutionIsInSameFolder(solution, givenFolderPath))
            {
                // All projects are applicable
                return solution.Projects.ToImmutableArray();
            }

            var builder = ArrayBuilder<Project>.GetInstance();
            foreach (var project in solution.Projects)
            {
                if (project.FilePath is not string projectFilePath ||
                    new DirectoryInfo(projectFilePath).Parent is not DirectoryInfo projectDirectoryPath)
                {
                    // Certain ASP.NET scenarios will create artificial projects that do not exist on disk
                    continue;
                }

                if (ContainsPath(new DirectoryInfo(givenFolderPath), projectDirectoryPath))
                {
                    builder.Add(project);
                }
            }

            return builder.ToImmutableAndFree();

            static bool SolutionIsInSameFolder(Solution givenSolution, string givenFolderPath)
            {
                if (givenSolution.FilePath is not string solutionFilePath)
                {
                    // The solution path is null
                    return false;
                }

                var givenDirectory = new DirectoryInfo(givenFolderPath);
                var solutionParentDirectory = new DirectoryInfo(solutionFilePath).Parent;
                if (solutionParentDirectory is null)
                {
                    // we have been given an invalid file path
                    return false;
                }

                if (solutionParentDirectory.FullName == givenDirectory.FullName)
                {
                    // Solution is in the same folder
                    return true;
                }

                return false;
            }

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
