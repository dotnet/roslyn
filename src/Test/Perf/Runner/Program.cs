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
using Mono.Options;

namespace Runner
{
    public static class Program
    {
        public static void Main(string[] args)
        {

            bool shouldReportBenchview = false;
            bool shouldUploadTrace = true;
            bool isCiTest = false;
            string traceDestination = @"\\mlangfs1\public\basoundr\PerfTraces";

            var parameterOptions = new OptionSet()
            {
                {"report-benchview", "report the performance retults to benview.", _ => shouldReportBenchview = true},
                {"ci-test", "mention that we are running in the continuous integration lab", _ => isCiTest = true},
                {"no-trace-upload", "disable the uploading of traces", _ => shouldUploadTrace = false},
                {"trace-upload_destination", "set the trace uploading destination", loc => { traceDestination = loc; }}
            };
            parameterOptions.Parse(args);

            AsyncMain(isCiTest).GetAwaiter().GetResult();
            if (isCiTest)
            {
                Log("Running under continuous integration");
            }

            if (shouldReportBenchview)
            {
                Log("Uploading results to benchview");
                UploadBenchviewReport();
            }

            if (shouldUploadTrace)
            {
                Log("Uploading traces");
                UploadTraces(GetCPCDirectoryPath(), traceDestination);
            }
        }

        private static async Task AsyncMain(bool isRunningUnderCI)
        {

            RuntimeSettings.IsRunnerAttached = true;

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
                var tests = RuntimeSettings.ResultTests;
                RuntimeSettings.ResultTests = null;
                foreach (var test in tests)
                {
                    test.SetWorkingDirectory(Path.GetDirectoryName(script));
                }
                testInstances.AddRange(tests);
            }


            foreach (var test in testInstances)
            {
                var traceManager = test.GetTraceManager();
                traceManager.Initialize();
                test.Setup();
                traceManager.Setup();

                int iterations;
                if (isRunningUnderCI)
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
