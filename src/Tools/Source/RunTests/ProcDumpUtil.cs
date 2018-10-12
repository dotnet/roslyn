// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace RunTests
{
    internal struct ProcDumpInfo
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
            bool validate(string s) => !string.IsNullOrEmpty(s) && Path.IsPathRooted(s);

            var procDumpFilePath = Environment.GetEnvironmentVariable(KeyProcDumpFilePath);
            var dumpDirectory = Environment.GetEnvironmentVariable(KeyProcDumpDirectory);

            if (!validate(procDumpFilePath) || !validate(dumpDirectory))
            {
                return null;
            }

            return new ProcDumpInfo(procDumpFilePath, dumpDirectory);
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
            var startInfo = new ProcessStartInfo(procDumpFilePath, GetProcDumpCommandLine(processId, dumpDirectory));

            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;

            var process = Process.Start(startInfo);

            void CopyStreamReaderToLogFile(StreamReader stream, string extensionSuffix)
            {
                new Thread(() =>
                {
                    using (var streamWriter = new StreamWriter(Path.Combine(dumpDirectory, $"procdump-pid{processId}" + extensionSuffix)))
                    {
                        string line;
                        while ((line = stream.ReadLine()) != null)
                        {
                            streamWriter.WriteLine(line);
                        }
                    }
                }).Start();
            }

            CopyStreamReaderToLogFile(process.StandardError, ".err.log");
            CopyStreamReaderToLogFile(process.StandardOutput, ".out.log");

            return process;
        }
    }
}
