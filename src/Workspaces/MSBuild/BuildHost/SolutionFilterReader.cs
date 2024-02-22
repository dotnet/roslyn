// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild;

internal static class SolutionFilterReader
{
    public static bool IsSolutionFilterFilename(string filename)
    {
        return Path.GetExtension(filename).Equals(".slnf", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetAbsolutePath(string path, string baseDirectory)
        => FileUtilities.NormalizeAbsolutePath(FileUtilities.ResolveRelativePath(path, baseDirectory) ?? path);

    public static (string solutionFileName, ImmutableHashSet<string> projects) Read(string filterFilename)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(filterFilename));
        var solution = document.RootElement.GetProperty("solution");
        // Convert directory separators to the platform's default, since that is what MSBuild provide us.
        var solutionPath = solution.GetProperty("path").GetString()?.Replace('\\', Path.DirectorySeparatorChar);
        if (solutionPath is null)
            throw new Exception("No solution path found in the solution filter file.");

        if (Path.GetDirectoryName(filterFilename) is not string baseDirectory)
            throw new Exception("No directory could be found containing the solution filter.");

        var solutionFilename = GetAbsolutePath(solutionPath, baseDirectory);

        if (!File.Exists(solutionFilename))
            throw new Exception($"The solution file '{solutionFilename}' does not exist.");

        // The base directory for projects is the solution folder.
        baseDirectory = Path.GetDirectoryName(solutionFilename)!;
        RoslynDebug.AssertNotNull(baseDirectory);

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
            var absoluteProjectPath = GetAbsolutePath(projectPath, baseDirectory);
            if (File.Exists(absoluteProjectPath))
            {
                filterProjects.Add(absoluteProjectPath);
            }
        }

        return (solutionFilename, filterProjects.ToImmutable());
    }
}
