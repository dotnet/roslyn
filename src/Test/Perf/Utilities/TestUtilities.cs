// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Test.Performance.Utilities
{
    /// <summary>
    /// Global statics shared between the runner and tests.
    /// </summary>
    public static class RuntimeSettings
    {
        /// <summary>
        /// Used as a pseudo-return value for tests to send test objects 
        /// back to the runner.
        /// </summary>
        public static PerfTest[] ResultTests = null;
        /// <summary>
        /// The logger that is being used by the process.
        /// </summary>
        public static ILogger Logger = new ConsoleAndFileLogger();
        /// <summary>
        /// True if the logger should be verbose.
        /// </summary>
        public static bool IsVerbose = true;
        /// <summary>
        /// True if a runner is orchestrating the test runs.
        /// </summary>
        public static bool IsRunnerAttached = false;
    }

    public static class TestUtilities
    {
        /// <returns>
        /// Returns the path to CPC
        /// </returns>
        public static string GetCPCDirectoryPath()
        {
            return Environment.ExpandEnvironmentVariables(@"%SYSTEMDRIVE%\CPC");
        }

        /// <returns>
        /// The path to the ViBenchToJson executable.
        /// </returns>
        public static string GetViBenchToJsonExeFilePath()
        {
            return Path.Combine(GetCPCDirectoryPath(), "ViBenchToJson.exe");
        }

        //
        // Process spawning and error handling.
        //

        /// <summary>
        /// The result of a ShellOut completing.
        /// </summary>
        public class ProcessResult
        {
            /// <summary>
            /// The path to the executable that was run.
            /// </summary>
            public string ExecutablePath { get; set; }
            /// <summary>
            /// The arguments that were passed to the process.
            /// </summary>
            public string Args { get; set; }
            /// <summary>
            /// The exit code of the process.
            /// </summary>
            public int Code { get; set; }
            /// <summary>
            /// The entire standard-out of the process.
            /// </summary>
            public string StdOut { get; set; }
            /// <summary>
            /// The entire standard-error of the process.
            /// </summary>
            public string StdErr { get; set; }

            /// <summary>
            /// True if the command returned an exit code other
            /// than zero.
            /// </summary>
            public bool Failed => Code != 0;
            /// <summary>
            /// True if the command returned an exit code of 0.
            /// </summary>
            public bool Succeeded => !Failed;
        }

        /// <summary>
        /// Shells out, and if the process fails, log the error and quit the script.
        /// </summary>
        public static void ShellOutVital(
                string file,
                string args,
                string workingDirectory = null,
                CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = ShellOut(file, args, workingDirectory, cancellationToken);
            if (result.Failed)
            {
                LogProcessResult(result);
                throw new System.Exception("ShellOutVital Failed");
            }
        }

        /// <summary>
        /// Shells out, blocks, and returns the ProcessResult.
        /// </summary>
        public static ProcessResult ShellOut(
                string file,
                string args,
                string workingDirectory = null,
                CancellationToken cancellationToken = default(CancellationToken))
        {
            workingDirectory ??= AppDomain.CurrentDomain.BaseDirectory;

            var tcs = new TaskCompletionSource<ProcessResult>();
            var startInfo = new ProcessStartInfo(file, args);
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = workingDirectory;
            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };

            if (cancellationToken != default(CancellationToken))
            {
                cancellationToken.Register(() => process.Kill());
            }

            if (RuntimeSettings.IsVerbose)
            {
                Log($"running \"{file}\" with arguments \"{args}\" from directory {workingDirectory}");
            }

            process.Start();

            var output = new StringWriter();
            var error = new StringWriter();

            process.OutputDataReceived += (s, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    output.WriteLine(e.Data);
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    error.WriteLine(e.Data);
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            return new ProcessResult
            {
                ExecutablePath = file,
                Args = args,
                Code = process.ExitCode,
                StdOut = output.ToString(),
                StdErr = error.ToString(),
            };
        }

        /// <summary>
        /// Shells out and returns the string gathered from the stdout of the 
        /// executing process.
        /// 
        /// Throws an exception if the process fails.
        /// </summary>
        public static string StdoutFrom(string program, string args = "", string workingDirectory = null)
        {
            var result = ShellOut(program, args, workingDirectory);
            if (result.Failed)
            {
                LogProcessResult(result);
                throw new Exception("Shelling out failed");
            }
            return result.StdOut.Trim();
        }

        /// <summary>
        /// Logs a message.
        ///
        /// The actual implementation of this method may change depending on
        /// if the script is being run standalone or through the test runner.
        /// </summary>
        public static void Log(string info)
        {
            RuntimeSettings.Logger.Log(info);
            RuntimeSettings.Logger.Flush();
        }

        /// <summary>
        /// Logs the result of a finished process
        /// </summary>
        public static void LogProcessResult(ProcessResult result)
        {
            RuntimeSettings.Logger.Log(String.Format("The process \"{0}\" {1} with code {2}",
                $"{result.ExecutablePath} {result.Args}",
                result.Failed ? "failed" : "succeeded",
                result.Code));
            RuntimeSettings.Logger.Log($"Standard Out:\n{result.StdOut}");
            RuntimeSettings.Logger.Log($"\nStandard Error:\n{result.StdErr}");
        }

        /// <summary>
        /// Either runs the provided tests, or schedules them to be run by the 
        /// runner. 
        /// </summary>
        public static void TestThisPlease(params PerfTest[] tests)
        {
            if (RuntimeSettings.IsRunnerAttached)
            {
                RuntimeSettings.ResultTests = tests;
            }
            else
            {
                foreach (var test in tests)
                {
                    test.Setup();
                    for (int i = 0; i < test.Iterations; i++)
                    {
                        test.Test();
                    }
                }
            }
        }
    }
}
