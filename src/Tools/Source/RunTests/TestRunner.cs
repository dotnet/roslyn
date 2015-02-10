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
            internal readonly TimeSpan TimeSpan;

            internal TestResult(bool succeeded, string assemblyName, TimeSpan timeSpan)
            {
                Succeeded = succeeded;
                AssemblyName = assemblyName;
                TimeSpan = timeSpan;
            }
        }

        private readonly string _xunitConsolePath;

        internal TestRunner(string xunitConsolePath)
        {
            _xunitConsolePath = xunitConsolePath;
        }

        internal async Task<bool> RunAll(IEnumerable<string> assemblyList)
        {
            var max = Environment.ProcessorCount;
            var allPassed = true;
            var waiting = new Stack<string>(assemblyList);
            var running = new List<Task<TestResult>>();
            var completed = new List<TestResult>();

            do
            {
                var i = 0;
                while (i < running.Count)
                {
                    var task = running[i];
                    if (task.IsCompleted)
                    {
                        var testResult = await task.ConfigureAwait(false);
                        if (!testResult.Succeeded)
                        {
                            allPassed = false;
                        }

                        completed.Add(testResult);
                        running.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                while (running.Count < max && waiting.Count > 0)
                {
                    var task = RunTest(waiting.Pop());
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
            testResults.Sort((x, y) => x.TimeSpan.CompareTo(y.TimeSpan));

            Console.WriteLine("================");
            foreach (var testResult in testResults)
            {
                var color = testResult.Succeeded ? Console.ForegroundColor : ConsoleColor.Red;
                ConsoleUtil.WriteLine(color, "{0,-75} {1} {2}", testResult.AssemblyName, testResult.Succeeded ? "PASSED" : "FAILED", testResult.TimeSpan);
            }
            Console.WriteLine("================");
        }

        private async Task<TestResult> RunTest(string assemblyPath)
        {
            var assemblyName = Path.GetFileName(assemblyPath);
            var resultsPath = Path.Combine(Path.GetDirectoryName(assemblyPath), Path.ChangeExtension(assemblyName, ".html"));
            var builder = new StringBuilder();
            builder.AppendFormat(@"""{0}""", assemblyPath);
            builder.AppendFormat(@" -html ""{0}""", resultsPath);
            builder.Append(" -noshadow");

            var start = DateTime.Now;
            var processOutput = await ProcessRunner.RunProcessAsync(_xunitConsolePath, builder.ToString(), lowPriority: false, displayWindow: false, captureOutput: true, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            var span = DateTime.Now - start;

            if (processOutput.ExitCode != 0)
            {
                // On occasion we get a non-0 output but no actual data in the result file.  Switch to output in this 
                // case.
                var all = File.ReadAllText(resultsPath).Trim();
                if (all.Length == 0)
                {
                    var output = processOutput.OutputLines.Concat(processOutput.ErrorLines).Aggregate((x, y) => x + Environment.NewLine + y);
                    File.WriteAllText(resultsPath, output);
                }

                Process.Start(resultsPath);
            }

            return new TestResult(processOutput.ExitCode == 0, assemblyName, span);
        }
    }
}
