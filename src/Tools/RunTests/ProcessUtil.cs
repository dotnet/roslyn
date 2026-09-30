// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.Versioning;

namespace RunTests
{
    internal static class ProcessUtil
    {
        private static readonly object s_snapshotGate = new();
        private static Dictionary<int, int> s_parentProcessIds = new();
        private static long s_snapshotTimestamp;

        internal static void KillTree(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
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

        internal static Dictionary<int, int> GetParentProcessIds()
        {
            lock (s_snapshotGate)
            {
                // All parallel work items can share this read-only snapshot instead of issuing
                // a machine-wide WMI query per work item each second.
                if (Stopwatch.GetElapsedTime(s_snapshotTimestamp) >= TimeSpan.FromSeconds(1))
                {
                    s_parentProcessIds = ReadParentProcessIds();
                    s_snapshotTimestamp = Stopwatch.GetTimestamp();
                }

                return s_parentProcessIds;
            }
        }

        private static Dictionary<int, int> ReadParentProcessIds()
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
                        var stat = File.ReadAllText(Path.Combine(directory, "stat"));
                        // The comm field may itself contain spaces and parentheses.
                        var fields = stat.Substring(stat.LastIndexOf(')') + 2).Split(' ');
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
