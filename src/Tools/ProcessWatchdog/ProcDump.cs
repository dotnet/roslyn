// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace ProcessWatchdog
{
    /// <summary>
    /// Wrapper class for invoking procdump.exe.
    /// </summary>
    internal class ProcDump
    {
        private readonly string _procDumpPath;
        private readonly string _outputFolder;

        internal ProcDump(string procDumpPath, string outputFolder)
        {
            _procDumpPath = procDumpPath;

            // Make sure everything is fully qualified as we are passing this to other processes
            _outputFolder = Path.GetFullPath(outputFolder);
        }

        public Process MonitorProcess(int processId, string description)
        {
            string dumpFileName = GenerateCrashDumpFileName(description);

            var processStartInfo = new ProcessStartInfo
            {
                Arguments = GetProcDumpArgumentsForMonitoring(processId, dumpFileName),
                CreateNoWindow = true,
                FileName = _procDumpPath,
                UseShellExecute = false
            };

            return Process.Start(processStartInfo);
        }

        public Process DumpProcessNow(int processId, string description)
        {
            string dumpFileName = GenerateCrashDumpFileName(description);

            ConsoleUtils.LogMessage(
                Resources.InfoTerminatingProcess,
                description,
                processId,
                dumpFileName);

            var processStartInfo = new ProcessStartInfo
            {
                Arguments = GetProcDumpArgumentsForImmediateDump(processId, dumpFileName),
                CreateNoWindow = true,
                FileName = _procDumpPath,
                UseShellExecute = false
            };

            return Process.Start(processStartInfo);
        }

        private string GenerateCrashDumpFileName(string description)
        {
            var outputFolderInfo = new DirectoryInfo(_outputFolder);
            if (!outputFolderInfo.Exists)
            {
                outputFolderInfo.Create();
                ConsoleUtils.LogMessage(Resources.InfoCreatedOutputFolder, outputFolderInfo.FullName);
            }

            var fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_ff") + " - " + description + ".dmp";
            fileName = Path.Combine(outputFolderInfo.FullName, fileName);
            return fileName;
        }

        private static string GetProcDumpArgumentsForMonitoring(int processId, string dumpFileName)
        {
            // Here's what these arguments mean:
            //
            // -g:  Run as a native debugger in a managed process (no interop).
            // -e:  Dump when an unhandled exception happens.
            // -b:  Dump when a breakpoint (__int 3 / Debugger.Break()) is encountered.
            // -h:  Dump when a window hang is encountered.
            // -r:  Dump using a clone.
            // -ma: Create a full memory dump.
            //
            // without -g, procdump will not catch unhandled managed exception since
            // the CLR will always handle unhandled exceptions. From procdump's point of
            // view, there is no such thing as an unhandled exception for a managed app.

            return $"-accepteula -g -e -b -h -r -ma {processId} \"{dumpFileName}\"";
        }

        private static string GetProcDumpArgumentsForImmediateDump(int processId, string dumpFileName)
        {
            // Here's what these arguments mean:
            //
            // -r:  Dump using a clone.
            // -ma: Create a full memory dump.

            return $"-accepteula -r -ma {processId} \"{dumpFileName}\"";
        }
    }
}
