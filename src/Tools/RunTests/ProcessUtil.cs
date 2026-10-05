// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace RunTests
{
    internal static class ProcessUtil
    {
        internal static long GetProcessStartIdentity(Process process)
        {
            if (OperatingSystem.IsLinux())
            {
                // Process.StartTime on Linux uses a boot-time estimate cached independently in
                // each caller. Use kernel clock ticks so identities match across dump helpers.
                var fields = ReadLinuxProcessStat(process.Id);
                return long.Parse(fields[19], CultureInfo.InvariantCulture);
            }

            return process.StartTime.ToUniversalTime().Ticks;
        }

        private static string[] ReadLinuxProcessStat(int pid)
        {
            var stat = File.ReadAllText($"/proc/{pid}/stat");
            // The comm field may itself contain spaces and parentheses.
            return stat.Substring(stat.LastIndexOf(')') + 2).Split(' ');
        }

        internal static void KillTree(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    using var current = Process.GetProcessById(process.Id);
                    if (current.StartTime == process.StartTime)
                        process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex)
            {
                ConsoleUtil.Warning($"Failed to kill process tree {process.Id}: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds descendants using a single parent-process snapshot. The caller owns the returned
        /// Process instances. Children that have already been reparented are not included.
        /// </summary>
        internal static async Task<List<Process>> GetChildProcessesAsync(Process root)
        {
            var parents = await GetParentProcessIdsAsync().ConfigureAwait(false);
            var children = new List<Process>();
            var pending = new Queue<Process>();
            var visited = new HashSet<int> { root.Id };
            pending.Enqueue(root);
            while (pending.TryDequeue(out var parent))
            {
                foreach (var (pid, parentId) in parents)
                {
                    if (parentId != parent.Id || !visited.Add(pid))
                        continue;

                    Process? child = null;
                    try
                    {
                        child = Process.GetProcessById(pid);
                        // A parent PID can refer to a newer process after PID reuse.
                        if (!parent.HasExited && child.StartTime >= parent.StartTime)
                        {
                            children.Add(child);
                            pending.Enqueue(child);
                            child = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleUtil.Warning($"Unable to inspect child process {pid}: {ex.Message}");
                    }
                    finally
                    {
                        child?.Dispose();
                    }
                }
            }

            return children;
        }

        private static async Task<Dictionary<int, int>> GetParentProcessIdsAsync()
        {
            if (OperatingSystem.IsWindows())
            {
                return GetParentProcessIdsWindows();
            }

            if (OperatingSystem.IsMacOS())
            {
                return await GetParentProcessIdsMacAsync().ConfigureAwait(false);
            }

            if (OperatingSystem.IsLinux())
            {
                return GetParentProcessIdsLinux();
            }

            throw new Exception($"Unknown operating system: {RuntimeInformation.OSDescription}");

            [SupportedOSPlatform("windows")]
            static Dictionary<int, int> GetParentProcessIdsWindows()
            {
                var result = new Dictionary<int, int>();
                using var searcher = new ManagementObjectSearcher("SELECT ProcessId, ParentProcessId FROM Win32_Process");
                using var processes = searcher.Get();
                foreach (ManagementObject process in processes)
                {
                    using (process)
                    {
                        result[checked((int)(uint)process["ProcessId"])] = checked((int)(uint)process["ParentProcessId"]);
                    }
                }

                return result;
            }

            static async Task<Dictionary<int, int>> GetParentProcessIdsMacAsync()
            {
                var info = ProcessRunner.CreateProcess("/bin/ps", "-A -o pid= -o ppid=", captureOutput: true, displayWindow: false);
                using var process = info.Process;
                var output = await info.Result.ConfigureAwait(false);
                if (output.ExitCode != 0)
                    throw new Exception($"ps exited with code {output.ExitCode}: {string.Join(Environment.NewLine, output.ErrorLines)}");

                var result = new Dictionary<int, int>();
                foreach (var line in output.OutputLines)
                {
                    var fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length != 2)
                        throw new Exception($"Unexpected ps output: '{line}'");

                    result.Add(int.Parse(fields[0], CultureInfo.InvariantCulture), int.Parse(fields[1], CultureInfo.InvariantCulture));
                }

                return result;
            }

            static Dictionary<int, int> GetParentProcessIdsLinux()
            {
                var result = new Dictionary<int, int>();
                foreach (var directory in Directory.EnumerateDirectories("/proc"))
                {
                    if (!int.TryParse(Path.GetFileName(directory), out var pid))
                        continue;

                    try
                    {
                        var fields = ReadLinuxProcessStat(pid);
                        result[pid] = int.Parse(fields[1]);
                    }
                    catch (Exception) { }
                }

                return result;
            }
        }
    }
}
