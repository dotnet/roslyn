// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild;

internal partial class SolutionFileReader
{
    private static class SolutionFilterReader
    {
        public static bool IsSolutionFilterFilename(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".slnf", StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryRead(string filterFilePath, PathResolver pathResolver, [NotNullWhen(true)] out string? solutionFilePath, out ImmutableHashSet<string> projectFilter)
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(filterFilePath));
                var solution = document.RootElement.GetProperty("solution");
                // Convert directory separators to the platform's default, since that is what MSBuild provide us.
                var solutionPath = solution.GetProperty("path").GetString()?.Replace('\\', Path.DirectorySeparatorChar);
                if (solutionPath is null || Path.GetDirectoryName(filterFilePath) is not string baseDirectory)
                {
                    solutionFilePath = string.Empty;
                    projectFilter = [];
                    return false;
                }

                pathResolver = pathResolver.WithBaseDirectory(baseDirectory);
                Contract.ThrowIfFalse(pathResolver.TryGetAbsoluteSolutionFilePath(solutionPath, DiagnosticReportingMode.Throw, out solutionFilePath));

                if (!File.Exists(solutionFilePath))
                {
                    projectFilter = [];
                    return false;
                }

                // The base directory for projects is the solution folder.
                baseDirectory = Path.GetDirectoryName(solutionFilePath)!;
                RoslynDebug.AssertNotNull(baseDirectory);
                pathResolver = pathResolver.WithBaseDirectory(baseDirectory);

                var filterProjects = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var project in solution.GetProperty("projects").EnumerateArray())
                {
                    // Convert directory separators to the platform's default, since that is what MSBuild provide us.
                    var projectFilePath = project.GetString()?.Replace('\\', Path.DirectorySeparatorChar);
                    if (projectFilePath is null)
                    {
                        continue;
                    }

                    // Fill the filter with the absolute project paths.
                    Contract.ThrowIfFalse(pathResolver.TryGetAbsoluteProjectFilePath(projectFilePath, DiagnosticReportingMode.Throw, out var absoluteProjectFilePath));
                    filterProjects.Add(absoluteProjectFilePath);
                }

                projectFilter = filterProjects.ToImmutable();
                return true;
            }
            catch
            {
                solutionFilePath = string.Empty;
                projectFilter = [];
                return false;
            }
        }
    }
}
