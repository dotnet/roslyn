// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RunTests.Cache;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.Immutable;
using Newtonsoft.Json;
using System.Reflection;
using System.Diagnostics;

namespace RunTests
{
    internal sealed partial class Program
    {
        private static readonly ImmutableHashSet<string> PrimaryProcessNames = ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "devenv",
            "xunit.console.x86");

        internal const int ExitSuccess = 0;
        internal const int ExitFailure = 1;

        private const long MaxTotalDumpSizeInMegabytes = 4096;

        internal static int Main(string[] args)
        {
            Logger.Log("RunTest command line");
            Logger.Log(string.Join(" ", args));

            var options = Options.Parse(args);
            if (options == null)
            {
                Options.PrintUsage();
                return ExitFailure;
            }

            // Setup cancellation for ctrl-c key presses
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += delegate
            {
                cts.Cancel();
            };

            var result = Run(options, cts.Token).GetAwaiter().GetResult();
            CheckTotalDumpFilesSize();
            return result;
        }

        private static async Task<int> Run(Options options, CancellationToken cancellationToken)
        {
            if (options.Timeout == null)
            {
                return await RunCore(options, cancellationToken);
            }

            var timeoutTask = Task.Delay(options.Timeout.Value);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var runTask = RunCore(options, cts.Token);

            if (cancellationToken.IsCancellationRequested)
            {
                return ExitFailure;
            }

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

        private static async Task<int> RunCore(Options options, CancellationToken cancellationToken)
        {
            if (!CheckAssemblyList(options))
            {
                return ExitFailure;
            }

            var testExecutor = CreateTestExecutor(options);
            var testRunner = new TestRunner(options, testExecutor);
            var start = DateTime.Now;
            var assemblyInfoList = GetAssemblyList(options);

            ConsoleUtil.WriteLine($"Data Storage: {testExecutor.DataStorage.Name}");
            ConsoleUtil.WriteLine($"Proc dump location: {options.ProcDumpDirectory}");
            ConsoleUtil.WriteLine($"Running {options.Assemblies.Count} test assemblies in {assemblyInfoList.Count} partitions");

            var result = await testRunner.RunAllAsync(assemblyInfoList, cancellationToken).ConfigureAwait(true);
            var elapsed = DateTime.Now - start;

            ConsoleUtil.WriteLine($"Test execution time: {elapsed}");

            LogProcessResultDetails(result.ProcessResults);
            WriteLogFile(options);
            DisplayResults(options.Display, result.TestResults);

            if (CanUseWebStorage() && options.UseCachedResults)
            {
                await SendRunStats(options, testExecutor.DataStorage, elapsed, result, assemblyInfoList.Count, cancellationToken).ConfigureAwait(true);
            }

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
            var logFilePath = Path.Combine(options.LogFilesOutputDirectory, "runtests.log");
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

            ConsoleUtil.WriteLine("Roslyn Error: test timeout exceeded, dumping remaining processes");
            var procDumpInfo = GetProcDumpInfo(options);
            if (procDumpInfo != null)
            {
                var counter = 0;
                foreach (var proc in ProcessUtil.GetProcessTree(Process.GetCurrentProcess()).OrderBy(x => x.ProcessName))
                {
                    var dumpDir = PrimaryProcessNames.Contains(proc.ProcessName)
                        ? procDumpInfo.Value.DumpDirectory
                        : procDumpInfo.Value.SecondaryDumpDirectory;
                    var dumpFilePath = Path.Combine(dumpDir, $"{proc.ProcessName}-{counter}.dmp");
                    await DumpProcess(proc, procDumpInfo.Value.ProcDumpFilePath, dumpFilePath);
                    counter++;
                }
            }
            else
            {
                ConsoleUtil.WriteLine("Could not locate procdump");
            }

            WriteLogFile(options);
        }

        private static ProcDumpInfo? GetProcDumpInfo(Options options)
        {
            if (!string.IsNullOrEmpty(options.ProcDumpDirectory))
            {
                return new ProcDumpInfo(Path.Combine(options.ProcDumpDirectory, "procdump.exe"), options.LogFilesOutputDirectory, options.LogFilesSecondaryOutputDirectory);
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
                var name = Path.GetFileName(assemblyPath);

                // As a starting point we will just schedule the items we know to be a performance
                // bottleneck.  Can adjust as we get real data.
                if (name == "Microsoft.CodeAnalysis.CSharp.Emit.UnitTests.dll" ||
                    name == "Microsoft.CodeAnalysis.EditorFeatures.UnitTests.dll" ||
                    name == "Microsoft.CodeAnalysis.EditorFeatures2.UnitTests.dll" ||
                    name == "Microsoft.VisualStudio.LanguageServices.UnitTests.dll" ||
                    name == "Microsoft.CodeAnalysis.CSharp.EditorFeatures.UnitTests.dll" ||
                    name == "Microsoft.CodeAnalysis.VisualBasic.EditorFeatures.UnitTests.dll")
                {
                    list.AddRange(scheduler.Schedule(assemblyPath));
                }
                else
                {
                    list.Add(scheduler.CreateAssemblyInfo(assemblyPath));
                }
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
                    ProcessRunner.OpenFile(cur.ResultsFilePath);
                }
            }
        }

        private static bool CanUseWebStorage()
        {
            // The web caching layer is still being worked on.  For now want to limit it to Roslyn developers
            // and Jenkins runs by default until we work on this a bit more.  Anyone reading this who wants
            // to try it out should feel free to opt into this.
            return
                StringComparer.OrdinalIgnoreCase.Equals("REDMOND", Environment.UserDomainName) ||
                Constants.IsJenkinsRun;
        }

        private static ITestExecutor CreateTestExecutor(Options options)
        {
            var testExecutionOptions = new TestExecutionOptions(
                xunitPath: options.XunitPath,
                procDumpInfo: options.UseProcDump ? GetProcDumpInfo(options) : null,
                outputDirectory: options.TestResultXmlOutputDirectory,
                trait: options.Trait,
                noTrait: options.NoTrait,
                useHtml: options.UseHtml,
                test64: options.Test64,
                testVsi: options.TestVsi);
            var processTestExecutor = new ProcessTestExecutor(testExecutionOptions);
            if (!options.UseCachedResults)
            {
                return processTestExecutor;
            }

            // The web caching layer is still being worked on.  For now want to limit it to Roslyn developers
            // and Jenkins runs by default until we work on this a bit more.  Anyone reading this who wants
            // to try it out should feel free to opt into this.
            IDataStorage dataStorage = new LocalDataStorage();
            if (CanUseWebStorage())
            {
                dataStorage = new WebDataStorage();
            }

            return new CachingTestExecutor(processTestExecutor, dataStorage);
        }

        /// <summary>
        /// Order the assembly list so that the largest assemblies come first.  This
        /// is not ideal as the largest assembly does not necessarily take the most time.
        /// </summary>
        /// <param name="list"></param>
        private static IOrderedEnumerable<string> OrderAssemblyList(IEnumerable<string> list)
        {
            return list.OrderByDescending((assemblyName) => new FileInfo(assemblyName).Length);
        }

        private static async Task SendRunStats(Options options, IDataStorage dataStorage, TimeSpan elapsed, RunAllResult result, int partitionCount, CancellationToken cancellationToken)
        {
            var testRunData = new TestRunData()
            {
                Cache = dataStorage.Name,
                ElapsedSeconds = (int)elapsed.TotalSeconds,
                JenkinsUrl = Constants.JenkinsUrl,
                IsJenkins = Constants.IsJenkinsRun,
                Is32Bit = !options.Test64,
                AssemblyCount = options.Assemblies.Count,
                ChunkCount = partitionCount,
                CacheCount = result.CacheCount,
                Succeeded = result.Succeeded,
                HasErrors = Logger.HasErrors
            };

            var request = new RestRequest("api/testData/run", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("text/json", JsonConvert.SerializeObject(testRunData), ParameterType.RequestBody);

            try
            {
                var client = new RestClient(Constants.DashboardUriString);
                var response = await client.ExecuteTaskAsync(request);
                if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    Logger.Log($"Unable to send results: {response.ErrorMessage}");
                }
            }
            catch
            {
                Logger.Log("Unable to send results");
            }
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
