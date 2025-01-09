// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RunTests;

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
        Verify(!string.IsNullOrEmpty(options.HelixApiAccessToken));

        // Currently, it's required for the client machine to use the same OS family as the target Helix queue.
        // We could relax this and allow for example Linux clients to kick off Windows jobs, but we'd have to
        // figure out solutions for issues such as creating file paths in the correct format for the target machine.
        var testOS = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows), RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) switch
        {
            (true, _) => TestOS.Windows,
            (_, true) => TestOS.Mac,
            _ => TestOS.Linux
        };

        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var platform = !string.IsNullOrEmpty(options.Architecture) ? options.Architecture : "x64";
        var dotnetSdkVersion = GetDotNetSdkVersion(options.ArtifactsDirectory);

        // Retrieve test runtimes from azure devops historical data.
        var testHistory = await TestHistoryManager.GetTestHistoryAsync(options, cancellationToken);
        var workItems = AssemblyScheduler.Schedule(assemblies, testHistory);

        // Note: this should be an explicit argument
        var executionDir = AppContext.BaseDirectory;

        var helixProjectFilePath = WriteHelixProjectFile(
            workItems,
            testOS,
            dotnetSdkVersion,
            platform,
            options.HelixQueueName,
            options.ArtifactsDirectory,
            executionDir);

        var arguments = $"build {helixProjectFilePath} -p:HelixAccessToken={options.HelixApiAccessToken}";
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

    private static string WriteHelixProjectFile(
        ImmutableArray<WorkItemInfo> workItems,
        TestOS testOS,
        string dotnetSdkVersion,
        string platform,
        string helixQueueName,
        string artifactsDir,
        string executionDir)
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
        var duplicateDir = Path.Combine(artifactsDir, ".duplicate");

        var builder = new StringBuilder();
        builder.AppendLine($"""
            <Project Sdk=""Microsoft.DotNet.Helix.Sdk"" DefaultTargets=""Test"">
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
                <HelixCorrelationPayload Include="{duplicateDir}" />";
            """);

        foreach (var workItemInfo in workItems)
        {
            AppendHelixWorkItemProject(builder, workItemInfo, platform, artifactsDir, executionDir, testOS);
        }

        builder.AppendLine("</ItemGroup>");

        var projectContent = builder.ToString();
        Console.WriteLine("Helix project file");
        Console.WriteLine("-------------------");
        Console.WriteLine(projectContent);
        Console.WriteLine("-------------------");

        var projectFilePath = Path.Combine(artifactsDir, "helix-tmp.csproj");
        File.WriteAllText(projectFilePath, projectContent);
        return projectFilePath;

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

        static void AppendHelixWorkItemProject(
            StringBuilder builder,
            WorkItemInfo workItemInfo,
            string platform,
            string artifactsDir,
            string executionDir,
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
            var payloadDirectory = Path.Combine(artifactsDir, "bin");

            // Update the assembly groups to test with the assembly paths in the context of the helix work item.
            workItemInfo = workItemInfo with { Filters = workItemInfo.Filters.ToImmutableSortedDictionary(kvp => kvp.Key with { AssemblyPath = GetHelixRelativeAssemblyPath(kvp.Key.AssemblyPath) }, kvp => kvp.Value) };

            AddRehydrateTestFoldersCommand(command, workItemInfo, isUnix);

            var xmlResultsFilePath = Path.Combine(
                executionDir,
                $"workitem_{workItemInfo.PartitionIndex}.xml");

            // Build an rsp file to send to dotnet test that contains all the assemblies and tests to run.
            // This gets around command line length limitations and avoids weird escaping issues.
            // See https://docs.microsoft.com/en-us/dotnet/standard/commandline/syntax#response-files
            var rspFileContent = GetRspFileContent(workItemInfo, platform, xmlResultsFilePath);
            var rspFileName = $"vstest_{workItemInfo.PartitionIndex}.rsp";
            File.WriteAllText(Path.Combine(payloadDirectory, rspFileName), rspFileContent);

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

            builder.AppendLine($"""
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
                """);
        }

        static string GetEnv(string name, string defaultValue)
        {
            if (Environment.GetEnvironmentVariable(name) is { } value)
            {
                return value;
            }

            Console.WriteLine($"The environment variable {name} was not set. Using the default value {defaultValue}");
            return defaultValue;
        }

        static string SetEnv(string name, string defaultValue)
        {
            if (Environment.GetEnvironmentVariable(name) is { } value)
            {
                return value;
            }

            Console.WriteLine($"The environment variable {name} was not set. Setting it to {defaultValue}");
            Environment.SetEnvironmentVariable(name, defaultValue);
            return defaultValue;
        }
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

    private static string GetRspFileContent(
        WorkItemInfo workItem,
        string platform,
        string xmlResultsFilePath)
    {
        var builder = new StringBuilder();

        // Add each assembly we want to test on a new line.
        foreach (var assembly in workItem.Filters.Keys)
        {
            builder.AppendLine($"\"{assembly.AssemblyPath}\"");
        }

        builder.AppendLine($@"/Platform:{platform}");
        builder.AppendLine($@"/Logger:xunit;LogFilePath={xmlResultsFilePath}");
        var blameOption = "CollectHangDump";

        // The 'CollectDumps' option uses operating system features to collect dumps when a process crashes. We
        // only enable the test executor blame feature in remaining cases, as the latter relies on ProcDump and
        // interferes with automatic crash dump collection on Windows.
        blameOption = "CollectDump;CollectHangDump";

        // The 25 minute timeout in integration tests accounts for the fact that VSIX deployment and/or experimental hive reset and
        // configuration can take significant time (seems to vary from ~10 seconds to ~15 minutes), and the blame
        // functionality cannot separate this configuration overhead from the first test which will eventually run.
        // https://github.com/dotnet/roslyn/issues/59851
        //
        // Helix timeout is 15 minutes as helix jobs fully timeout in 30minutes.  So in order to capture dumps we need the timeout
        // to be 2x shorter than the expected test run time (15min) in case only the last test hangs.
        builder.AppendLine($"/Blame:{blameOption};TestTimeout=15minutes;DumpType=full");

        // Build the filter string
        var testMethods = workItem.Filters.SelectMany(x => x.Value);
        if (testMethods.Any())
        {
            builder.Append("/TestCaseFilter:\"");
            var any = false;
            foreach (var testMethod in testMethods)
            {
                MaybeAddSeparator();
                builder.Append($"FullyQualifiedName={testMethod.FullyQualifiedName}");
            }

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
}
