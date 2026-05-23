// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace RunTests;

public sealed class HelixWorkItem(
    int id,
    ImmutableArray<string> assemblyFilePaths,
    ImmutableArray<string> testMethodNames,
    TimeSpan? estimatedExecutionTime)
{
    public string DisplayName { get; } = $"workitem_{id}";
    public int Id { get; } = id;
    public ImmutableArray<string> AssemblyFilePaths { get; } = assemblyFilePaths;
    public ImmutableArray<string> TestMethodNames { get; } = testMethodNames;
    public TimeSpan? EstimatedExecutionTime { get; } = estimatedExecutionTime;

    public override string ToString() => DisplayName;
}

internal sealed partial class HelixTestRunner
{
    /// <summary>
    /// The amount of time we will allocate for each helix work item. When changing this value, consider that test execution time is only part of the 
    /// total time in a work item:
    ///   1.  Downloading assets to the helix machine.
    ///   2.  Test discovery.
    ///   3.  Setting up the test host for each assembly.
    /// </summary>
    internal static TimeSpan WorkItemScheduleTime { get; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// This is the amount of time we will wait for a helix work item to complete before we consider it a severe error
    /// and cancel the helix job entirely.
    /// </summary>
    /// <remarks>
    /// The only lever that vstest provides is for timeout on an individual test, not the entire test run. We need a guard
    /// against the helix job executing for the entire AzDO job time (currently set at six hours).
    /// </remarks>
    internal static TimeSpan WorkItemExecutionTimeout { get; } = WorkItemScheduleTime * 3;

    /// <summary>
    /// This is the timeout value for _individual tests_ that we pass to vstest.
    /// </summary>
    internal static TimeSpan TestMethodTimeout { get; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// This is the amount of time we will wait between polling the Helix service for updates.
    /// </summary>
    private static TimeSpan HelixPollTime { get; } = TimeSpan.FromMinutes(2.5);

    [GeneratedRegex(@"HelixJobId=(\S+) HelixJobCancellationToken=(\S+)")]
    private static partial Regex HelixJobInfoRegex();

    internal enum TestOS
    {
        Windows,
        Linux,
        Mac,
    }

    internal static async Task<int> RunAsync(Options options, ImmutableArray<AssemblyInfo> assemblies)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += delegate
        {
            cts.Cancel();
        };

        var helixProjectFilePath = await CreateHelixArtifactsAsync(options, assemblies, cts.Token).ConfigureAwait(false);
        var (process, helixJobInfoTask) = StartHelixJob(options, helixProjectFilePath);
        try
        {
            await Task.WhenAny(process.WaitForExitAsync(cts.Token), helixJobInfoTask);
            if (cts.IsCancellationRequested)
            {
                process.Kill(entireProcessTree: true);
                return -1;
            }

            if (!helixJobInfoTask.IsCompletedSuccessfully)
            {
                ConsoleUtil.Error($"Helix completed without specifying a job id in the output. This breaks our tooling");
                return -1;
            }

            var (helixJobId, helixJobCancellationToken) = await helixJobInfoTask;
            return await WaitForHelixJobCompletionAsync(process, helixJobId, helixJobCancellationToken, options.HelixApiAccessToken, Path.Combine(options.ArtifactsDirectory, "TestResults", options.Configuration), cts.Token);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    /// <summary>
    /// This method will wait for the helix job to complete using the timeout for Helix work item execution.
    /// 
    /// The most important factor is the local helix process itself. If that exits then the assumption is that it 
    /// successfully reported the status of work items. Hence the moment it is done we can stop waiting and
    /// return. The polling for Helix information is all about adding context and real errors to the tests
    /// in the case helix has issues and cannot complete the process successfully.
    /// </summary>
    private static async Task<int> WaitForHelixJobCompletionAsync(Process process, string helixJobId, string helixCancellationToken, string? helixApiAccessToken, string testResultsDirectory, CancellationToken cancellationToken)
    {
        ConsoleUtil.WriteLine($"Waiting for Helix job {helixJobId} to complete...");
        using var helixApi = new HelixApi(helixApiAccessToken);
        var processWaitTask = process.WaitForExitAsync(cancellationToken);

        try
        {
            await WaitForAllWorkItemsRunningAsync(helixApi, helixJobId, processWaitTask, cancellationToken);
            await WaitForWorkItemsToCompleteOrTimeoutAsync(helixApi, helixJobId, helixCancellationToken, testResultsDirectory, processWaitTask, cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode;
        }
        catch (OperationCanceledException)
        {
            ConsoleUtil.WriteLine($"Cancellation requested. Attempting to cancel Helix job {helixJobId}...");
            await helixApi.CancelJobAsync(helixJobId, helixCancellationToken, CancellationToken.None);
            throw;
        }

        // Wait until all of the work items have started running
        static async Task WaitForAllWorkItemsRunningAsync(HelixApi helixApi, string helixJobId, Task processWaitTask, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            Console.WriteLine($"Waiting for all work items in Helix job {helixJobId} to start running...");
            do
            {
                var delayTask = Task.Delay(HelixPollTime, cancellationToken);
                var completedTask = await Task.WhenAny(processWaitTask, delayTask);
                if (completedTask == processWaitTask)
                {
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var details = await helixApi.GetJobDetailsAsync(helixJobId, cancellationToken);
                    var workItems = details.WorkItems;
                    if (workItems.Unscheduled == 0 && workItems.Waiting == 0)
                    {
                        Console.WriteLine($"All work items are running");
                        return;
                    }

                    var elapsed = DateTime.UtcNow - startTime;
                    Console.WriteLine($"Job Time: {elapsed:hh\\:mm} Work Item States Running: {workItems.Running} Unscheduled: {workItems.Unscheduled} Waiting: {workItems.Waiting} Finished: {workItems.Finished}");

                    if (workItems.Waiting > 0 && elapsed > TimeSpan.FromMinutes(20))
                    {
                        ConsoleUtil.Warning($"Helix job {helixJobId} has {details.WorkItems.Waiting} queued work items after {elapsed:hh\\:mm}. This indicates a queue backup");
                    }
                }
                catch (Exception ex)
                {
                    ConsoleUtil.Warning($"Error while polling Helix API for job status: {ex.Message}");
                }

            } while (true);
        }

        static async Task WaitForWorkItemsToCompleteOrTimeoutAsync(HelixApi helixApi, string helixJobId, string helixCancellationToken, string testResultsDirectory, Task processWaitTask, CancellationToken cancellationToken)
        {
            do
            {
                var delayTask = Task.Delay(HelixPollTime, cancellationToken);
                await Task.WhenAny(processWaitTask, delayTask);

                if (processWaitTask.IsCompleted)
                {
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var workItemList = await helixApi.GetWorkItemsAsync(helixJobId, cancellationToken);
                    var workItemGroups = workItemList.GroupBy(x => x.State).Select(g => $"{g.Key}: {g.Count()}");
                    Console.WriteLine($"Work item states: {string.Join(", ", workItemGroups)}");

                    var timedOutWorkItems = new List<string>();
                    foreach (var workItem in workItemList.Where(x => x.State == "Running"))
                    {
                        var details = await helixApi.GetWorkItemDetailsAsync(helixJobId, workItem.Name, cancellationToken);
                        if (details.Finished is not null)
                        {
                            continue;
                        }

                        if (HasTimedOut(details))
                        {
                            ConsoleUtil.Error($"Helix Job {helixJobId} Work item {workItem.Name} has been in state {details.State} for more than {WorkItemExecutionTimeout}. Timing out the job.");
                            timedOutWorkItems.Add(workItem.Name);
                        }
                    }

                    if (timedOutWorkItems.Count > 0)
                    {
                        WriteSyntheticTimeoutResults(testResultsDirectory, helixJobId, timedOutWorkItems);
                        await helixApi.CancelJobAsync(helixJobId, helixCancellationToken, cancellationToken);
                        throw new Exception($"One or more work items in Helix job {helixJobId} have timed out. Timing out the entire job.");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException and not TimeoutException)
                {
                    ConsoleUtil.Warning($"Error while using Helix API for job status: {ex.Message}");
                }
            } while (true);
        }

        // If a work item has been running for more than WorkItemExecutionTimeout, consider it timed out.
        static bool HasTimedOut(HelixWorkItemDetails details)
        {
            if (details.Started is null)
            {
                return false;
            }

            var started = DateTimeOffset.Parse(details.Started);
            return DateTimeOffset.UtcNow - started > WorkItemExecutionTimeout;
        }

        static void WriteSyntheticTimeoutResults(string testResultsDirectory, string helixJobId, List<string> timedOutWorkItems)
        {
            try
            {
                if (!Directory.Exists(testResultsDirectory))
                {
                    Directory.CreateDirectory(testResultsDirectory);
                }

                foreach (var workItemName in timedOutWorkItems)
                {
                    var escapedWorkItemName = System.Security.SecurityElement.Escape(workItemName);
                    var escapedJobId = System.Security.SecurityElement.Escape(helixJobId);
                    var workItemUrl = System.Security.SecurityElement.Escape(HelixApi.GetWorkItemUrl(helixJobId, workItemName));
                    var consoleUrl = System.Security.SecurityElement.Escape(HelixApi.GetWorkItemConsoleUrl(helixJobId, workItemName));
                    var xml = $"""
                        <?xml version="1.0" encoding="utf-8"?>
                        <assemblies>
                          <assembly name="{escapedWorkItemName}" total="1" passed="0" failed="1" skipped="0">
                            <collection name="Helix Timeout Detection" total="1" passed="0" failed="1" skipped="0">
                              <test name="{escapedWorkItemName}" type="RunTests.TimeoutDetection" method="{escapedWorkItemName}" time="0" result="Fail">
                                <failure exception-type="WorkItemTimeoutException">
                                  <message>Helix work item '{escapedWorkItemName}' in job '{escapedJobId}' exceeded the maximum execution time of {WorkItemExecutionTimeout}.
                        Work Item: {workItemUrl}
                        Console: {consoleUrl}</message>
                                  <stack-trace>The work item was still in the Running state when the timeout was detected.
                        See {workItemUrl} for more details.</stack-trace>
                                </failure>
                              </test>
                            </collection>
                          </assembly>
                        </assemblies>
                        """;

                    var fileName = $"helix_timeout_{workItemName}.xml";
                    var filePath = Path.Combine(testResultsDirectory, fileName);
                    File.WriteAllText(filePath, xml);
                    ConsoleUtil.WriteLine($"Wrote synthetic timeout result to {filePath}");
                }
            }
            catch (Exception ex)
            {
                ConsoleUtil.Warning($"Failed to write synthetic timeout test results: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Creates the helix project file and payload artifacts on disk. Returns the path to the
    /// generated helix project file.
    /// </summary>
    internal static async Task<string> CreateHelixArtifactsAsync(Options options, ImmutableArray<AssemblyInfo> assemblies, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(options.UseHelix);
        Contract.ThrowIfTrue(options.IncludeHtml);
        Contract.ThrowIfFalse(string.IsNullOrEmpty(options.TestFilter));
        Contract.ThrowIfFalse(!string.IsNullOrEmpty(options.ArtifactsDirectory));
        Contract.ThrowIfFalse(!string.IsNullOrEmpty(options.HelixQueueName));
        Contract.ThrowIfFalse(!string.IsNullOrEmpty(options.Configuration));

        // Currently, it's required for the client machine to use the same OS family as the target Helix queue.
        // We could relax this and allow for example Linux clients to kick off Windows jobs, but we'd have to
        // figure out solutions for issues such as creating file paths in the correct format for the target machine.
        var testOS = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? TestOS.Windows
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? TestOS.Mac
            : TestOS.Linux;

        var platform = !string.IsNullOrEmpty(options.Architecture) ? options.Architecture : "x64";
        var dotnetSdkVersion = GetDotNetSdkVersion(options.ArtifactsDirectory);

        // This is the directory where all of the work item payloads are stored.
        var payloadsDir = Path.Combine(options.ArtifactsDirectory, "payloads");
        var logsDir = Path.Combine(options.ArtifactsDirectory, "log", options.Configuration);

        // Retrieve test runtimes from azure devops historical data.
        var testHistory = await TestHistoryManager.GetTestHistoryAsync(options, cancellationToken);
        var helixWorkItems = AssemblyScheduler.Schedule(assemblies.Select(x => x.AssemblyPath), testHistory);
        var helixProjectFileContent = GetHelixProjectFileContent(
            helixWorkItems,
            testOS,
            dotnetSdkVersion,
            platform,
            options.HelixQueueName,
            options.ArtifactsDirectory,
            payloadsDir);

        var helixFilePath = Path.Combine(options.ArtifactsDirectory, "helix.proj");
        File.WriteAllText(helixFilePath, helixProjectFileContent);

        CopyPayloadFilesToLogs(logsDir, payloadsDir);
        File.Copy(helixFilePath, Path.Combine(logsDir, "helix.proj"));

        return helixFilePath;
    }

    /// <summary>
    /// Constructs the dotnet build arguments, launches the process, and returns the process
    /// along with a task that completes with the helix job id once it is parsed from process output.
    /// </summary>
    internal static (Process Process, Task<(string HelixJobId, string HelixJobCancellationToken)> HelixJobInfo) StartHelixJob(Options options, string helixProjectFilePath)
    {
        var logsDir = Path.Combine(options.ArtifactsDirectory, "log", options.Configuration);
        var arguments = $"build -bl:{Path.Combine(logsDir, "helix.binlog")} {helixProjectFilePath}";
        if (!string.IsNullOrEmpty(options.HelixApiAccessToken))
        {
            // Internal queues require an access token.
            // We don't put it in the project string itself since it can cause escaping issues.
            arguments += $" -p:HelixAccessToken={options.HelixApiAccessToken}";
        }
        else
        {
            // If we're not using authenticated access we need to specify a creator.
            var queuedBy = GetEnv("BUILD_QUEUEDBY", "roslyn");
            arguments += $" -p:Creator=\"{queuedBy}\"";
        }

        var helixJobInfoSource = new TaskCompletionSource<(string HelixJobId, string HelixJobCancellationToken)>(TaskCreationOptions.RunContinuationsAsynchronously);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = options.DotnetFilePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
                return;

            ConsoleUtil.WriteLine(e.Data);

            if (!helixJobInfoSource.Task.IsCompleted)
            {
                var match = HelixJobInfoRegex().Match(e.Data);
                if (match.Success)
                {
                    helixJobInfoSource.TrySetResult((match.Groups[1].Value, match.Groups[2].Value));
                }
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                ConsoleUtil.Error(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return (process, helixJobInfoSource.Task);
    }

    /// <summary>
    /// Build up the contents of the helix project file. All paths should be relative to <paramref name="artifactsDir"/>.
    /// </summary>
    private static string GetHelixProjectFileContent(
        IEnumerable<HelixWorkItem> helixWorkItems,
        TestOS testOS,
        string dotnetSdkVersion,
        string platform,
        string helixQueueName,
        string artifactsDir,
        string payloadsDir)
    {
        // Setup the environment variables that are required for the helix project.
        //
        // https://github.com/dotnet/arcade/blob/e7cb34898a1b610eb2a22591a2178da6f1fb7e3c/src/Microsoft.DotNet.Helix/Sdk/Readme.md#developing-helix-sdk
        //
        // Note: rather than setting these variables in the RunTests program it would be better to 
        // emit a .cmd / .sh file that sets these variables and then calls the dotnet command. The current
        // setup makes running the RunTests program destructive to the environment variables
        // of the runner
        //
        var sourceBranch = SetEnv("BUILD_SOURCEBRANCH", "local");
        _ = SetEnv("BUILD_REPOSITORY_NAME", "dotnet/roslyn");
        _ = SetEnv("SYSTEM_TEAMPROJECT", "dnceng-public");
        _ = SetEnv("BUILD_REASON", "pr");

        // https://github.com/dotnet/roslyn/issues/50661
        // it's possible we should be using the BUILD_SOURCEVERSIONAUTHOR instead here a la https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.Helix/Sdk/tools/xharness-runner/Readme.md#how-to-use
        // however that variable isn't documented at https://docs.microsoft.com/en-us/azure/devops/pipelines/build/variables?view=azure-devops&tabs=yaml
        var queuedBy = GetEnv("BUILD_QUEUEDBY", "roslyn").Replace(" ", "");
        var jobName = GetEnv("SYSTEM_JOBDISPLAYNAME", "");
        var buildNumber = GetEnv("BUILD_BUILDNUMBER", "0");
        var duplicateDir = Path.Combine(Path.GetDirectoryName(artifactsDir)!, ".duplicate");

        var builder = new StringBuilder();
        builder.AppendLine($"""
            <Project Sdk="Microsoft.DotNet.Helix.Sdk" DefaultTargets="Test">
              <PropertyGroup>
                <TestRunNamePrefix>{jobName}_</TestRunNamePrefix>
                <HelixSource>pr/{sourceBranch}</HelixSource>
                <HelixType>test</HelixType>
                <HelixBuild>{buildNumber}</HelixBuild>
                <HelixTargetQueues>{helixQueueName}</HelixTargetQueues>
                <IncludeDotNetCli>true</IncludeDotNetCli>
                <DotNetCliVersion>{dotnetSdkVersion}</DotNetCliVersion>
                <DotNetCliPackageType>sdk</DotNetCliPackageType>
                <EnableAzurePipelinesReporter>true</EnableAzurePipelinesReporter>
              </PropertyGroup>

              <ItemGroup>
                <HelixCorrelationPayload Include="{duplicateDir}" />
            """);

        foreach (var helixWorkItem in helixWorkItems)
        {
            AppendHelixWorkItemProject(builder, helixWorkItem, platform, artifactsDir, payloadsDir, testOS);
        }

        builder.AppendLine("""
              </ItemGroup>

              <!-- 
                This target runs after the tests have executed but before the project finishes. 
                It's used to print out the HelixJobId and HelixJobCancellationToken properties to the console
                so we can grab them in the process output and setup our helix watching.
              -->
              <Target Name="PrintHelixInfo" AfterTargets="CoreTest">
                <Message Text="HelixJobId=$(HelixJobId) HelixJobCancellationToken=$(HelixJobCancellationToken)" Importance="high" />
              </Target>

            </Project>
            """);

        return builder.ToString();

        static void AppendHelixWorkItemProject(
            StringBuilder builder,
            HelixWorkItem helixWorkItem,
            string platform,
            string artifactsDir,
            string payloadsDir,
            TestOS testOS)
        {
            var isUnix = testOS != TestOS.Windows;

            // This is the work item payload directory. It needs to contain all of the assets needed to 
            // run the tests on the machine. That includes the assemblies directories, the rsp files, etc ...
            // will be used
            var workItemPayloadDir = Path.Combine(payloadsDir, helixWorkItem.DisplayName);
            _ = Directory.CreateDirectory(workItemPayloadDir);

            var binDir = Path.Combine(artifactsDir, "bin");
            var assemblyRelativeFilePaths = helixWorkItem.AssemblyFilePaths
                .Select(x => Path.GetRelativePath(binDir, x))
                .ToList();

            foreach (var assemblyRelativePath in assemblyRelativeFilePaths)
            {
                var name = Path.GetDirectoryName(assemblyRelativePath)!;
                var targetDir = Path.Combine(workItemPayloadDir, name);
                var sourceDir = Path.Combine(binDir, name);
                _ = Directory.CreateDirectory(Path.GetDirectoryName(targetDir)!);
                Directory.CreateSymbolicLink(targetDir, sourceDir);
            }

            var rspFileName = $"vstest.rsp";
            File.WriteAllText(
                Path.Combine(workItemPayloadDir, rspFileName),
                GetRspFileContent(assemblyRelativeFilePaths, helixWorkItem.TestMethodNames, platform));

            Directory.CreateSymbolicLink(
                path: Path.Combine(workItemPayloadDir, "eng"),
                pathToTarget: Path.Combine(artifactsDir, "..", "eng"));
            File.CreateSymbolicLink(
                path: Path.Combine(workItemPayloadDir, "global.json"),
                pathToTarget: Path.Combine(artifactsDir, "..", "global.json"));

            var (commandFileName, commandContent) = GetHelixCommandContent(assemblyRelativeFilePaths, rspFileName, testOS);
            File.WriteAllText(Path.Combine(workItemPayloadDir, commandFileName), commandContent);

            var (postCommandFileName, postCommandContent) = GetHelixPostCommandContent(testOS);
            File.WriteAllText(Path.Combine(workItemPayloadDir, postCommandFileName), postCommandContent);

            var commandPrefix = testOS != TestOS.Windows ? "./" : "call ";
            builder.AppendLine($"""
                    <HelixWorkItem Include="{helixWorkItem.DisplayName}">
                        <PayloadDirectory>{workItemPayloadDir}</PayloadDirectory>
                        <Command>{commandPrefix}{commandFileName}</Command>
                        <PostCommands>{commandPrefix}{postCommandFileName}</PostCommands>
                        <Timeout>01:00:00</Timeout>
                        <ExpectedExecutionTime>{helixWorkItem.EstimatedExecutionTime}</ExpectedExecutionTime>
                    </HelixWorkItem>
                """);
        }

        static (string FileName, string Content) GetHelixCommandContent(
            IEnumerable<string> assemblyRelativeFilePaths,
            string vstestRspFileName,
            TestOS testOS)
        {
            var isUnix = testOS != TestOS.Windows;
            var isMac = testOS == TestOS.Mac;
            var setEnvironmentVariable = isUnix ? "export" : "set";

            var command = new StringBuilder();
            command.AppendLine($"{setEnvironmentVariable} DOTNET_ROLL_FORWARD=LatestMajor");
            command.AppendLine($"{setEnvironmentVariable} DOTNET_ROLL_FORWARD_TO_PRERELEASE=1");
            command.AppendLine(isUnix ? $"ls -l" : $"dir");
            command.AppendLine("dotnet --info");

            string[] knownEnvironmentVariables =
            [
                "ROSLYN_TEST_IOPERATION",
                "ROSLYN_TEST_USEDASSEMBLIES",
                "DOTNET_RuntimeAsync"
            ];

            foreach (var knownEnvironmentVariable in knownEnvironmentVariables)
            {
                if (Environment.GetEnvironmentVariable(knownEnvironmentVariable) is string { Length: > 0 } value)
                {
                    command.AppendLine($"{setEnvironmentVariable} {knownEnvironmentVariable}={value}");
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

            command.AppendLine("powershell -ExecutionPolicy ByPass -NoProfile -File ./eng/enable-preview-sdks.ps1");

            // Rehydrate assemblies that we need to run as part of this work item.
            foreach (var assemblyRelativeFilePath in assemblyRelativeFilePaths)
            {
                var directoryName = Path.GetDirectoryName(assemblyRelativeFilePath);
                if (isUnix)
                {
                    // If we're on unix make sure we have permissions to run the rehydrate script.
                    command.AppendLine($"chmod +x {directoryName}/rehydrate.sh");
                }

                command.AppendLine(isUnix ? $"./{directoryName}/rehydrate.sh" : $@"call {directoryName}\rehydrate.cmd");
                command.AppendLine(isUnix ? $"ls -l {directoryName}" : $"dir {directoryName}");
            }

            // Build the command to run the rsp file. dotnet test does not pass rsp files correctly the vs test 
            // console, so we have to manually invoke vs test console.
            // See https://github.com/microsoft/vstest/issues/3513
            // The dotnet sdk includes the vstest.console.dll executable in the sdk folder in the installed version, 
            // so we look it up using the/ DOTNET_ROOT environment variable set by helix.
            if (isUnix)
            {
                // $ is a special character in msbuild so we replace it with %24 in the helix project.
                // https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-special-characters?view=vs-2022
                command.AppendLine("vstestConsolePath=$(find ${DOTNET_ROOT} -name \"vstest.console.dll\")");
                command.AppendLine("echo ${vstestConsolePath}");
                command.AppendLine($"dotnet exec \"${{vstestConsolePath}}\" @{vstestRspFileName}");
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
                command.AppendLine($"dotnet exec \"%vstestConsolePath%\" @{vstestRspFileName}");
            }

            return (isUnix ? "command.sh" : "command.cmd", command.ToString());
        }

        static (string FileName, string Content) GetHelixPostCommandContent(TestOS testOS)
        {
            var isUnix = testOS != TestOS.Windows;

            // We want to collect diagnostic artifacts during the post command step here; these commands
            // are ran after the return value of the main command is captured. A Helix Job is considered
            // to fail if the main command returns a non-zero error code, and we don't want the cleanup
            // steps to interfere with that. PostCommands exist precisely to address this problem.
            //
            // The artifacts collected are:
            //  - *.dmp: crash dump files. This is still necessary even with us setting
            //    DOTNET_DbgMiniDumpName because the system can create non .NET Core dump files that
            //    aren't controlled by that value.
            //  - Sequence_*.xml: xunit sequence files that track the order of tests executed and which
            //    ones were active when the test host crashed.
            string command;

            if (isUnix)
            {
                // Write out this command into a separate file; unfortunately the use of single quotes and ; that is required
                // for the command to work causes too much escaping issues in MSBuild.
                command = """
                    find . -name '*.dmp' -exec cp {} $HELIX_DUMP_FOLDER \;
                    find . -name 'Sequence_*.xml' -exec cp {} $HELIX_DUMP_FOLDER \;
                    """;
            }
            else
            {
                command = """
                    for /r %%f in (*.dmp) do copy %%f %HELIX_DUMP_FOLDER%
                    for /r %%f in (Sequence_*.xml) do copy %%f %HELIX_DUMP_FOLDER%
                    """;
            }

            return (isUnix ? "post-command.sh" : "post-command.cmd", command);
        }
    }

    private static string GetEnv(string name, string defaultValue)
    {
        if (Environment.GetEnvironmentVariable(name) is { } value)
        {
            return value;
        }

        Console.WriteLine($"The environment variable {name} was not set. Using the default value {defaultValue}");
        return defaultValue;
    }

    private static string SetEnv(string name, string defaultValue)
    {
        if (Environment.GetEnvironmentVariable(name) is { } value)
        {
            return value;
        }

        Console.WriteLine($"The environment variable {name} was not set. Setting it to {defaultValue}");
        Environment.SetEnvironmentVariable(name, defaultValue);
        return defaultValue;
    }

    private static string GetDotNetSdkVersion(string artifactsDir)
    {
        var globalJsonFilePath = GetGlobalJsonPath(artifactsDir);
        var globalJson = JsonConvert.DeserializeAnonymousType(File.ReadAllText(globalJsonFilePath), new { sdk = new { version = "" } })
            ?? throw new InvalidOperationException("Failed to deserialize global.json.");
        return globalJson.sdk.version;

        static string GetGlobalJsonPath(string artifactsDir)
        {
            var path = artifactsDir;
            while (path is object)
            {
                var globalJsonPath = Path.Join(path, "global.json");
                if (File.Exists(globalJsonPath))
                {
                    return globalJsonPath;
                }
                path = Path.GetDirectoryName(path);
            }
            throw new Exception($@"Could not find global.json by walking up from ""{artifactsDir}"".");
        }
    }

    /// <summary>
    /// Build an rsp file to send to dotnet test that contains all the assemblies and tests to run.
    /// This gets around command line length limitations and avoids weird escaping issues.
    /// See https://docs.microsoft.com/en-us/dotnet/standard/commandline/syntax#response-files
    /// </summary>
    private static string GetRspFileContent(
        List<string> assemblyRelativeFilePaths,
        IEnumerable<string> testMethodNames,
        string platform)
    {
        var builder = new StringBuilder();

        // Add each assembly we want to test on a new line.
        foreach (var filePath in assemblyRelativeFilePaths)
        {
            builder.AppendLine($"\"{filePath}\"");
        }

        builder.AppendLine($@"/Platform:{platform}");

        // The xml file must end in test-results.xml for the Azure Pipelines reporter to pick it up.
        builder.AppendLine($@"/Logger:xunit;LogFilePath=work-item-test-results.xml");

        // Also add a console logger so that the helix log reports results as we go.
        builder.AppendLine($@"/Logger:console;verbosity=detailed");

        // Specifies the results directory - this is where dumps from the blame options will get published. 
        builder.AppendLine($"/ResultsDirectory:.");

        var blameOption = "CollectDump;CollectHangDump";
        builder.AppendLine($"/Blame:{blameOption};TestTimeout={(int)TestMethodTimeout.TotalMinutes}minutes;DumpType=full");

        // Build the filter string
        if (testMethodNames.Any())
        {
            builder.Append("/TestCaseFilter:\"");
            var any = false;
            foreach (var testMethodName in testMethodNames)
            {
                MaybeAddSeparator();
                builder.Append($"FullyQualifiedName={testMethodName}");
            }
            builder.AppendLine("\"");

            void MaybeAddSeparator(char separator = '|')
            {
                if (any)
                {
                    builder.Append(separator);
                }

                any = true;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// This method will copy the generated files from the payloads directory to the logs/{configuration} 
    /// directory. This will cause them to be uploaded as part of the artifacts for the Helix run so that
    /// we can see / debug them if there are any issues.
    /// </summary>
    private static void CopyPayloadFilesToLogs(string logsDir, string payloadsDir)
    {
        _ = Directory.CreateDirectory(logsDir);

        CopyDir(payloadsDir);
        foreach (var workItemPayloadDir in Directory.EnumerateDirectories(payloadsDir))
        {
            CopyDir(workItemPayloadDir);
        }

        void CopyDir(string dir)
        {
            foreach (var filePath in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
            {
                var relativePath = Path.GetRelativePath(payloadsDir, filePath);
                var destinationPath = Path.Combine(logsDir, relativePath);
                _ = Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Copy(filePath, destinationPath);
            }
        }
    }
}
