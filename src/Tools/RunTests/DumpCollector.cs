// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;

namespace RunTests
{
    /// <summary>
    /// Orchestrates dump collection for a work item's owned processes, prioritizing test hosts.
    /// Delegates the actual per-process dump mechanics to <see cref="Roslyn.Test.Utilities.DumpCollector"/>,
    /// but does so from a separate helper process to isolate native failures and avoid concurrent
    /// calls to Windows' single-threaded DbgHelp API.
    /// </summary>
    internal static class DumpCollector
    {
        internal static int RunHelper(string[] args)
        {
            try
            {
                if (args.Length != 4)
                {
                    Console.Error.WriteLine("Dump helper requires a PID, process start identity, and output path.");
                    return Program.ExitFailure;
                }

                using var process = Process.GetProcessById(int.Parse(args[1], CultureInfo.InvariantCulture));
                if (ProcessUtil.GetProcessStartIdentity(process) != long.Parse(args[2], CultureInfo.InvariantCulture))
                {
                    Console.Error.WriteLine($"Process {process.Id} no longer matches the requested dump target.");
                    return Program.ExitFailure;
                }

                return Roslyn.Test.Utilities.DumpCollector.TryDumpProcess(process, args[3], Console.Error.WriteLine) ? Program.ExitSuccess : Program.ExitFailure;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Program.ExitFailure;
            }
            finally
            {
                Logger.WriteTo(Console.Out);
            }
        }

        internal static readonly TimeSpan HelperTimeout = TimeSpan.FromMinutes(2);
        internal static readonly TimeSpan TotalTimeout = TimeSpan.FromMinutes(10);

        internal static async Task CollectAsync(IReadOnlyList<Process> processes, Options options, string directory, string label, CancellationToken budget)
        {
            Directory.CreateDirectory(directory);
            var candidates = new List<(Process Process, string Name)>();
            foreach (var process in processes)
            {
                try
                {
                    candidates.Add((process, process.ProcessName));
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    ConsoleUtil.Warning($"[{label}] dump target inspection failed: PID={process.Id}: {ex}");
                }
            }

            ConsoleUtil.WriteLine($"[{label}] dump collection started: {candidates.Count} candidates, helper limit={HelperTimeout}, aggregate limit={TotalTimeout}.");
            var collected = 0;
            // Prioritize test hosts over the launcher and other owned .NET processes.
            foreach (var (process, name) in candidates.OrderByDescending(p => p.Name.StartsWith("testhost", StringComparison.Ordinal)))
            {
                if (budget.IsCancellationRequested)
                {
                    ConsoleUtil.Warning($"[{label}] aggregate diagnostic deadline exceeded; remaining targets will be terminated without another dump attempt.");
                    break;
                }
                string? path = null;
                var watch = Stopwatch.StartNew();
                try
                {
                    var dumpPath = Path.GetFullPath(Path.Combine(directory, $"{name}-{process.Id}-hangdump.dmp"));
                    // Never publish an interrupted dump as a complete .dmp.
                    path = dumpPath + ".partial";
                    var startInfo = new ProcessStartInfo(options.DotnetFilePath)
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    };
                    startInfo.ArgumentList.Add(typeof(Program).Assembly.Location);
                    startInfo.ArgumentList.Add("--dump-process");
                    startInfo.ArgumentList.Add(process.Id.ToString(CultureInfo.InvariantCulture));
                    startInfo.ArgumentList.Add(ProcessUtil.GetProcessStartIdentity(process).ToString(CultureInfo.InvariantCulture));
                    startInfo.ArgumentList.Add(path);
                    ConsoleUtil.WriteLine($"[{label}] dump helper start: target={name}, PID={process.Id}, start={ProcessUtil.GetProcessStartIdentity(process)}, path={dumpPath}.");
                    await CollectWithHelperAsync(startInfo, options.DotnetFilePath, path, dumpPath, label, HelperTimeout, budget).ConfigureAwait(false);
                    var dumpSize = new FileInfo(dumpPath).Length;
                    collected++;
                    ConsoleUtil.WriteLine($"[{label}] dump completed: PID={process.Id}, path={dumpPath}, bytes={dumpSize}, elapsed={watch.Elapsed}.");
                }
                catch (Exception ex)
                {
                    ConsoleUtil.Warning($"[{label}] dump failed: PID={process.Id}, partial={path}, elapsed={watch.Elapsed}: {ex}");
                    if (path is not null)
                    {
                        try
                        {
                            File.Delete(path);
                        }
                        catch (Exception cleanupException)
                        {
                            ConsoleUtil.Warning($"[{label}] partial dump cleanup failed: '{path}': {cleanupException}");
                        }
                    }
                }
            }
            ConsoleUtil.WriteLine($"[{label}] dump collection finished: collected={collected}, candidates={candidates.Count}.");
            if (collected == 0)
                ConsoleUtil.Warning($"[{label}] no timeout dumps collected.");
        }

        internal static async Task CollectWithHelperAsync(ProcessStartInfo startInfo, string dotnetPath, string partialPath, string dumpPath, string label, TimeSpan timeout, CancellationToken budget)
        {
            using var helper = OwnedProcess.Start(startInfo, dotnetPath, label + " dump helper", diagnosticOutput: true);
            ConsoleUtil.WriteLine($"[{label}] dump helper running: PID={helper.Info.Id}, limit={timeout}.");
            try
            {
                var result = await helper.Info.Result.WaitAsync(timeout, budget).ConfigureAwait(false);
                ConsoleUtil.WriteLine($"[{label}] dump helper exited: PID={helper.Info.Id}, code={result.ExitCode}; validating output.");
                if (result.ExitCode != 0)
                    throw new InvalidOperationException($"Dump helper {helper.Info.Id} exited with code {result.ExitCode}: {string.Join(Environment.NewLine, result.ErrorLines)}");
                if (!File.Exists(partialPath) || new FileInfo(partialPath).Length == 0)
                    throw new InvalidOperationException($"Dump helper {helper.Info.Id} succeeded without a nonempty dump at '{partialPath}'.");
                File.Move(partialPath, dumpPath);
            }
            finally
            {
                await helper.CleanupAsync().ConfigureAwait(false);
                helper.Info.Process.Dispose();
            }
        }
    }
}
