// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Extensions
{
    internal static class SolutionExtensions
    {
        public static ImmutableArray<Project> GetProjectsForPath(this Solution solution, string givenPath)
        {
            var givenFolder = new DirectoryInfo(givenPath).Parent;
            var solutionFolder = new DirectoryInfo(solution.FilePath).Parent;
            if (givenFolder.FullName == solutionFolder.FullName)
            {
                return solution.Projects.ToImmutableArray();
            }

            var builder = ArrayBuilder<Project>.GetInstance();
            foreach (var (projectDirectoryPath, project) in solution.Projects.Select(p => (new DirectoryInfo(p.FilePath).Parent, p)))
            {
                if (ContainsPath(givenFolder, projectDirectoryPath))
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
