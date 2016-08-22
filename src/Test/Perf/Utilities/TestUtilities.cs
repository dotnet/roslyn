// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Test.Performance.Utilities
{
    public static class RuntimeSettings
    {
        public static PerfTest[] resultTests = null;
        public static ILogger logger = new ConsoleAndFileLogger();
        public static bool isVerbose = true;
        public static bool isRunnerAttached = false;
    }

    public static class TestUtilities
    {
        public static bool IsRunFromRunner()
        {
            return RuntimeSettings.isRunnerAttached;
        }

        public static bool IsVerbose()
        {
            return RuntimeSettings.isVerbose;
        }

        public static ILogger Logger()
        {
            return RuntimeSettings.logger;
        }

        public static string GetCPCDirectoryPath()
        {
            return Environment.ExpandEnvironmentVariables(@"%SYSTEMDRIVE%\CPC");
        }

        public static string GetViBenchToJsonExeFilePath()
        {
            return Path.Combine(GetCPCDirectoryPath(), "ViBenchToJson.exe");
        }

        //
        // Process spawning and error handling.
        //

        public class ProcessResult
        {
            public string ExecutablePath { get; set; }
            public string Args { get; set; }
            public int Code { get; set; }
            public string StdOut { get; set; }
            public string StdErr { get; set; }

            public bool Failed => Code != 0;
            public bool Succeeded => !Failed;
        }

        // Shells out, and if the process fails, log the error
        /// and quit the script.
        public static void ShellOutVital(
                string file,
                string args,
                string workingDirectory,
                CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = ShellOut(file, args, workingDirectory, cancellationToken);
            if (result.Failed)
            {
                LogProcessResult(result);
                throw new System.Exception("ShellOutVital Failed");
            }
        }

        public static ProcessResult ShellOut(
                string file,
                string args,
                string workingDirectory,
                CancellationToken cancellationToken = default(CancellationToken))
        {
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

            if (RuntimeSettings.isVerbose)
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

        public static string StdoutFrom(string program, string args = "", string workingDirectory = null)
        {
            if (workingDirectory == null)
            {
                workingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            var result = ShellOut(program, args, workingDirectory);
            if (result.Failed)
            {
                LogProcessResult(result);
                throw new Exception("Shelling out failed");
            }
            return result.StdOut.Trim();
        }

        // Logs a message.
        //
        // The actual implementation of this method may change depending on
        // if the script is being run standalone or through the test runner.
        public static void Log(string info)
        {
            RuntimeSettings.logger.Log(info);
            RuntimeSettings.logger.Flush();
        }

        // Logs the result of a finished process
        public static void LogProcessResult(ProcessResult result)
        {
            RuntimeSettings.logger.Log(String.Format("The process \"{0}\" {1} with code {2}",
                $"{result.ExecutablePath} {result.Args}",
                result.Failed ? "failed" : "succeeded",
                result.Code));
            RuntimeSettings.logger.Log($"Standard Out:\n{result.StdOut}");
            RuntimeSettings.logger.Log($"\nStandard Error:\n{result.StdErr}");
        }

        public static void TestThisPlease(params PerfTest[] tests)
        {
            if (IsRunFromRunner())
            {
                RuntimeSettings.resultTests = tests;
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
