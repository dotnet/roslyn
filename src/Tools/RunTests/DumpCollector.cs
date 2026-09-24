// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests
{
    /// <summary>
    /// Collects dump files from processes by running the pinned dotnet-dump local tool out-of-process.
    /// </summary>
    internal static class DumpCollector
    {
        internal static readonly TimeSpan DefaultDumpTimeout = TimeSpan.FromMinutes(2);

        internal static async Task<DumpCollectionResult> TryDumpProcessAsync(
            Process process,
            string dumpFilePath,
            DotnetDumpTool dotnetDumpTool,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            var target = new DumpTarget(process.Id, process.ProcessName);
            var outputLines = new List<string>();
            var errorLines = new List<string>();

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dumpFilePath)!);
                var startInfo = dotnetDumpTool.CreateStartInfo(target, dumpFilePath);
                ConsoleUtil.WriteLine($"Starting dump collection for process {target.ProcessName} ({target.ProcessId}) to '{dumpFilePath}'.");
                ConsoleUtil.WriteLine($"Collector command: {dotnetDumpTool.GetDisplayCommand(target, dumpFilePath)}");
                ConsoleUtil.WriteLine($"Collector timeout: {timeout}");

                using var collectorProcess = new Process()
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true,
                };

                collectorProcess.OutputDataReceived += (_, e) =>
                {
                    if (e.Data is not null)
                    {
                        outputLines.Add(e.Data);
                    }
                };
                collectorProcess.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data is not null)
                    {
                        errorLines.Add(e.Data);
                    }
                };

                collectorProcess.Start();
                collectorProcess.BeginOutputReadLine();
                collectorProcess.BeginErrorReadLine();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);
                try
                {
                    await WaitForExitAsync(collectorProcess, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    ConsoleUtil.WriteLine($"Dump collection timed out after {timeout} for process {target.ProcessName} ({target.ProcessId}); terminating collector process tree.");
                    KillProcessTree(collectorProcess);
                    return new DumpCollectionResult(Succeeded: false, TimedOut: true, ExitCode: null, DumpFileExists: File.Exists(dumpFilePath));
                }

                foreach (var line in outputLines)
                {
                    Logger.Log($"dotnet-dump stdout: {line}");
                }

                foreach (var line in errorLines)
                {
                    Logger.Log($"dotnet-dump stderr: {line}");
                }

                var dumpFileExists = File.Exists(dumpFilePath);
                if (collectorProcess.ExitCode == 0 && dumpFileExists)
                {
                    ConsoleUtil.WriteLine($"Dump collection succeeded for process {target.ProcessName} ({target.ProcessId}); output '{dumpFilePath}'.");
                    return new DumpCollectionResult(Succeeded: true, TimedOut: false, collectorProcess.ExitCode, DumpFileExists: true);
                }

                ConsoleUtil.WriteLine($"Dump collection failed for process {target.ProcessName} ({target.ProcessId}); exit code {collectorProcess.ExitCode}, output exists: {dumpFileExists}.");
                return new DumpCollectionResult(Succeeded: false, TimedOut: false, collectorProcess.ExitCode, dumpFileExists);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to dump process {target.ProcessName} ({target.ProcessId}): {ex.Message}");
                return new DumpCollectionResult(Succeeded: false, TimedOut: false, ExitCode: null, DumpFileExists: File.Exists(dumpFilePath));
            }
        }

        private static async Task WaitForExitAsync(Process process, CancellationToken cancellationToken)
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            process.WaitForExit();
        }

        private static void KillProcessTree(Process process)
        {
            if (process.HasExited)
            {
                return;
            }

            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process exited after the HasExited check.
            }
        }

        internal readonly record struct DumpTarget(int ProcessId, string ProcessName);

        internal readonly record struct DumpCollectionResult(bool Succeeded, bool TimedOut, int? ExitCode, bool DumpFileExists);

        internal readonly record struct DotnetDumpTool(string DotnetFilePath)
        {
            internal ProcessStartInfo CreateStartInfo(DumpTarget target, string dumpFilePath)
            {
                var startInfo = new ProcessStartInfo(DotnetFilePath)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                startInfo.ArgumentList.Add("tool");
                startInfo.ArgumentList.Add("run");
                startInfo.ArgumentList.Add("dotnet-dump");
                startInfo.ArgumentList.Add("--");
                startInfo.ArgumentList.Add("collect");
                startInfo.ArgumentList.Add("--process-id");
                startInfo.ArgumentList.Add(target.ProcessId.ToString());
                startInfo.ArgumentList.Add("--type");
                startInfo.ArgumentList.Add("Full");
                startInfo.ArgumentList.Add("--output");
                startInfo.ArgumentList.Add(dumpFilePath);
                return startInfo;
            }

            internal string GetDisplayCommand(DumpTarget target, string dumpFilePath)
                => $"{Quote(DotnetFilePath)} tool run dotnet-dump -- collect --process-id {target.ProcessId} --type Full --output {Quote(dumpFilePath)}";

            private static string Quote(string argument)
                => argument.Contains(' ') ? $"\"{argument}\"" : argument;
        }
    }

}
