// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

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
    }
}
