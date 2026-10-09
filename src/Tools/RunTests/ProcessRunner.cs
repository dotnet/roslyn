// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace RunTests
{
    public sealed class ProcessOutputDrainException : TimeoutException
    {
        public ProcessResult Result { get; }

        internal ProcessOutputDrainException(ProcessResult result, TimeSpan timeout)
            : base($"Process {result.Process.Id} exited with code {result.ExitCode}, but redirected output did not reach EOF within {timeout}. " +
                $"A descendant may still hold an inherited output handle.{Environment.NewLine}" +
                $"Standard output:{Environment.NewLine}{string.Join(Environment.NewLine, result.OutputLines)}{Environment.NewLine}" +
                $"Standard error:{Environment.NewLine}{string.Join(Environment.NewLine, result.ErrorLines)}")
        {
            Result = result;
        }
    }

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

        public int Id => Process.Id;

        public ProcessInfo(Process process, ProcessStartInfo startInfo, Task<ProcessResult> result)
        {
            Process = process;
            StartInfo = startInfo;
            Result = result;
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
            Action<DataReceivedEventArgs>? onOutputDataReceived = null)
            => CreateProcess(
                CreateProcessStartInfo(executable, arguments, workingDirectory, captureOutput, displayWindow, environmentVariables),
                lowPriority: lowPriority,
                onProcessStartHandler: onProcessStartHandler,
                onOutputDataReceived: onOutputDataReceived);

        public static ProcessInfo CreateProcess(
            ProcessStartInfo processStartInfo,
            bool lowPriority = false,
            Action<Process>? onProcessStartHandler = null,
            Action<DataReceivedEventArgs>? onOutputDataReceived = null)
        {
            var errorLines = new List<string>();
            var outputLines = new List<string>();
            var process = new Process();
            process.StartInfo = processStartInfo;
            var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            process.Exited += OnExited;
            process.EnableRaisingEvents = true;

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    onOutputDataReceived?.Invoke(e);
                    lock (outputLines)
                        outputLines.Add(e.Data);
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    lock (errorLines)
                        errorLines.Add(e.Data);
                }
            };

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

            return new ProcessInfo(process, processStartInfo, CompleteAsync());

            void OnExited(object? sender, EventArgs e)
                => exited.TrySetResult();

            async Task<ProcessResult> CompleteAsync()
            {
                // WaitForExitAsync also waits for pipe EOF, which an inherited handle can delay
                // indefinitely even after this process exits.
                await exited.Task.ConfigureAwait(false);
                process.Exited -= OnExited;
                var drainTimeout = TimeSpan.FromSeconds(30);
                var drainTimedOut = false;
                try
                {
                    await process.WaitForExitAsync().WaitAsync(drainTimeout).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    drainTimedOut = true;
                    if (processStartInfo.RedirectStandardOutput)
                        process.CancelOutputRead();
                    if (processStartInfo.RedirectStandardError)
                        process.CancelErrorRead();
                }

                ReadOnlyCollection<string> output;
                ReadOnlyCollection<string> error;
                lock (outputLines)
                    output = Array.AsReadOnly(outputLines.ToArray());
                lock (errorLines)
                    error = Array.AsReadOnly(errorLines.ToArray());
                var result = new ProcessResult(process, process.ExitCode, output, error);
                if (drainTimedOut)
                    throw new ProcessOutputDrainException(result, drainTimeout);

                return result;
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
