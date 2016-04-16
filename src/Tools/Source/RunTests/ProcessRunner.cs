// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests
{
    public sealed class ProcessOutput
    {
        private readonly int _exitCode;
        private readonly IList<string> _outputLines;
        private readonly IList<string> _errorLines;

        public int ExitCode
        {
            get { return _exitCode; }
        }

        public IList<string> OutputLines
        {
            get { return _outputLines; }
        }

        public IList<string> ErrorLines
        {
            get { return _errorLines; }
        }

        public ProcessOutput(int exitCode, IList<string> outputLines, IList<string> errorLines)
        {
            _exitCode = exitCode;
            _outputLines = outputLines;
            _errorLines = errorLines;
        }
    }

    public static class ProcessRunner
    {
        public static void OpenFile(string file)
        {
            if (File.Exists(file))
            {
                Process.Start(file);
            }
        }

        public static Task<ProcessOutput> RunProcessAsync(
            string executable,
            string arguments,
            bool lowPriority,
            CancellationToken cancellationToken,
            string workingDirectory = null,
            bool captureOutput = false,
            bool displayWindow = true,
            Dictionary<string, string> environmentVariables = null,
            Action<Process> processMonitor = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var taskCompletionSource = new TaskCompletionSource<ProcessOutput>();

            var process = new Process();
            process.EnableRaisingEvents = true;
            process.StartInfo = CreateProcessStartInfo(executable, arguments, workingDirectory, captureOutput, displayWindow);

            var task = CreateTask(process, taskCompletionSource, cancellationToken);

            process.Start();

            processMonitor?.Invoke(process);

            if (lowPriority)
            {
                process.PriorityClass = ProcessPriorityClass.BelowNormal;
            }

            if (process.StartInfo.RedirectStandardOutput)
            {
                process.BeginOutputReadLine();
            }

            if (process.StartInfo.RedirectStandardError)
            {
                process.BeginErrorReadLine();
            }

            return task;
        }

        private static Task<ProcessOutput> CreateTask(
            Process process,
            TaskCompletionSource<ProcessOutput> taskCompletionSource,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (taskCompletionSource == null)
            {
                throw new ArgumentException("taskCompletionSource");
            }

            if (process == null)
            {
                return taskCompletionSource.Task;
            }

            var errorLines = new List<string>();
            var outputLines = new List<string>();

            process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        outputLines.Add(e.Data);
                    }
                };

            process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        errorLines.Add(e.Data);
                    }
                };

            process.Exited += (s, e) =>
                {
                    var processOutput = new ProcessOutput(process.ExitCode, outputLines, errorLines);
                    taskCompletionSource.TrySetResult(processOutput);
                };

            var registration = cancellationToken.Register(() =>
                {
                    if (taskCompletionSource.TrySetCanceled())
                    {
                        // If the underlying process is still running, we should kill it
                        if (!process.HasExited)
                        {
                            try
                            {
                                process.Kill();
                            }
                            catch (InvalidOperationException)
                            {
                                // Ignore, since the process is already dead
                            }
                        }
                    }
                });

            return taskCompletionSource.Task;
        }

        private static ProcessStartInfo CreateProcessStartInfo(
            string executable,
            string arguments,
            string workingDirectory,
            bool captureOutput,
            bool displayWindow,
            Dictionary<string, string> environmentVariables = null)
        {
            var processStartInfo = new ProcessStartInfo(executable, arguments);

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                processStartInfo.WorkingDirectory = workingDirectory;
            }

            if (environmentVariables != null)
            {
                foreach (var pair in environmentVariables)
                {
                    processStartInfo.EnvironmentVariables[pair.Key] = pair.Value;
                }
            }

            if (captureOutput)
            {
                processStartInfo.UseShellExecute = false;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardError = true;
            }
            else
            {
                processStartInfo.CreateNoWindow = !displayWindow;
                processStartInfo.UseShellExecute = displayWindow;
            }

            return processStartInfo;
        }
    }
}
