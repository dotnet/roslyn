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
        private readonly int exitCode;
        private readonly IEnumerable<string> outputLines;
        private readonly IEnumerable<string> errorLines;

        public ProcessOutput(int exitCode, IEnumerable<string> outputLines, IEnumerable<string> errorLines)
        {
            this.exitCode = exitCode;
            this.outputLines = outputLines;
            this.errorLines = errorLines;
        }

        public int ExitCode { get { return exitCode; } }

        public IEnumerable<string> OutputLines
        {
            get { return outputLines; }
        }

        public IEnumerable<string> ErrorLines
        {
            get { return errorLines; }
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
            bool elevated = false,
            Dictionary<string, string> environmentVariables = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var taskCompletionSource = new TaskCompletionSource<ProcessOutput>();

            var process = new Process();
            process.EnableRaisingEvents = true;
            if (elevated)
            {
                process.StartInfo = CreateElevatedStartInfo(executable, arguments, workingDirectory, captureOutput, displayWindow);
            }
            else
            {
                process.StartInfo = CreateProcessStartInfo(executable, arguments, workingDirectory, captureOutput, displayWindow);
            }

            var task = CreateTask(process, taskCompletionSource, cancellationToken);

            process.Start();

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
            string executable, string arguments,
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

        public static ProcessStartInfo CreateElevatedStartInfo(string executable, string arguments, string workingDirectory, bool captureOutput, bool displayWindow)
        {
            var adminInfo = new ProcessStartInfo(executable, arguments);
            adminInfo.WindowStyle = ProcessWindowStyle.Hidden;
            adminInfo.CreateNoWindow = true;
            adminInfo.Verb = "runas";

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                adminInfo.WorkingDirectory = workingDirectory;
            }

            return adminInfo;
        }

        public static bool WaitForProcessExit(string procName, int timeoutInSeconds = 300)
        {
            int count = 0;
            var procs = Process.GetProcessesByName(procName);
            while (procs.Length > 0)
            {
                Thread.Sleep(1000);
                procs = Process.GetProcessesByName(procName);
                count++;
                if (count > timeoutInSeconds)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
