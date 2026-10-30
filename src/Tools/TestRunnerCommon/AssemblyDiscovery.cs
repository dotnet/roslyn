// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace TestRunner;

internal static class AssemblyDiscovery
{
    internal static ImmutableArray<AssemblyInfo> GetAssemblyFilePaths(Options options)
    {
        var list = new List<AssemblyInfo>();
        var binDirectory = Path.Combine(options.ArtifactsDirectory, "bin");
        foreach (var project in Directory.EnumerateDirectories(binDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(project);
            if (!shouldInclude(name, options) || shouldExclude(name, options))
            {
                Console.WriteLine($"Skipping {name} because it is not included or is excluded");
                continue;
            }

            var fileName = $"{name}.dll";
            var configDirectory = Path.Combine(project, options.Configuration);
            if (!Directory.Exists(configDirectory))
            {
                Console.WriteLine($"Skipping {name} because {options.Configuration} does not exist");
                continue;
            }

            foreach (var targetFrameworkDirectory in Directory.EnumerateDirectories(configDirectory))
            {
                var tfm = Path.GetFileName(targetFrameworkDirectory);
                if (!IsMatch(options.TestRuntime, tfm))
                {
                    Console.WriteLine($"Skipping {name} {tfm} does not match the target framework");
                    continue;
                }

                var filePath = Path.Combine(targetFrameworkDirectory, fileName);
                if (File.Exists(filePath))
                {
                    list.Add(new AssemblyInfo(filePath));
                }
                else if (Directory.GetFiles(targetFrameworkDirectory, searchPattern: "*.UnitTests.dll") is { Length: > 0 } matches)
                {
                    // A project may have a different assembly name, but must not contain another test project's output.
                    if (matches.Length > 1)
                    {
                        var message = $"Multiple unit test assemblies found in '{targetFrameworkDirectory}'. Please adjust the build to prevent this. Matches:{Environment.NewLine}{string.Join(Environment.NewLine, matches)}";
                        throw new Exception(message);
                    }

                    Console.WriteLine($"Found unit test assembly '{matches[0]}' in '{targetFrameworkDirectory}'");
                    list.Add(new AssemblyInfo(matches[0]));
                }
                else
                {
                    Console.WriteLine($"{targetFrameworkDirectory} does not contain unit tests");
                }
            }
        }

        if (list.Count == 0)
        {
            throw new InvalidOperationException($"Did not find any test assemblies");
        }

        list.Sort();
        return list.ToImmutableArray();

        static bool shouldInclude(string name, Options options)
        {
            foreach (var pattern in options.IncludeFilter)
            {
                if (Regex.IsMatch(name, pattern.Trim('\'', '"')))
                {
                    return true;
                }
            }

            return false;
        }

        static bool shouldExclude(string name, Options options)
        {
            foreach (var pattern in options.ExcludeFilter)
            {
                if (Regex.IsMatch(name, pattern.Trim('\'', '"')))
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsMatch(TestRuntime testRuntime, string dirName)
        {
            if (dirName is "net472")
                return (testRuntime & TestRuntime.Framework) != 0;

            if (Regex.IsMatch(dirName, @"^net\d+\."))
                return (testRuntime & TestRuntime.Core) != 0 && IsCompatibleWithCurrentPlatform(dirName);

            return false;
        }

        static bool IsCompatibleWithCurrentPlatform(string tfmDirName)
        {
            if (tfmDirName.EndsWith("-windows", StringComparison.Ordinal))
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            }

            if (tfmDirName.EndsWith("-macos", StringComparison.Ordinal))
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            }

            return true;
        }
    }
}
