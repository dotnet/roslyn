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
        // -h	Write dump if process has a hung window (does not respond to window messages for at least 5 seconds).
        // -t	Write a dump when the process terminates.
        // -w 	Wait for the specified process to launch if it's not running.
        private const string ProcDumpKeys = "/accepteula -ma -e -h -t -w";

        /// <summary>
        /// Starts procdump.exe against the process.
        /// </summary>
        /// <param name="processId">process id</param>
        /// <param name="processName">process name</param>
        /// <param name="loggingMethod">method to log diagnostics to</param>
        /// <param name="destinationDirectory">destination directory for dumps</param>
        public static void StartProcDump(int processId, string processName, string destinationDirectory, Action<string> loggingMethod)
        {
            var environmentVariables = Environment.GetEnvironmentVariables();
            if (environmentVariables.Contains(ProcDumpPathEnvironmentVariableKey))
            {
                var procDumpPath = (string)environmentVariables[ProcDumpPathEnvironmentVariableKey];
                var procDumpFilePath = Path.Combine(procDumpPath, ProcDumpExeFileName);

                var dumpDirectory = Path.Combine(destinationDirectory, "Dumps");
                Directory.CreateDirectory(dumpDirectory);

                var procDumpProcess = Process.Start(procDumpFilePath, $" {ProcDumpKeys} {processId} {dumpDirectory}");
                loggingMethod($"Launched ProcDump attached to {processName} (process Id: {processId})");
            }
            else
            {
                loggingMethod($"Environment variables do not contain {ProcDumpPathEnvironmentVariableKey} (path to procdump.exe). Will skip attaching procdump to VS instance.");
            }
        }
    }
}