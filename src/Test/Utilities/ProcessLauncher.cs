﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class ProcessLauncher
    {
        /// <summary>
        /// Launch a process, wait for it to complete, and return output, error, and exit code.
        /// </summary>
        public static ProcessResult Run(
            string fileName,
            string arguments,
            string workingDirectory = null,
            IEnumerable<KeyValuePair<string, string>> additionalEnvironmentVars = null)
        {
            if (fileName == null) throw new ArgumentNullException("fileName");

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

                process.WaitForExit();

                return new ProcessResult(process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
            }
        }

        /// <summary>
        /// Launch a process, and return Process object. The process continues to run asynchrously.
        /// You cannot capture the output.
        /// </summary>
        public static Process StartProcess(string fileName, string arguments, string workingDirectory = null)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
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
    }
}
