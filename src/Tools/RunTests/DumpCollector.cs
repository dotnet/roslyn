// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;

namespace RunTests
{
    /// <summary>
    /// Collects dumps in separate helper processes. Windows uses MiniDumpWriteDump for both
    /// Framework and Core processes.
    /// </summary>
    internal static class DumpCollector
    {
        internal static int RunHelper(string[] args)
        {
            try
            {
                if (args.Length != 4)
                {
                    Console.Error.WriteLine("Dump helper requires a PID, process start identity, and output path.");
                    return Program.ExitFailure;
                }

                using var process = Process.GetProcessById(int.Parse(args[1], CultureInfo.InvariantCulture));
                if (ProcessUtil.GetProcessStartIdentity(process) != long.Parse(args[2], CultureInfo.InvariantCulture))
                {
                    Console.Error.WriteLine($"Process {process.Id} no longer matches the requested dump target.");
                    return Program.ExitFailure;
                }

                return TryDumpProcess(process, args[3]) ? Program.ExitSuccess : Program.ExitFailure;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Program.ExitFailure;
            }
            finally
            {
                Logger.WriteTo(Console.Out);
            }
        }

        /// <summary>
        /// Attempts full dumps of the supplied process list,
        /// prioritizing test hosts. Starts a separate RunTests helper subprocess for each dump
        /// and waits for it without a timeout. Collection continues until the helpers finish or
        /// the processes are externally terminated; only successfully completed dumps are published.
        /// </summary>
        internal static async Task CollectAsync(IReadOnlyList<Process> processes, Options options, string directory)
        {
            Directory.CreateDirectory(directory);
            var candidates = new List<(Process Process, string Name)>();
            foreach (var process in processes)
            {
                try
                {
                    candidates.Add((process, process.ProcessName));
                }
                catch (InvalidOperationException) { }
            }

            // Prioritize test hosts over the launcher and other owned .NET processes.
            foreach (var (process, name) in candidates.OrderByDescending(p => p.Name.StartsWith("testhost", StringComparison.Ordinal)))
            {
                string? path = null;
                try
                {
                    var dumpPath = Path.GetFullPath(Path.Combine(directory, $"{name}-{process.Id}-hangdump.dmp"));
                    // Never publish an interrupted dump as a complete .dmp.
                    path = dumpPath + ".partial";
                    var startInfo = new ProcessStartInfo(options.DotnetFilePath)
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    };
                    startInfo.ArgumentList.Add(typeof(Program).Assembly.Location);
                    startInfo.ArgumentList.Add("--dump-process");
                    startInfo.ArgumentList.Add(process.Id.ToString(CultureInfo.InvariantCulture));
                    startInfo.ArgumentList.Add(ProcessUtil.GetProcessStartIdentity(process).ToString(CultureInfo.InvariantCulture));
                    startInfo.ArgumentList.Add(path);
                    ConsoleUtil.WriteLine($"Dumping owned process {process.Id} to {dumpPath}");
                    var helper = ProcessRunner.CreateProcess(startInfo);
                    Logger.Log($"Dump helper {helper.Id} for owned process {process.Id}");
                    var result = await helper.Result.ConfigureAwait(false);
                    Logger.Log(string.Join(Environment.NewLine, result.OutputLines));
                    Logger.Log(string.Join(Environment.NewLine, result.ErrorLines));
                    if (result.ExitCode != 0)
                        throw new IOException($"Dump helper exited with code {result.ExitCode}");

                    File.Move(path, dumpPath);
                    ConsoleUtil.WriteLine($"Dump collected: {dumpPath} ({new FileInfo(dumpPath).Length} bytes)");
                }
                catch (Exception ex)
                {
                    ConsoleUtil.Warning($"Dump collection failed: {ex.Message}");
                    if (path is not null)
                    {
                        try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to collect a full memory dump from the specified process.
        /// Returns true if the dump was successfully written.
        /// </summary>
        internal static bool TryDumpProcess(Process process, string dumpFilePath)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return TryDumpWithMiniDumpWriteDump(process, dumpFilePath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return TryDumpNetCoreProcess(process, dumpFilePath);
                }
                else
                {
                    Logger.Log($"Dump collection is not supported on {RuntimeInformation.OSDescription}.");
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
