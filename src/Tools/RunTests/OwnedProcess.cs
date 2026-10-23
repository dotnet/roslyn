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

    internal readonly record struct Target(Process Process, string Name, long StartIdentity);

    internal ProcessInfo Info { get; }

    private OwnedProcess(ProcessInfo info, SafeFileHandle? job, string label)
    {
        Info = info;
        _job = job;
        _label = label;
    }

    internal static OwnedProcess Start(ProcessStartInfo command, string label, bool diagnosticOutput = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SafeFileHandle? job = null;
        try
        {
            if (OperatingSystem.IsWindows())
            {
                job = CreateJobObjectW(IntPtr.Zero, null);
                if (job.IsInvalid)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                var limits = new JobLimits { Basic = new BasicJobLimits { LimitFlags = 0x00002000 } };
                if (!SetInformationJobObject(job, 9, ref limits, Marshal.SizeOf<JobLimits>()))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            Logger.Log($"[{label}] ownership startup started: executable={command.FileName}.");
            var process = NativeProcess.Start(command, job,
                line => LogOutput("stdout", line), line => LogOutput("stderr", line), cancellationToken);
            var owned = new OwnedProcess(process, job, label);
            Logger.Log($"[{label}] ownership launcher started: PID={process.Id}, scope={(job is null ? "process group" : "Windows job")}.");
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

    internal async Task<List<Target>> GetProcessesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ConsoleUtil.WriteLine($"[{_label}] discovery started: scope launcher PID={Info.Id}, exited={Info.ExitCode.IsCompleted}.");
        var pids = _job is not null ? GetWindowsProcessIds(_job) : await GetUnixProcessIdsAsync(Info.Id, cancellationToken).ConfigureAwait(false);
        var processes = new List<Target>();
        foreach (var pid in pids)
        {
            Process? process = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                process = Process.GetProcessById(pid);
                // Cache metadata while available, including for cleanup after later exit.
                _ = process.SafeHandle;
                _ = process.StartTime;
                var target = new Target(process, process.ProcessName, ProcessUtil.GetProcessStartIdentity(process));
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
                ConsoleUtil.WriteLine($"[{_label}] owned target: PID={pid}, start={target.StartIdentity}, name={target.Name}.");
                processes.Add(target);
                process = null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                foreach (var target in processes)
                    target.Process.Dispose();
                throw;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception or IOException or UnauthorizedAccessException)
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
        var processes = new List<Target>();
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
            await Task.WhenAll(processes.Select(target => target.Process.WaitForExitAsync())).WaitAsync(CleanupTimeout).ConfigureAwait(false);
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
                process.Process.Dispose();
        }

        ConsoleUtil.WriteLine($"[{_label}] output drain started: launcher PID={Info.Id}, limit={CleanupTimeout}.");
        var outputCanceled = false;
        try
        {
            await Info.Result.WaitAsync(CleanupTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            ConsoleUtil.Warning($"[{_label}] output drain deadline exceeded: {ex}");
            outputCanceled = true;
            Info.StopReadingOutput();
            await Info.Result.WaitAsync(CleanupTimeout).ConfigureAwait(false);
        }
        if (terminationFailure is not null)
            throw new InvalidOperationException($"[{_label}] cleanup did not complete successfully.", terminationFailure);
        ConsoleUtil.WriteLine(outputCanceled
            ? $"[{_label}] cleanup completed; redirected output capture was canceled after its deadline."
            : $"[{_label}] cleanup and output drain completed.");

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

    private static async Task<List<int>> GetUnixProcessIdsAsync(int groupId, CancellationToken cancellationToken)
    {
        var pids = new List<int>();
        if (OperatingSystem.IsLinux())
        {
            foreach (var directory in Directory.EnumerateDirectories("/proc"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!int.TryParse(Path.GetFileName(directory), out var pid))
                    continue;
                try
                {
                    var stat = File.ReadAllText(Path.Combine(directory, "stat"));
                    var fields = stat.Substring(stat.LastIndexOf(')') + 2).Split(' ');
                    if (int.Parse(fields[2], CultureInfo.InvariantCulture) == groupId)
                        pids.Add(pid);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Logger.Log($"Process group inspection failed: PID={pid}: {ex}");
                }
            }
            return pids;
        }

        var ps = ProcessRunner.CreateProcess("/bin/ps", "-A -o pid= -o pgid=", captureOutput: true, displayWindow: false);
        try
        {
            var result = await ps.Result.WaitAsync(CleanupTimeout, cancellationToken).ConfigureAwait(false);
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
            try
            {
                ProcessUtil.KillTree(ps.Process);
                ps.StopReadingOutput();
                await ps.Result.WaitAsync(CleanupTimeout).ConfigureAwait(false);
            }
            finally
            {
                ps.Process.Dispose();
            }
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
    private static extern SafeFileHandle CreateJobObjectW(IntPtr attributes, string? name);
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
