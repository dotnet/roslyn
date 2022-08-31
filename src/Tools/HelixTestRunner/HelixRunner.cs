// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RunTestsUtils;

namespace HelixTestRunner;
internal class HelixRunner
{
    internal static async Task<int> RunAllOnHelixAsync(ImmutableArray<WorkItemInfo> workItems, HelixOptions options, CancellationToken cancellationToken)
    {
        var sourceBranch = Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCH");
        if (sourceBranch is null)
        {
            sourceBranch = "local";
            Console.WriteLine($@"BUILD_SOURCEBRANCH environment variable was not set. Using source branch ""{sourceBranch}"" instead");
            Environment.SetEnvironmentVariable("BUILD_SOURCEBRANCH", sourceBranch);
        }

        var msbuildTestPayloadRoot = Path.GetDirectoryName(options.ArtifactsDirectory);
        if (msbuildTestPayloadRoot is null)
        {
            throw new IOException($@"Malformed ArtifactsDirectory in options: ""{options.ArtifactsDirectory}""");
        }

        var isAzureDevOpsRun = Environment.GetEnvironmentVariable("BUILD_BUILDID") is not null;
        if (!isAzureDevOpsRun)
        {
            Console.WriteLine("BUILD_BUILDID environment variable was not set, will not publish test results for a local run.");
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
            Console.WriteLine($@"BUILD_QUEUEDBY environment variable was not set. Using value ""{queuedBy}"" instead");
        }

        var jobName = Environment.GetEnvironmentVariable("SYSTEM_JOBDISPLAYNAME");
        if (jobName is null)
        {
            Console.WriteLine($"SYSTEM_JOBDISPLAYNAME environment variable was not set. Using a blank TestRunNamePrefix for Helix job.");
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
        <HelixTargetQueues>" + options.HelixQueueName + @"</HelixTargetQueues>
        <Creator>" + queuedBy + @"</Creator>
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

        var process = ProcessRunner.CreateProcess(
            executable: options.DotnetExecutablePath,
            arguments: "build helix-tmp.csproj",
            captureOutput: true,
            onOutputDataReceived: (e) => Console.WriteLine(e.Data),
            cancellationToken: cancellationToken);
        var result = await process.Result;

        return result.ExitCode;

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

            var setEnvironmentVariable = isUnix ? "export" : "set";

            var command = new StringBuilder();
            command.AppendLine($"{setEnvironmentVariable} DOTNET_ROLL_FORWARD=LatestMajor");
            command.AppendLine($"{setEnvironmentVariable} DOTNET_ROLL_FORWARD_TO_PRERELEASE=1");
            command.AppendLine(isUnix ? $"ls -l" : $"dir");
            command.AppendLine("dotnet --info");

            var knownEnvironmentVariables = new[] { "ROSLYN_TEST_IOPERATION", "ROSLYN_TEST_USEDASSEMBLIES" };
            foreach (var knownEnvironmentVariable in knownEnvironmentVariables)
            {
                if (string.Equals(Environment.GetEnvironmentVariable(knownEnvironmentVariable), "true", StringComparison.OrdinalIgnoreCase))
                {
                    command.AppendLine($"{setEnvironmentVariable} {knownEnvironmentVariable}=true");
                }
            }

            // Create a payload directory that contains all the assemblies in the work item in separate folders.
            var payloadDirectory = Path.Combine(msbuildTestPayloadRoot, "artifacts", "bin");

            // Update the assembly groups to test with the assembly paths in the context of the helix work item.
            workItemInfo = workItemInfo with { Filters = workItemInfo.Filters.ToImmutableSortedDictionary(kvp => kvp.Key with { AssemblyPath = GetHelixRelativeAssemblyPath(kvp.Key.AssemblyPath) }, kvp => kvp.Value) };

            AddRehydrateTestFoldersCommand(command, workItemInfo, isUnix);

            // Build an rsp file to send to dotnet test that contains all the assemblies and tests to run.
            // This gets around command line length limitations and avoids weird escaping issues.
            // See https://docs.microsoft.com/en-us/dotnet/standard/commandline/syntax#response-files
            var rspFileContents = BuildRspFileContents(workItemInfo, options);
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

    public static string BuildRspFileContents(WorkItemInfo workItem, HelixOptions options)
    {
        var fileContentsBuilder = new StringBuilder();

        // Add each assembly we want to test on a new line.
        var assemblyPaths = workItem.Filters.Keys.Select(assembly => assembly.AssemblyPath);
        foreach (var path in assemblyPaths)
        {
            fileContentsBuilder.AppendLine($"\"{path}\"");
        }

        fileContentsBuilder.AppendLine($@"/Platform:{options.Architecture}");
        fileContentsBuilder.AppendLine($@"/Logger:xunit;LogFilePath={GetResultsFilePath(workItem, options)}");

        var blameOption = "CollectDump;CollectHangDump";

        // Helix timeout is 15 minutes as helix jobs fully timeout in 30minutes.  So in order to capture dumps we need the timeout
        // to be 2x shorter than the expected test run time (15min) in case only the last test hangs.
        fileContentsBuilder.AppendLine($"/Blame:{blameOption};TestTimeout=15minutes;DumpType=full");

        // Build the filter string
        var filterStringBuilder = new StringBuilder();
        var filters = workItem.Filters.Values.SelectMany(filter => filter).Where(filter => !string.IsNullOrEmpty(filter.FullyQualifiedName)).ToImmutableArray();

        if (filters.Length > 0 || !string.IsNullOrWhiteSpace(options.TestFilter))
        {
            filterStringBuilder.Append("/TestCaseFilter:\"");
            var any = false;
            foreach (var filter in filters)
            {
                MaybeAddSeparator();
                filterStringBuilder.Append($"FullyQualifiedName={filter.FullyQualifiedName}");
            }

            if (options.TestFilter is not null)
            {
                MaybeAddSeparator();
                filterStringBuilder.Append(options.TestFilter);
            }

            filterStringBuilder.Append('"');

            void MaybeAddSeparator(char separator = '|')
            {
                if (any)
                {
                    filterStringBuilder.Append(separator);
                }

                any = true;
            }
        }

        fileContentsBuilder.AppendLine(filterStringBuilder.ToString());
        return fileContentsBuilder.ToString();
    }

    private static string GetResultsFilePath(WorkItemInfo workItemInfo, HelixOptions options)
    {
        var fileName = $"WorkItem_{workItemInfo.PartitionIndex}_{options.Architecture}_test_results.xml";
        return Path.Combine(".", fileName);
    }
}
