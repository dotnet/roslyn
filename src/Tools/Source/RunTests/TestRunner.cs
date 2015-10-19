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

        private readonly string _xunitConsolePath;
        private readonly bool _useHtml;

        internal TestRunner(string xunitConsolePath, bool useHtml)
        {
            _xunitConsolePath = xunitConsolePath;
            _useHtml = useHtml;
        }

        internal async Task<bool> RunAllAsync(IEnumerable<string> assemblyList, CancellationToken cancellationToken)
        {
            var max = Environment.ProcessorCount;
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

        private static readonly string[] UpgradedTests = new string[] {
            "Microsoft.DiaSymReader.PortablePdb.UnitTests.dll",
            "Microsoft.CodeAnalysis.Scripting.VisualBasic.UnitTests.dll",
            "Microsoft.CodeAnalysis.Scripting.CSharp.UnitTests.dll"
        };

        private async Task<TestResult> RunTest(string assemblyPath, CancellationToken cancellationToken)
        {
            var assemblyName = Path.GetFileName(assemblyPath);
            var extension = _useHtml ? ".TestResults.html" : ".TestResults.xml";
            var resultsPath = Path.Combine(Path.GetDirectoryName(assemblyPath), Path.ChangeExtension(assemblyName, extension));
            DeleteFile(resultsPath);

            var builder = new StringBuilder();
            builder.AppendFormat(@"""{0}""", assemblyPath);
            builder.AppendFormat(@" -{0} ""{1}""", _useHtml ? "html" : "xml", resultsPath);
            builder.Append(" -noshadow");

            var errorOutput = new StringBuilder();
            var start = DateTime.UtcNow;

            var xunitPath = UpgradedTests.Contains(assemblyName) ? Path.Combine($"{Path.GetDirectoryName(_xunitConsolePath)}", @"..\..\..\xunit.runner.console\2.1.0-beta4-build3109\tools", $"{Path.GetFileName(_xunitConsolePath)}") : _xunitConsolePath;
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
                var all = string.Empty;
                try
                {
                    all = File.ReadAllText(resultsPath).Trim();
                }
                catch
                {
                    // Happens if xunit didn't produce a log file
                }

                bool noResultsData = (all.Length == 0);
                if (noResultsData)
                {
                    var output = processOutput.OutputLines.Concat(processOutput.ErrorLines);
                    Console.Write(string.Join(Environment.NewLine, output));

                    // Delete the output file.
                    File.Delete(resultsPath);
                }

                errorOutput.AppendLine($"Command: {_xunitConsolePath} {builder}");

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

                if (_useHtml && !noResultsData)
                {
                    Process.Start(resultsPath);
                }
            }

            return new TestResult(processOutput.ExitCode == 0, assemblyName, span, errorOutput.ToString());
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
