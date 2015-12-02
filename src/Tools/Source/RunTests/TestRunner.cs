// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests
{
    internal sealed class TestRunner
    {
        private struct TestResult
        {
            internal readonly bool Succeeded;
            internal readonly string AssemblyName;
            internal readonly TimeSpan Elapsed;
            internal readonly string ErrorOutput;

            internal TestResult(bool succeeded, string assemblyName, TimeSpan elapsed, string errorOutput)
            {
                Succeeded = succeeded;
                AssemblyName = assemblyName;
                Elapsed = elapsed;
                ErrorOutput = errorOutput;
            }
        }

        private readonly Options _options;

        internal TestRunner(Options options)
        {
            _options = options;
        }

        internal async Task<bool> RunAllAsync(IEnumerable<string> assemblyList, CancellationToken cancellationToken)
        {
            var max = (int)Environment.ProcessorCount * 1.5;
            var allPassed = true;
            var waiting = new Stack<string>(assemblyList);
            var running = new List<Task<TestResult>>();
            var completed = new List<TestResult>();

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                var i = 0;
                while (i < running.Count)
                {
                    var task = running[i];
                    if (task.IsCompleted)
                    {
                        try
                        {
                            var testResult = await task.ConfigureAwait(false);
                            if (!testResult.Succeeded)
                            {
                                allPassed = false;
                            }

                            completed.Add(testResult);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                            allPassed = false;
                        }

                        running.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                while (running.Count < max && waiting.Count > 0)
                {
                    var task = RunTest(waiting.Pop(), cancellationToken);
                    running.Add(task);
                }

                Console.WriteLine("  {0} running, {1} queued, {2} completed", running.Count, waiting.Count, completed.Count);
                Task.WaitAny(running.ToArray());
            } while (running.Count > 0);

            Print(completed);

            return allPassed;
        }

        private void Print(List<TestResult> testResults)
        {
            testResults.Sort((x, y) => x.Elapsed.CompareTo(y.Elapsed));

            foreach (var testResult in testResults.Where(x => !x.Succeeded))
            {
                Console.WriteLine("Errors {0}: ", testResult.AssemblyName);
                Console.WriteLine(testResult.ErrorOutput);
            }

            Console.WriteLine("================");
            foreach (var testResult in testResults)
            {
                var color = testResult.Succeeded ? Console.ForegroundColor : ConsoleColor.Red;
                ConsoleUtil.WriteLine(color, "{0,-75} {1} {2}", testResult.AssemblyName, testResult.Succeeded ? "PASSED" : "FAILED", testResult.Elapsed);
            }
            Console.WriteLine("================");
        }

        private async Task<TestResult> RunTest(string assemblyPath, CancellationToken cancellationToken)
        {
            try
            {
                var assemblyName = Path.GetFileName(assemblyPath);
                var resultsFile = Path.Combine(Path.GetDirectoryName(assemblyPath), "xUnitResults", $"{assemblyName}.{(_options.UseHtml ? "html" : "xml")}");
                var resultsDir = Path.GetDirectoryName(resultsFile);
                var outputLogPath = Path.Combine(resultsDir, $"{assemblyName}.out.log");

                // NOTE: xUnit doesn't always create the log directory
                Directory.CreateDirectory(resultsDir);

                // NOTE: xUnit seems to have an occasional issue creating logs create
                // an empty log just in case, so our runner will still fail.
                File.Create(resultsFile).Close();

                var builder = new StringBuilder();
                builder.AppendFormat(@"""{0}""", assemblyPath);
                builder.AppendFormat(@" -{0} ""{1}""", _options.UseHtml ? "html" : "xml", resultsFile);
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

                var errorOutput = new StringBuilder();
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
                    File.WriteAllLines(outputLogPath, processOutput.OutputLines);

                    // On occasion we get a non-0 output but no actual data in the result file.  The could happen
                    // if xunit manages to crash when running a unit test (a stack overflow could cause this, for instance).
                    // To avoid losing information, write the process output to the console.  In addition, delete the results
                    // file to avoid issues with any tool attempting to interpret the (potentially malformed) text.
                    var resultData = string.Empty;
                    try
                    {
                        resultData = File.ReadAllText(resultsFile).Trim();
                    }
                    catch
                    {
                        // Happens if xunit didn't produce a log file
                    }

                    if (resultData.Length == 0)
                    {
                        // Delete the output file.
                        File.Delete(resultsFile);
                    }

                    errorOutput.AppendLine($"Command: {_options.XunitPath} {builder}");
                    errorOutput.AppendLine($"xUnit output: {outputLogPath}");

                    if (processOutput.ErrorLines.Any())
                    {
                        foreach (var line in processOutput.ErrorLines)
                        {
                            errorOutput.AppendLine(line);
                        }
                    }
                    else
                    {
                        errorOutput.AppendLine($"xunit produced no error output but had exit code {processOutput.ExitCode}");
                    }

                    // If the results are html, use Process.Start to open in the browser.

                    if (_options.UseHtml && resultData.Length > 0)
                    {
                        Process.Start(resultsFile);
                    }
                }

                return new TestResult(processOutput.ExitCode == 0, assemblyName, span, errorOutput.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to run {assemblyPath} with {_options.XunitPath}. {ex}");
            }
        }

        private static void DeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Ignore
            }
        }
    }
}
