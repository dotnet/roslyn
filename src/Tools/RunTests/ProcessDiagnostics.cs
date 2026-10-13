// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.Versioning;

namespace RunTests;

internal static class ProcessDiagnostics
{
    [SupportedOSPlatform("windows")]
    internal static void LogWindowsProcessTree()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT ProcessId, ParentProcessId, Name, CreationDate, CommandLine FROM Win32_Process");
            using var processes = searcher.Get();
            var snapshot = new Dictionary<int, (int Parent, string Details)>();
            foreach (ManagementObject data in processes)
            {
                using (data)
                {
                    try
                    {
                        var pid = checked((int)(uint)data["ProcessId"]);
                        var parent = checked((int)(uint)data["ParentProcessId"]);
                        var start = ManagementDateTimeConverter.ToDateTime((string)data["CreationDate"]).ToUniversalTime();
                        // Runner arguments can contain pipeline/Helix access tokens.
                        var command = pid == Environment.ProcessId ? "<runner arguments omitted>" : data["CommandLine"] ?? "<unavailable>";
                        snapshot.Add(pid, (parent, $"PID={pid}, parent={parent}, name={data["Name"]}, " +
                            $"started={start:O}, command={command}"));
                    }
                    catch (Exception ex)
                    {
                        ConsoleUtil.Warning($"Unable to read process snapshot entry: {ex.Message}");
                    }
                }
            }

            // Restrict output to this runner's tree; do not log unrelated machine processes.
            var visited = new HashSet<int> { Environment.ProcessId };
            var pending = new Queue<int>(visited);
            while (pending.TryDequeue(out var pid))
            {
                if (snapshot.TryGetValue(pid, out var entry))
                    ConsoleUtil.WriteLine($"[{DateTime.UtcNow:O}] Timeout process snapshot: {entry.Details}");
                foreach (var (childId, child) in snapshot)
                    if (child.Parent == pid && visited.Add(childId))
                        pending.Enqueue(childId);
            }
        }
        catch (Exception ex)
        {
            ConsoleUtil.Warning($"Unable to capture Windows process tree: {ex.Message}");
        }
    }
}
