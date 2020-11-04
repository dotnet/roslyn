// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using System.Collections.Immutable;
using Newtonsoft.Json;
using System.Diagnostics;

namespace RunTests
{
    internal sealed partial class Program
    {
        private static readonly ImmutableHashSet<string> PrimaryProcessNames = ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "devenv",
            "xunit.console",
            "xunit.console.x86",
            "ServiceHub.RoslynCodeAnalysisService",
            "ServiceHub.RoslynCodeAnalysisService32");

        internal const int ExitSuccess = 0;
        internal const int ExitFailure = 1;

        private const long MaxTotalDumpSizeInMegabytes = 4096;

        internal static async Task<int> Main(string[] args)
        {
            Logger.Log("RunTest command line");
            Logger.Log(string.Join(" ", args));

            var options = Options.Parse(args);
            if (options == null)
            {
                return ExitFailure;
            }

            if (options.CollectDumps)
            {
                if (!DumpUtil.IsAdministrator())
                {
                    ConsoleUtil.WriteLine(ConsoleColor.Yellow, "Dump collection specified but user is not administrator so cannot modify registry");
                }
                else
                {
                    DumpUtil.EnableRegistryDumpCollection(options.LogFilesDirectory);
                }
            }

            try
            {
                // Setup cancellation for ctrl-c key presses
                using var cts = new CancellationTokenSource();
                Console.CancelKeyPress += delegate
                {
                    cts.Cancel();
                    DisableRegistryDumpCollection();
                };

                int result;
                if (options.Timeout is { } timeout)
                {
                    result = await RunAsync(options, timeout, cts.Token);
                }
                else
                {
                    result = await RunAsync(options, cts.Token);
                }

                CheckTotalDumpFilesSize();
                return result;
            }
            finally
            {
                DisableRegistryDumpCollection();
            }

            void DisableRegistryDumpCollection()
            {
                if (options.CollectDumps && DumpUtil.IsAdministrator())
                {
                    DumpUtil.DisableRegistryDumpCollection();
                }
            }
        }

        private static async Task<int> RunAsync(Options options, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var runTask = RunAsync(options, cts.Token);
            var timeoutTask = Task.Delay(options.Timeout.Value, cancellationToken);

            var finishedTask = await Task.WhenAny(timeoutTask, runTask);
            if (finishedTask == timeoutTask)
            {
                await HandleTimeout(options, cancellationToken);
                cts.Cancel();

                try
                {
                    // Need to await here to ensure that all of the child processes are properly 
                    // killed before we exit.
                    await runTask;
                }
                catch
                {
                    // Cancellation exceptions expected here. 
                }

                return ExitFailure;
            }

            return await runTask;
        }

        private static async Task<int> RunAsync(Options options, CancellationToken cancellationToken)
        {
            if (!CheckAssemblyList(options))
            {
                return ExitFailure;
            }

            var testExecutor = CreateTestExecutor(options);
            var testRunner = new TestRunner(options, testExecutor);
            var start = DateTime.Now;
            var assemblyInfoList = GetAssemblyList(options);

            ConsoleUtil.WriteLine($"Proc dump location: {options.ProcDumpFilePath}");
            ConsoleUtil.WriteLine($"Running {options.Assemblies.Count} test assemblies in {assemblyInfoList.Count} partitions");

            var result = await testRunner.RunAllAsync(assemblyInfoList, cancellationToken).ConfigureAwait(true);
            var elapsed = DateTime.Now - start;

            ConsoleUtil.WriteLine($"Test execution time: {elapsed}");

            LogProcessResultDetails(result.ProcessResults);
            WriteLogFile(options);
            DisplayResults(options.Display, result.TestResults);

            if (!result.Succeeded)
            {
                ConsoleUtil.WriteLine(ConsoleColor.Red, $"Test failures encountered");
                return ExitFailure;
            }

            ConsoleUtil.WriteLine($"All tests passed");
            return ExitSuccess;
        }

        private static void LogProcessResultDetails(ImmutableArray<ProcessResult> processResults)
        {
            Logger.Log("### Begin logging executed process details");
            foreach (var processResult in processResults)
            {
                var process = processResult.Process;
                var startInfo = process.StartInfo;
                Logger.Log($"### Begin {process.Id}");
                Logger.Log($"### {startInfo.FileName} {startInfo.Arguments}");
                Logger.Log($"### Exit code {process.ExitCode}");
                Logger.Log("### Standard Output");
                foreach (var line in processResult.OutputLines)
                {
                    Logger.Log(line);
                }
                Logger.Log("### Standard Error");
                foreach (var line in processResult.ErrorLines)
                {
                    Logger.Log(line);
                }
                Logger.Log($"### End {process.Id}");
            }

            Logger.Log("End logging executed process details");
        }

        private static void WriteLogFile(Options options)
        {
            var logFilePath = Path.Combine(options.LogFilesDirectory, "runtests.log");
            try
            {
                using (var writer = new StreamWriter(logFilePath, append: false))
                {
                    Logger.WriteTo(writer);
                }
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteLine($"Error writing log file {logFilePath}");
                ConsoleUtil.WriteLine(ex.ToString());
            }

            Logger.Clear();
        }

        /// <summary>
        /// Invoked when a timeout occurs and we need to dump all of the test processes and shut down 
        /// the runnner.
        /// </summary>
        private static async Task HandleTimeout(Options options, CancellationToken cancellationToken)
        {
            async Task DumpProcess(Process targetProcess, string procDumpExeFilePath, string dumpFilePath)
            {
                var name = targetProcess.ProcessName;

                // Our space for saving dump files is limited. Skip dumping for processes that won't contribute
                // to bug investigations.
                if (name == "procdump" || name == "conhost")
                {
                    return;
                }

                ConsoleUtil.Write($"Dumping {name} {targetProcess.Id} to {dumpFilePath} ... ");
                try
                {
                    var args = $"-accepteula -ma {targetProcess.Id} {dumpFilePath}";
                    var processInfo = ProcessRunner.CreateProcess(procDumpExeFilePath, args, cancellationToken: cancellationToken);
                    var processOutput = await processInfo.Result;

                    // The exit code for procdump doesn't obey standard windows rules.  It will return non-zero
                    // for successful cases (possibly returning the count of dumps that were written).  Best 
                    // backup is to test for the dump file being present.
                    if (File.Exists(dumpFilePath))
                    {
                        ConsoleUtil.WriteLine($"succeeded ({new FileInfo(dumpFilePath).Length} bytes)");
                    }
                    else
                    {
                        ConsoleUtil.WriteLine($"FAILED with {processOutput.ExitCode}");
                        ConsoleUtil.WriteLine($"{procDumpExeFilePath} {args}");
                        ConsoleUtil.WriteLine(string.Join(Environment.NewLine, processOutput.OutputLines));
                    }
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    ConsoleUtil.WriteLine("FAILED");
                    ConsoleUtil.WriteLine(ex.Message);
                    Logger.Log("Failed to dump process", ex);
                }
            }

            if (options.CollectDumps && GetProcDumpInfo(options) is { } procDumpInfo)
            {
                ConsoleUtil.WriteLine("Roslyn Error: test timeout exceeded, dumping remaining processes");

                var counter = 0;
                foreach (var proc in ProcessUtil.GetProcessTree(Process.GetCurrentProcess()).OrderBy(x => x.ProcessName))
                {
                    var dumpDir = procDumpInfo.DumpDirectory;
                    var dumpFilePath = Path.Combine(dumpDir, $"{proc.ProcessName}-{counter}.dmp");
                    await DumpProcess(proc, procDumpInfo.ProcDumpFilePath, dumpFilePath);
                    counter++;
                }
            }

            WriteLogFile(options);
        }

        private static ProcDumpInfo? GetProcDumpInfo(Options options)
        {
            if (!string.IsNullOrEmpty(options.ProcDumpFilePath))
            {
                return new ProcDumpInfo(options.ProcDumpFilePath, options.LogFilesDirectory);
            }

            return null;
        }

        /// <summary>
        /// Quick sanity check to look over the set of assemblies to make sure they are valid and something was
        /// specified.
        /// </summary>
        private static bool CheckAssemblyList(Options options)
        {
            var anyMissing = false;
            foreach (var assemblyPath in options.Assemblies)
            {
                if (!File.Exists(assemblyPath))
                {
                    ConsoleUtil.WriteLine(ConsoleColor.Red, $"The file '{assemblyPath}' does not exist, is an invalid file name, or you do not have sufficient permissions to read the specified file.");
                    anyMissing = true;
                }
            }

            if (anyMissing)
            {
                return false;
            }

            if (options.Assemblies.Count == 0)
            {
                ConsoleUtil.WriteLine("No test assemblies specified.");
                return false;
            }

            return true;
        }

        private static List<AssemblyInfo> GetAssemblyList(Options options)
        {
            var scheduler = new AssemblyScheduler(options);
            var list = new List<AssemblyInfo>();

            foreach (var assemblyPath in options.Assemblies.OrderByDescending(x => new FileInfo(x).Length))
            {
                list.AddRange(scheduler.Schedule(assemblyPath).Select(x => new AssemblyInfo(x, options.TargetFramework, options.Platform)));
            }

            return list;
        }

        private static void DisplayResults(Display display, ImmutableArray<TestResult> testResults)
        {
            foreach (var cur in testResults)
            {
                var open = false;
                switch (display)
                {
                    case Display.All:
                        open = true;
                        break;
                    case Display.None:
                        open = false;
                        break;
                    case Display.Succeeded:
                        open = cur.Succeeded;
                        break;
                    case Display.Failed:
                        open = !cur.Succeeded;
                        break;
                }

                if (open)
                {
                    ProcessRunner.OpenFile(cur.ResultsDisplayFilePath);
                }
            }
        }

        private static ProcessTestExecutor CreateTestExecutor(Options options)
        {
            var testExecutionOptions = new TestExecutionOptions(
                dotnetFilePath: options.DotnetFilePath,
                procDumpInfo: options.CollectDumps ? GetProcDumpInfo(options) : null,
                testResultsDirectory: options.TestResultsDirectory,
                trait: options.Trait,
                noTrait: options.NoTrait,
                includeHtml: options.IncludeHtml,
                testVsi: options.TestVsi,
                retry: options.Retry);
            return new ProcessTestExecutor(testExecutionOptions);
        }

        /// <summary>
        /// Checks the total size of dump file and removes files exceeding a limit.
        /// </summary>
        private static void CheckTotalDumpFilesSize()
        {
            var directory = Directory.GetCurrentDirectory();
            var dumpFiles = Directory.EnumerateFiles(directory, "*.dmp", SearchOption.AllDirectories).ToArray();
            long currentTotalSize = 0;

            foreach (var dumpFile in dumpFiles)
            {
                long fileSizeInMegabytes = (new FileInfo(dumpFile).Length / 1024) / 1024;
                currentTotalSize += fileSizeInMegabytes;
                if (currentTotalSize > MaxTotalDumpSizeInMegabytes)
                {
                    File.Delete(dumpFile);
                }
            }
        }
    }
}
