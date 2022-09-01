// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using RunTestsUtils;

namespace RunTests
{
    internal sealed class ProcessTestExecutor
    {
        public static string GetCommandLineArguments(string assemblyPath, Options options)
        {
            var commandLineArgumentsBuilder = new StringBuilder();
            commandLineArgumentsBuilder.Append($"test \"{assemblyPath}\"");
            commandLineArgumentsBuilder.Append($" --logger \"xunit;LogFilePath={GetResultsFilePath(assemblyPath, options)}\"");
            commandLineArgumentsBuilder.Append(options.DotnetTestArgs);
            return commandLineArgumentsBuilder.ToString();
        }

        private static string GetResultsFilePath(string assemblyPath, Options options)
        {
            var fileName = $"{Path.GetFileName(assemblyPath)}_test_results.xml";
            return Path.Combine(options.LogFilesDirectory, fileName);
        }

        public static async Task<TestResult> RunTestAsync(string assemblyPath, Options options, CancellationToken cancellationToken)
        {
            var result = await RunTestAsyncInternal(assemblyPath, options, isRetry: false, cancellationToken);

            // For the old integration test framework we need to retry manually here.
            if (!HasBuiltInRetry(assemblyPath) && !result.Succeeded)
            {
                return await RunTestAsyncInternal(assemblyPath, options, isRetry: true, cancellationToken);
            }

            return result;

            static bool HasBuiltInRetry(string assemblyPath)
            {
                // vs-extension-testing handles test retry internally.
                return assemblyPath.Contains("Microsoft.VisualStudio.LanguageServices.New.IntegrationTests.dll");
            }
        }

        private static async Task<TestResult> RunTestAsyncInternal(string assemblyPath, Options options, bool isRetry, CancellationToken cancellationToken)
        {
            try
            {
                var commandLineArguments = GetCommandLineArguments(assemblyPath, options);
                var resultsFilePath = GetResultsFilePath(assemblyPath, options);
                var resultsDir = Path.GetDirectoryName(resultsFilePath);
                var processResultList = new List<ProcessResult>();

                // NOTE: xUnit doesn't always create the log directory
                Directory.CreateDirectory(resultsDir!);

                // Define environment variables for processes started via ProcessRunner.
                var environmentVariables = new Dictionary<string, string>();

                if (isRetry && File.Exists(resultsFilePath))
                {
                    ConsoleUtil.WriteLine("Starting a retry. Tests which failed will run a second time to reduce flakiness.");
                    try
                    {
                        var doc = XDocument.Load(resultsFilePath);
                        foreach (var test in doc.XPathSelectElements("/assemblies/assembly/collection/test[@result='Fail']"))
                        {
                            ConsoleUtil.WriteLine($"  {test.Attribute("name")!.Value}: {test.Attribute("result")!.Value}");
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
                var dotnetProcessInfo = ProcessRunner.CreateProcess(
                    ProcessRunner.CreateProcessStartInfo(
                        options.DotnetFilePath,
                        commandLineArguments,
                        displayWindow: false,
                        captureOutput: true,
                        environmentVariables: environmentVariables),
                    lowPriority: false,
                    cancellationToken: cancellationToken);
                Logger.Log($"Create xunit process with id {dotnetProcessInfo.Id} for assembly {assemblyPath}");

                var xunitProcessResult = await dotnetProcessInfo.Result;
                var span = DateTime.UtcNow - start;

                Logger.Log($"Exit xunit process with id {dotnetProcessInfo.Id} for test {assemblyPath} with code {xunitProcessResult.ExitCode}");
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
                    }
                }

                Logger.Log($"Command line {assemblyPath} completed in {span.TotalSeconds} seconds: {options.DotnetFilePath} {commandLineArguments}");
                var standardOutput = string.Join(Environment.NewLine, xunitProcessResult.OutputLines) ?? "";
                var errorOutput = string.Join(Environment.NewLine, xunitProcessResult.ErrorLines) ?? "";

                var testResultInfo = new TestResultInfo(
                    exitCode: xunitProcessResult.ExitCode,
                    resultsFilePath: resultsFilePath,
                    elapsed: span,
                    standardOutput: standardOutput,
                    errorOutput: errorOutput);

                return new TestResult(
                    assemblyPath,
                    testResultInfo,
                    commandLineArguments,
                    processResults: ImmutableArray.CreateRange(processResultList));
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to run {assemblyPath} with {options.DotnetFilePath}. {ex}");
            }
        }
    }
}
