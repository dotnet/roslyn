// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using RunTests.Cache;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests
{
    internal sealed class ProcessTestExecutor : ITestExecutor
    {
        private readonly TestExecutionOptions _options;

        public IDataStorage DataStorage => EmptyDataStorage.Instance;

        internal ProcessTestExecutor(TestExecutionOptions options)
        {
            _options = options;
        }

        public string GetCommandLine(AssemblyInfo assemblyInfo)
        {
            return $"{_options.XunitPath} {GetCommandLineArguments(assemblyInfo)}";
        }

        public string GetCommandLineArguments(AssemblyInfo assemblyInfo)
        {
            var assemblyName = Path.GetFileName(assemblyInfo.AssemblyPath);
            var resultsFilePath = GetResultsFilePath(assemblyInfo);

            var builder = new StringBuilder();
            builder.AppendFormat(@"""{0}""", assemblyInfo.AssemblyPath);
            builder.AppendFormat(@" {0}", assemblyInfo.ExtraArguments);
            builder.AppendFormat(@" -{0} ""{1}""", _options.UseHtml ? "html" : "xml", resultsFilePath);
            builder.Append(" -noshadow -verbose");

            if (!string.IsNullOrWhiteSpace(_options.Trait))
            {
                var traits = _options.Trait.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var trait in traits)
                {
                    builder.AppendFormat(" -trait {0}", trait);
                }
            }

            if (!string.IsNullOrWhiteSpace(_options.NoTrait))
            {
                var traits = _options.NoTrait.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var trait in traits)
                {
                    builder.AppendFormat(" -notrait {0}", trait);
                }
            }

            return builder.ToString();
        }

        private string GetResultsFilePath(AssemblyInfo assemblyInfo)
        {
            var resultsDir = Path.Combine(Path.GetDirectoryName(assemblyInfo.AssemblyPath), Constants.ResultsDirectoryName);
            return Path.Combine(resultsDir, assemblyInfo.ResultsFileName);
        }

        public async Task<TestResult> RunTestAsync(AssemblyInfo assemblyInfo, CancellationToken cancellationToken)
        {
            try
            {
                var commandLineArguments = GetCommandLineArguments(assemblyInfo);
                var resultsFilePath = GetResultsFilePath(assemblyInfo);
                var resultsDir = Path.GetDirectoryName(resultsFilePath);

                // NOTE: xUnit doesn't always create the log directory
                Directory.CreateDirectory(resultsDir);

                // NOTE: xUnit seems to have an occasional issue creating logs create
                // an empty log just in case, so our runner will still fail.
                File.Create(resultsFilePath).Close();

                var dumpOutputFilePath = Path.Combine(resultsDir, $"{assemblyInfo.DisplayName}.dmp");

                var start = DateTime.UtcNow;
                var xunitPath = _options.XunitPath;
                var processOutput = await ProcessRunner.RunProcessAsync(
                    xunitPath,
                    commandLineArguments,
                    lowPriority: false,
                    displayWindow: false,
                    captureOutput: true,
                    cancellationToken: cancellationToken,
                    processMonitor: p => CrashDumps.TryMonitorProcess(p, dumpOutputFilePath)).ConfigureAwait(false);
                var span = DateTime.UtcNow - start;

                if (processOutput.ExitCode != 0)
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
                var standardOutput = string.Join(Environment.NewLine, processOutput.OutputLines);
                var errorOutput = string.Join(Environment.NewLine, processOutput.ErrorLines);

                return new TestResult(
                    exitCode: processOutput.ExitCode,
                    assemblyInfo: assemblyInfo,
                    resultDir: resultsDir,
                    resultsFilePath: resultsFilePath,
                    commandLine: commandLine,
                    elapsed: span,
                    standardOutput: standardOutput,
                    errorOutput: errorOutput,
                    isResultFromCache: false);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to run {assemblyInfo.AssemblyPath} with {_options.XunitPath}. {ex}");
            }
        }
    }
}
