// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private readonly ITestExecutor _testExecutor;
        private readonly Options _options;

        internal TestRunner(Options options, ITestExecutor testExecutor)
        {
            _testExecutor = testExecutor;
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
                    var task = _testExecutor.RunTestAsync(waiting.Pop(), cancellationToken);
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
                PrintFailedTestResult(testResult);
            }

            Console.WriteLine("================");
            foreach (var testResult in testResults)
            {
                var color = testResult.Succeeded ? Console.ForegroundColor : ConsoleColor.Red;
                ConsoleUtil.WriteLine(color, "{0,-75} {1} {2}", testResult.AssemblyName, testResult.Succeeded ? "PASSED" : "FAILED", testResult.Elapsed);
            }
            Console.WriteLine("================");
        }

        private void PrintFailedTestResult(TestResult testResult)
        {
            // Save out the error output for easy artifact inspecting
            var resultsDir = Path.GetDirectoryName(testResult.ResultsFilePath);
            var outputLogPath = Path.Combine(resultsDir, $"{testResult.AssemblyName}.out.log");
            File.WriteAllText(outputLogPath, testResult.StandardOutput);

            Console.WriteLine("Errors {0}: ", testResult.AssemblyName);
            Console.WriteLine(testResult.ErrorOutput);

            Console.WriteLine($"Command: {testResult.CommandLine}");
            Console.WriteLine($"xUnit output: {outputLogPath}");

            if (!string.IsNullOrEmpty(testResult.ErrorOutput))
            {
                Console.WriteLine(testResult.ErrorOutput);
            }
            else
            { 
                Console.WriteLine($"xunit produced no error output but had exit code {testResult.ExitCode}");
            }

            // If the results are html, use Process.Start to open in the browser.
            if (_options.UseHtml)
            {
                try
                {
                    Process.Start(testResult.ResultsFilePath);
                }
                catch (Win32Exception e) when ((uint)e.ErrorCode == 0x80004005)
                {
                    // if the file isn't present (e.g., if xunit crashed and the potentially malformed result file was
                    // deleted) then `Process.Start()` will throw:
                    //     System.ComponentModel.Win32Exception (0x80004005): The system cannot find the file specified
                }
            }
        }
    }
}
