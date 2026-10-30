// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace RunTests;

internal static class NativeProcess
{
    internal static ProcessInfo Start(ProcessStartInfo command, SafeFileHandle? job,
        Action<string> onOutput, Action<string> onError, CancellationToken cancellationToken)
    {
        if (command.UseShellExecute || command.Arguments.Length != 0 || !command.RedirectStandardOutput || !command.RedirectStandardError)
            throw new ArgumentException("Owned processes require ArgumentList, redirected output, and UseShellExecute=false.", nameof(command));

        cancellationToken.ThrowIfCancellationRequested();
        var inheritability = OperatingSystem.IsWindows() ? HandleInheritability.Inheritable : HandleInheritability.None;
        var output = new AnonymousPipeServerStream(PipeDirection.In, inheritability);
        AnonymousPipeServerStream? error = null;
        try
        {
            error = new AnonymousPipeServerStream(PipeDirection.In, inheritability);
            return OperatingSystem.IsWindows()
                ? StartWindows(command, job!, output, error, onOutput, onError, cancellationToken)
                : StartUnix(command, output, error, onOutput, onError, cancellationToken);
        }
        catch
        {
            output.Dispose();
            error?.Dispose();
            throw;
        }
        finally
        {
            output.DisposeLocalCopyOfClientHandle();
            error?.DisposeLocalCopyOfClientHandle();
        }
    }

    private static ProcessInfo StartWindows(ProcessStartInfo command, SafeFileHandle job,
        AnonymousPipeServerStream output, AnonymousPipeServerStream error,
        Action<string> onOutput, Action<string> onError, CancellationToken cancellationToken)
    {
        var security = new SecurityAttributes { Length = Marshal.SizeOf<SecurityAttributes>(), InheritHandle = true };
        using var input = CreateFileW("NUL", 0x80000000, 3, ref security, 3, 0, IntPtr.Zero);
        if (input.IsInvalid)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Opening null standard input failed.");

        IntPtr attributeSize = IntPtr.Zero;
        _ = InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attributeSize);
        var attributes = Marshal.AllocHGlobal(attributeSize);
        var handles = Marshal.AllocHGlobal(IntPtr.Size * 3);
        var initialized = false;
        try
        {
            if (!InitializeProcThreadAttributeList(attributes, 1, 0, ref attributeSize))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Initializing inherited handle list failed.");
            initialized = true;
            var stdout = output.ClientSafePipeHandle.DangerousGetHandle();
            var stderr = error.ClientSafePipeHandle.DangerousGetHandle();
            Marshal.WriteIntPtr(handles, input.DangerousGetHandle());
            Marshal.WriteIntPtr(handles, IntPtr.Size, stdout);
            Marshal.WriteIntPtr(handles, IntPtr.Size * 2, stderr);
            if (!UpdateProcThreadAttribute(attributes, 0, (IntPtr)0x00020002, handles, (IntPtr)(IntPtr.Size * 3), IntPtr.Zero, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Setting inherited handle list failed.");

            var startup = new StartupInfoEx
            {
                Info = new StartupInfo
                {
                    Size = Marshal.SizeOf<StartupInfoEx>(),
                    Flags = 0x00000100,
                    StandardInput = input.DangerousGetHandle(),
                    StandardOutput = stdout,
                    StandardError = stderr,
                },
                Attributes = attributes,
            };
            var arguments = new StringBuilder();
            AppendArgument(arguments, command.FileName);
            foreach (var argument in command.ArgumentList)
            {
                arguments.Append(' ');
                AppendArgument(arguments, argument);
            }
            var environment = string.Join('\0', command.Environment.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Where(pair => pair.Value is not null).Select(pair => pair.Key + "=" + pair.Value)) + "\0\0";
            // Keep the target suspended until it belongs to the job and its process handle is cached.
            if (!CreateProcessW(command.FileName, arguments, IntPtr.Zero, IntPtr.Zero, true,
                0x00000004 | 0x00000400 | 0x00080000 | 0x08000000, environment,
                string.IsNullOrEmpty(command.WorkingDirectory) ? null : command.WorkingDirectory, ref startup, out var native))
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Creating suspended process '{command.FileName}' failed.");
            using var nativeProcess = new SafeFileHandle(native.Process, ownsHandle: true);
            using var thread = new SafeFileHandle(native.Thread, ownsHandle: true);
            Process? process = null;
            try
            {
                if (!AssignProcessToJobObject(job, nativeProcess))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Assigning PID={native.ProcessId} to its job failed.");
                cancellationToken.ThrowIfCancellationRequested();
                process = Process.GetProcessById(native.ProcessId);
                _ = process.SafeHandle;
                _ = process.StartTime;
                if (ResumeThread(thread) == uint.MaxValue)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Resuming PID={native.ProcessId} failed.");
                return ProcessRunner.TrackProcess(process, command, new StreamReader(output), new StreamReader(error),
                    WaitAsync(process), onOutput, onError);
            }
            catch
            {
                if (!TerminateProcess(nativeProcess, 1))
                    ConsoleUtil.Warning($"Startup cleanup failed for PID={native.ProcessId}: error={Marshal.GetLastWin32Error()}.");
                if (WaitForSingleObject(nativeProcess, 30_000) != 0)
                    ConsoleUtil.Warning($"Startup cleanup did not observe PID={native.ProcessId} exit within thirty seconds.");
                process?.Dispose();
                throw;
            }
        }
        finally
        {
            if (initialized)
                DeleteProcThreadAttributeList(attributes);
            Marshal.FreeHGlobal(attributes);
            Marshal.FreeHGlobal(handles);
        }

        static async Task<int> WaitAsync(Process process)
        {
            await process.WaitForExitAsync().ConfigureAwait(false);
            return process.ExitCode;
        }
    }

    private static ProcessInfo StartUnix(ProcessStartInfo command, AnonymousPipeServerStream output,
        AnonymousPipeServerStream error, Action<string> onOutput, Action<string> onError, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("Owned process launch requires Windows, Linux, or macOS.");

        // Linux's public spawn.h defines inline storage (glibc and musl); Darwin defines pointer-sized opaque types.
        var attributes = Marshal.AllocHGlobal(OperatingSystem.IsMacOS() ? IntPtr.Size : Marshal.SizeOf<LinuxSpawnAttributes>());
        var actions = Marshal.AllocHGlobal(OperatingSystem.IsMacOS() ? IntPtr.Size : Marshal.SizeOf<LinuxSpawnActions>());
        var signals = Marshal.AllocHGlobal(OperatingSystem.IsMacOS() ? sizeof(uint) : Marshal.SizeOf<LinuxSignalSet>());
        var attributesInitialized = false;
        var actionsInitialized = false;
        try
        {
            Check(posix_spawnattr_init(attributes), "initializing spawn attributes");
            attributesInitialized = true;
            Check(posix_spawn_file_actions_init(actions), "initializing spawn file actions");
            actionsInitialized = true;
            Check(posix_spawnattr_setpgroup(attributes, 0), "setting child process group");
            if (sigemptyset(signals) != 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Clearing child signal mask failed.");
            Check(posix_spawnattr_setsigmask(attributes, signals), "setting child signal mask");
            if (sigfillset(signals) != 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Initializing child signal defaults failed.");
            Check(posix_spawnattr_setsigdefault(attributes, signals), "setting child signal defaults");
            Check(posix_spawnattr_setflags(attributes, 0x02 | 0x04 | 0x08), "setting spawn flags");
            Check(posix_spawn_file_actions_addopen(actions, 0, "/dev/null", 0, 0), "redirecting standard input");
            AddPipe(output, 1);
            AddPipe(error, 2);
            if (!string.IsNullOrEmpty(command.WorkingDirectory))
                Check(posix_spawn_file_actions_addchdir_np(actions, command.WorkingDirectory), "setting child working directory");

            using var argv = new Utf8Vector(new[] { command.FileName }.Concat(command.ArgumentList).ToArray());
            using var environment = new Utf8Vector(command.Environment.Where(pair => pair.Value is not null)
                .Select(pair => pair.Key + "=" + pair.Value).ToArray());
            cancellationToken.ThrowIfCancellationRequested();
            Check(posix_spawnp(out var pid, command.FileName, actions, attributes, argv.Pointer, environment.Pointer), "spawning " + command.FileName);
            Process? process = null;
            try
            {
                // Do not reap until the Process and start identity have been captured, even for an immediate exit.
                process = Process.GetProcessById(pid);
                _ = process.SafeHandle;
                _ = process.StartTime;
                cancellationToken.ThrowIfCancellationRequested();
                return ProcessRunner.TrackProcess(process, command, new StreamReader(output), new StreamReader(error),
                    WaitUnixAsync(pid), onOutput, onError);
            }
            catch
            {
                if (kill(-pid, 9) != 0 && Marshal.GetLastWin32Error() != 3)
                    ConsoleUtil.Warning($"Startup group cleanup failed: PID={pid}, error={Marshal.GetLastWin32Error()}.");
                var reap = WaitUnixAsync(pid);
                if (!reap.Wait(TimeSpan.FromSeconds(30)))
                    ConsoleUtil.Warning($"Startup cleanup did not reap PID={pid} within thirty seconds.");
                process?.Dispose();
                throw;
            }
        }
        finally
        {
            if (actionsInitialized)
                LogDestroy(posix_spawn_file_actions_destroy(actions), "file actions");
            if (attributesInitialized)
                LogDestroy(posix_spawnattr_destroy(attributes), "attributes");
            Marshal.FreeHGlobal(attributes);
            Marshal.FreeHGlobal(actions);
            Marshal.FreeHGlobal(signals);
        }

        void AddPipe(AnonymousPipeServerStream pipe, int destination)
        {
            var read = pipe.SafePipeHandle.DangerousGetHandle().ToInt32();
            var write = pipe.ClientSafePipeHandle.DangerousGetHandle().ToInt32();
            Check(posix_spawn_file_actions_adddup2(actions, write, destination), "redirecting child output");
            Check(posix_spawn_file_actions_addclose(actions, read), "closing child pipe reader");
            Check(posix_spawn_file_actions_addclose(actions, write), "closing child pipe writer");
        }

        static void LogDestroy(int error, string operation)
        {
            if (error != 0)
                ConsoleUtil.Warning($"Destroying spawn {operation} failed: {new Win32Exception(error)}.");
        }
    }

    private static async Task<int> WaitUnixAsync(int pid)
    {
        while (true)
        {
            var result = waitpid(pid, out var status, 1);
            if (result == pid)
            {
                var signal = status & 0x7f;
                return signal == 0 ? (status >> 8) & 0xff : 128 + signal;
            }
            if (result < 0 && Marshal.GetLastWin32Error() != 4)
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Reaping PID={pid} failed.");
            await Task.Delay(50).ConfigureAwait(false);
        }
    }

    private static void Check(int error, string operation)
    {
        // posix_spawn APIs return errno directly, rather than setting the thread's errno.
        if (error != 0)
            throw new Win32Exception(error, $"Native process launch failed while {operation}.");
    }

    private static void AppendArgument(StringBuilder builder, string value)
    {
        builder.Append('"');
        var slashes = 0;
        foreach (var ch in value)
        {
            if (ch == '\\')
            {
                slashes++;
                continue;
            }
            builder.Append('\\', ch == '"' ? slashes * 2 + 1 : slashes);
            builder.Append(ch);
            slashes = 0;
        }
        builder.Append('\\', slashes * 2);
        builder.Append('"');
    }

    private sealed class Utf8Vector : IDisposable
    {
        private readonly IntPtr[] _strings;
        internal IntPtr Pointer { get; }

        internal Utf8Vector(string[] values)
        {
            _strings = new IntPtr[values.Length];
            Pointer = Marshal.AllocHGlobal((values.Length + 1) * IntPtr.Size);
            try
            {
                for (var i = 0; i < values.Length; i++)
                {
                    if (values[i].Contains('\0'))
                        throw new ArgumentException("Process arguments and environment values cannot contain null characters.");
                    _strings[i] = Marshal.StringToCoTaskMemUTF8(values[i]);
                    Marshal.WriteIntPtr(Pointer, i * IntPtr.Size, _strings[i]);
                }
                Marshal.WriteIntPtr(Pointer, values.Length * IntPtr.Size, IntPtr.Zero);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            foreach (var value in _strings)
                Marshal.FreeCoTaskMem(value);
            Marshal.FreeHGlobal(Pointer);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SecurityAttributes
    {
        public int Length;
        public IntPtr Descriptor;
        [MarshalAs(UnmanagedType.Bool)] public bool InheritHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInfo
    {
        public int Size;
        public IntPtr Reserved, Desktop, Title;
        public int X, Y, XSize, YSize, XCountChars, YCountChars, FillAttribute, Flags;
        public short ShowWindow, ReservedSize;
        public IntPtr ReservedData, StandardInput, StandardOutput, StandardError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInfoEx
    {
        public StartupInfo Info;
        public IntPtr Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public IntPtr Process, Thread;
        public int ProcessId, ThreadId;
    }

    [StructLayout(LayoutKind.Sequential, Size = 128)]
    private struct LinuxSignalSet { public ulong First; }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct LinuxSpawnPadding { public IntPtr First; }

    [StructLayout(LayoutKind.Sequential)]
    private struct LinuxSpawnAttributes
    {
        public int Flags, Group;
        public LinuxSignalSet Defaults, Mask;
        public int Priority, Policy;
        public LinuxSpawnPadding Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LinuxSpawnActions
    {
        public int Allocated, Used;
        public IntPtr Actions;
        public LinuxSpawnPadding Padding;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateProcessW(string application, StringBuilder commandLine, IntPtr processAttributes,
        IntPtr threadAttributes, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint flags, string environment,
        string? directory, ref StartupInfoEx startup, out ProcessInformation process);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string name, uint access, uint share, ref SecurityAttributes security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InitializeProcThreadAttributeList(IntPtr attributes, int count, int flags, ref IntPtr size);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UpdateProcThreadAttribute(IntPtr attributes, uint flags, IntPtr attribute, IntPtr value, IntPtr size, IntPtr previous, IntPtr returnSize);
    [DllImport("kernel32.dll")]
    private static extern void DeleteProcThreadAttributeList(IntPtr attributes);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AssignProcessToJobObject(SafeFileHandle job, SafeFileHandle process);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint ResumeThread(SafeFileHandle thread);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateProcess(SafeFileHandle process, uint code);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(SafeFileHandle handle, uint milliseconds);
    [DllImport("libc")]
    private static extern int posix_spawnattr_init(IntPtr attributes);
    [DllImport("libc")]
    private static extern int posix_spawnattr_destroy(IntPtr attributes);
    [DllImport("libc")]
    private static extern int posix_spawnattr_setpgroup(IntPtr attributes, int group);
    [DllImport("libc")]
    private static extern int posix_spawnattr_setflags(IntPtr attributes, short flags);
    [DllImport("libc")]
    private static extern int posix_spawnattr_setsigmask(IntPtr attributes, IntPtr mask);
    [DllImport("libc")]
    private static extern int posix_spawnattr_setsigdefault(IntPtr attributes, IntPtr mask);
    [DllImport("libc", SetLastError = true)]
    private static extern int sigemptyset(IntPtr mask);
    [DllImport("libc", SetLastError = true)]
    private static extern int sigfillset(IntPtr mask);
    [DllImport("libc")]
    private static extern int posix_spawn_file_actions_init(IntPtr actions);
    [DllImport("libc")]
    private static extern int posix_spawn_file_actions_destroy(IntPtr actions);
    [DllImport("libc")]
    private static extern int posix_spawn_file_actions_adddup2(IntPtr actions, int source, int destination);
    [DllImport("libc")]
    private static extern int posix_spawn_file_actions_addclose(IntPtr actions, int descriptor);
    [DllImport("libc")]
    private static extern int posix_spawn_file_actions_addopen(IntPtr actions, int descriptor,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags, uint mode);
    [DllImport("libc")]
    private static extern int posix_spawn_file_actions_addchdir_np(IntPtr actions, [MarshalAs(UnmanagedType.LPUTF8Str)] string directory);
    [DllImport("libc")]
    private static extern int posix_spawnp(out int pid, [MarshalAs(UnmanagedType.LPUTF8Str)] string file,
        IntPtr actions, IntPtr attributes, IntPtr argv, IntPtr environment);
    [DllImport("libc", SetLastError = true)]
    private static extern int waitpid(int pid, out int status, int options);
    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int signal);
}
