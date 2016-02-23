// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Xunit;

namespace Roslyn.Test.Utilities
{
    public static class ProcessUtilities
    {
        /// <summary>
        /// Launch a process, wait for it to complete, and return output, error, and exit code.
        /// </summary>
        public static ProcessResult Run(
            string fileName,
            string arguments,
            string workingDirectory = null,
            IEnumerable<KeyValuePair<string, string>> additionalEnvironmentVars = null,
            string stdInput = null)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = stdInput != null,
                WorkingDirectory = workingDirectory
            };

            if (additionalEnvironmentVars != null)
            {
                foreach (var entry in additionalEnvironmentVars)
                {
                    startInfo.EnvironmentVariables[entry.Key] = entry.Value;
                }
            }

            using (var process = new Process { StartInfo = startInfo })
            {
                StringBuilder outputBuilder = new StringBuilder();
                StringBuilder errorBuilder = new StringBuilder();
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                        outputBuilder.AppendLine(args.Data);
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                        errorBuilder.AppendLine(args.Data);
                };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (stdInput != null)
                {
                    process.StandardInput.Write(stdInput);
                }

                process.WaitForExit();

                return new ProcessResult(process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
            }
        }

        /// <summary>
        /// Launch a process, and return Process object. The process continues to run asynchronously.
        /// You cannot capture the output.
        /// </summary>
        public static Process StartProcess(string fileName, string arguments, string workingDirectory = null)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            };

            Process p = new Process { StartInfo = startInfo };
            p.Start();
            return p;
        }

        public static string RunAndGetOutput(string exeFileName, string arguments = null, int expectedRetCode = 0, string startFolder = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(exeFileName);
            if (arguments != null)
            {
                startInfo.Arguments = arguments;
            }
            string result = null;

            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;

            if (startFolder != null)
            {
                startInfo.WorkingDirectory = startFolder;
            }

            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
                // Do not wait for the child process to exit before reading to the end of its
                // redirected stream. Read the output stream first and then wait. Doing otherwise
                // might cause a deadlock.
                result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                Assert.True(expectedRetCode == process.ExitCode, $"Unexpected exit code: {process.ExitCode} (expecting {expectedRetCode}). Process output: {result}");
            }

            return result;
        }
    }
}
