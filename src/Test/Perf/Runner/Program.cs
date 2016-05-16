using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Roslyn.Test.Performance.Utilities;
using static Roslyn.Test.Performance.Utilities.TestUtilities;
using static Roslyn.Test.Performance.Runner.Tools;
using Roslyn.Test.Performance.Runner;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncMain(args).GetAwaiter().GetResult();
        }
        static async Task AsyncMain(string[] args)
        {

            RuntimeSettings.isRunnerAttached = true;

            var testDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Perf", "tests");

            // Print message at startup
            Log("Starting Performance Test Run");
            Log("hash: " + StdoutFrom("git", "show --format=\"%h\" HEAD --").Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)[0]);
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

                var iterations = traceManager.HasWarmUpIteration ?
                                 test.Iterations + 1 :
                                 test.Iterations;

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
