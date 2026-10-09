// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PrepareTests;

internal class TestDiscovery
{
    private static readonly object s_lock = new();

    public static bool RunDiscovery(string repoRootDirectory, string dotnetPath, bool isUnix)
    {
        var binDirectory = Path.Combine(repoRootDirectory, "artifacts", "bin");
        var assemblies = GetAssemblies(binDirectory, isUnix);
        var testDiscoveryWorkerFolder = Path.Combine(binDirectory, "TestDiscoveryWorker");
        var (dotnetCoreWorker, dotnetFrameworkWorker) = GetWorkers(binDirectory);

        Console.WriteLine($"Found {assemblies.Count} test assemblies");

        var success = true;
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        Parallel.ForEach(assemblies, assembly =>
        {
            var workerPath = assembly.Contains("net472")
                ? dotnetFrameworkWorker
                : dotnetCoreWorker;

            var (workerSucceeded, output) = RunWorker(dotnetPath, workerPath, assembly);
            lock (s_lock)
            {
                Console.WriteLine(output);
                success &= workerSucceeded;
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

    static (string tfm, string configuration) GetTfmAndConfiguration()
    {
        var dir = Path.GetDirectoryName(typeof(TestDiscovery).Assembly.Location);
        var tfm = Path.GetFileName(dir)!;
        var configuration = Path.GetFileName(Path.GetDirectoryName(dir))!;
        return (tfm, configuration);
    }

    static (string dotnetCoreWorker, string dotnetFrameworkWorker) GetWorkers(string binDirectory)
    {
        var (tfm, configuration) = GetTfmAndConfiguration();
        var testDiscoveryWorkerFolder = Path.Combine(binDirectory, "TestDiscoveryWorker");
        return (Path.Combine(testDiscoveryWorkerFolder, configuration, tfm, "TestDiscoveryWorker.dll"),
                Path.Combine(testDiscoveryWorkerFolder, configuration, "net472", "TestDiscoveryWorker.exe"));
    }

    static (bool Succeeded, string Output) RunWorker(string dotnetPath, string pathToWorker, string pathToAssembly)
    {
        var worker = new Process();
        var arguments = new StringBuilder();
        if (pathToWorker.EndsWith("dll"))
        {
            arguments.Append($"exec {pathToWorker}");
            worker.StartInfo.FileName = dotnetPath;
            AddDotNetRootEnvironmentVariables(worker.StartInfo, dotnetPath);
        }
        else
        {
            worker.StartInfo.FileName = pathToWorker;
        }

        var pathToOutput = Path.Combine(Path.GetDirectoryName(pathToAssembly)!, "testlist.json");
        arguments.Append($" --assembly {pathToAssembly} --out {pathToOutput}");

        var output = new StringBuilder();
        worker.StartInfo.Arguments = arguments.ToString();
        worker.StartInfo.UseShellExecute = false;
        worker.StartInfo.RedirectStandardOutput = true;
        worker.OutputDataReceived += (sender, e) => output.Append(e.Data);
        worker.Start();
        worker.BeginOutputReadLine();
        worker.WaitForExit();
        var success = worker.ExitCode == 0;
        worker.Close();

        return (success, output.ToString());
    }

    private static void AddDotNetRootEnvironmentVariables(ProcessStartInfo startInfo, string dotnetPath)
    {
        var dotnetDirectory = Path.GetDirectoryName(dotnetPath);
        if (string.IsNullOrEmpty(dotnetDirectory))
        {
            return;
        }

        dotnetDirectory = Path.GetFullPath(dotnetDirectory);
        startInfo.EnvironmentVariables["DOTNET_ROOT"] = dotnetDirectory;

        var architectureSuffix = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "X86",
            Architecture.X64 => "X64",
            Architecture.Arm64 => "ARM64",
            _ => null,
        };

        if (architectureSuffix is not null)
        {
            startInfo.EnvironmentVariables[$"DOTNET_ROOT_{architectureSuffix}"] = dotnetDirectory;
        }
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
            // (e.g. IdeBenchmarks references a unit test project). Those copies are never run and may lack the
            // app host xUnit v3 needs for discovery, so only consider assemblies under test project folders.
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
