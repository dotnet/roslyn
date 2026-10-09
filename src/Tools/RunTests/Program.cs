// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;

namespace TestRunner.RunTests
{
    internal sealed partial class Program
    {
        internal const int ExitSuccess = 0;
        internal const int ExitFailure = 1;

        private const long MaxTotalDumpSizeInMegabytes = 8196;

        internal static async Task<int> Main(string[] args)
        {
            if (args is ["--dump-process", ..])
                return DumpCollector.RunHelper(args);

            Logger.Log("RunTest command line");
            Logger.Log(string.Join(" ", args));
            var options = RunTestOptions.Parse(args, out var helpShown);
            if (options == null)
            {
                return helpShown ? ExitSuccess : ExitFailure;
            }

            ConsoleUtil.WriteLine($"Running '{options.DotnetFilePath} --version'..");
            var dotnetResult = await ProcessRunner.CreateProcess(options.DotnetFilePath, arguments: "--version", captureOutput: true).Result;
            ConsoleUtil.WriteLine(string.Join(Environment.NewLine, dotnetResult.OutputLines));
            ConsoleUtil.WriteLine(ConsoleColor.Red, string.Join(Environment.NewLine, dotnetResult.ErrorLines));

            Directory.CreateDirectory(options.LogFilesDirectory);

            if (options.CollectDumps && OperatingSystem.IsWindows())
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
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                    DisableRegistryDumpCollection();
                };

                var result = await RunCoreAsync(options, options.Timeout, cts.Token);

                CheckTotalDumpFilesSize();
                return result;
            }
            finally
            {
                DisableRegistryDumpCollection();
            }

            void DisableRegistryDumpCollection()
            {
                if (options.CollectDumps && OperatingSystem.IsWindows() && DumpUtil.IsAdministrator())
                {
                    DumpUtil.DisableRegistryDumpCollection();
                }
            }
        }

        private static async Task<int> RunCoreAsync(RunTestOptions options, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var runTask = RunAsync(options, cts.Token, cancellationToken);
            var timeoutTask = Task.Delay(timeout, cancellationToken);

            var finishedTask = await Task.WhenAny(timeoutTask, runTask);
            if (finishedTask == timeoutTask)
            {
                if (!cancellationToken.IsCancellationRequested)
                    ConsoleUtil.Error("Global test timeout exceeded; collecting owned work-item dumps before termination.");
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

                WriteLogFile(options);
                return ExitFailure;
            }

            return await runTask;
        }

        private static async Task<int> RunAsync(RunTestOptions options, CancellationToken cancellationToken, CancellationToken userCancellationToken)
        {
            var assemblyFilePaths = AssemblyDiscovery.GetAssemblyFilePaths(options);

            var testExecutor = new ProcessTestExecutor(userCancellationToken);
            var testRunner = new TestRunner(options, testExecutor);
            var start = DateTime.Now;
            if (assemblyFilePaths.Length == 0)
            {
                WriteLogFile(options);
                ConsoleUtil.WriteLine(ConsoleColor.Red, "No assemblies to test");
                return ExitFailure;
            }

            var result = await testRunner.RunAllAsync(assemblyFilePaths, cancellationToken).ConfigureAwait(true);
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
                Logger.Log($"### Exit code {processResult.ExitCode}");
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

        private static void WriteLogFile(RunTestOptions options)
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
