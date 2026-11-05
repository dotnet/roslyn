// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.NETCore.Client;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// Collects dump files from processes. Uses <see cref="DiagnosticsClient"/> for .NET Core
    /// processes and MiniDumpWriteDump P/Invoke for .NET Framework processes.
    /// </summary>
    public static class DumpCollector
    {
        /// <summary>
        /// Attempts to collect a full memory dump from the specified process.
        /// Returns true if the dump was successfully written.
        /// </summary>
        public static bool TryDumpProcess(Process process, string dumpFilePath, Action<string>? log = null)
        {
            try
            {
                log?.Invoke($"Dump backend discovery started for PID={process.Id}, path={dumpFilePath}.");
                if (IsNetCoreProcess(process, log))
                {
                    log?.Invoke($"DiagnosticsClient.WriteDump started for PID={process.Id}.");
                    return TryDumpNetCoreProcess(process, dumpFilePath, log);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    log?.Invoke($"MiniDumpWriteDump started for PID={process.Id}.");
                    return TryDumpWithMiniDumpWriteDump(process, dumpFilePath, log);
                }
                else
                {
                    log?.Invoke($"Cannot dump non-.NET Core process {process.ProcessName} ({process.Id}) on non-Windows platform.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"Failed to dump process PID={process.Id}: {ex}");
                return false;
            }
        }

        private static bool TryDumpNetCoreProcess(Process process, string dumpFilePath, Action<string>? log)
        {
            try
            {
                var client = new DiagnosticsClient(process.Id);
                client.WriteDump(DumpType.Full, dumpFilePath, logDumpGeneration: false);
                log?.Invoke($"DiagnosticsClient.WriteDump returned for PID={process.Id}.");
                return File.Exists(dumpFilePath);
            }
            catch (Exception ex)
            {
                log?.Invoke($"DiagnosticsClient.WriteDump failed for process {process.Id}: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Determines if a process is a .NET Core process by checking if the diagnostics
        /// IPC channel is available (the pipe/socket exists).
        /// </summary>
        private static bool IsNetCoreProcess(Process process, Action<string>? log)
        {
            try
            {
                // On Windows, .NET Core processes create a named pipe: dotnet-diagnostic-{pid}
                // On Unix, they create a Unix domain socket in the temp directory.
                // DiagnosticsClient.GetPublishedProcesses() returns all PIDs with active diagnostic ports.
                foreach (var pid in DiagnosticsClient.GetPublishedProcesses())
                {
                    if (pid == process.Id)
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                log?.Invoke($"Diagnostic-port discovery failed for PID={process.Id}: {ex}");
                return false;
            }
        }

#pragma warning disable CA1416 // Validate platform compatibility
        private static bool TryDumpWithMiniDumpWriteDump(Process process, string dumpFilePath, Action<string>? log)
        {
            try
            {
                using var fileStream = new FileStream(dumpFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                // MiniDumpWithFullMemory = 0x00000002
                var success = NativeMethods.MiniDumpWriteDump(
                    process.Handle,
                    (uint)process.Id,
                    fileStream.SafeFileHandle.DangerousGetHandle(),
                    NativeMethods.MINIDUMP_TYPE.MiniDumpWithFullMemory,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero);
                var errorCode = success ? 0 : Marshal.GetLastWin32Error();
                log?.Invoke($"MiniDumpWriteDump returned for PID={process.Id}: success={success}, error={errorCode}.");

                if (!success)
                {
                    log?.Invoke($"MiniDumpWriteDump failed for process {process.Id} with error code {errorCode}");
                    // Clean up the empty/partial file
                    try
                    {
                        fileStream.Close();
                        File.Delete(dumpFilePath);
                    }
                    catch
                    {
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                log?.Invoke($"MiniDumpWriteDump failed for process {process.Id}: {ex.Message}");
                return false;
            }
        }

        private static class NativeMethods
        {
            [Flags]
            internal enum MINIDUMP_TYPE : uint
            {
                MiniDumpWithFullMemory = 0x00000002,
            }

            [DllImport("dbghelp.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool MiniDumpWriteDump(
                IntPtr hProcess,
                uint processId,
                IntPtr hFile,
                MINIDUMP_TYPE dumpType,
                IntPtr exceptionParam,
                IntPtr userStreamParam,
                IntPtr callbackParam);
        }
#pragma warning restore CA1416 // Validate platform compatibility
    }
}
