// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace RunTests
{
    internal static class ProcessUtil
    {
        /// <summary>
        /// Get the command line of the provided <paramref name="process"/>, or <see langword="null"/>
        /// if it can't be determined.
        /// </summary>
        /// <remarks>
        /// This is a best effort API. The process may exit while the command line is being read, or the
        /// current platform may not be supported.
        /// </remarks>
        internal static string? TryGetCommandLine(Process process)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return TryGetCommandLineWindows(process);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return TryGetCommandLineLinux(process);
            }

            return null;
        }

        [SupportedOSPlatform("windows")]
        private static string? TryGetCommandLineWindows(Process process)
        {
            try
            {
                using var mo = new ManagementObject("win32_process.handle='" + process.Id + "'");
                mo.Get();
                return mo["CommandLine"] as string;
            }
            catch (Exception ex)
            {
                ConsoleUtil.Warning($"Failed to get command line for process {process.Id}: {ex.Message}");
                return null;
            }
        }

        [SupportedOSPlatform("linux")]
        private static string? TryGetCommandLineLinux(Process process)
        {
            try
            {
                // /proc/<pid>/cmdline contains the arguments separated by null characters.
                var raw = File.ReadAllText($"/proc/{process.Id}/cmdline");
                return raw.Replace('\0', ' ').Trim();
            }
            catch (Exception ex)
            {
                ConsoleUtil.Warning($"Failed to get command line for process {process.Id}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Return the set of <c>testhost</c> processes spawned by <c>dotnet test</c>. A process is
        /// considered a test host when either:
        /// <list type="number">
        /// <item>its process name starts with <c>testhost</c>; or</item>
        /// <item>its process name is <c>dotnet</c> and its command line references <c>testhost</c>.</item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// This is a best effort API.  It can be thwarted by process instances starting / stopping during
        /// the building of this list.
        /// </remarks>
        internal static List<Process> GetTestHostProcesses()
        {
            var list = new List<Process>();
            foreach (var process in Process.GetProcesses())
            {
                if (IsTestHostProcess(process))
                {
                    list.Add(process);
                }
            }

            return list;
        }

        private static bool IsTestHostProcess(Process process)
        {
            string name;
            try
            {
                name = process.ProcessName;
            }
            catch
            {
                ConsoleUtil.Warning($"Failed to get process name for process {process.Id}");
                // The process may have exited between enumeration and inspection.
                return false;
            }

            // Process.ProcessName omits the file extension, but normalize defensively in case a future
            // runtime change includes it.
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - 4);
            }

            if (name.StartsWith("testhost", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(name, "dotnet", StringComparison.OrdinalIgnoreCase))
            {
                var commandLine = TryGetCommandLine(process);
                if (commandLine is not null &&
                    commandLine.IndexOf("testhost", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
