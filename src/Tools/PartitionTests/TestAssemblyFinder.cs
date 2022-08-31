// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace PartitionTests;
internal class TestAssemblyFinder
{
    internal static ImmutableArray<AssemblyInfo> GetAssemblyFilePaths(string artifactsDirectory, string[] targetFrameworks, string configuration, string[] include, string[] exclude)
    {
        var list = new List<AssemblyInfo>();
        var binDirectory = Path.Combine(artifactsDirectory, "bin");
        foreach (var project in Directory.EnumerateDirectories(binDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(project);
            if (!shouldInclude(name, include) || shouldExclude(name, exclude))
            {
                continue;
            }

            var fileName = $"{name}.dll";
            // Find the dlls matching the request configuration and target frameworks.
            foreach (var targetFramework in targetFrameworks)
            {
                var targetFrameworkDirectory = Path.Combine(project, configuration, targetFramework);
                var filePath = Path.Combine(targetFrameworkDirectory, fileName);
                if (File.Exists(filePath))
                {
                    list.Add(new AssemblyInfo(filePath));
                }
                else if (Directory.Exists(targetFrameworkDirectory) && Directory.GetFiles(targetFrameworkDirectory, searchPattern: "*.UnitTests.dll") is { Length: > 0 } matches)
                {
                    // If the unit test assembly name doesn't match the project folder name, but still matches our "unit test" name pattern, we want to run it.
                    // If more than one such assembly is present in a project output folder, we assume something is wrong with the build configuration.
                    // For example, one unit test project might be referencing another unit test project.
                    if (matches.Length > 1)
                    {
                        var message = $"Multiple unit test assemblies found in '{targetFrameworkDirectory}'. Please adjust the build to prevent this. Matches:{Environment.NewLine}{string.Join(Environment.NewLine, matches)}";
                        throw new Exception(message);
                    }
                    list.Add(new AssemblyInfo(matches[0]));
                }
            }
        }

        if (list.Count == 0)
        {
            throw new InvalidOperationException($"Did not find any test assemblies");
        }

        list.Sort();
        return list.ToImmutableArray();

        static bool shouldInclude(string name, string[] includeFilter)
        {
            foreach (var pattern in includeFilter)
            {
                if (Regex.IsMatch(name, pattern.Trim('\'', '"')))
                {
                    return true;
                }
            }

            return false;
        }

        static bool shouldExclude(string name, string[] excludeFilter)
        {
            foreach (var pattern in excludeFilter)
            {
                if (Regex.IsMatch(name, pattern.Trim('\'', '"')))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
