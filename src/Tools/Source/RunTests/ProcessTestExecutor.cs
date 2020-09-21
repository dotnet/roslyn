// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace RunTests
{
    internal sealed class ProcessTestExecutor
    {
        public TestExecutionOptions Options { get; }

        internal ProcessTestExecutor(TestExecutionOptions options)
        {
            Options = options;
        }

        public string GetCommandLine(AssemblyInfo assemblyInfo)
        {
            return $"{Options.XunitPath} {GetCommandLineArguments(assemblyInfo)}";
        }

        public string GetCommandLineArguments(AssemblyInfo assemblyInfo)
        {
            var assemblyName = Path.GetFileName(assemblyInfo.AssemblyPath);
            var resultsFilePath = GetResultsFilePath(assemblyInfo);

            var builder = new StringBuilder();
            builder.AppendFormat(@"""{0}""", assemblyInfo.AssemblyPath);
            builder.AppendFormat(@" {0}", assemblyInfo.ExtraArguments);
            builder.AppendFormat(@" -{0} ""{1}""", Options.UseHtml ? "html" : "xml", resultsFilePath);
            builder.Append(" -noshadow -verbose");

            if (!string.IsNullOrWhiteSpace(Options.Trait))
            {
                var traits = Options.Trait.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var trait in traits)
                {
                    builder.AppendFormat(" -trait {0}", trait);
                }
            }

            if (!string.IsNullOrWhiteSpace(Options.NoTrait))
            {
                var traits = Options.NoTrait.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var trait in traits)
                {
                    builder.AppendFormat(" -notrait {0}", trait);
                }
            }

            return builder.ToString();
        }

        private string GetResultsFilePath(AssemblyInfo assemblyInfo)
        {
            return Path.Combine(Options.OutputDirectory, assemblyInfo.ResultsFileName);
        }

        public async Task<TestResult> RunTestAsync(AssemblyInfo assemblyInfo, CancellationToken cancellationToken)
        {
            var result = await RunTestAsyncInternal(assemblyInfo, retry: false, cancellationToken);

            // For integration tests (TestVsi), we make one more attempt to re-run failed tests.
            if (Options.TestVsi && !Options.UseHtml && !result.Succeeded)
            {
                return await RunTestAsyncInternal(assemblyInfo, retry: true, cancellationToken);
            }

            return result;
        }

        private async Task<TestResult> RunTestAsyncInternal(AssemblyInfo assemblyInfo, bool retry, CancellationToken cancellationToken)
        {
            try
            {
                var commandLineArguments = GetCommandLineArguments(assemblyInfo);
                var resultsFilePath = GetResultsFilePath(assemblyInfo);
                var resultsDir = Path.GetDirectoryName(resultsFilePath);
                var processResultList = new List<ProcessResult>();
                ProcessInfo? procDumpProcessInfo = null;

                // NOTE: xUnit doesn't always create the log directory
                Directory.CreateDirectory(resultsDir);

                // Define environment variables for processes started via ProcessRunner.
                var environmentVariables = new Dictionary<string, string>();
                Options.ProcDumpInfo?.WriteEnvironmentVariables(environmentVariables);

                if (retry && File.Exists(resultsFilePath))
                {
                    ConsoleUtil.WriteLine("Starting a retry. Tests which failed will run a second time to reduce flakiness.");
                    try
                    {
                        var doc = XDocument.Load(resultsFilePath);
                        foreach (var test in doc.XPathSelectElements("/assemblies/assembly/collection/test[@result='Fail']"))
                        {
                            ConsoleUtil.WriteLine($"  {test.Attribute("name").Value}: {test.Attribute("result").Value}");
                        }
                    }
                    catch
                    {
                        ConsoleUtil.WriteLine("  ...Failed to identify the list of specific failures.");
                    }

                    // Copy the results file path, since the new xunit run will overwrite it
                    var backupResultsFilePath = Path.ChangeExtension(resultsFilePath, ".old");
                    File.Copy(resultsFilePath, backupResultsFilePath, overwrite: true);

                    // If running the process with this varialbe added, we assume that this file contains 
                    // xml logs from the first attempt.
                    environmentVariables.Add("OutputXmlFilePath", backupResultsFilePath);
                }

                // NOTE: xUnit seems to have an occasional issue creating logs create
                // an empty log just in case, so our runner will still fail.
                File.Create(resultsFilePath).Close();

                var start = DateTime.UtcNow;
                var xunitProcessInfo = ProcessRunner.CreateProcess(
                    ProcessRunner.CreateProcessStartInfo(
                        Options.XunitPath,
                        commandLineArguments,
                        displayWindow: false,
                        captureOutput: true,
                        environmentVariables: environmentVariables),
                    lowPriority: false,
                    cancellationToken: cancellationToken);
                Logger.Log($"Create xunit process with id {xunitProcessInfo.Id} for test {assemblyInfo.DisplayName}");

                // Now that xunit is running we should kick off a procDump process if it was specified
                if (Options.ProcDumpInfo != null)
                {
                    var procDumpInfo = Options.ProcDumpInfo.Value;
                    var procDumpStartInfo = ProcessRunner.CreateProcessStartInfo(
                        procDumpInfo.ProcDumpFilePath,
                        ProcDumpUtil.GetProcDumpCommandLine(xunitProcessInfo.Id, procDumpInfo.DumpDirectory),
                        captureOutput: true,
                        displayWindow: false);
                    Directory.CreateDirectory(procDumpInfo.DumpDirectory);
                    procDumpProcessInfo = ProcessRunner.CreateProcess(procDumpStartInfo, cancellationToken: cancellationToken);
                    Logger.Log($"Create procdump process with id {procDumpProcessInfo.Value.Id} for xunit {xunitProcessInfo.Id} for test {assemblyInfo.DisplayName}");
                }

                var xunitProcessResult = await xunitProcessInfo.Result;
                var span = DateTime.UtcNow - start;

                Logger.Log($"Exit xunit process with id {xunitProcessInfo.Id} for test {assemblyInfo.DisplayName} with code {xunitProcessResult.ExitCode}");
                processResultList.Add(xunitProcessResult);
                if (procDumpProcessInfo != null)
                {
                    var procDumpProcessResult = await procDumpProcessInfo.Value.Result;
                    Logger.Log($"Exit procdump process with id {procDumpProcessInfo.Value.Id} for {xunitProcessInfo.Id} for test {assemblyInfo.DisplayName} with code {procDumpProcessResult.ExitCode}");
                    processResultList.Add(procDumpProcessResult);
                }

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
                    }
                }

                var commandLine = GetCommandLine(assemblyInfo);
                Logger.Log($"Command line {assemblyInfo.DisplayName}: {commandLine}");
                var standardOutput = string.Join(Environment.NewLine, xunitProcessResult.OutputLines) ?? "";
                var errorOutput = string.Join(Environment.NewLine, xunitProcessResult.ErrorLines) ?? "";
                var testResultInfo = new TestResultInfo(
                    exitCode: xunitProcessResult.ExitCode,
                    resultsFilePath: resultsFilePath,
                    elapsed: span,
                    standardOutput: standardOutput,
                    errorOutput: errorOutput);

                return new TestResult(
                    assemblyInfo,
                    testResultInfo,
                    commandLine,
                    processResults: ImmutableArray.CreateRange(processResultList));
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to run {assemblyInfo.AssemblyPath} with {Options.XunitPath}. {ex}");
            }
        }
    }
}
