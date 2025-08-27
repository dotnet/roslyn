// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using DTE = EnvDTE.DTE;

namespace Microsoft.VisualStudio.LanguageServices;

/// <summary>
/// This class is a debugging aid to help attach the VS debugger to the Roslyn ServiceHub process.
/// As suggested in https://github.com/dotnet/roslyn/pull/53601#pullrequestreview-665903594
/// </summary>
internal static class ServiceHubDebuggingService
{
    /// <summary>
    /// Attaches the Roslyn ServiceHub process to the attached VS debugger when the appropriate environment variable is set.
    /// </summary>
    /// <remarks>
    /// If the ServiceHub process is not running yet, this method will listen for its creation and attach it when it starts. 
    /// </remarks>
    public static void TryAttachToServiceHub()
    {
        if (System.Diagnostics.Debugger.IsAttached && Environment.GetEnvironmentVariable("ATTACH_TO_ROSLYN_SERVICEHUB") == "1")
        {
            var hostProcess = Process.GetCurrentProcess();
            var serviceHubProcess = Process
                .GetProcessesByName("ServiceHub.RoslynCodeAnalysisService")
                .FirstOrDefault(p => IsChildProcess(p, hostProcess));

            // We do not really expect to the service to be running at this point but are prepared to handle it.
            if (serviceHubProcess != null)
            {
                AttachServiceHubToDebugger(serviceHubProcess);
            }
            else
            {
                ListenForProcessStart(hostProcess);
            }
        }

        return;

        static void ListenForProcessStart(Process hostProcess)
        {
            var query = "SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process'";
            var watcher = new ManagementEventWatcher(new WqlEventQuery(query));

            watcher.EventArrived += (sender, e) =>
            {
                var process = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                if (!"ServiceHub.RoslynCodeAnalysisService.exe".Equals(process["Name"]))
                    return;

                var serviceHubProcess = Process.GetProcessById(Convert.ToInt32(process["ProcessId"]));
                if (!IsChildProcess(serviceHubProcess, hostProcess))
                    return;

                AttachServiceHubToDebugger(serviceHubProcess);

                watcher.Stop();
                watcher.Dispose();
            };

            watcher.Start();
        }

        static void AttachServiceHubToDebugger(Process serviceHubProcess)
        {
            // Since this can be called after a delay, check that the debugger is still attached.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Locate the DTE for the debugger host process (devenv.exe) that launched us.
                var debuggerHostDte = GetDebuggerHostDte();
                if (debuggerHostDte == null)
                    return;

                var localProcess = debuggerHostDte.Debugger
                    .LocalProcesses.OfType<EnvDTE80.Process2>()
                    .FirstOrDefault(p => p.ProcessID == serviceHubProcess.Id);
                localProcess?.Attach2("Managed (.NET Core, .NET 5+)");
            }
        }
    }

    // This is based on https://github.com/dotnet/roslyn/blob/075251e1692937f3af207887de0ba7c326ef888a/src/VisualStudio/IntegrationTest/TestUtilities/IntegrationHelper.cs#L407
    private static DTE? GetDebuggerHostDte()
    {
        var currentProcessId = Process.GetCurrentProcess().Id;
        foreach (var process in Process.GetProcessesByName("devenv"))
        {
            var dte = TryLocateDteForProcess(process);
            if (dte?.Debugger?.DebuggedProcesses?.OfType<EnvDTE.Process>().Any(p => p.ProcessID == currentProcessId) ?? false)
                return dte;
        }

        return null;

        static DTE? TryLocateDteForProcess(Process process)
        {
            object? dte = null;
            var monikers = new IMoniker?[1];

            NativeMethods.GetRunningObjectTable(0, out var runningObjectTable);
            runningObjectTable.EnumRunning(out var enumMoniker);
            NativeMethods.CreateBindCtx(0, out var bindContext);

            do
            {
                monikers[0] = null;
                var hresult = enumMoniker.Next(1, monikers, default);

                if (hresult == VSConstants.S_FALSE)
                    return null;

                Marshal.ThrowExceptionForHR(hresult);

                if (monikers is not [{ } moniker])
                    continue;

                moniker.GetDisplayName(bindContext, null, out var fullDisplayName);

                // FullDisplayName will look something like: <ProgID>:<ProcessId>
                if (!fullDisplayName.StartsWith("!VisualStudio.DTE", StringComparison.OrdinalIgnoreCase))
                    continue;

                var displayNameParts = fullDisplayName.Split(':');
                if (!int.TryParse(displayNameParts.Last(), out var displayNameProcessId))
                    continue;

                if (displayNameProcessId == process.Id)
                    runningObjectTable.GetObject(moniker, out dte);
            }
            while (dte == null);

            return (DTE?)dte;
        }
    }

    // This is based on https://github.com/dotnet/msbuild/blob/b499c93e95f440b98967b8d5edd910ee8556f504/src/Shared/NativeMethodsShared.cs#L1127
    private static bool IsChildProcess(Process possibleChild, Process parent)
    {
        // Hold the child process handle open so that children cannot die and restart with a different parent after we've started looking at it.
        // This way, any handle we pass back is guaranteed to be one of our actual children.
        var childHandle = NativeMethods.OpenProcess(NativeMethods.eDesiredAccess.PROCESS_QUERY_INFORMATION, false, possibleChild.Id);
        if (childHandle.IsInvalid)
            return false;

        try
        {
            // The child process must start after the parent process.
            if (possibleChild.StartTime < parent.StartTime)
                return false;

            var childParentProcessId = GetParentProcessId(possibleChild.Id);
            return parent.Id == childParentProcessId;
        }
        finally
        {
            childHandle.Dispose();
        }

        // Returns the parent process id for the specified process or zero if it cannot be determined.
        static int GetParentProcessId(int processId)
        {
            var hProcess = NativeMethods.OpenProcess(NativeMethods.eDesiredAccess.PROCESS_QUERY_INFORMATION, false, processId);
            if (hProcess.IsInvalid)
                return 0;

            try
            {
                // UNDONE: NtQueryInformationProcess will fail if we are not elevated and other process is.
                // Advice is to change to use ToolHelp32 API's. For now just return zero and worst case we will not kill some children.
                var processBasicInformation = new NativeMethods.PROCESS_BASIC_INFORMATION();
                var pSize = 0;

                var result = NativeMethods.NtQueryInformationProcess(hProcess, NativeMethods.PROCESSINFOCLASS.ProcessBasicInformation, ref processBasicInformation, processBasicInformation.Size, ref pSize);
                return result == VSConstants.S_OK ? (int)processBasicInformation.InheritedFromUniqueProcessId : 0;
            }
            finally
            {
                hProcess.Dispose();
            }
        }
    }

    internal static class NativeMethods
    {
        private const string Kernel32 = "kernel32.dll";
        private const string Ole32 = "ole32.dll";
        private const string NtDll = "ntdll.dll";

        #region kernel32.dll

        [DllImport(Kernel32)]
        public static extern SafeProcessHandle OpenProcess(eDesiredAccess dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        public enum eDesiredAccess : int
        {
            DELETE = 0x00010000,
            READ_CONTROL = 0x00020000,
            WRITE_DAC = 0x00040000,
            WRITE_OWNER = 0x00080000,
            SYNCHRONIZE = 0x00100000,
            STANDARD_RIGHTS_ALL = 0x001F0000,

            PROCESS_TERMINATE = 0x0001,
            PROCESS_CREATE_THREAD = 0x0002,
            PROCESS_SET_SESSIONID = 0x0004,
            PROCESS_VM_OPERATION = 0x0008,
            PROCESS_VM_READ = 0x0010,
            PROCESS_VM_WRITE = 0x0020,
            PROCESS_DUP_HANDLE = 0x0040,
            PROCESS_CREATE_PROCESS = 0x0080,
            PROCESS_SET_QUOTA = 0x0100,
            PROCESS_SET_INFORMATION = 0x0200,
            PROCESS_QUERY_INFORMATION = 0x0400,
            PROCESS_ALL_ACCESS = SYNCHRONIZE | 0xFFF
        }

        #endregion

        #region ole32.dll

        [DllImport(Ole32, PreserveSig = false)]
        public static extern void CreateBindCtx(int reserved, [MarshalAs(UnmanagedType.Interface)] out IBindCtx bindContext);

        [DllImport(Ole32, PreserveSig = false)]
        public static extern void GetRunningObjectTable(int reserved, [MarshalAs(UnmanagedType.Interface)] out IRunningObjectTable runningObjectTable);

        #endregion

        #region ntdll.dll

        [DllImport(NtDll)]
        public static extern int NtQueryInformationProcess(SafeProcessHandle hProcess, PROCESSINFOCLASS pic, ref PROCESS_BASIC_INFORMATION pbi, int cb, ref int pSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public IntPtr BasePriority;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;

            public int Size
            {
                get { return (6 * IntPtr.Size); }
            }
        };

        public enum PROCESSINFOCLASS : int
        {
            ProcessBasicInformation = 0,
            ProcessQuotaLimits,
            ProcessIoCounters,
            ProcessVmCounters,
            ProcessTimes,
            ProcessBasePriority,
            ProcessRaisePriority,
            ProcessDebugPort,
            ProcessExceptionPort,
            ProcessAccessToken,
            ProcessLdtInformation,
            ProcessLdtSize,
            ProcessDefaultHardErrorMode,
            ProcessIoPortHandlers, // Note: this is kernel mode only
            ProcessPooledUsageAndLimits,
            ProcessWorkingSetWatch,
            ProcessUserModeIOPL,
            ProcessEnableAlignmentFaultFixup,
            ProcessPriorityClass,
            ProcessWx86Information,
            ProcessHandleCount,
            ProcessAffinityMask,
            ProcessPriorityBoost,
            MaxProcessInfoClass
        };

        #endregion

    }
}
