// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests
{
    public readonly struct ProcessResult
    {
        public Process Process { get; }
        public int ExitCode { get; }
        public ReadOnlyCollection<string> OutputLines { get; }
        public ReadOnlyCollection<string> ErrorLines { get; }

        public ProcessResult(Process process, int exitCode, ReadOnlyCollection<string> outputLines, ReadOnlyCollection<string> errorLines)
        {
            Process = process;
            ExitCode = exitCode;
            OutputLines = outputLines;
            ErrorLines = errorLines;
        }
    }

    public readonly struct ProcessInfo
    {
        public Process Process { get; }
        public ProcessStartInfo StartInfo { get; }
        public Task<ProcessResult> Result { get; }
        private readonly CancellationTokenSource _outputCancellation;

        public int Id => Process.Id;

        public ProcessInfo(Process process, ProcessStartInfo startInfo, Task<ProcessResult> result, CancellationTokenSource outputCancellation)
        {
            Process = process;
            StartInfo = startInfo;
            Result = result;
            _outputCancellation = outputCancellation;
        }

        internal void StopReadingOutput()
        {
            try
            {
                _outputCancellation.Cancel();
            }
            catch (ObjectDisposedException) when (Result.IsCompleted)
            {
            }
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

        public static ProcessInfo CreateProcess(
            string executable,
            string arguments,
            bool lowPriority = false,
            string? workingDirectory = null,
            bool captureOutput = false,
            bool displayWindow = true,
            Dictionary<string, string>? environmentVariables = null,
            Action<Process>? onProcessStartHandler = null,
            Action<string>? onOutputDataReceived = null,
            Action<string>? onErrorDataReceived = null)
            => CreateProcess(
                CreateProcessStartInfo(executable, arguments, workingDirectory, captureOutput, displayWindow, environmentVariables),
                lowPriority: lowPriority,
                onProcessStartHandler: onProcessStartHandler,
                onOutputDataReceived: onOutputDataReceived,
                onErrorDataReceived: onErrorDataReceived);

        public static ProcessInfo CreateProcess(
            ProcessStartInfo processStartInfo,
            bool lowPriority = false,
            Action<Process>? onProcessStartHandler = null,
            Action<string>? onOutputDataReceived = null,
            Action<string>? onErrorDataReceived = null)
        {
            var errorLines = new List<string>();
            var outputLines = new List<string>();
            var process = new Process();
            process.StartInfo = processStartInfo;

            process.Start();
            // Cache identity before a short-lived launcher can exit.
            _ = process.StartTime;
            onProcessStartHandler?.Invoke(process);

            if (lowPriority)
            {
                process.PriorityClass = ProcessPriorityClass.BelowNormal;
            }

            var outputCancellation = new CancellationTokenSource();
            var outputTask = processStartInfo.RedirectStandardOutput
                ? ReadAsync(process.StandardOutput, outputLines, onOutputDataReceived)
                : Task.CompletedTask;
            var errorTask = processStartInfo.RedirectStandardError
                ? ReadAsync(process.StandardError, errorLines, onErrorDataReceived)
                : Task.CompletedTask;

            return new ProcessInfo(process, processStartInfo, CompleteAsync(), outputCancellation);

            async Task<ProcessResult> CompleteAsync()
            {
                try
                {
                    // Do not use BeginOutputReadLine: its EOF wait is folded into WaitForExitAsync,
                    // even after the process exits, if a surviving child inherited the pipe.
                    await process.WaitForExitAsync().ConfigureAwait(false);
                    Logger.Log($"Process {process.Id} exited with code {process.ExitCode}; draining redirected output.");
                    await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
                    return new ProcessResult(process, process.ExitCode,
                        new ReadOnlyCollection<string>(outputLines), new ReadOnlyCollection<string>(errorLines));
                }
                finally
                {
                    outputCancellation.Dispose();
                }
            }

            async Task ReadAsync(StreamReader reader, List<string> lines, Action<string>? onLine)
            {
                try
                {
                    while (await reader.ReadLineAsync(outputCancellation.Token).ConfigureAwait(false) is { } line)
                    {
                        lines.Add(line);
                        onLine?.Invoke(line);
                    }
                }
                catch (OperationCanceledException) when (outputCancellation.IsCancellationRequested)
                {
                    ConsoleUtil.Warning($"Process {process.Id}: redirected output drain canceled during cleanup.");
                }
            }
        }

        public static ProcessStartInfo CreateProcessStartInfo(
            string executable,
            string arguments,
            string? workingDirectory = null,
            bool captureOutput = false,
            bool displayWindow = true,
            Dictionary<string, string>? environmentVariables = null)
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
