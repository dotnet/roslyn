// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TestExecutor;
public class TestPlatformWrapper
{
    public static TestResultInfo RunTests(IEnumerable<string> assemblies, string filterString, string runsettingsContents, string dotnetPath)
    {
        var vsTestConsole = Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(dotnetPath)!, "sdk"), "vstest.console.dll", SearchOption.AllDirectories).Last();
        var vstestConsoleWrapper = new VsTestConsoleWrapper(vsTestConsole, new ConsoleParameters
        {
            LogFilePath = Path.Combine(AppContext.BaseDirectory, "logs", "test_discovery_logs.txt"),
            TraceLevel = TraceLevel.Error,
        });

        var exitResult = Program.ExitFailure;
        TestRunCompleteEventArgs? testRunComplete = null;
        var outputLines = new List<string>();
        var errorLines = new List<string>();

        var runHandler = new RunTestsHandler
        {
            HandleCompletion = (testRunCompleteArgs, testRunChanged, _, _2) =>
            {
                WriteSummary(testRunCompleteArgs);

                testRunComplete = testRunCompleteArgs;
                if (DidPass(testRunCompleteArgs))
                {
                    exitResult = Program.ExitSuccess;
                }
            },
            HandleLog = (severity, logMessage) =>
            {
                if (severity == TestMessageLevel.Error)
                {
                    errorLines.Add(logMessage);
                }

                outputLines.Add(logMessage);
            }
        };

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        vstestConsoleWrapper.RunTests(assemblies, runsettingsContents, new TestPlatformOptions
        {
            TestCaseFilter = filterString,
        }, runHandler);
        stopwatch.Stop();

        var ranToCompletion = testRunComplete != null ? !testRunComplete.IsCanceled && !testRunComplete.IsAborted : false;
        return new TestResultInfo(exitResult, stopwatch.Elapsed, string.Join(Environment.NewLine, outputLines), string.Join(Environment.NewLine, errorLines), ranToCompletion);
    }

    public record struct TestExecutionInfo(List<string> Assemblies, List<string> Tests);

    private static void WriteSummary(TestRunCompleteEventArgs testRunComplete)
    {
        if (testRunComplete.TestRunStatistics != null)
        {
            testRunComplete.TestRunStatistics.Stats.TryGetValue(TestOutcome.Passed, out var passed);
            testRunComplete.TestRunStatistics.Stats.TryGetValue(TestOutcome.Failed, out var failed);
            testRunComplete.TestRunStatistics.Stats.TryGetValue(TestOutcome.Skipped, out var skipped);
            Console.WriteLine($"Summary: Passed: {passed}, Failed: {failed}, Skipped {skipped}");
        }

        if (testRunComplete.Error != null)
        {
            Console.WriteLine($"ERROR: {testRunComplete.Error}");
        }
    }

    private static bool DidPass(TestRunCompleteEventArgs testRunComplete)
    {
        if (testRunComplete.Error != null || testRunComplete.IsAborted || testRunComplete.IsCanceled || testRunComplete.TestRunStatistics == null)
        {
            return false;
        }

        testRunComplete.TestRunStatistics.Stats.TryGetValue(TestOutcome.Failed, out var failed);
        return failed == 0;
    }

}
