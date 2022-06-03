// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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
            string stdInput = null,
            bool redirectStandardInput = false)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = stdInput != null || redirectStandardInput,
                WorkingDirectory = workingDirectory
            };

            // In case the process is a console application that expects standard input
            // do not set CreateNoWindow to true to ensure that the input encoding
            // of both the test and the process fileName is equal.
            if (stdInput == null)
            {
                startInfo.CreateNoWindow = true;
            }

            if (additionalEnvironmentVars != null)
            {
                foreach (var entry in additionalEnvironmentVars)
                {
                    startInfo.Environment[entry.Key] = entry.Value;
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
                    process.StandardInput.Dispose();
                }

                process.WaitForExit();

                // Double check the process has actually exited
                Debug.Assert(process.HasExited);

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
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
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
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                Assert.True(expectedRetCode == process.ExitCode, $"Unexpected exit code: {process.ExitCode} (expecting {expectedRetCode}). Process output: {result}. Process error: {error}");
            }

            return result;
        }
    }
}
