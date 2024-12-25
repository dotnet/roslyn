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
using System.Text;

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

            var result = RunWorker(dotnetPath, workerPath, assembly);
            lock (s_lock)
            {
                success &= result;
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

    static bool RunWorker(string dotnetPath, string pathToWorker, string pathToAssembly)
    {
        var success = true;
        var pipeClient = new Process();
        var arguments = new List<string>();
        if (pathToWorker.EndsWith("dll"))
        {
            arguments.Add(pathToWorker);
            pipeClient.StartInfo.FileName = dotnetPath;
        }
        else
        {
            pipeClient.StartInfo.FileName = pathToWorker;
        }

        var errorOutput = new StringBuilder();

        using (var pipeServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
        {
            // Pass the client process a handle to the server.
            arguments.Add(pipeServer.GetClientHandleAsString());
            pipeClient.StartInfo.Arguments = string.Join(" ", arguments);
            pipeClient.StartInfo.UseShellExecute = false;

            // Errors will be logged to stderr, redirect to us so we can capture it.
            pipeClient.StartInfo.RedirectStandardError = true;
            pipeClient.ErrorDataReceived += PipeClient_ErrorDataReceived;
            pipeClient.Start();

            pipeClient.BeginErrorReadLine();

            pipeServer.DisposeLocalCopyOfClientHandle();

            try
            {
                // Read user input and send that to the client process.
                using var sw = new StreamWriter(pipeServer);
                sw.AutoFlush = true;
                // Send a 'sync message' and wait for client to receive it.
                sw.WriteLine("ASSEMBLY");
                // Send the console input to the client process.
                sw.WriteLine(pathToAssembly);
            }
            // Catch the IOException that is raised if the pipe is broken
            // or disconnected.
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error: {e.Message}");
                success = false;
            }
        }

        pipeClient.WaitForExit();
        success &= pipeClient.ExitCode == 0;
        pipeClient.Close();

        if (!success)
        {
            Console.WriteLine($"Failed to discover tests in {pathToAssembly}:{Environment.NewLine}{errorOutput}");
        }

        return success;

        void PipeClient_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            errorOutput.AppendLine(e.Data);
        }
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
