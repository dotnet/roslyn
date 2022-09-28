// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using RunTestsUtils;

namespace RunTests
{
    internal sealed partial class Program
    {
        internal const int ExitSuccess = 0;
        internal const int ExitFailure = 1;

        private const long MaxTotalDumpSizeInMegabytes = 8196;

        /// <summary>
        /// Timeout used to kill the integration test run if it exceeds this time.
        /// </summary>
        private static readonly TimeSpan s_timeout = TimeSpan.FromMinutes(110);

        internal static async Task<int> Main(string[] args)
        {
            Logger.Log("RunTest command line");
            Logger.Log(string.Join(" ", args));
            var options = Options.Parse(args);
            if (options == null)
            {
                return ExitFailure;
            }

            ConsoleUtil.WriteLine($"Running '{options.DotnetFilePath} --version'..");
            var dotnetResult = await ProcessRunner.CreateProcess(options.DotnetFilePath, arguments: "--version", captureOutput: true).Result;
            ConsoleUtil.WriteLine(string.Join(Environment.NewLine, dotnetResult.OutputLines));
            ConsoleUtil.WriteLine(ConsoleColor.Red, string.Join(Environment.NewLine, dotnetResult.ErrorLines));

            // Setup cancellation for ctrl-c key presses
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += delegate
            {
                cts.Cancel();
            };

            int result = await RunWithoutTimeoutAsync(options, cts.Token);

            CheckTotalDumpFilesSize();
            return result;
        }

        private static async Task<int> RunWithoutTimeoutAsync(Options options, CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var runTask = RunAsync(options, cts.Token);
            var timeoutTask = Task.Delay(s_timeout, cancellationToken);

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
            var start = DateTime.Now;
            var assemblies = GetAssemblies(options);
            if (assemblies.Length == 0)
            {
                WriteLogFile(options);
                ConsoleUtil.WriteLine(ConsoleColor.Red, "No assemblies to test");
                return ExitFailure;
            }

            ConsoleUtil.WriteLine($"Proc dump location: {options.ProcDumpFilePath}");
            ConsoleUtil.WriteLine($"Running tests in {assemblies.Length} partitions");

            var result = await TestRunner.RunAllAsync(assemblies, options, cancellationToken).ConfigureAwait(true);
            var elapsed = DateTime.Now - start;

            ConsoleUtil.WriteLine($"Test execution time: {elapsed}");

            LogProcessResultDetails(result.ProcessResults);
            WriteLogFile(options);

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
                Directory.CreateDirectory(options.LogFilesDirectory);
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
                if (name is "procdump" or "conhost")
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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var screenshotPath = Path.Combine(options.LogFilesDirectory, $"timeout.png");
                ConsoleUtil.WriteLine($"Taking screenshot on timeout at {screenshotPath}");
                var output = await ProcessRunner.CreateProcess("Powershell.exe", $"-command \"& {{ . .\\eng\\build-utils-win.ps1; Capture-Screenshot {screenshotPath} }}\"", displayWindow: false, cancellationToken: cancellationToken).Result;
                ConsoleUtil.WriteLine(string.Join(Environment.NewLine, output.OutputLines));
                ConsoleUtil.WriteLine(string.Join(Environment.NewLine, output.ErrorLines));
            }

            if (options.CollectDumps && !string.IsNullOrEmpty(options.ProcDumpFilePath))
            {
                ConsoleUtil.WriteLine("Roslyn Error: test timeout exceeded, dumping remaining processes");

                var counter = 0;
                foreach (var proc in ProcessUtil.GetProcessTree(Process.GetCurrentProcess()).OrderBy(x => x.ProcessName))
                {
                    var dumpDir = options.LogFilesDirectory;
                    var dumpFilePath = Path.Combine(dumpDir, $"{proc.ProcessName}-{counter}.dmp");
                    await DumpProcess(proc, options.ProcDumpFilePath, dumpFilePath);
                    counter++;
                }
            }

            WriteLogFile(options);
        }

        private static ImmutableArray<string> GetAssemblies(Options options)
        {
            var assemblies = File.ReadAllLines(options.TestAssembliesPath).ToImmutableArray();
            foreach (var assembly in assemblies)
            {
                if (!File.Exists(assembly))
                {
                    throw new ArgumentException($"{assembly} does not exist on disk");
                }
            }

            return assemblies;
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
                    ConsoleUtil.WriteLine($"Deleting '{dumpFile}' because we have exceeded our total dump size of {MaxTotalDumpSizeInMegabytes} megabytes.");
                    File.Delete(dumpFile);
                }
            }
        }
    }
}
