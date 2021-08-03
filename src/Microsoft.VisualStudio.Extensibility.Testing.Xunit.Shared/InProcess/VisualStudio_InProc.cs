// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.InProcess
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Xunit.Harness;
    using File = System.IO.File;
    using IVsUIShell = Microsoft.VisualStudio.Shell.Interop.IVsUIShell;
    using OLECMDEXECOPT = Microsoft.VisualStudio.OLE.Interop.OLECMDEXECOPT;
    using Path = System.IO.Path;
    using SVsUIShell = Microsoft.VisualStudio.Shell.Interop.SVsUIShell;

    internal partial class VisualStudio_InProc : InProcComponent
    {
        private VisualStudio_InProc()
        {
        }

        public static VisualStudio_InProc Create()
            => new VisualStudio_InProc();

        public new void WaitForSystemIdle()
            => InProcComponent.WaitForSystemIdle();

        public new bool IsCommandAvailable(string commandName)
            => InProcComponent.IsCommandAvailable(commandName);

        public new void ExecuteCommand(string commandName, string args = "")
            => InProcComponent.ExecuteCommand(commandName, args);

        public void AddCodeBaseDirectory(string directory)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                string path = Path.Combine(directory, new AssemblyName(e.Name).Name + ".dll");
                if (File.Exists(path))
                {
                    return Assembly.LoadFrom(path);
                }

                return null;
            };
        }

        public void ActivateMainWindow()
        {
            InvokeOnUIThread(() =>
            {
                var dte = GetDTE();

                var activeVisualStudioWindow = (IntPtr)dte.ActiveWindow.HWnd;
                Debug.WriteLine($"DTE.ActiveWindow.HWnd = {activeVisualStudioWindow}");

                if (activeVisualStudioWindow == IntPtr.Zero)
                {
                    activeVisualStudioWindow = (IntPtr)dte.MainWindow.HWnd;
                    Debug.WriteLine($"DTE.MainWindow.HWnd = {activeVisualStudioWindow}");
                }

                SetForegroundWindow(activeVisualStudioWindow);
            });
        }

        public void Quit()
        {
            BeginInvokeOnUIThread(() =>
            {
                var shell = GetGlobalService<SVsUIShell, IVsUIShell>();
                var cmdGroup = VSConstants.GUID_VSStandardCommandSet97;
                var cmdId = VSConstants.VSStd97CmdID.Exit;
                var cmdExecOpt = OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER;
                Marshal.ThrowExceptionForHR(shell.PostExecCommand(cmdGroup, (uint)cmdId, (uint)cmdExecOpt, pvaIn: null));
            });
        }

        private static void SetForegroundWindow(IntPtr window)
        {
            var activeWindow = NativeMethods.GetLastActivePopup(window);
            activeWindow = NativeMethods.IsWindowVisible(activeWindow) ? activeWindow : window;
            NativeMethods.SwitchToThisWindow(activeWindow, true);

            if (!NativeMethods.SetForegroundWindow(activeWindow))
            {
                if (!NativeMethods.AllocConsole())
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }

                try
                {
                    var consoleWindow = NativeMethods.GetConsoleWindow();
                    if (consoleWindow == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("Failed to obtain the console window.");
                    }

                    if (!NativeMethods.SetWindowPos(consoleWindow, IntPtr.Zero, 0, 0, 0, 0, NativeMethods.SWP_NOZORDER))
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }
                }
                finally
                {
                    if (!NativeMethods.FreeConsole())
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }
                }

                if (!NativeMethods.SetForegroundWindow(activeWindow))
                {
                    throw new InvalidOperationException("Failed to set the foreground window.");
                }
            }
        }
    }
}
