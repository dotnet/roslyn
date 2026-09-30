// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RunTests
{
    internal sealed class ProcessTestExecutor
    {
        public static string BuildRspFileContents(WorkItemInfo workItem, Options options, string xmlResultsFilePath, string? htmlResultsFilePath, string? diagnosticsDirectory = null)
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

            // Always use CollectDump and CollectHangDump to ensure we get actionable data on both
            // crashes and hangs, matching the Helix configuration.
            var blameOption = "CollectDump;CollectHangDump";

            // Helix work items have a separate infrastructure deadline. Local integration runs can
            // explicitly allow longer VSIX deployment/hive setup with --integration.
            // https://github.com/dotnet/roslyn/issues/59851
            var timeout = options.UseHelix ? "15minutes" : options.Integration ? "25minutes" : "10minutes";
            fileContentsBuilder.AppendLine($"/Blame:{blameOption};TestTimeout={timeout};DumpType=full");

            // Specifies the results directory - this is where dumps from the blame options will get published.
            fileContentsBuilder.AppendLine($"/ResultsDirectory:\"{diagnosticsDirectory ?? GetWorkItemDirectory(workItem, options)}\"");

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

        private static string GetWorkItemDirectory(WorkItemInfo workItemInfo, Options options)
            => options.UseHelix ? options.TestResultsDirectory :
                Path.Combine(options.TestResultsDirectory, $"WorkItem_{workItemInfo.PartitionIndex}_{options.Architecture}");

        public async Task<TestResult> RunTestAsync(WorkItemInfo workItemInfo, Options options, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var resultsFilePath = GetResultsFilePath(workItemInfo, options);
                var workItemDirectory = GetWorkItemDirectory(workItemInfo, options);
                // Separate each invocation's diagnostics so old dumps cannot be attributed to this run.
                Directory.CreateDirectory(workItemDirectory);
                workItemDirectory = Path.Combine(workItemDirectory, Guid.NewGuid().ToString("N"));
                var htmlResultsFilePath = options.IncludeHtml ? GetResultsFilePath(workItemInfo, options, "html") : null;
                File.Delete(GetSyntheticFailurePath(resultsFilePath));
                var rspFileContents = BuildRspFileContents(workItemInfo, options, resultsFilePath, htmlResultsFilePath, workItemDirectory);
                var rspFilePath = Path.Combine(getRspDirectory(), $"vstest_{workItemInfo.PartitionIndex}.rsp");
                File.WriteAllText(rspFilePath, rspFileContents);

                var vsTestConsolePath = GetVsTestConsolePath(options.DotnetFilePath);

                var commandLineArguments = $"exec \"{vsTestConsolePath}\" @\"{rspFilePath}\"";

                var resultsDir = Path.GetDirectoryName(resultsFilePath);
                var processResultList = new List<ProcessResult>();

                // NOTE: xUnit doesn't always create the log directory
                Directory.CreateDirectory(resultsDir!);

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
                        environmentVariables: options.EnvironmentVariables),
                    lowPriority: false);
                using var tree = new OwnedProcessTree(dotnetProcessInfo.Process);
                Logger.Log($"Create xunit process with id {dotnetProcessInfo.Id} for test {workItemInfo.DisplayName}");

                using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var cancellationTask = Task.Delay(Timeout.InfiniteTimeSpan, cancellationSource.Token);
                string? timeoutMessage = null;
                if (await Task.WhenAny(dotnetProcessInfo.Result, cancellationTask).ConfigureAwait(false) == cancellationTask)
                {
                    timeoutMessage = $"Run cancelled or global deadline exceeded while running {workItemInfo.DisplayName}.";
                    ConsoleUtil.Error(timeoutMessage);
                    WriteSyntheticFailure(resultsFilePath, workItemInfo.DisplayName, timeoutMessage);
                    try
                    {
                        await DumpCollector.CollectAsync(tree, options, workItemDirectory).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        ConsoleUtil.Warning($"Unable to collect timeout dumps: {ex.Message}");
                    }
                    finally
                    {
                        tree.Kill();
                    }
                }
                else
                {
                    cancellationSource.Cancel();
                }

                ProcessResult xunitProcessResult;
                try
                {
                    xunitProcessResult = timeoutMessage is null
                        ? await dotnetProcessInfo.Result
                        : await dotnetProcessInfo.Result.WaitAsync(TimeSpan.FromSeconds(10));
                }
                catch (TimeoutException)
                {
                    ConsoleUtil.Warning($"Process {dotnetProcessInfo.Id} did not finish cleanup within 10 seconds.");
                    xunitProcessResult = new ProcessResult(dotnetProcessInfo.Process, Program.ExitFailure,
                        new ReadOnlyCollection<string>([]), new ReadOnlyCollection<string>([timeoutMessage!]));
                }
                var span = DateTime.UtcNow - start;

                Logger.Log($"Exit xunit process with id {dotnetProcessInfo.Id} for test {workItemInfo.DisplayName} with code {xunitProcessResult.ExitCode}");
                processResultList.Add(xunitProcessResult);

                if (timeoutMessage is not null || xunitProcessResult.ExitCode != 0)
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

                var exitCode = timeoutMessage is not null ? Program.ExitFailure : xunitProcessResult.ExitCode;
                if (exitCode != 0)
                {
                    if (timeoutMessage is null && !ContainsFailedTest(resultsFilePath))
                    {
                        WriteSyntheticFailure(GetResultsFilePath(workItemInfo, options), workItemInfo.DisplayName,
                            $"Test runner exited with code {exitCode} without a failed test result.{Environment.NewLine}{errorOutput}");
                    }

                    CheckForCrashes(GetResultsFilePath(workItemInfo, options), workItemInfo.DisplayName,
                        workItemDirectory, timeoutMessage);
                    resultsFilePath ??= GetSyntheticFailurePath(GetResultsFilePath(workItemInfo, options));
                }

                var testResultInfo = new TestResultInfo(
                    exitCode: exitCode,
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

        private static string GetSyntheticFailurePath(string resultsFilePath)
            => Path.Combine(Path.GetDirectoryName(resultsFilePath)!,
                Path.GetFileNameWithoutExtension(resultsFilePath) + "_synthetic_failure.xml");

        private static bool ContainsFailedTest(string? resultsFilePath)
        {
            if (resultsFilePath is null)
                return false;

            try
            {
                using var reader = XmlReader.Create(resultsFilePath, new XmlReaderSettings { XmlResolver = null });
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "test" && reader.GetAttribute("result") == "Fail")
                        return true;
                }
            }
            catch (IOException) { }
            catch (XmlException) { }

            return false;
        }

        private static void WriteSyntheticFailure(string resultsFilePath, string displayName, string message)
        {
            var doc = new XDocument(
                new XElement("assemblies",
                    new XElement("assembly", new XAttribute("name", displayName),
                        new XAttribute("total", 1), new XAttribute("passed", 0), new XAttribute("failed", 1), new XAttribute("skipped", 0),
                        new XElement("collection", new XAttribute("name", "RunTests"),
                            new XAttribute("total", 1), new XAttribute("passed", 0), new XAttribute("failed", 1), new XAttribute("skipped", 0),
                            new XElement("test", new XAttribute("name", displayName),
                                new XAttribute("type", "RunTests.WorkItem"), new XAttribute("method", "Execute"),
                                new XAttribute("time", 0), new XAttribute("result", "Fail"),
                                new XElement("failure", new XAttribute("exception-type", "WorkItemFailure"),
                                    new XElement("message", message)))))));
            doc.Save(GetSyntheticFailurePath(resultsFilePath));
        }

        /// <summary>
        /// Surface host failures in AzDO even when VSTest did not produce a failed test result.
        /// Only inspect the diagnostics directory belonging to this work-item invocation.
        /// </summary>
        private static void CheckForCrashes(string resultsFilePath, string displayName, string testResultsDirectory, string? timeoutMessage)
        {
            var (dumpFiles, sequenceFiles, crashingTest, isHang) = detectDumpFiles();
            if (dumpFiles.Length == 0 && timeoutMessage is null)
            {
                return;
            }

            isHang |= timeoutMessage is not null;
            Logger.Log($"Detected dump files for {displayName}: {string.Join(", ", dumpFiles)}");

            // Emit as AzDO timeline errors so they display prominently in the build results
            var failureType = isHang ? "timeout" : "crash";
            if (crashingTest is string test)
            {
                ConsoleUtil.Error($"Test host {failureType} detected for {displayName}. Test running at time of {failureType}: {test}");
            }
            else
            {
                ConsoleUtil.Error($"Test host {failureType} detected for {displayName}");
            }

            foreach (var dump in dumpFiles)
            {
                ConsoleUtil.WriteLine(ConsoleColor.Red, $"  Dump: {dump}");
            }

            // Copy sequence files to the test results directory so they are included in artifacts
            copySequenceFilesToArtifacts(sequenceFiles, testResultsDirectory);

            writeSyntheticFailure(resultsFilePath, dumpFiles, crashingTest, isHang);

            (string[] DumpFiles, string[] SequenceFiles, string? CrashingTest, bool IsHang) detectDumpFiles()
            {
                if (!Directory.Exists(testResultsDirectory))
                {
                    return ([], [], null, false);
                }

                var dumpFiles = Directory.GetFiles(testResultsDirectory, "*.dmp", SearchOption.AllDirectories);
                if (dumpFiles.Length == 0)
                {
                    return ([], [], null, false);
                }

                var isHang = dumpFiles.Any(f => Path.GetFileName(f).Contains("hangdump", StringComparison.OrdinalIgnoreCase));
                string? crashingTest = null;
                var allSequenceFiles = new List<string>();

                var dumpDirectories = dumpFiles.Select(Path.GetDirectoryName).Distinct();
                foreach (var dir in dumpDirectories)
                {
                    if (dir == null) continue;
                    var sequenceFiles = Directory.GetFiles(dir, "Sequence_*.xml", SearchOption.TopDirectoryOnly);
                    allSequenceFiles.AddRange(sequenceFiles);
                    if (sequenceFiles.Length > 0 && crashingTest == null)
                    {
                        crashingTest = getLastTestFromSequenceFile(sequenceFiles[0]);
                    }
                }

                return (dumpFiles, allSequenceFiles.ToArray(), crashingTest, isHang);
            }

            static string? getLastTestFromSequenceFile(string sequenceFilePath)
            {
                try
                {
                    var doc = XDocument.Load(sequenceFilePath);
                    var tests = doc.Descendants("Test");
                    var incomplete = tests.Where(t => t.Attribute("Completed")?.Value == "False");
                    var target = incomplete.FirstOrDefault() ?? tests.LastOrDefault();
                    return target?.Attribute("Name")?.Value;
                }
                catch
                {
                    return null;
                }
            }

            static void copySequenceFilesToArtifacts(string[] sequenceFiles, string testResultsDirectory)
            {
                foreach (var sequenceFile in sequenceFiles)
                {
                    try
                    {
                        var destPath = Path.Combine(testResultsDirectory, Path.GetFileName(sequenceFile));
                        if (!string.Equals(Path.GetFullPath(sequenceFile), Path.GetFullPath(destPath), StringComparison.OrdinalIgnoreCase))
                        {
                            File.Copy(sequenceFile, destPath, overwrite: true);
                        }

                        Logger.Log($"Copied sequence file to artifacts: {destPath}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Warning: Failed to copy sequence file {sequenceFile}: {ex.Message}");
                    }
                }
            }

            void writeSyntheticFailure(string resultsFilePath, string[] dumpFiles, string? crashingTest, bool isHang)
            {
                try
                {
                    var failureType = isHang ? "HANG" : "CRASH";
                    var testName = crashingTest ?? "Unknown";
                    var dumpFileNames = string.Join(", ", dumpFiles.Select(Path.GetFileName));
                    var dumpFilePaths = string.Join("\n", dumpFiles);

                    var escapedWorkItemName = SecurityElement.Escape(displayName);
                    var escapedTestName = SecurityElement.Escape(testName);
                    var escapedDumpFileNames = SecurityElement.Escape(dumpFileNames);
                    var escapedDumpFilePaths = SecurityElement.Escape(dumpFilePaths);

                    var syntheticPath = GetSyntheticFailurePath(resultsFilePath);

                    var xml = $"""
                        <?xml version="1.0" encoding="utf-8"?>
                        <assemblies>
                          <assembly name="{escapedWorkItemName}" total="1" passed="0" failed="1" skipped="0">
                            <collection name="Crash/Hang Detection" total="1" passed="0" failed="1" skipped="0">
                              <test name="[{failureType}] {escapedTestName}" type="RunTests.{failureType}Detection" method="{escapedTestName}" time="0" result="Fail">
                                <failure exception-type="TestHost{failureType}Exception">
                                  <message>{SecurityElement.Escape(timeoutMessage)} Test host {failureType.ToLower()} detected. Test running at time of {failureType.ToLower()}: {escapedTestName}. Dump files: {escapedDumpFileNames}</message>
                                  <stack-trace>Dump files collected:
                        {escapedDumpFilePaths}</stack-trace>
                                </failure>
                              </test>
                            </collection>
                          </assembly>
                        </assemblies>
                        """;

                    File.WriteAllText(syntheticPath, xml);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Warning: Failed to write synthetic test failure: {ex.Message}");
                }
            }
        }
    }
}
