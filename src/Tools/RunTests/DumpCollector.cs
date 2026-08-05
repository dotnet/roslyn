// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.NETCore.Client;

namespace RunTests
{
    /// <summary>
    /// Collects dump files from processes. Uses <see cref="DiagnosticsClient"/> for .NET Core
    /// processes and MiniDumpWriteDump P/Invoke for .NET Framework processes.
    /// </summary>
    internal static class DumpCollector
    {
        /// <summary>
        /// Attempts to collect a full memory dump from the specified process.
        /// Returns true if the dump was successfully written.
        /// </summary>
        internal static bool TryDumpProcess(Process process, string dumpFilePath)
        {
            try
            {
                if (IsNetCoreProcess(process))
                {
                    return TryDumpNetCoreProcess(process, dumpFilePath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return TryDumpWithMiniDumpWriteDump(process, dumpFilePath);
                }
                else
                {
                    Logger.Log($"Cannot dump non-.NET Core process {process.ProcessName} ({process.Id}) on non-Windows platform.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to dump process {process.ProcessName} ({process.Id}): {ex.Message}");
                return false;
            }
        }

        private static bool TryDumpNetCoreProcess(Process process, string dumpFilePath)
        {
            try
            {
                var client = new DiagnosticsClient(process.Id);
                client.WriteDump(DumpType.Full, dumpFilePath, logDumpGeneration: false);
                return File.Exists(dumpFilePath);
            }
            catch (Exception ex)
            {
                Logger.Log($"DiagnosticsClient.WriteDump failed for process {process.Id}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Determines if a process is a .NET Core process by checking if the diagnostics
        /// IPC channel is available (the pipe/socket exists).
        /// </summary>
        private static bool IsNetCoreProcess(Process process)
        {
            try
            {
                // On Windows, .NET Core processes create a named pipe: dotnet-diagnostic-{pid}
                // On Unix, they create a Unix domain socket in the temp directory.
                // DiagnosticsClient.GetPublishedProcesses() returns all PIDs with active diagnostic ports.
                var publishedProcesses = DiagnosticsClient.GetPublishedProcesses();
                foreach (var pid in publishedProcesses)
                {
                    if (pid == process.Id)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

#pragma warning disable CA1416 // Validate platform compatibility
        private static bool TryDumpWithMiniDumpWriteDump(Process process, string dumpFilePath)
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

                if (!success)
                {
                    var errorCode = Marshal.GetLastWin32Error();
                    Logger.Log($"MiniDumpWriteDump failed for process {process.Id} with error code {errorCode}");
                    // Clean up the empty/partial file
                    try { fileStream.Close(); File.Delete(dumpFilePath); } catch { }
                }

                return success;
            }
            catch (Exception ex)
            {
                Logger.Log($"MiniDumpWriteDump failed for process {process.Id}: {ex.Message}");
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
