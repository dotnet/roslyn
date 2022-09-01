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
using RunTestsUtils;

namespace RunTests
{
    internal readonly struct RunAllResult
    {
        internal bool Succeeded { get; }
        internal ImmutableArray<TestResult> TestResults { get; }
        internal ImmutableArray<ProcessResult> ProcessResults { get; }

        internal RunAllResult(bool succeeded, ImmutableArray<TestResult> testResults, ImmutableArray<ProcessResult> processResults)
        {
            Succeeded = succeeded;
            TestResults = testResults;
            ProcessResults = processResults;
        }
    }

    internal sealed class TestRunner
    {
        internal static async Task<RunAllResult> RunAllAsync(ImmutableArray<string> assemblies, Options options, CancellationToken cancellationToken)
        {
            // Integration tests always run sequentially since they require UI interactions.
            var max = 1;
            var waiting = new Stack<string>(assemblies);
            var running = new List<Task<TestResult>>();
            var completed = new List<TestResult>();
            var failures = 0;

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
                                failures++;
                                if (testResult.ResultsDisplayFilePath is string resultsPath)
                                {
                                    ConsoleUtil.WriteLine(ConsoleColor.Red, resultsPath);
                                }
                                else
                                {
                                    foreach (var result in testResult.ProcessResults)
                                    {
                                        foreach (var line in result.ErrorLines)
                                        {
                                            ConsoleUtil.WriteLine(ConsoleColor.Red, line);
                                        }
                                    }
                                }
                            }

                            completed.Add(testResult);
                        }
                        catch (Exception ex)
                        {
                            ConsoleUtil.WriteLine(ConsoleColor.Red, $"Error: {ex.Message}");
                            failures++;
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
                    var task = ProcessTestExecutor.RunTestAsync(waiting.Pop(), options, cancellationToken);
                    running.Add(task);
                }

                // Display the current status of the TestRunner.
                // Note: The { ... , 2 } is to right align the values, thus aligns sections into columns.
                ConsoleUtil.Write($"  {running.Count,2} running, {waiting.Count,2} queued, {completed.Count,2} completed");
                if (failures > 0)
                {
                    ConsoleUtil.Write($", {failures,2} failures");
                }
                ConsoleUtil.WriteLine();

                if (running.Count > 0)
                {
                    await Task.WhenAny(running.ToArray());
                }
            } while (running.Count > 0);

            Print(completed, options);

            var processResults = ImmutableArray.CreateBuilder<ProcessResult>();
            foreach (var c in completed)
            {
                processResults.AddRange(c.ProcessResults);
            }

            return new RunAllResult((failures == 0), completed.ToImmutableArray(), processResults.ToImmutable());
        }

        private static void Print(List<TestResult> testResults, Options options)
        {
            testResults.Sort((x, y) => x.Elapsed.CompareTo(y.Elapsed));

            foreach (var testResult in testResults.Where(x => !x.Succeeded))
            {
                PrintFailedTestResult(testResult, options);
            }

            ConsoleUtil.WriteLine("================");
            var line = new StringBuilder();
            foreach (var testResult in testResults)
            {
                line.Length = 0;
                var color = testResult.Succeeded ? Console.ForegroundColor : ConsoleColor.Red;
                line.Append($"{testResult.DisplayName,-75}");
                line.Append($" {(testResult.Succeeded ? "PASSED" : "FAILED")}");
                line.Append($" {testResult.Elapsed}");
                line.Append($" {(!string.IsNullOrEmpty(testResult.Diagnostics) ? "?" : "")}");

                var message = line.ToString();
                ConsoleUtil.WriteLine(color, message);
            }
            ConsoleUtil.WriteLine("================");

            // Print diagnostics out last so they are cleanly visible at the end of the test summary
            ConsoleUtil.WriteLine("Extra run diagnostics for logging, did not impact run results");
            foreach (var testResult in testResults.Where(x => !string.IsNullOrEmpty(x.Diagnostics)))
            {
                ConsoleUtil.WriteLine(testResult.Diagnostics!);
            }
        }

        private static void PrintFailedTestResult(TestResult testResult, Options options)
        {
            // Save out the error output for easy artifact inspecting
            var outputLogPath = Path.Combine(options.LogFilesDirectory, $"xUnitFailure-{testResult.DisplayName}.log");

            ConsoleUtil.WriteLine($"Errors {testResult.DisplayName}");
            ConsoleUtil.WriteLine(testResult.ErrorOutput);

            // TODO: Put this in the log and take it off the ConsoleUtil output to keep it simple?
            ConsoleUtil.WriteLine($"Command: {testResult.CommandLine}");
            ConsoleUtil.WriteLine($"xUnit output log: {outputLogPath}");

            File.WriteAllText(outputLogPath, testResult.StandardOutput ?? "");

            if (!string.IsNullOrEmpty(testResult.ErrorOutput))
            {
                ConsoleUtil.WriteLine(testResult.ErrorOutput);
            }
            else
            {
                ConsoleUtil.WriteLine($"xunit produced no error output but had exit code {testResult.ExitCode}. Writing standard output:");
                ConsoleUtil.WriteLine(testResult.StandardOutput ?? "(no standard output)");
            }
        }
    }
}
