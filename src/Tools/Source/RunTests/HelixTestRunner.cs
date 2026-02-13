// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

internal sealed class HelixTestRunner
{
    internal enum TestOS
    {
        Windows,
        Linux,
        Mac,
    }

    internal static async Task<int> RunAsync(Options options, ImmutableArray<AssemblyInfo> assemblies, CancellationToken cancellationToken)
    {
        Verify(options.UseHelix);
        Verify(!options.IncludeHtml);
        Verify(string.IsNullOrEmpty(options.TestFilter));
        Verify(!string.IsNullOrEmpty(options.ArtifactsDirectory));
        Verify(!string.IsNullOrEmpty(options.HelixQueueName));
        Verify(!string.IsNullOrEmpty(options.Configuration));

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

        var arguments = $"build -bl:{Path.Combine(logsDir, "helix.binlog")} {helixFilePath}";
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

        CopyPayloadFilesToLogs(logsDir, payloadsDir);
        File.Copy(helixFilePath, Path.Combine(logsDir, "helix.proj"));

        var process = ProcessRunner.CreateProcess(
            executable: options.DotnetFilePath,
            arguments: arguments,
            captureOutput: true,
            onOutputDataReceived: (e) => { Debug.Assert(e.Data is not null); ConsoleUtil.WriteLine(e.Data); },
            cancellationToken: cancellationToken);
        var processResult = await process.Result.ConfigureAwait(false);
        return processResult.ExitCode;

        void Verify([DoesNotReturnIf(false)] bool condition, [CallerArgumentExpression("condition")] string? message = null)
        {
            if (!condition)
            {
                throw new Exception($"Verify failed: {message}");
            }
        }
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

            // We want to collect any dumps during the post command step here; these commands are ran after the
            // return value of the main command is captured; a Helix Job is considered to fail if the main command returns a
            // non-zero error code, and we don't want the cleanup steps to interfere with that. PostCommands exist
            // precisely to address this problem.
            //
            // This is still necessary even with us setting DOTNET_DbgMiniDumpName because the system can create 
            // non .NET Core dump files that aren't controlled by that value.
            var command = new StringBuilder();

            if (isUnix)
            {
                // Write out this command into a separate file; unfortunately the use of single quotes and ; that is required
                // for the command to work causes too much escaping issues in MSBuild.
                command.AppendLine("find . -name '*.dmp' -exec cp {} $HELIX_DUMP_FOLDER \\;");
                command.AppendLine("find . -name 'diag_log*' -exec cp {} $HELIX_WORKITEM_UPLOAD_ROOT \\;");
            }
            else
            {
                command.AppendLine("for /r %%f in (*.dmp) do copy %%f %HELIX_DUMP_FOLDER%");
                command.AppendLine("for %%f in (diag_log*) do copy %%f %HELIX_WORKITEM_UPLOAD_ROOT%");
            }

            return (isUnix ? "post-command.sh" : "post-command.cmd", command.ToString());
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

        builder.AppendLine(@$"/Diag:diag_log.txt;tracelevel=verbose");

        var blameOption = "CollectDump;CollectHangDump";
        builder.AppendLine($"/Blame:{blameOption};TestTimeout=15minutes;DumpType=full");

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
