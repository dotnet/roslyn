// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Roslyn.Test.Performance.Utilities;
using static Roslyn.Test.Performance.Utilities.TestUtilities;
using static Roslyn.Test.Performance.Runner.Tools;
using static Roslyn.Test.Performance.Runner.TraceBackup;
using Mono.Options;

namespace Runner
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("args: \n" + String.Join("\n  ", args));

            bool shouldReportBenchview = false;
            bool shouldUploadTrace = true;
            bool isCiTest = false;
            string submissionName = null;
            string submissionType = null;
            string traceDestination = @"\\mlangfs1\public\basoundr\PerfTraces";
            string branch = null;
            string searchDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var parameterOptions = new OptionSet()
            {
                {"report-benchview", "report the performance results to benchview.", _ => shouldReportBenchview = true},
                {"benchview-submission-type=", $"submission type to use when uploading to benchview ({String.Join(",", Benchview.ValidSubmissionTypes)})", type => { submissionType = type; } },
                {"benchview-submission-name=", "submission name to use when uploading to benchview (required for private and local submissions)", name => { submissionName = name; } },
                {"branch=", "name of the branch you are measuring on", name => { branch = name; } },
                {"ci-test", "mention that we are running in the continuous integration lab", _ => isCiTest = true},
                {"no-trace-upload", "disable the uploading of traces", _ => shouldUploadTrace = false},
                {"trace-upload_destination=", "set the trace uploading destination", loc => { traceDestination = loc; } },
                {"search-directory=", "the directory to recursively search for tests", dir => { searchDirectory = dir; } }
            };


            parameterOptions.Parse(args);

            Log($"shouldReportBenchview: {shouldReportBenchview}");
            Log($"submissionType: {submissionType}");

            if (shouldReportBenchview)
            {
                if (!CheckBenchViewOptions(submissionType, submissionName) ||
                    !Benchview.CheckEnvironment() ||
                    !DetermineBranch(ref branch))
                {
                    return -1;
                }

                Benchview.SetConfiguration(submissionType, branch);
            }

            Cleanup();
            AsyncMain(isCiTest, searchDirectory).GetAwaiter().GetResult();

            if (isCiTest)
            {
                Log("Running under continuous integration");
            }

            if (shouldReportBenchview)
            {
                Log("Uploading results to benchview");
                Benchview.UploadBenchviewReport(submissionName);
            }

            if (shouldUploadTrace)
            {
                Log("Uploading traces");
                UploadTraces(GetCPCDirectoryPath(), traceDestination);
            }

            return 0;
        }

        private static bool CheckBenchViewOptions(string submissionType, string submissionName)
        {
            if (String.IsNullOrWhiteSpace(submissionType))
            {
                Log("Parameter --benchview-submission-type is required when --report-benchview is specified");
                return false;
            }

            if (!Benchview.IsValidSubmissionType(submissionType))
            {
                Log($"Parameter --benchview-submission-type must be one of ({String.Join(",", Benchview.ValidSubmissionTypes)})");
                return false;
            }

            if (String.IsNullOrWhiteSpace(submissionName) && submissionType != "rolling")
            {
                Log("Parameter --benchview-submission-name is required for \"private\" and \"local\" submissions");
                return false;
            }

            return true;
        }

        private static bool DetermineBranch(ref string branch)
        {
            if (branch != null)
            {
                // Workaround for Jenkins. GIT_BRANCH env var prefixes branch name with origin/
                string prefix = "origin/";
                if (branch.StartsWith(prefix))
                {
                    branch = branch.Substring(prefix.Length);
                }
            }

            // If user did not specify branch, determine if we can automatically determine branch name
            if (String.IsNullOrWhiteSpace(branch))
            {
                var result = ShellOut("git", "symbolic-ref --short HEAD");
                if (result.Failed)
                {
                    Log("Parameter --branch is required because we were unable to automatically determine the branch name. You may be in a detached head state");
                    return false;
                }

                branch = result.StdOut;
            }

            return true;
        }

        private static void Cleanup()
        {
            var consumptionTempResultsPath = Path.Combine(GetCPCDirectoryPath(), "ConsumptionTempResults.xml");
            if (File.Exists(consumptionTempResultsPath))
            {
                File.Delete(consumptionTempResultsPath);
            }

            if (Directory.Exists(GetCPCDirectoryPath()))
            {
                var databackDirectories = Directory.GetDirectories(GetCPCDirectoryPath(), "DataBackup*", SearchOption.AllDirectories);
                foreach (var databackDirectory in databackDirectories)
                {
                    Directory.Delete(databackDirectory, true);
                }
            }
        }

        private static async Task AsyncMain(bool isRunningUnderCI, string searchDirectory)
        {
            RuntimeSettings.IsRunnerAttached = true;

            // Print message at startup
            Log("Starting Performance Test Run");
            Log("hash: " + FirstLine(StdoutFromOrDefault("git.exe", args: "show --format=\"%h\" HEAD --", workingDirectory: Environment.CurrentDirectory, defaultText: "git missing")));
            Log("time: " + DateTime.Now.ToString());

            var testInstances = new List<PerfTest>();

            // Find all the tests from inside of the csx files.
            foreach (var script in GetAllCsxRecursive(searchDirectory))
            {
                var scriptName = Path.GetFileNameWithoutExtension(script);
                Log($"Collecting tests from {scriptName}");
                var state = await RunFileInItsDirectory(script).ConfigureAwait(false);
                var tests = RuntimeSettings.ResultTests;
                RuntimeSettings.ResultTests = null;
                if (tests != null)
                {
                    foreach (var test in tests)
                    {
                        test.SetWorkingDirectory(Path.GetDirectoryName(script));
                    }
                    testInstances.AddRange(tests);
                }
            }


            foreach (var test in testInstances)
            {
                var traceManager = test.GetTraceManager();
                test.Setup();
                traceManager.Setup();

                int iterations;
                if (isRunningUnderCI)
                {
                    if (RuntimeSettings.IsVerbose)
                    {
                        Log("Running one iteration per test because we are under CI");
                    }
                    iterations = 1;
                }
                else if (traceManager.HasWarmUpIteration)
                {
                    iterations = test.Iterations + 1;
                    if (RuntimeSettings.IsVerbose)
                    {
                        Log("With warmup iteration");
                    }
                }
                else
                {
                    if (RuntimeSettings.IsVerbose)
                    {
                        Log("No warmup iteration");
                    }
                    iterations = test.Iterations;
                }

                Log($"Number of iterations: {iterations}");

                try
                {
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
                }
                catch (Exception)
                {
                    traceManager.Stop();
                }
                finally
                {
                    traceManager.Cleanup();
                }
            }
        }

        private static string StdoutFromOrDefault(string program, string args = "", string workingDirectory = null, string defaultText = "")
        {
            try
            {

                return StdoutFrom(program, args, workingDirectory);
            }
            catch
            {
                return defaultText;
            }
        }
    }
}
