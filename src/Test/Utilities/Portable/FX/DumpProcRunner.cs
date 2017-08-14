// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class ProcDumpRunner
    {
        public const string ProcDumpPathEnvironmentVariableKey = "ProcDumpPath";

        private const string ProcDumpExeFileName = "procdump.exe";

        // /accepteula command line option to automatically accept the Sysinternals license agreement.
        // -ma	Write a 'Full' dump file. Includes All the Image, Mapped and Private memory.
        // -e	Write a dump when the process encounters an unhandled exception. Include the 1 to create dump on first chance exceptions.
        // -t	Write a dump when the process terminates.
        private const string ProcDumpSwitches = "/accepteula -ma -e -t";

        /// <summary>
        /// Starts procdump.exe against the process.
        /// </summary>
        /// <param name="procDumpPath">The path to the procdump executable</param>
        /// <param name="processId">process id</param>
        /// <param name="processName">process name</param>
        /// <param name="loggingMethod">method to log diagnostics to</param>
        /// <param name="destinationDirectory">destination directory for dumps</param>
        public static void StartProcDump(string procDumpPath, int processId, string processName, string destinationDirectory, Action<string> loggingMethod)
        {
            if (!string.IsNullOrWhiteSpace(procDumpPath))
            {
                var procDumpFilePath = Path.Combine(procDumpPath, ProcDumpExeFileName);
                var dumpDirectory = Path.Combine(destinationDirectory, "Dumps");
                Directory.CreateDirectory(dumpDirectory);

                var procDumpProcess = Process.Start(procDumpFilePath, $" {ProcDumpSwitches} {processId} \"{dumpDirectory}\"");
                loggingMethod($"Launched ProcDump attached to {processName} (process Id: {processId})");
            }
            else
            {
                loggingMethod($"Environment variables do not contain {ProcDumpPathEnvironmentVariableKey} (path to procdump.exe). Will skip attaching procdump to VS instance.");
            }
        }
    }
}
