// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

    private static List<string> GetAssemblies(string binDirectory, bool isUnix)
    {
        var unitTestAssemblies = Directory.GetFiles(binDirectory, "*UnitTests.dll", SearchOption.AllDirectories);
        var integrationTestAssemblies = Directory.GetFiles(binDirectory, "*IntegrationTests.dll", SearchOption.AllDirectories);
        var assemblies = unitTestAssemblies.Concat(integrationTestAssemblies).Where(ShouldInclude);
        return assemblies.ToList();

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
