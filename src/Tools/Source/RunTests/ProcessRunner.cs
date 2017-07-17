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
        public int ExitCode { get; }
        public IList<string> OutputLines { get; }
        public IList<string> ErrorLines { get; }

        public ProcessOutput(int exitCode, IList<string> outputLines, IList<string> errorLines)
        {
            ExitCode = exitCode;
            OutputLines = outputLines;
            ErrorLines = errorLines;
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
            CancellationToken cancellationToken,
            bool lowPriority = false,
            string workingDirectory = null,
            bool captureOutput = false,
            bool displayWindow = true,
            Dictionary<string, string> environmentVariables = null,
            Action<Process> onProcessStartHandler = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var taskCompletionSource = new TaskCompletionSource<ProcessOutput>();
            var processStartInfo = CreateProcessStartInfo(executable, arguments, workingDirectory, captureOutput, displayWindow, environmentVariables);
            return CreateProcessTask(processStartInfo, lowPriority, taskCompletionSource, onProcessStartHandler, cancellationToken);
        }

        private static Task<ProcessOutput> CreateProcessTask(
            ProcessStartInfo processStartInfo,
            bool lowPriority,
            TaskCompletionSource<ProcessOutput> taskCompletionSource,
            Action<Process> onProcessStartHandler,
            CancellationToken cancellationToken)
        {
            var errorLines = new List<string>();
            var outputLines = new List<string>();
            var process = new Process();

            process.EnableRaisingEvents = true;
            process.StartInfo = processStartInfo;

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
                    // We must call WaitForExit to make sure we've received all OutputDataReceived/ErrorDataReceived calls
                    // or else we'll be returning a list we're still modifying. For paranoia, we'll start a task here rather
                    // than enter right back into the Process type and start a wait which isn't guaranteed to be safe.
                    Task.Run(() =>
                    {
                        process.WaitForExit();
                        var processOutput = new ProcessOutput(process.ExitCode, outputLines, errorLines);
                        taskCompletionSource.TrySetResult(processOutput);
                    });
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

            process.Start();
            onProcessStartHandler?.Invoke(process);

            if (lowPriority)
            {
                process.PriorityClass = ProcessPriorityClass.BelowNormal;
            }

            if (processStartInfo.RedirectStandardOutput)
            {
                process.BeginOutputReadLine();
            }

            if (processStartInfo.RedirectStandardError)
            {
                process.BeginErrorReadLine();
            }

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
