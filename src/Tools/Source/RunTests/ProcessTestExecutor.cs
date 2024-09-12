// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests
{
    internal sealed class ProcessTestExecutor
    {
        public static string BuildRspFileContents(WorkItemInfo workItem, Options options, string xmlResultsFilePath, string? htmlResultsFilePath)
        {
            var fileContentsBuilder = new StringBuilder();

            // Add each assembly we want to test on a new line.
            var assemblyPaths = workItem.Filters.Keys.Select(assembly => assembly.AssemblyPath);
            foreach (var path in assemblyPaths)
            {
                fileContentsBuilder.AppendLine($"\"{path}\"");
            }

            fileContentsBuilder.AppendLine($@"/Platform:{options.Architecture}");
            fileContentsBuilder.AppendLine($@"/Logger:xunit;LogFilePath={xmlResultsFilePath}");
            if (htmlResultsFilePath != null)
            {
                fileContentsBuilder.AppendLine($@"/Logger:html;LogFileName={htmlResultsFilePath}");
            }

            var blameOption = "CollectHangDump";
            if (!options.CollectDumps)
            {
                // The 'CollectDumps' option uses operating system features to collect dumps when a process crashes. We
                // only enable the test executor blame feature in remaining cases, as the latter relies on ProcDump and
                // interferes with automatic crash dump collection on Windows.
                blameOption = "CollectDump;CollectHangDump";
            }

            // The 25 minute timeout in integration tests accounts for the fact that VSIX deployment and/or experimental hive reset and
            // configuration can take significant time (seems to vary from ~10 seconds to ~15 minutes), and the blame
            // functionality cannot separate this configuration overhead from the first test which will eventually run.
            // https://github.com/dotnet/roslyn/issues/59851
            //
            // Helix timeout is 15 minutes as helix jobs fully timeout in 30minutes.  So in order to capture dumps we need the timeout
            // to be 2x shorter than the expected test run time (15min) in case only the last test hangs.
            var timeout = options.UseHelix ? "15minutes" : "25minutes";
            fileContentsBuilder.AppendLine($"/Blame:{blameOption};TestTimeout={timeout};DumpType=full");

            // Specifies the results directory - this is where dumps from the blame options will get published.
            fileContentsBuilder.AppendLine($"/ResultsDirectory:{options.TestResultsDirectory}");

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

        private static string GetVsTestConsolePath(string dotnetPath)
        {
            var dotnetDir = Path.GetDirectoryName(dotnetPath)!;
            var sdkDir = Path.Combine(dotnetDir, "sdk");
            var vsTestConsolePath = Directory.EnumerateFiles(sdkDir, "vstest.console.dll", SearchOption.AllDirectories).Last();
            return vsTestConsolePath;
        }

        public static string GetResultsFilePath(WorkItemInfo workItemInfo, Options options, string suffix = "xml")
        {
            var fileName = $"WorkItem_{workItemInfo.PartitionIndex}_{options.Architecture}_test_results.{suffix}";
            return Path.Combine(options.TestResultsDirectory, fileName);
        }

        public async Task<TestResult> RunTestAsync(WorkItemInfo workItemInfo, Options options, CancellationToken cancellationToken)
        {
            try
            {
                var resultsFilePath = GetResultsFilePath(workItemInfo, options);
                var htmlResultsFilePath = options.IncludeHtml ? GetResultsFilePath(workItemInfo, options, "html") : null;
                var rspFileContents = BuildRspFileContents(workItemInfo, options, resultsFilePath, htmlResultsFilePath);
                var rspFilePath = Path.Combine(getRspDirectory(), $"vstest_{workItemInfo.PartitionIndex}.rsp");
                File.WriteAllText(rspFilePath, rspFileContents);

                var vsTestConsolePath = GetVsTestConsolePath(options.DotnetFilePath);

                var commandLineArguments = $"exec \"{vsTestConsolePath}\" @\"{rspFilePath}\"";

                var resultsDir = Path.GetDirectoryName(resultsFilePath);
                var processResultList = new List<ProcessResult>();

                // NOTE: xUnit doesn't always create the log directory
                Directory.CreateDirectory(resultsDir!);

                // Define environment variables for processes started via ProcessRunner.
                var environmentVariables = new Dictionary<string, string>();

                // NOTE: xUnit seems to have an occasional issue creating logs create
                // an empty log just in case, so our runner will still fail.
                File.Create(resultsFilePath).Close();

                var start = DateTime.UtcNow;
                var dotnetProcessInfo = ProcessRunner.CreateProcess(
                    ProcessRunner.CreateProcessStartInfo(
                        options.DotnetFilePath,
                        commandLineArguments,
                        displayWindow: false,
                        captureOutput: true,
                        environmentVariables: environmentVariables),
                    lowPriority: false,
                    cancellationToken: cancellationToken);
                Logger.Log($"Create xunit process with id {dotnetProcessInfo.Id} for test {workItemInfo.DisplayName}");

                var xunitProcessResult = await dotnetProcessInfo.Result;
                var span = DateTime.UtcNow - start;

                Logger.Log($"Exit xunit process with id {dotnetProcessInfo.Id} for test {workItemInfo.DisplayName} with code {xunitProcessResult.ExitCode}");
                processResultList.Add(xunitProcessResult);

                if (xunitProcessResult.ExitCode != 0)
                {
                    // On occasion we get a non-0 output but no actual data in the result file.  The could happen
                    // if xunit manages to crash when running a unit test (a stack overflow could cause this, for instance).
                    // To avoid losing information, write the process output to the console.  In addition, delete the results
                    // file to avoid issues with any tool attempting to interpret the (potentially malformed) text.
                    var resultData = string.Empty;
                    try
                    {
                        resultData = File.ReadAllText(resultsFilePath).Trim();
                    }
                    catch
                    {
                        // Happens if xunit didn't produce a log file
                    }

                    if (resultData.Length == 0)
                    {
                        // Delete the output file.
                        File.Delete(resultsFilePath);
                        resultsFilePath = null;
                        htmlResultsFilePath = null;
                    }
                }

                Logger.Log($"Command line {workItemInfo.DisplayName} completed in {span.TotalSeconds} seconds: {options.DotnetFilePath} {commandLineArguments}");
                var standardOutput = string.Join(Environment.NewLine, xunitProcessResult.OutputLines) ?? "";
                var errorOutput = string.Join(Environment.NewLine, xunitProcessResult.ErrorLines) ?? "";

                var testResultInfo = new TestResultInfo(
                    exitCode: xunitProcessResult.ExitCode,
                    resultsFilePath: resultsFilePath,
                    htmlResultsFilePath: htmlResultsFilePath,
                    elapsed: span,
                    standardOutput: standardOutput,
                    errorOutput: errorOutput);

                return new TestResult(
                    workItemInfo,
                    testResultInfo,
                    commandLineArguments,
                    processResults: ImmutableArray.CreateRange(processResultList));

                string getRspDirectory()
                {
                    // There is no artifacts directory on Helix, just use the current directory
                    if (options.UseHelix)
                    {
                        return Directory.GetCurrentDirectory();
                    }

                    var dirPath = Path.Combine(options.ArtifactsDirectory, "tmp", options.Configuration, "vstest-rsp");
                    Directory.CreateDirectory(dirPath);
                    return dirPath;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to run {workItemInfo.DisplayName} with {options.DotnetFilePath}. {ex}");
            }
        }
    }
}
