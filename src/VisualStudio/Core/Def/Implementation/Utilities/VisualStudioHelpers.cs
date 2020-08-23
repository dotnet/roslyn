// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal static class VisualStudioHelpers
    {
        public static bool TryAttachTo(int processId)
        {
            var debuggerHostDte = GetDebuggerHostDte();
            if (debuggerHostDte == null)
            {
                return false;
            }

            var process = debuggerHostDte?.Debugger.LocalProcesses.OfType<EnvDTE80.Process2>().FirstOrDefault(p => p.ProcessID == processId);
            if (process != null)
            {
                process.Attach2("Managed");
                return true;
            }

            return false;
        }

        public static void AttachTo(int processId)
        {
            if (!TryAttachTo(processId))
            {
                Debug.Fail($"Failed to attach debugger to {processId}");
            }
        }

        public static EnvDTE.DTE GetDebuggerHostDte()
        {
            var currentProcessId = Process.GetCurrentProcess().Id;
            foreach (var process in Process.GetProcessesByName("devenv"))
            {
                var dte = TryLocateDteForProcess(process);
                if (dte?.Debugger?.DebuggedProcesses?.OfType<EnvDTE.Process>().Any(p => p.ProcessID == currentProcessId) ?? false)
                {
                    return dte;
                }
            }

            return null;
        }

        /// <summary>Locates the DTE object for the specified process.</summary>
        public static EnvDTE.DTE TryLocateDteForProcess(Process process)
        {
            object dte = null;
            var monikers = new OLE.Interop.IMoniker[1];

            GetRunningObjectTable(0, out var runningObjectTable);
            runningObjectTable.EnumRunning(out var enumMoniker);
            CreateBindCtx(0, out var bindContext);

            do
            {
                monikers[0] = null;
                var hresult = enumMoniker.Next(1, monikers, out _);

                if (hresult == VSConstants.S_FALSE)
                {
                    // There's nothing further to enumerate, so fail
                    return null;
                }
                else
                {
                    Marshal.ThrowExceptionForHR(hresult);
                }

                var moniker = monikers[0];
                moniker.GetDisplayName(bindContext, null, out var fullDisplayName);

                // FullDisplayName will look something like: <ProgID>:<ProcessId>
                var displayNameParts = fullDisplayName.Split(':');
                if (!int.TryParse(displayNameParts.Last(), out var displayNameProcessId))
                {
                    continue;
                }

                if (displayNameParts[0].StartsWith("!VisualStudio.DTE", StringComparison.OrdinalIgnoreCase) &&
                    displayNameProcessId == process.Id)
                {
                    runningObjectTable.GetObject(moniker, out dte);
                }
            }
            while (dte == null);

            return (EnvDTE.DTE)dte;
        }

        [DllImport("ole32.dll", PreserveSig = false)]
        public static extern void GetRunningObjectTable(int reserved, [MarshalAs(UnmanagedType.Interface)] out VisualStudio.OLE.Interop.IRunningObjectTable runningObjectTable);

        [DllImport("ole32.dll", PreserveSig = false)]
        public static extern void CreateBindCtx(int reserved, [MarshalAs(UnmanagedType.Interface)] out Microsoft.VisualStudio.OLE.Interop.IBindCtx bindContext);
    }
}
