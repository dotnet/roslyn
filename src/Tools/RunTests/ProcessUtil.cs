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
using System.Runtime.Versioning;

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
            catch (ArgumentException)
            {
                // The process exited before its identity could be checked.
            }
            catch (InvalidOperationException)
            {
                // The process exited between checking and killing it.
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
        internal static List<Process> GetChildProcesses(Process root)
        {
            var parents = GetParentProcessIds();
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
                    catch (ArgumentException) { }
                    catch (InvalidOperationException) { }
                    catch (Win32Exception ex)
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

        private static Dictionary<int, int> GetParentProcessIds()
        {
            if (OperatingSystem.IsWindows())
            {
                return GetParentProcessIdsWindows();
            }

            var result = new Dictionary<int, int>();
            if (OperatingSystem.IsLinux())
            {
                foreach (var directory in Directory.EnumerateDirectories("/proc"))
                {
                    if (!int.TryParse(Path.GetFileName(directory), out var pid))
                        continue;

                    try
                    {
                        var fields = ReadLinuxProcessStat(pid);
                        result[pid] = int.Parse(fields[1]);
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }

            return result;
        }

        [SupportedOSPlatform("windows")]
        private static Dictionary<int, int> GetParentProcessIdsWindows()
        {
            var result = new Dictionary<int, int>();
            using var searcher = new ManagementObjectSearcher("SELECT ProcessId, ParentProcessId FROM Win32_Process");
            searcher.Options.Timeout = TimeSpan.FromSeconds(2);
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
    }
}
