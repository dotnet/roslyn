// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;

namespace RunTests
{
    internal readonly struct ProcDumpInfo
    {
        private const string KeyProcDumpFilePath = "ProcDumpFilePath";
        private const string KeyProcDumpDirectory = "ProcDumpOutputPath";

        internal string ProcDumpFilePath { get; }
        internal string DumpDirectory { get; }

        internal ProcDumpInfo(string procDumpFilePath, string dumpDirectory)
        {
            Debug.Assert(Path.IsPathRooted(procDumpFilePath));
            Debug.Assert(Path.IsPathRooted(dumpDirectory));
            ProcDumpFilePath = procDumpFilePath;
            DumpDirectory = dumpDirectory;
        }

        internal void WriteEnvironmentVariables(Dictionary<string, string> environment)
        {
            environment[KeyProcDumpFilePath] = ProcDumpFilePath;
            environment[KeyProcDumpDirectory] = DumpDirectory;
        }

        internal static ProcDumpInfo? ReadFromEnvironment()
        {
            bool validate([NotNullWhen(true)] string? s) => !string.IsNullOrEmpty(s) && Path.IsPathRooted(s);

            var procDumpFilePath = Environment.GetEnvironmentVariable(KeyProcDumpFilePath);
            var dumpDirectory = Environment.GetEnvironmentVariable(KeyProcDumpDirectory);

            if (!validate(procDumpFilePath) || !validate(dumpDirectory))
            {
                return null;
            }

            return new ProcDumpInfo(procDumpFilePath, dumpDirectory);
        }
    }

    internal static class DumpUtil
    {
#pragma warning disable CA1416 // Validate platform compatibility
        internal static void EnableRegistryDumpCollection(string dumpDirectory)
        {
            Debug.Assert(IsAdministrator());

            using var registryKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps", writable: true);
            registryKey.SetValue("DumpType", 2, RegistryValueKind.DWord);
            registryKey.SetValue("DumpCount", 2, RegistryValueKind.DWord);
            registryKey.SetValue("DumpFolder", dumpDirectory, RegistryValueKind.String);
        }

        internal static void DisableRegistryDumpCollection()
        {
            Debug.Assert(IsAdministrator());

            using var registryKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps", writable: true);
            registryKey.DeleteValue("DumpType", throwOnMissingValue: false);
            registryKey.DeleteValue("DumpCount", throwOnMissingValue: false);
            registryKey.DeleteValue("DumpFolder", throwOnMissingValue: false);
        }

        internal static bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
#pragma warning restore CA1416 // Validate platform compatibility
    }

    internal static class ProcDumpUtil
    {
        internal static Process AttachProcDump(ProcDumpInfo procDumpInfo, int processId)
        {
            return AttachProcDump(procDumpInfo.ProcDumpFilePath, processId, procDumpInfo.DumpDirectory);
        }

        internal static string GetProcDumpCommandLine(int processId, string dumpDirectory)
        {
            // /accepteula command line option to automatically accept the Sysinternals license agreement.
            // -ma	Write a 'Full' dump file. Includes All the Image, Mapped and Private memory.
            // -e	Write a dump when the process encounters an unhandled exception. Include the 1 to create dump on first chance exceptions.
            // -f C00000FD.STACK_OVERFLOWC Dump when a stack overflow first chance exception is encountered. 
            const string procDumpSwitches = "/accepteula -ma -e -f C00000FD.STACK_OVERFLOW";
            dumpDirectory = dumpDirectory.TrimEnd('\\');
            return $" {procDumpSwitches} {processId} \"{dumpDirectory}\"";
        }

        /// <summary>
        /// Attaches a new procdump.exe against the specified process.
        /// </summary>
        /// <param name="procDumpFilePath">The path to the procdump executable</param>
        /// <param name="processId">process id</param>
        /// <param name="dumpDirectory">destination directory for dumps</param>
        internal static Process AttachProcDump(string procDumpFilePath, int processId, string dumpDirectory)
        {
            Directory.CreateDirectory(dumpDirectory);
            return Process.Start(procDumpFilePath, GetProcDumpCommandLine(processId, dumpDirectory));
        }
    }
}
