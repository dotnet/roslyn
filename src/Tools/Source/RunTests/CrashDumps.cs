using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests
{
    public static class CrashDumps
    {
        public static bool TryMonitorProcess(Process processToMonitor, string outputPath)
        {
            var procDumpPath = TryGetProcDumpPath();

            // Make sure everything is fully qualified as we are passing this to other processes
            outputPath = Path.GetFullPath(outputPath);

            if (procDumpPath == null)
            {
                return false;
            }

            var processStart = new ProcessStartInfo();

            processStart.Arguments = GetProcDumpArgumentsForMonitoring(processToMonitor.Id, outputPath);
            processStart.CreateNoWindow = true;
            processStart.FileName = procDumpPath;
            processStart.UseShellExecute = false;

            var process = Process.Start(processStart);

            return true;
        }

        private static string GetProcDumpArgumentsForMonitoring(int pid, string outputPath)
        {
            // Here's what these arguments mean:
            //
            // -g:  run as a native debugger in a managed process (no interop).
            // -e:  dump when an unhandled exception happens
            // -b:  dump when a breakpoint (__int 3 / Debugger.Break()) is encountered
            // -ma: create a full memory dump
            //
            // without -g, procdump will not catch unhandled managed exception since CLR will always
            // handle unhandled exception. for procdump point of view, there is no such thing as unhandled
            // exception for managed app.

            return $"-accepteula -g -e -b -ma {pid} {outputPath}";
        }

        private const string DotNetContinuousIntegrationProcDumpLocation = @"C:\Sysinternals\Procdump.exe";

        private static string TryGetProcDumpPath()
        {
            if (File.Exists(DotNetContinuousIntegrationProcDumpLocation))
            {
                return DotNetContinuousIntegrationProcDumpLocation;
            }

            return null;
        }
    }
}
