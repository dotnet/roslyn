﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly Options _options;

        internal ProcessTestExecutor(Options options)
        {
            _options = options;
        }

        public async Task<TestResult> RunTestAsync(string assemblyPath, CancellationToken cancellationToken)
        {
            try
            {
                var assemblyName = Path.GetFileName(assemblyPath);
                var resultsDir = Path.Combine(Path.GetDirectoryName(assemblyPath), Constants.ResultsDirectoryName);
                var resultsFilePath = Path.Combine(resultsDir, $"{assemblyName}.{(_options.UseHtml ? "html" : "xml")}");

                // NOTE: xUnit doesn't always create the log directory
                Directory.CreateDirectory(resultsDir);

                // NOTE: xUnit seems to have an occasional issue creating logs create
                // an empty log just in case, so our runner will still fail.
                File.Create(resultsFilePath).Close();

                var builder = new StringBuilder();
                builder.AppendFormat(@"""{0}""", assemblyPath);
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

                var start = DateTime.UtcNow;

                var xunitPath = _options.XunitPath;
                var processOutput = await ProcessRunner.RunProcessAsync(
                    xunitPath,
                    builder.ToString(),
                    lowPriority: false,
                    displayWindow: false,
                    captureOutput: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
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

                var commandLine = $"{xunitPath} {builder.ToString()}";
                var standardOutput = string.Join(Environment.NewLine, processOutput.OutputLines);
                var errorOutput = string.Join(Environment.NewLine, processOutput.ErrorLines);

                return new TestResult(
                    exitCode: processOutput.ExitCode,
                    assemblyPath: assemblyPath,
                    resultDir: resultsDir,
                    resultsFilePath: resultsFilePath,
                    commandLine: commandLine,
                    elapsed: span,
                    standardOutput: standardOutput,
                    errorOutput: errorOutput);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to run {assemblyPath} with {_options.XunitPath}. {ex}");
            }
        }
    }
}
