// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PrepareTests;

internal class TestDiscovery
{
    private static readonly object s_lock = new();

    public static bool RunDiscovery(string repoRootDirectory, string dotnetPath, bool isUnix)
    {
        var binDirectory = Path.Combine(repoRootDirectory, "artifacts", "bin");
        var assemblies = GetAssemblies(binDirectory, isUnix);

        Console.WriteLine($"Found {assemblies.Count} test assemblies");

        var success = true;
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        Parallel.ForEach(assemblies, assembly =>
        {
            var (discoverySucceeded, output) = DiscoverTests(dotnetPath, assembly);
            lock (s_lock)
            {
                Console.WriteLine(output);
                success &= discoverySucceeded;
            }
        });
        stopwatch.Stop();

        if (success)
        {
            Console.WriteLine($"Discovered tests in {stopwatch.Elapsed}");
        }
        else
        {
            Console.WriteLine($"Test discovery failed");
        }

        return success;
    }

    /// <summary>
    /// Lists the tests in an assembly by running the test executable with xUnit's <c>-list methods/json</c>, and
    /// writes the result to <c>testlist.json</c> next to the assembly. The result is a JSON array of fully qualified
    /// test method names, which RunTests reads when scheduling tests.
    /// </summary>
    static (bool Succeeded, string Output) DiscoverTests(string dotnetPath, string pathToAssembly)
    {
        var assemblyFileName = Path.GetFileName(pathToAssembly);
        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        // .NET Framework test assemblies are executables. On .NET Core the test assembly is run through dotnet.
        if (pathToAssembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.FileName = dotnetPath;
            startInfo.ArgumentList.Add("exec");
            startInfo.ArgumentList.Add(pathToAssembly);
        }
        else
        {
            startInfo.FileName = pathToAssembly;
        }

        startInfo.ArgumentList.Add("-list");
        startInfo.ArgumentList.Add("methods/json");

        // On Unix xUnit emits ANSI color escape codes even when output is redirected, which would follow the JSON.
        startInfo.ArgumentList.Add("-noColor");

        using var process = Process.Start(startInfo)!;
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        var output = standardOutput.GetAwaiter().GetResult();
        var error = standardError.GetAwaiter().GetResult();
        if (process.ExitCode != 0)
        {
            return (false, $"Discovering tests in {assemblyFileName} failed with exit code {process.ExitCode}:{Environment.NewLine}{output}{error}");
        }

        string[]? testNames;
        try
        {
            testNames = JsonSerializer.Deserialize<string[]>(output);
        }
        catch (JsonException ex)
        {
            return (false, $"Discovering tests in {assemblyFileName} did not produce a JSON list of tests: {ex.Message}{Environment.NewLine}{output}{error}");
        }

        if (testNames is null)
        {
            return (false, $"Discovering tests in {assemblyFileName} did not produce a list of tests:{Environment.NewLine}{output}{error}");
        }

        File.WriteAllText(Path.Combine(Path.GetDirectoryName(pathToAssembly)!, "testlist.json"), output);
        return (true, $"Discovered {testNames.Length} tests in {assemblyFileName}");
    }

    private static List<string> GetAssemblies(string binDirectory, bool isUnix)
    {
        var assemblies = new[] { "*UnitTests.dll", "*UnitTests.exe", "*IntegrationTests.dll", "*IntegrationTests.exe" }
            .SelectMany(pattern => Directory.GetFiles(binDirectory, pattern, SearchOption.AllDirectories))
            .Where(ShouldInclude);
        return assemblies.ToList();

        bool ShouldInclude(string path)
        {
            // Test assemblies are also copied into the output of non-test projects that reference them
            // (e.g. IdeBenchmarks references a unit test project). Those copies are never run, so only consider
            // assemblies under test project folders.
            var projectDirName = Path.GetRelativePath(binDirectory, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
            if (!projectDirName.Contains("UnitTests", StringComparison.Ordinal) &&
                !projectDirName.Contains("IntegrationTests", StringComparison.Ordinal))
            {
                return false;
            }

            // .NET Framework test assemblies are native executables. On .NET Core the .exe is only an app host
            // for the managed .dll.
            var dirName = Path.GetFileName(Path.GetDirectoryName(path));
            if (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && dirName is not "net472")
            {
                return false;
            }

            if (dirName is "net472" && path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                File.Exists(Path.ChangeExtension(path, ".exe")))
            {
                return false;
            }

            if (isUnix)
            {
                // Our unix build will build net framework dlls for multi-targeted projects.
                // These are not valid testing on unix and discovery will throw if we try.
                if (dirName is "net472")
                {
                    return false;
                }

                // TFMs with OS-specific suffixes (e.g. net10-windows) should not run on unix.
                if (dirName is not null && dirName.EndsWith("-windows", StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
