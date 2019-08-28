// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace RunTests
{
    internal readonly struct ProcDumpInfo
    {
        private const string KeyProcDumpFilePath = "ProcDumpFilePath";
        private const string KeyProcDumpDirectory = "ProcDumpOutputPath";
        private const string KeyProcDumpSecondaryDirectory = "ProcDumpSecondaryOutputPath";

        internal string ProcDumpFilePath { get; }
        internal string DumpDirectory { get; }
        internal string SecondaryDumpDirectory { get; }

        internal ProcDumpInfo(string procDumpFilePath, string dumpDirectory, string secondaryDumpDirectory)
        {
            Debug.Assert(Path.IsPathRooted(procDumpFilePath));
            Debug.Assert(Path.IsPathRooted(dumpDirectory));
            Debug.Assert(Path.IsPathRooted(secondaryDumpDirectory));
            ProcDumpFilePath = procDumpFilePath;
            DumpDirectory = dumpDirectory;
            SecondaryDumpDirectory = secondaryDumpDirectory;
        }

        internal void WriteEnvironmentVariables(Dictionary<string, string> environment)
        {
            environment[KeyProcDumpFilePath] = ProcDumpFilePath;
            environment[KeyProcDumpDirectory] = DumpDirectory;
            environment[KeyProcDumpSecondaryDirectory] = SecondaryDumpDirectory;
        }

        internal static ProcDumpInfo? ReadFromEnvironment()
        {
            bool validate(string s) => !string.IsNullOrEmpty(s) && Path.IsPathRooted(s);

            var procDumpFilePath = Environment.GetEnvironmentVariable(KeyProcDumpFilePath);
            var dumpDirectory = Environment.GetEnvironmentVariable(KeyProcDumpDirectory);
            var secondaryDumpDirectory = Environment.GetEnvironmentVariable(KeyProcDumpSecondaryDirectory);

            if (!validate(procDumpFilePath) || !validate(dumpDirectory) || !validate(secondaryDumpDirectory))
            {
                return null;
            }

            return new ProcDumpInfo(procDumpFilePath, dumpDirectory, secondaryDumpDirectory);
        }
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
