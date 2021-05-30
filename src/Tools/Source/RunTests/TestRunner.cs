﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RunTests
{
    internal struct RunAllResult
    {
        internal bool Succeeded { get; }
        internal ImmutableArray<TestResult> TestResults { get; }
        internal ImmutableArray<ProcessResult> ProcessResults { get; }

        internal RunAllResult(bool succeeded, ImmutableArray<TestResult> testResults, ImmutableArray<ProcessResult> processResults)
        {
            Succeeded = succeeded;
            TestResults = testResults;
            ProcessResults = processResults;
        }
    }

    internal sealed class TestRunner
    {
        private readonly ProcessTestExecutor _testExecutor;
        private readonly Options _options;

        internal TestRunner(Options options, ProcessTestExecutor testExecutor)
        {
            _testExecutor = testExecutor;
            _options = options;
        }

        internal async Task<RunAllResult> RunAllOnHelixAsync(IEnumerable<AssemblyInfo> assemblyInfoList, CancellationToken cancellationToken)
        {
            var sourceBranch = Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCH");
            if (sourceBranch is null)
            {
                sourceBranch = "local";
                ConsoleUtil.WriteLine($@"BUILD_SOURCEBRANCH environment variable was not set. Using source branch ""{sourceBranch}"" instead");
                Environment.SetEnvironmentVariable("BUILD_SOURCEBRANCH", sourceBranch);
            }

            var msbuildTestPayloadRoot = Path.GetDirectoryName(_options.ArtifactsDirectory);
            if (msbuildTestPayloadRoot is null)
            {
                throw new IOException($@"Malformed ArtifactsDirectory in options: ""{_options.ArtifactsDirectory}""");
            }

            var isAzureDevOpsRun = Environment.GetEnvironmentVariable("SYSTEM_ACCESSTOKEN") is not null;
            if (!isAzureDevOpsRun)
            {
                ConsoleUtil.WriteLine("SYSTEM_ACCESSTOKEN environment variable was not set, so test results will not be published.");
                // in a local run we assume the user runs using the root test.sh and that the test payload is nested in the artifacts directory.
                msbuildTestPayloadRoot = Path.Combine(msbuildTestPayloadRoot, "artifacts/testPayload");
            }
            var duplicateDir = Path.Combine(msbuildTestPayloadRoot, ".duplicate");
            var correlationPayload = $@"<HelixCorrelationPayload Include=""{duplicateDir}"" />";

            // https://github.com/dotnet/roslyn/issues/50661
            // it's possible we should be using the BUILD_SOURCEVERSIONAUTHOR instead here a la https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.Helix/Sdk/tools/xharness-runner/Readme.md#how-to-use
            // however that variable isn't documented at https://docs.microsoft.com/en-us/azure/devops/pipelines/build/variables?view=azure-devops&tabs=yaml
            var queuedBy = Environment.GetEnvironmentVariable("BUILD_QUEUEDBY");
            if (queuedBy is null)
            {
                queuedBy = "roslyn";
                ConsoleUtil.WriteLine($@"BUILD_QUEUEDBY environment variable was not set. Using value ""{queuedBy}"" instead");
            }

            var jobName = Environment.GetEnvironmentVariable("SYSTEM_JOBDISPLAYNAME");
            if (jobName is null)
            {
                ConsoleUtil.WriteLine($"SYSTEM_JOBDISPLAYNAME environment variable was not set. Using a blank TestRunNamePrefix for Helix job.");
            }

            if (Environment.GetEnvironmentVariable("BUILD_REPOSITORY_NAME") is null)
                Environment.SetEnvironmentVariable("BUILD_REPOSITORY_NAME", "dotnet/roslyn");

            if (Environment.GetEnvironmentVariable("SYSTEM_TEAMPROJECT") is null)
                Environment.SetEnvironmentVariable("SYSTEM_TEAMPROJECT", "dnceng");

            if (Environment.GetEnvironmentVariable("BUILD_REASON") is null)
                Environment.SetEnvironmentVariable("BUILD_REASON", "pr");

            var buildNumber = Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER") ?? "0";
            var workItems = assemblyInfoList.Select(ai => makeHelixWorkItemProject(ai));

            var globalJson = JsonConvert.DeserializeAnonymousType(File.ReadAllText(getGlobalJsonPath()), new { sdk = new { version = "" } });
            var project = @"
<Project Sdk=""Microsoft.DotNet.Helix.Sdk"" DefaultTargets=""Test"">
    <PropertyGroup>
        <TestRunNamePrefix>" + jobName + @"_</TestRunNamePrefix>
        <HelixSource>pr/" + sourceBranch + @"</HelixSource>
        <HelixType>test</HelixType>
        <HelixBuild>" + buildNumber + @"</HelixBuild>
        <HelixTargetQueues>" + _options.HelixQueueName + @"</HelixTargetQueues>
        <Creator>" + queuedBy + @"</Creator>
        <IncludeDotNetCli>true</IncludeDotNetCli>
        <DotNetCliVersion>" + globalJson.sdk.version + @"</DotNetCliVersion>
        <DotNetCliPackageType>sdk</DotNetCliPackageType>
        <EnableAzurePipelinesReporter>" + (isAzureDevOpsRun ? "true" : "false") + @"</EnableAzurePipelinesReporter>
    </PropertyGroup>

    <ItemGroup>
        " + correlationPayload + string.Join("", workItems) + @"
    </ItemGroup>
</Project>
";

            File.WriteAllText("helix-tmp.csproj", project);
            var process = ProcessRunner.CreateProcess(
                executable: _options.DotnetFilePath,
                arguments: "build helix-tmp.csproj",
                captureOutput: true,
                onOutputDataReceived: (e) => ConsoleUtil.WriteLine(e.Data),
                cancellationToken: cancellationToken);
            var result = await process.Result;

            return new RunAllResult(result.ExitCode == 0, ImmutableArray<TestResult>.Empty, ImmutableArray.Create(result));

            static string getGlobalJsonPath()
            {
                var path = AppContext.BaseDirectory;
                while (path is object)
                {
                    var globalJsonPath = Path.Join(path, "global.json");
                    if (File.Exists(globalJsonPath))
                    {
                        return globalJsonPath;
                    }
                    path = Path.GetDirectoryName(path);
                }
                throw new IOException($@"Could not find global.json by walking up from ""{AppContext.BaseDirectory}"".");
            }

            string makeHelixWorkItemProject(AssemblyInfo assemblyInfo)
            {
                // Currently, it's required for the client machine to use the same OS family as the target Helix queue.
                // We could relax this and allow for example Linux clients to kick off Windows jobs, but we'd have to
                // figure out solutions for issues such as creating file paths in the correct format for the target machine.
                var isUnix = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

                var commandLineArguments = _testExecutor.GetCommandLineArguments(assemblyInfo, useSingleQuotes: isUnix);
                commandLineArguments = SecurityElement.Escape(commandLineArguments);

                var rehydrateFilename = isUnix ? "rehydrate.sh" : "rehydrate.cmd";
                var lsCommand = isUnix ? "ls" : "dir";
                var rehydrateCommand = isUnix ? $"./{rehydrateFilename}" : $@"call .\{rehydrateFilename}";
                var setRollforward = $"{(isUnix ? "export" : "set")} DOTNET_ROLL_FORWARD=LatestMajor";
                var setPrereleaseRollforward = $"{(isUnix ? "export" : "set")} DOTNET_ROLL_FORWARD_TO_PRERELEASE=1";
                var setTestIOperation = Environment.GetEnvironmentVariable("ROSLYN_TEST_IOPERATION") is { } iop
                    ? $"{(isUnix ? "export" : "set")} ROSLYN_TEST_IOPERATION={iop}"
                    : "";
                var workItem = $@"
        <HelixWorkItem Include=""{assemblyInfo.DisplayName}"">
            <PayloadDirectory>{Path.Combine(msbuildTestPayloadRoot, Path.GetDirectoryName(assemblyInfo.AssemblyPath)!)}</PayloadDirectory>
            <Command>
                {lsCommand}
                {rehydrateCommand}
                {lsCommand}
                {setRollforward}
                {setPrereleaseRollforward}
                dotnet --info
                {setTestIOperation}
                dotnet {commandLineArguments}
            </Command>
            <Timeout>00:15:00</Timeout>
        </HelixWorkItem>
";
                return workItem;
            }
        }

        internal async Task<RunAllResult> RunAllAsync(IEnumerable<AssemblyInfo> assemblyInfoList, CancellationToken cancellationToken)
        {
            // Use 1.5 times the number of processors for unit tests, but only 1 processor for the open integration tests
            // since they perform actual UI operations (such as mouse clicks and sending keystrokes) and we don't want two
            // tests to conflict with one-another.
            var max = _options.Sequential ? 1 : (int)(Environment.ProcessorCount * 1.5);
            var waiting = new Stack<AssemblyInfo>(assemblyInfoList);
            var running = new List<Task<TestResult>>();
            var completed = new List<TestResult>();
            var failures = 0;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                var i = 0;
                while (i < running.Count)
                {
                    var task = running[i];
                    if (task.IsCompleted)
                    {
                        try
                        {
                            var testResult = await task.ConfigureAwait(false);
                            if (!testResult.Succeeded)
                            {
                                failures++;
                                if (testResult.ResultsDisplayFilePath is string resultsPath)
                                {
                                    ConsoleUtil.WriteLine(ConsoleColor.Red, resultsPath);
                                }
                                else
                                {
                                    foreach (var result in testResult.ProcessResults)
                                    {
                                        foreach (var line in result.ErrorLines)
                                        {
                                            ConsoleUtil.WriteLine(ConsoleColor.Red, line);
                                        }
                                    }
                                }
                            }

                            completed.Add(testResult);
                        }
                        catch (Exception ex)
                        {
                            ConsoleUtil.WriteLine(ConsoleColor.Red, $"Error: {ex.Message}");
                            failures++;
                        }

                        running.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                while (running.Count < max && waiting.Count > 0)
                {
                    var task = _testExecutor.RunTestAsync(waiting.Pop(), cancellationToken);
                    running.Add(task);
                }

                // Display the current status of the TestRunner.
                // Note: The { ... , 2 } is to right align the values, thus aligns sections into columns.
                ConsoleUtil.Write($"  {running.Count,2} running, {waiting.Count,2} queued, {completed.Count,2} completed");
                if (failures > 0)
                {
                    ConsoleUtil.Write($", {failures,2} failures");
                }
                ConsoleUtil.WriteLine();

                if (running.Count > 0)
                {
                    await Task.WhenAny(running.ToArray());
                }
            } while (running.Count > 0);

            Print(completed);

            var processResults = ImmutableArray.CreateBuilder<ProcessResult>();
            foreach (var c in completed)
            {
                processResults.AddRange(c.ProcessResults);
            }

            return new RunAllResult((failures == 0), completed.ToImmutableArray(), processResults.ToImmutable());
        }

        private void Print(List<TestResult> testResults)
        {
            testResults.Sort((x, y) => x.Elapsed.CompareTo(y.Elapsed));

            foreach (var testResult in testResults.Where(x => !x.Succeeded))
            {
                PrintFailedTestResult(testResult);
            }

            ConsoleUtil.WriteLine("================");
            var line = new StringBuilder();
            foreach (var testResult in testResults)
            {
                line.Length = 0;
                var color = testResult.Succeeded ? Console.ForegroundColor : ConsoleColor.Red;
                line.Append($"{testResult.DisplayName,-75}");
                line.Append($" {(testResult.Succeeded ? "PASSED" : "FAILED")}");
                line.Append($" {testResult.Elapsed}");
                line.Append($" {(!string.IsNullOrEmpty(testResult.Diagnostics) ? "?" : "")}");

                var message = line.ToString();
                ConsoleUtil.WriteLine(color, message);
            }
            ConsoleUtil.WriteLine("================");

            // Print diagnostics out last so they are cleanly visible at the end of the test summary
            ConsoleUtil.WriteLine("Extra run diagnostics for logging, did not impact run results");
            foreach (var testResult in testResults.Where(x => !string.IsNullOrEmpty(x.Diagnostics)))
            {
                ConsoleUtil.WriteLine(testResult.Diagnostics!);
            }
        }

        private void PrintFailedTestResult(TestResult testResult)
        {
            // Save out the error output for easy artifact inspecting
            var outputLogPath = Path.Combine(_options.LogFilesDirectory, $"xUnitFailure-{testResult.DisplayName}.log");

            ConsoleUtil.WriteLine($"Errors {testResult.AssemblyName}");
            ConsoleUtil.WriteLine(testResult.ErrorOutput);

            // TODO: Put this in the log and take it off the ConsoleUtil output to keep it simple?
            ConsoleUtil.WriteLine($"Command: {testResult.CommandLine}");
            ConsoleUtil.WriteLine($"xUnit output log: {outputLogPath}");

            File.WriteAllText(outputLogPath, testResult.StandardOutput ?? "");

            if (!string.IsNullOrEmpty(testResult.ErrorOutput))
            {
                ConsoleUtil.WriteLine(testResult.ErrorOutput);
            }
            else
            {
                ConsoleUtil.WriteLine($"xunit produced no error output but had exit code {testResult.ExitCode}. Writing standard output:");
                ConsoleUtil.WriteLine(testResult.StandardOutput ?? "(no standard output)");
            }

            // If the results are html, use Process.Start to open in the browser.
            var htmlResultsFilePath = testResult.TestResultInfo.HtmlResultsFilePath;
            if (!string.IsNullOrEmpty(htmlResultsFilePath))
            {
                var startInfo = new ProcessStartInfo() { FileName = htmlResultsFilePath, UseShellExecute = true };
                Process.Start(startInfo);
            }
        }
    }
}
