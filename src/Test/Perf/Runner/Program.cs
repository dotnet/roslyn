// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Roslyn.Test.Performance.Utilities;
using static Roslyn.Test.Performance.Utilities.TestUtilities;
using static Roslyn.Test.Performance.Runner.Tools;
using static Roslyn.Test.Performance.Runner.Benchview;
using static Roslyn.Test.Performance.Runner.TraceBackup;
using Roslyn.Test.Performance.Runner;

namespace Runner
{
    public static class Program
    {
        private static bool LaunchedWithArgument(string arg)
        {
            return Environment.GetCommandLineArgs().Contains($"/{arg}") ||
                   Environment.GetCommandLineArgs().Contains($"--{arg}");
        }
        private static bool ShouldReportBenchview => LaunchedWithArgument("report-benchview");
        private static bool ShouldUploadTrace => !LaunchedWithArgument("no-trace-upload");
        private static bool IsRunningUnderCI => LaunchedWithArgument("ci-test");
        public static void Main(string[] args)
        {
            AsyncMain(args).GetAwaiter().GetResult();
            if (IsRunningUnderCI)
            {
                Log("Running under continuous integration");
            }

            if (ShouldReportBenchview)
            {
                Log("Uploading results to benchview");
                UploadBenchviewReport();
            }

            if (ShouldUploadTrace)
            {
                Log("Uploading traces");
                UploadTraces();
            }
        }

        private static async Task AsyncMain(string[] args)
        {

            RuntimeSettings.isRunnerAttached = true;

            var testDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Perf", "tests");

            // Print message at startup
            Log("Starting Performance Test Run");
            Log("hash: " + FirstLine(StdoutFrom("git", "show --format=\"%h\" HEAD --")));
            Log("time: " + DateTime.Now.ToString());

            var testInstances = new List<PerfTest>();

            // Find all the tests from inside of the csx files.
            foreach (var script in GetAllCsxRecursive(testDirectory))
            {
                var scriptName = Path.GetFileNameWithoutExtension(script);
                Log($"Collecting tests from {scriptName}");
                var state = await RunFile(script).ConfigureAwait(false);
                var tests = RuntimeSettings.resultTests;
                RuntimeSettings.resultTests = null;
                testInstances.AddRange(tests);
            }

            var traceManager = TraceManagerFactory.GetTraceManager();

            traceManager.Initialize();
            foreach (var test in testInstances)
            {
                test.Setup();
                traceManager.Setup();

                int iterations;
                if (IsRunningUnderCI)
                {
                    Log("Running one iteration per test");
                    iterations = 1;
                }
                else if (traceManager.HasWarmUpIteration)
                {
                    iterations = test.Iterations + 1;
                }
                else
                {
                    iterations = test.Iterations;
                }

                for (int i = 0; i < iterations; i++)
                {
                    traceManager.Start();
                    traceManager.StartScenarios();

                    if (test.ProvidesScenarios)
                    {
                        traceManager.WriteScenarios(test.GetScenarios());
                        test.Test();
                    }
                    else
                    {
                        traceManager.StartScenario(test.Name, test.MeasuredProc);
                        traceManager.StartEvent();
                        test.Test();
                        traceManager.EndEvent();
                        traceManager.EndScenario();
                    }

                    traceManager.EndScenarios();
                    traceManager.WriteScenariosFileToDisk();
                    traceManager.Stop();
                    traceManager.ResetScenarioGenerator();
                }

                traceManager.Cleanup();
            }
        }
    }
}
