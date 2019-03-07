// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests
{
    internal struct RunAllResult
    {
        internal bool Succeeded { get; }
        internal int CacheCount { get; }
        internal ImmutableArray<TestResult> TestResults { get; }
        internal ImmutableArray<ProcessResult> ProcessResults { get; }

        internal RunAllResult(bool succeeded, int cacheCount, ImmutableArray<TestResult> testResults, ImmutableArray<ProcessResult> processResults)
        {
            Succeeded = succeeded;
            CacheCount = cacheCount;
            TestResults = testResults;
            ProcessResults = processResults;
        }
    }

    internal sealed class TestRunner
    {
        private readonly ITestExecutor _testExecutor;
        private readonly Options _options;

        internal TestRunner(Options options, ITestExecutor testExecutor)
        {
            _testExecutor = testExecutor;
            _options = options;
        }

        internal async Task<RunAllResult> RunAllAsync(IEnumerable<AssemblyInfo> assemblyInfoList, CancellationToken cancellationToken)
        {
            // Use 1.5 times the number of processors for unit tests, but only 1 processor for the open integration tests
            // since they perform actual UI operations (such as mouse clicks and sending keystrokes) and we don't want two
            // tests to conflict with one-another.
            var max = (_options.TestVsi) ? 1 : (int)(Environment.ProcessorCount * 1.5);
            var cacheCount = 0;
            var waiting = new Stack<AssemblyInfo>(assemblyInfoList);
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
                            }

                            if (testResult.IsFromCache)
                            {
                                cacheCount++;
                            }

                            completed.Add(testResult);
                        }
                        catch (Exception ex)
                        {
                            ConsoleUtil.WriteLine($"Error: {ex.Message}");
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
                    var task = _testExecutor.RunTestAsync(waiting.Pop(), cancellationToken);
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

            Print(completed);

            var processResults = ImmutableArray.CreateBuilder<ProcessResult>();
            foreach (var c in completed)
            {
                processResults.AddRange(c.ProcessResults);
            }

            return new RunAllResult((failures == 0), cacheCount, completed.ToImmutableArray(), processResults.ToImmutable());
        }

        private void Print(List<TestResult> testResults)
        {
            testResults.Sort((x, y) => x.Elapsed.CompareTo(y.Elapsed));

            foreach (var testResult in testResults.Where(x => !x.Succeeded))
            {
                PrintFailedTestResult(testResult);
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
                line.Append($" {(testResult.IsFromCache ? "*" : "")}");
                line.Append($" {(!string.IsNullOrEmpty(testResult.Diagnostics) ? "?" : "")}");

                var message = line.ToString();
                ConsoleUtil.WriteLine(color, message);
            }
            ConsoleUtil.WriteLine("================");

            // Print diagnostics out last so they are cleanly visible at the end of the test summary
            ConsoleUtil.WriteLine("Extra run diagnostics for logging, did not impact run results");
            foreach (var testResult in testResults.Where(x => !string.IsNullOrEmpty(x.Diagnostics)))
            {
                ConsoleUtil.WriteLine(testResult.Diagnostics);
            }
        }

        private void PrintFailedTestResult(TestResult testResult)
        {
            // Save out the error output for easy artifact inspecting
            var outputLogPath = Path.Combine(_options.LogFilesOutputDirectory, $"xUnitFailure-{testResult.DisplayName}.log");

            ConsoleUtil.WriteLine($"Errors {testResult.AssemblyName}");
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
                ConsoleUtil.WriteLine($"xunit produced no error output but had exit code {testResult.ExitCode}");
            }

            // If the results are html, use Process.Start to open in the browser.
            if (_options.UseHtml && !string.IsNullOrEmpty(testResult.ResultsFilePath))
            {
                Process.Start(testResult.ResultsFilePath);
            }
        }
    }
}
