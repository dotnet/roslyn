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
    /// Collects dump files from processes by running dotnet-dump out-of-process.
    /// </summary>
    internal static class DumpCollector
    {
        internal static readonly TimeSpan DefaultDumpTimeout = TimeSpan.FromMinutes(2);

        internal static async Task<DumpCollectionResult> TryDumpProcessAsync(
            Process process,
            string dumpFilePath,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            var processId = process.Id;
            var processName = process.ProcessName;
            var outputLines = new List<string>();
            var errorLines = new List<string>();

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dumpFilePath)!);
                var startInfo = new ProcessStartInfo("dotnet-dump")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                startInfo.ArgumentList.Add("collect");
                startInfo.ArgumentList.Add("--process-id");
                startInfo.ArgumentList.Add(processId.ToString());
                startInfo.ArgumentList.Add("--type");
                startInfo.ArgumentList.Add("Full");
                startInfo.ArgumentList.Add("--output");
                startInfo.ArgumentList.Add(dumpFilePath);

                ConsoleUtil.WriteLine($"Starting dump collection for process {processName} ({processId}) to '{dumpFilePath}'.");
                ConsoleUtil.WriteLine($"Collector command: dotnet-dump collect --process-id {processId} --type Full --output {dumpFilePath}");
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
                    await collectorProcess.WaitForExitAsync(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    KillProcessTree(collectorProcess);
                    cancellationToken.ThrowIfCancellationRequested();

                    ConsoleUtil.WriteLine($"Dump collection timed out after {timeout} for process {processName} ({processId}); terminated collector process tree.");
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
                    ConsoleUtil.WriteLine($"Dump collection succeeded for process {processName} ({processId}); output '{dumpFilePath}'.");
                    return new DumpCollectionResult(Succeeded: true, TimedOut: false, collectorProcess.ExitCode, DumpFileExists: true);
                }

                ConsoleUtil.WriteLine($"Dump collection failed for process {processName} ({processId}); exit code {collectorProcess.ExitCode}, output exists: {dumpFileExists}.");
                return new DumpCollectionResult(Succeeded: false, TimedOut: false, collectorProcess.ExitCode, dumpFileExists);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to dump process {processName} ({processId}): {ex.Message}");
                return new DumpCollectionResult(Succeeded: false, TimedOut: false, ExitCode: null, DumpFileExists: File.Exists(dumpFilePath));
            }
        }

        private static void KillProcessTree(Process process)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to terminate dump collector process tree: {ex.Message}");
            }
        }

        internal readonly record struct DumpCollectionResult(bool Succeeded, bool TimedOut, int? ExitCode, bool DumpFileExists);
    }

}
