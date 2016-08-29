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

        internal RunAllResult(bool succeeded, int cacheCount, ImmutableArray<TestResult> testResults)
        {
            Succeeded = succeeded;
            CacheCount = cacheCount;
            TestResults = testResults;
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
            var max = (int)(Environment.ProcessorCount * 1.5);
            var allPassed = true;
            var cacheCount = 0;
            var waiting = new Stack<AssemblyInfo>(assemblyInfoList);
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

                            if (testResult.IsResultFromCache)
                            {
                                cacheCount++;
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
                    var task = _testExecutor.RunTestAsync(waiting.Pop(), cancellationToken);
                    running.Add(task);
                }

                Console.WriteLine($"  { running.Count} running, { waiting.Count} queued, { completed.Count} completed");
                Task.WaitAny(running.ToArray());
            } while (running.Count > 0);

            Print(completed);

            return new RunAllResult(allPassed, cacheCount, completed.ToImmutableArray());
        }

        private void Print(List<TestResult> testResults)
        {
            testResults.Sort((x, y) => x.Elapsed.CompareTo(y.Elapsed));

            foreach (var testResult in testResults.Where(x => !x.Succeeded))
            {
                PrintFailedTestResult(testResult);
            }

            Console.WriteLine("================");
            foreach (var testResult in testResults)
            {
                var color = testResult.Succeeded ? Console.ForegroundColor : ConsoleColor.Red;
                var message = $"{testResult.DisplayName,-75} {(testResult.Succeeded ? "PASSED" : "FAILED")} {testResult.Elapsed}{(testResult.IsResultFromCache ? "*" : "")}";
                ConsoleUtil.WriteLine(color, message);
                Logger.Log(message);
            }
            Console.WriteLine("================");
        }

        private void PrintFailedTestResult(TestResult testResult)
        {
            // Save out the error output for easy artifact inspecting
            var resultsDir = testResult.ResultDir;
            var outputLogPath = Path.Combine(resultsDir, $"{testResult.DisplayName}.out.log");
            File.WriteAllText(outputLogPath, testResult.StandardOutput);

            Console.WriteLine("Errors {0}: ", testResult.AssemblyName);
            Console.WriteLine(testResult.ErrorOutput);

            // TODO: Put this in the log and take it off the console output to keep it simple? 
            Console.WriteLine($"Command: {testResult.CommandLine}");
            Console.WriteLine($"xUnit output log: {outputLogPath}");

            if (!string.IsNullOrEmpty(testResult.ErrorOutput))
            {
                Console.WriteLine(testResult.ErrorOutput);
            }
            else
            {
                Console.WriteLine($"xunit produced no error output but had exit code {testResult.ExitCode}");
            }

            // If the results are html, use Process.Start to open in the browser.
            if (_options.UseHtml && !string.IsNullOrEmpty(testResult.ResultsFilePath))
            {
                Process.Start(testResult.ResultsFilePath);
            }
        }
    }
}
