// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace RunTests;

/// <summary>
/// Starts the launcher inside an ownership scope before it can create children. The scope survives
/// launcher exit and Unix reparenting; neither process names nor a live ancestor establish ownership.
/// </summary>
internal sealed class OwnedProcess : IDisposable, IAsyncDisposable
{
    internal static readonly TimeSpan CleanupTimeout = TimeSpan.FromSeconds(30);
    private readonly SafeFileHandle? _job;
    private readonly string _label;
    private readonly object _cleanupGate = new();
    private Task? _cleanupTask;

    internal ProcessInfo Info { get; }

    private OwnedProcess(ProcessInfo info, SafeFileHandle? job, string label)
    {
        Info = info;
        _job = job;
        _label = label;
    }

    internal static async Task<OwnedProcess> StartAsync(ProcessStartInfo command, string dotnetPath, string label, bool diagnosticOutput = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SafeFileHandle? job = null;
        var scopeName = "-";
        try
        {
            if (OperatingSystem.IsWindows())
            {
                scopeName = @"Local\Roslyn.RunTests." + Guid.NewGuid().ToString("N");
                job = CreateJobObjectW(IntPtr.Zero, scopeName);
                if (job.IsInvalid)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                var limits = new JobLimits { Basic = new BasicJobLimits { LimitFlags = 0x00002000 } };
                if (!SetInformationJobObject(job, 9, ref limits, Marshal.SizeOf<JobLimits>()))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var info = new ProcessStartInfo(dotnetPath)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                WorkingDirectory = command.WorkingDirectory,
            };
            // Target startup hooks must not run before the wrapper establishes ownership.
            info.Environment.Remove("DOTNET_STARTUP_HOOKS");
            info.ArgumentList.Add(typeof(Program).Assembly.Location);
            info.ArgumentList.Add("--run-owned-process");
            info.ArgumentList.Add(scopeName);
            info.ArgumentList.Add(command.FileName);
            info.ArgumentList.Add(command.ArgumentList.Count == 0 ? "arguments" : "argument-list");
            if (command.ArgumentList.Count == 0)
                info.ArgumentList.Add(command.Arguments);
            else
                foreach (var argument in command.ArgumentList)
                    info.ArgumentList.Add(argument);

            var process = ProcessRunner.CreateProcess(info,
                onOutputDataReceived: line => LogOutput("stdout", line),
                onErrorDataReceived: line => LogOutput("stderr", line));
            var owned = new OwnedProcess(process, job, label);
            Logger.Log($"[{label}] ownership startup started: launcher PID={process.Id}, limit={CleanupTimeout}.");
            long startIdentity;
            try
            {
                // The wrapper cannot exit normally until it receives the target environment.
                _ = process.Process.StartTime;
                startIdentity = ProcessUtil.GetProcessStartIdentity(process.Process);
                await process.Process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(command.Environment))
                    .WaitAsync(CleanupTimeout, cancellationToken).ConfigureAwait(false);
                process.Process.StandardInput.Close();
            }
            catch (Exception ex)
            {
                ConsoleUtil.Warning($"[{label}] ownership startup failed: PID={process.Id}: {ex}");
                await owned.DisposeAsync().ConfigureAwait(false);
                process.Process.Dispose();
                throw;
            }
            Logger.Log($"[{label}] ownership launcher started: PID={process.Id}, start={startIdentity}, scope={scopeName}.");
            return owned;

            void LogOutput(string stream, string line)
            {
                if (diagnosticOutput)
                    ConsoleUtil.WriteLine($"[{label}] {stream}: {line}");
                else
                    Logger.Log($"[{label}] {stream}: {line}");
            }
        }
        catch
        {
            job?.Dispose();
            throw;
        }
    }

    internal static int RunHelper(string[] args)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var job = OpenJobObjectW(0x0001, false, args[1]);
                if (job.IsInvalid || !AssignProcessToJobObject(job, Process.GetCurrentProcess().Handle))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            else if (setpgid(0, 0) != 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var info = new ProcessStartInfo(args[2]) { UseShellExecute = false };
            var environment = JsonSerializer.Deserialize<Dictionary<string, string?>>(Console.ReadLine()
                ?? throw new InvalidOperationException("Ownership wrapper did not receive the target environment."))
                ?? throw new InvalidOperationException("Ownership wrapper received a null target environment.");
            info.Environment.Clear();
            foreach (var pair in environment)
                info.Environment[pair.Key] = pair.Value;
            if (Environment.GetEnvironmentVariable("ROSLYN_TIMEOUT_PROBE_MODE") is not null)
                info.Environment["ROSLYN_TIMEOUT_PROBE_LAUNCHER_PID"] = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
            if (args[3] == "arguments")
                info.Arguments = args[4];
            else
                foreach (var argument in args.Skip(4))
                    info.ArgumentList.Add(argument);

            // Inherit the wrapper's pipes. Waiting for this process must not also wait for pipe EOF.
            using var process = Process.Start(info)!;
            Console.Error.WriteLine($"Ownership scope ready: launcher={Environment.ProcessId}, target={process.Id}.");
            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Ownership scope failed: {ex}");
            return Program.ExitFailure;
        }
    }

    internal async Task<List<Process>> GetProcessesAsync()
    {
        ConsoleUtil.WriteLine($"[{_label}] discovery started: scope launcher PID={Info.Id}, exited={Info.Process.HasExited}.");
        var pids = _job is not null ? GetWindowsProcessIds(_job) : await GetUnixProcessIdsAsync(Info.Id).ConfigureAwait(false);
        var processes = new List<Process>();
        foreach (var pid in pids)
        {
            Process? process = null;
            try
            {
                process = Process.GetProcessById(pid);
                // Cache metadata while available, including for cleanup after later exit.
                _ = process.SafeHandle;
                _ = process.StartTime;
                if (_job is not null)
                {
                    if (!IsProcessInJob(process.Handle, _job, out var isOwned))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    if (!isOwned)
                        throw new InvalidOperationException($"PID={pid} no longer belongs to the work-item job.");
                }
                else if (getpgid(pid) != Info.Id)
                {
                    throw new InvalidOperationException($"PID={pid} no longer belongs to process group {Info.Id}.");
                }
                ConsoleUtil.WriteLine($"[{_label}] owned target: PID={pid}, start={ProcessUtil.GetProcessStartIdentity(process)}, name={process.ProcessName}.");
                processes.Add(process);
                process = null;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception or IOException)
            {
                ConsoleUtil.Warning($"[{_label}] target inspection failed: PID={pid}: {ex}");
            }
            finally
            {
                process?.Dispose();
            }
        }
        ConsoleUtil.WriteLine($"[{_label}] discovery completed: {processes.Count} owned targets.");
        return processes;
    }

    internal Task CleanupAsync()
    {
        lock (_cleanupGate)
            return _cleanupTask ??= CleanupCoreAsync();
    }

    private async Task CleanupCoreAsync()
    {
        ConsoleUtil.WriteLine($"[{_label}] cleanup started: scope launcher PID={Info.Id}.");
        var processes = new List<Process>();
        Exception? terminationFailure = null;
        try
        {
            processes = await GetProcessesAsync().ConfigureAwait(false);
            if (_job is not null)
            {
                if (!TerminateJobObject(_job, 1))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            else
            {
                KillGroup();
            }
            ProcessUtil.KillTree(Info.Process);
            if (_job is null)
                KillGroup();
            await Task.WhenAll(processes.Select(process => process.WaitForExitAsync())).WaitAsync(CleanupTimeout).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            terminationFailure = ex;
            ConsoleUtil.Warning($"[{_label}] target termination failed: {ex}");
            // Discovery failure must not prevent scope-level termination.
            if (_job is not null && !TerminateJobObject(_job, 1))
                ConsoleUtil.Warning($"[{_label}] job termination retry failed: error={Marshal.GetLastWin32Error()}.");
            ProcessUtil.KillTree(Info.Process);
            if (_job is null)
            {
                try
                {
                    KillGroup();
                }
                catch (Win32Exception retryException)
                {
                    ConsoleUtil.Warning($"[{_label}] process group termination retry failed: {retryException}");
                }
            }
        }
        finally
        {
            foreach (var process in processes)
                process.Dispose();
        }

        ConsoleUtil.WriteLine($"[{_label}] output drain started: launcher PID={Info.Id}, limit={CleanupTimeout}.");
        try
        {
            await Info.Result.WaitAsync(CleanupTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            ConsoleUtil.Warning($"[{_label}] output drain deadline exceeded: {ex}");
            Info.StopReadingOutput();
            await Info.Result.WaitAsync(CleanupTimeout).ConfigureAwait(false);
        }
        if (terminationFailure is not null)
            throw new InvalidOperationException($"[{_label}] cleanup did not complete successfully.", terminationFailure);
        ConsoleUtil.WriteLine($"[{_label}] cleanup and output drain completed.");

        void KillGroup()
        {
            if (kill(-Info.Id, 9) != 0)
            {
                var error = Marshal.GetLastWin32Error();
                if (error != 3)
                    throw new Win32Exception(error);
            }
        }
    }

    private static List<int> GetWindowsProcessIds(SafeFileHandle job)
    {
        var capacity = 64;
        while (true)
        {
            var size = checked(8 + capacity * IntPtr.Size);
            var buffer = Marshal.AllocHGlobal(size);
            try
            {
                if (QueryInformationJobObject(job, 3, buffer, size, out _))
                {
                    var count = Marshal.ReadInt32(buffer, 4);
                    var pids = new List<int>(count);
                    for (var i = 0; i < count; i++)
                        pids.Add(checked((int)Marshal.ReadIntPtr(buffer, 8 + i * IntPtr.Size)));
                    return pids;
                }
                var error = Marshal.GetLastWin32Error();
                if (error != 234)
                    throw new Win32Exception(error);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            capacity = checked(capacity * 2);
        }
    }

    private static async Task<List<int>> GetUnixProcessIdsAsync(int groupId)
    {
        var pids = new List<int>();
        if (OperatingSystem.IsLinux())
        {
            foreach (var directory in Directory.EnumerateDirectories("/proc"))
            {
                if (!int.TryParse(Path.GetFileName(directory), out var pid))
                    continue;
                try
                {
                    var stat = File.ReadAllText(Path.Combine(directory, "stat"));
                    var fields = stat.Substring(stat.LastIndexOf(')') + 2).Split(' ');
                    if (int.Parse(fields[2], CultureInfo.InvariantCulture) == groupId)
                        pids.Add(pid);
                }
                catch (IOException ex)
                {
                    Logger.Log($"Process group inspection raced process exit: PID={pid}: {ex.Message}");
                }
            }
            return pids;
        }

        var ps = ProcessRunner.CreateProcess("/bin/ps", "-A -o pid= -o pgid=", captureOutput: true, displayWindow: false);
        try
        {
            var result = await ps.Result.WaitAsync(CleanupTimeout).ConfigureAwait(false);
            if (result.ExitCode != 0)
                throw new InvalidOperationException($"ps failed: {string.Join(Environment.NewLine, result.ErrorLines)}");
            foreach (var line in result.OutputLines)
            {
                var fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (int.Parse(fields[1], CultureInfo.InvariantCulture) == groupId)
                    pids.Add(int.Parse(fields[0], CultureInfo.InvariantCulture));
            }
            return pids;
        }
        finally
        {
            ProcessUtil.KillTree(ps.Process);
            ps.StopReadingOutput();
            ps.Process.Dispose();
        }
    }

    public void Dispose()
    {
        _job?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await CleanupAsync().ConfigureAwait(false);
        }
        finally
        {
            Dispose();
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateJobObjectW(IntPtr attributes, string name);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle OpenJobObjectW(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, string name);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AssignProcessToJobObject(SafeFileHandle job, IntPtr process);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsProcessInJob(IntPtr process, SafeFileHandle job, [MarshalAs(UnmanagedType.Bool)] out bool result);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryInformationJobObject(SafeFileHandle job, int informationClass, IntPtr information, int length, out int returnLength);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateJobObject(SafeFileHandle job, uint exitCode);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetInformationJobObject(SafeFileHandle job, int informationClass, ref JobLimits information, int length);
    [DllImport("libc", SetLastError = true)]
    private static extern int setpgid(int pid, int pgid);
    [DllImport("libc", SetLastError = true)]
    private static extern int getpgid(int pid);
    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int signal);

    [StructLayout(LayoutKind.Sequential)]
    private struct BasicJobLimits
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobLimits
    {
        public BasicJobLimits Basic;
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }
}
