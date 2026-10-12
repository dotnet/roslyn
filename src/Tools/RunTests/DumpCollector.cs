// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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

        internal static async Task CollectAsync(IReadOnlyList<Process> processes, Options options, string directory)
        {
            Directory.CreateDirectory(directory);
            var candidates = new List<(Process Process, string Name)>();
            foreach (var process in processes)
            {
                try
                {
                    candidates.Add((process, process.ProcessName));
                }
                catch (InvalidOperationException) { }
            }

            // Prioritize test hosts over the launcher and other owned .NET processes.
            foreach (var (process, name) in candidates.OrderByDescending(p => p.Name.StartsWith("testhost", StringComparison.Ordinal)))
            {
                string? path = null;
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
                    ConsoleUtil.WriteLine($"Dumping owned process {process.Id} to {dumpPath}");
                    var helper = ProcessRunner.CreateProcess(startInfo);
                    Logger.Log($"Dump helper {helper.Id} for owned process {process.Id}");
                    var result = await helper.Result.ConfigureAwait(false);
                    Logger.Log(string.Join(Environment.NewLine, result.OutputLines));
                    Logger.Log(string.Join(Environment.NewLine, result.ErrorLines));
                    if (result.ExitCode != 0)
                        throw new Exception($"Dump helper exited with code {result.ExitCode}");

                    File.Move(path, dumpPath);
                    ConsoleUtil.WriteLine($"Dump collected: {dumpPath} ({new FileInfo(dumpPath).Length} bytes)");
                }
                catch (Exception ex)
                {
                    ConsoleUtil.Warning($"Dump collection failed: {ex.Message}");
                    if (path is not null)
                    {
                        try
                        {
                            File.Delete(path);
                        }
                        catch (Exception cleanupException)
                        {
                            ConsoleUtil.Warning($"Failed to delete partial dump '{path}': {cleanupException.Message}");
                        }
                    }
                }
            }
        }
    }
}
