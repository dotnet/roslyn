// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;

namespace Microsoft.CodeAnalysis.MSBuild
{
    public partial class MSBuildProjectLoader
    {
        private static class SolutionFilterReader
        {
            public static bool IsSolutionFilterFilename(string filename)
            {
                return Path.GetExtension(filename).Equals(".slnf", StringComparison.OrdinalIgnoreCase);
            }

            public static bool TryRead(string filterFilename, PathResolver pathResolver, out string solutionFilename, out ImmutableHashSet<string> projectFilter)
            {
                try
                {
                    using var document = JsonDocument.Parse(File.ReadAllText(filterFilename));
                    var solution = document.RootElement.GetProperty("solution");
                    // Convert directory separators to the platform's default, since that is what MSBuild provide us.
                    var solutionPath = solution.GetProperty("path").GetString()?.Replace('\\', Path.DirectorySeparatorChar);

                    if (!pathResolver.TryGetAbsoluteSolutionPath(solutionPath, baseDirectory: Path.GetDirectoryName(filterFilename), DiagnosticReportingMode.Throw, out solutionFilename))
                    {
                        // TryGetAbsoluteSolutionPath should throw before we get here.
                        solutionFilename = string.Empty;
                        projectFilter = ImmutableHashSet<string>.Empty;
                        return false;
                    }

                    if (!File.Exists(solutionFilename))
                    {
                        projectFilter = ImmutableHashSet<string>.Empty;
                        return false;
                    }

                    // The base directory for projects is the solution folder.
                    var baseDirectory = Path.GetDirectoryName(solutionFilename);

                    var filterProjects = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var project in solution.GetProperty("projects").EnumerateArray())
                    {
                        // Convert directory separators to the platform's default, since that is what MSBuild provide us.
                        var projectPath = project.GetString()?.Replace('\\', Path.DirectorySeparatorChar);
                        if (projectPath is null)
                        {
                            continue;
                        }

                        // Fill the filter with the absolute project paths.
                        if (pathResolver.TryGetAbsoluteProjectPath(projectPath, baseDirectory, DiagnosticReportingMode.Throw, out var absoluteProjectPath))
                        {
                            filterProjects.Add(absoluteProjectPath);
                        }
                    }

                    projectFilter = filterProjects.ToImmutable();
                    return true;
                }
                catch
                {
                    solutionFilename = string.Empty;
                    projectFilter = ImmutableHashSet<string>.Empty;
                    return false;
                }
            }
        }
    }
}
