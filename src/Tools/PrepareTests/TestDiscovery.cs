// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Threading;
using RunTests;

namespace PrepareTests;
internal class TestDiscovery
{
    public static async Task<bool> RunDiscovery(string repoRootDirectory, string dotnetPath, bool isUnix)
    {
        var binDirectory = Path.Combine(repoRootDirectory, "artifacts", "bin");
        var assemblies = GetAssemblies(binDirectory, isUnix);
        var testDiscoveryWorkerFolder = Path.Combine(binDirectory, "TestDiscoveryWorker");
        var (dotnetCoreWorker, dotnetFrameworkWorker) = GetWorkers(binDirectory);

        Console.WriteLine($"Found {assemblies.Count} test assemblies");

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var list = new List<Task<bool>>();
        foreach (var assemblyPath in assemblies)
        {
            var workerPath = assemblyPath.Contains("net472")
                ? dotnetFrameworkWorker
                : dotnetCoreWorker;
            list.Add(RunWorker(dotnetPath, workerPath, assemblyPath));
        }

        await Task.WhenAll(list).ConfigureAwait(false);
        stopwatch.Stop();

        Console.WriteLine($"Discovered tests in {stopwatch.Elapsed}");
        return list.All(x => x.Result);
    }

    static (string dotnetCoreWorker, string dotnetFrameworkWorker) GetWorkers(string binDirectory)
    {
        var testDiscoveryWorkerFolder = Path.Combine(binDirectory, "TestDiscoveryWorker");
        var configuration = Directory.Exists(Path.Combine(testDiscoveryWorkerFolder, "Debug")) ? "Debug" : "Release";
        return (Path.Combine(testDiscoveryWorkerFolder, configuration, "net7.0", "TestDiscoveryWorker.dll"),
                Path.Combine(testDiscoveryWorkerFolder, configuration, "net472", "TestDiscoveryWorker.exe"));
    }

    static async Task<bool> RunWorker(string dotnetPath, string pathToWorker, string pathToAssembly)
    {
        var arguments = new List<string>();
        var startInfo = new ProcessStartInfo();
        if (pathToWorker.EndsWith("dll"))
        {
            arguments.Add(pathToWorker);
            startInfo.FileName = dotnetPath;
        }
        else
        {
            startInfo.FileName = pathToWorker;
        }

        arguments.Add(pathToAssembly);
        startInfo.Arguments = string.Join(" ", arguments);
        startInfo.UseShellExecute = false;

        var result = ProcessRunner.CreateProcess(startInfo);
        await result.Result;

        return result.Process.ExitCode == 0;
    }

    private static List<string> GetAssemblies(string binDirectory, bool isUnix)
    {
        var unitTestAssemblies = Directory.GetFiles(binDirectory, "*.UnitTests.dll", SearchOption.AllDirectories).Where(ShouldInclude);
        return unitTestAssemblies.ToList();

        bool ShouldInclude(string path)
        {
            if (isUnix)
            {
                // Our unix build will build net framework dlls for multi-targeted projects.
                // These are not valid testing on unix and discovery will throw if we try.
                return Path.GetFileName(Path.GetDirectoryName(path)) != "net472";
            }

            return true;
        }
    }
}
