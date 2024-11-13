// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Contracts;
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
    internal readonly struct RunAllResult
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

        internal async Task<RunAllResult> RunAllOnHelixAsync(ImmutableArray<WorkItemInfo> workItems, Options options, CancellationToken cancellationToken)
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

            var isAzureDevOpsRun = Environment.GetEnvironmentVariable("BUILD_BUILDID") is not null;
            if (!isAzureDevOpsRun)
            {
                ConsoleUtil.WriteLine("BUILD_BUILDID environment variable was not set, will not publish test results for a local run.");
                // in a local run we assume the user runs using the root test.sh and that the test payload is nested in the artifacts directory.
                msbuildTestPayloadRoot = Path.Combine(msbuildTestPayloadRoot, "artifacts/testPayload");
            }
            var duplicateDir = Path.Combine(msbuildTestPayloadRoot, ".duplicate");
            var correlationPayload = $@"<HelixCorrelationPayload Include=""{duplicateDir}"" />";

            // https://github.com/dotnet/roslyn/issues/50661
            // it's possible we should be using the BUILD_SOURCEVERSIONAUTHOR instead here a la https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.Helix/Sdk/tools/xharness-runner/Readme.md#how-to-use
            // however that variable isn't documented at https://docs.microsoft.com/en-us/azure/devops/pipelines/build/variables?view=azure-devops&tabs=yaml
            var queuedBy = Environment.GetEnvironmentVariable("BUILD_QUEUEDBY")?.Replace(" ", "");
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
            var helixWorkItems = workItems.Select(workItem => MakeHelixWorkItemProject(workItem));

            var globalJson = JsonConvert.DeserializeAnonymousType(File.ReadAllText(getGlobalJsonPath()), new { sdk = new { version = "" } })
                ?? throw new InvalidOperationException("Failed to deserialize global.json.");

            var project = @"
<Project Sdk=""Microsoft.DotNet.Helix.Sdk"" DefaultTargets=""Test"">
    <PropertyGroup>
        <TestRunNamePrefix>" + jobName + @"_</TestRunNamePrefix>
        <HelixSource>pr/" + sourceBranch + @"</HelixSource>
        <HelixType>test</HelixType>
        <HelixBuild>" + buildNumber + @"</HelixBuild>
        <HelixTargetQueues>" + _options.HelixQueueName + @"</HelixTargetQueues>
        <IncludeDotNetCli>true</IncludeDotNetCli>
        <DotNetCliVersion>" + globalJson.sdk.version + @"</DotNetCliVersion>
        <DotNetCliPackageType>sdk</DotNetCliPackageType>
        <EnableAzurePipelinesReporter>" + (isAzureDevOpsRun ? "true" : "false") + @"</EnableAzurePipelinesReporter>
    </PropertyGroup>

    <ItemGroup>
        " + correlationPayload + string.Join("", helixWorkItems) + @"
    </ItemGroup>
</Project>
";

            File.WriteAllText("helix-tmp.csproj", project);

            var arguments = $"build helix-tmp.csproj";
            if (!string.IsNullOrEmpty(_options.HelixApiAccessToken))
            {
                // Internal queues require an access token.
                // We don't put it in the project string itself since it can cause escaping issues.
                arguments += $" /p:HelixAccessToken={_options.HelixApiAccessToken}";
            }
            else
            {
                // If we're not using authenticated access we need to specify a creator.
                arguments += $" /p:Creator={queuedBy}";
            }

            var process = ProcessRunner.CreateProcess(
                executable: _options.DotnetFilePath,
                arguments: arguments,
                captureOutput: true,
                onOutputDataReceived: (e) => { Debug.Assert(e.Data is not null); ConsoleUtil.WriteLine(e.Data); },
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

            static void AddRehydrateTestFoldersCommand(StringBuilder commandBuilder, WorkItemInfo workItemInfo, bool isUnix)
            {
                // Rehydrate assemblies that we need to run as part of this work item.
                foreach (var testAssembly in workItemInfo.Filters.Keys)
                {
                    var directoryName = Path.GetDirectoryName(testAssembly.AssemblyPath);
                    if (isUnix)
                    {
                        // If we're on unix make sure we have permissions to run the rehydrate script.
                        commandBuilder.AppendLine($"chmod +x {directoryName}/rehydrate.sh");
                    }

                    commandBuilder.AppendLine(isUnix ? $"./{directoryName}/rehydrate.sh" : $@"call {directoryName}\rehydrate.cmd");
                    commandBuilder.AppendLine(isUnix ? $"ls -l {directoryName}" : $"dir {directoryName}");
                }
            }

            static string GetHelixRelativeAssemblyPath(string assemblyPath)
            {
                var tfmDir = Path.GetDirectoryName(assemblyPath)!;
                var configurationDir = Path.GetDirectoryName(tfmDir)!;
                var projectDir = Path.GetDirectoryName(configurationDir)!;

                var assemblyRelativePath = Path.Combine(Path.GetFileName(projectDir), Path.GetFileName(configurationDir), Path.GetFileName(tfmDir), Path.GetFileName(assemblyPath));
                return assemblyRelativePath;
            }

            string MakeHelixWorkItemProject(WorkItemInfo workItemInfo)
            {
                // Currently, it's required for the client machine to use the same OS family as the target Helix queue.
                // We could relax this and allow for example Linux clients to kick off Windows jobs, but we'd have to
                // figure out solutions for issues such as creating file paths in the correct format for the target machine.
                var isUnix = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

                var setEnvironmentVariable = isUnix ? "export" : "set";

                var command = new StringBuilder();
                command.AppendLine($"{setEnvironmentVariable} DOTNET_ROLL_FORWARD=LatestMajor");
                command.AppendLine($"{setEnvironmentVariable} DOTNET_ROLL_FORWARD_TO_PRERELEASE=1");
                command.AppendLine(isUnix ? $"ls -l" : $"dir");
                command.AppendLine("dotnet --info");

                string[] knownEnvironmentVariables =
                [
                    "ROSLYN_TEST_IOPERATION",
                    "ROSLYN_TEST_USEDASSEMBLIES"
                ];

                foreach (var knownEnvironmentVariable in knownEnvironmentVariables)
                {
                    if (Environment.GetEnvironmentVariable(knownEnvironmentVariable) is string { Length: > 0 } value)
                    {
                        command.AppendLine($"{setEnvironmentVariable} {knownEnvironmentVariable}=\"{value}\"");
                    }
                }

                // OSX produces extremely large dump files that commonly exceed the limits of Helix 
                // uploads. These settings limit the dump file size + produce a .json detailing crash 
                // reasons that work better with Helix size limitations.
                if (isMac)
                {
                    command.AppendLine($"{setEnvironmentVariable} DOTNET_DbgEnableMiniDump=1");
                    command.AppendLine($"{setEnvironmentVariable} DOTNET_DbgMiniDumpType=1");
                    command.AppendLine($"{setEnvironmentVariable} DOTNET_EnableCrashReport=1");
                }

                // Set the dump folder so that dotnet writes all dump files to this location automatically. 
                // This saves the need to scan for all the different types of dump files later and copy
                // them around.
                var helixDumpFolder = isUnix
                    ? @"$HELIX_DUMP_FOLDER/crash.%d.%e.dmp"
                    : @"%HELIX_DUMP_FOLDER%\crash.%d.%e.dmp";
                command.AppendLine($"{setEnvironmentVariable} DOTNET_DbgMiniDumpName=\"{helixDumpFolder}\"");

                command.AppendLine(isUnix ? "env | sort" : "set");

                // Create a payload directory that contains all the assemblies in the work item in separate folders.
                var payloadDirectory = Path.Combine(msbuildTestPayloadRoot, "artifacts", "bin");

                // Update the assembly groups to test with the assembly paths in the context of the helix work item.
                workItemInfo = workItemInfo with { Filters = workItemInfo.Filters.ToImmutableSortedDictionary(kvp => kvp.Key with { AssemblyPath = GetHelixRelativeAssemblyPath(kvp.Key.AssemblyPath) }, kvp => kvp.Value) };

                AddRehydrateTestFoldersCommand(command, workItemInfo, isUnix);

                var xmlResultsFilePath = ProcessTestExecutor.GetResultsFilePath(workItemInfo, options, "xml");
                Contract.Assert(!options.IncludeHtml);

                // Build an rsp file to send to dotnet test that contains all the assemblies and tests to run.
                // This gets around command line length limitations and avoids weird escaping issues.
                // See https://docs.microsoft.com/en-us/dotnet/standard/commandline/syntax#response-files
                var rspFileContents = ProcessTestExecutor.BuildRspFileContents(workItemInfo, options, xmlResultsFilePath, htmlResultsFilePath: null);
                var rspFileName = $"vstest_{workItemInfo.PartitionIndex}.rsp";
                File.WriteAllText(Path.Combine(payloadDirectory, rspFileName), rspFileContents);

                // Build the command to run the rsp file.
                // dotnet test does not pass rsp files correctly the vs test console, so we have to manually invoke vs test console.
                // See https://github.com/microsoft/vstest/issues/3513
                // The dotnet sdk includes the vstest.console.dll executable in the sdk folder in the installed version, so we look it up using the
                // DOTNET_ROOT environment variable set by helix.
                if (isUnix)
                {
                    // $ is a special character in msbuild so we replace it with %24 in the helix project.
                    // https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-special-characters?view=vs-2022
                    command.AppendLine("vstestConsolePath=%24(find %24{DOTNET_ROOT} -name \"vstest.console.dll\")");
                    command.AppendLine("echo %24{vstestConsolePath}");
                    command.AppendLine($"dotnet exec \"%24{{vstestConsolePath}}\" @{rspFileName}");
                }
                else
                {
                    command.AppendLine(@"powershell -NoProfile -Command { Set-MpPreference -DisableRealtimeMonitoring $true }");
                    command.AppendLine(@"powershell -NoProfile -Command { Set-MpPreference -ExclusionPath (Resolve-Path 'artifacts') }");
                    // Windows cmd doesn't have an easy way to set the output of a command to a variable.
                    // So send the output of the command to a file, then set the variable based on the file.
                    command.AppendLine("where /r %DOTNET_ROOT% vstest.console.dll > temp.txt");
                    command.AppendLine("set /p vstestConsolePath=<temp.txt");
                    command.AppendLine("echo %vstestConsolePath%");
                    command.AppendLine($"dotnet exec \"%vstestConsolePath%\" @{rspFileName}");
                }

                // The command string contains characters like % which are not valid XML to pass into the helix csproj.
                var escapedCommand = SecurityElement.Escape(command.ToString());

                // We want to collect any dumps during the post command step here; these commands are ran after the
                // return value of the main command is captured; a Helix Job is considered to fail if the main command returns a
                // non-zero error code, and we don't want the cleanup steps to interefere with that. PostCommands exist
                // precisely to address this problem.
                //
                // This is still necessary even with us setting  DOTNET_DbgMiniDumpName because the system can create 
                // non .NET Core dump files that aren't controlled by that value.
                var postCommands = new StringBuilder();

                if (isUnix)
                {
                    // Write out this command into a separate file; unfortunately the use of single quotes and ; that is required
                    // for the command to work causes too much escaping issues in MSBuild.
                    File.WriteAllText(Path.Combine(payloadDirectory, "copy-dumps.sh"), "find . -name '*.dmp' -exec cp {} $HELIX_DUMP_FOLDER \\;");
                    postCommands.AppendLine("./copy-dumps.sh");
                }
                else
                {
                    postCommands.AppendLine("for /r %%f in (*.dmp) do copy %%f %HELIX_DUMP_FOLDER%");
                }

                var workItem = $@"
        <HelixWorkItem Include=""{workItemInfo.DisplayName}"">
            <PayloadDirectory>{payloadDirectory}</PayloadDirectory>
            <Command>
                {escapedCommand}
            </Command>
            <PostCommands>
                {postCommands}
            </PostCommands>
            <Timeout>00:30:00</Timeout>
        </HelixWorkItem>
";
                return workItem;
            }
        }

        internal async Task<RunAllResult> RunAllAsync(ImmutableArray<WorkItemInfo> workItems, CancellationToken cancellationToken)
        {
            // Use 1.5 times the number of processors for unit tests, but only 1 processor for the open integration tests
            // since they perform actual UI operations (such as mouse clicks and sending keystrokes) and we don't want two
            // tests to conflict with one-another.
            var max = _options.Sequential ? 1 : (int)(Environment.ProcessorCount * 1.5);
            var waiting = new Stack<WorkItemInfo>(workItems);
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
                    var task = _testExecutor.RunTestAsync(waiting.Pop(), _options, cancellationToken);
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

            ConsoleUtil.WriteLine($"Errors {testResult.DisplayName}");
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
