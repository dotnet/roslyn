// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.InProcess
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.VisualStudio.Shell.Interop;
    using Windows.Win32;
    using Windows.Win32.Foundation;
    using Windows.Win32.UI.WindowsAndMessaging;
    using Xunit.Harness;
    using File = System.IO.File;
    using IVsUIShell = Microsoft.VisualStudio.Shell.Interop.IVsUIShell;
    using OLECMDEXECOPT = Microsoft.VisualStudio.OLE.Interop.OLECMDEXECOPT;
    using Path = System.IO.Path;
    using SVsActivityLog = Microsoft.VisualStudio.Shell.Interop.SVsActivityLog;
    using SVsUIShell = Microsoft.VisualStudio.Shell.Interop.SVsUIShell;

    internal partial class VisualStudio_InProc : InProcComponent
    {
        /// <summary>
        /// We need to add a root element so that the Visual Studio activity log file is valid xml and can easily be consumed using Xml.Linq.
        /// </summary>
        private const string VisualStudioActivityLogRoot = "entries";

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

                nint activeWindow = dte.ActiveWindow.HWnd;
                var activeVisualStudioWindow = (HWND)activeWindow;
                Debug.WriteLine($"DTE.ActiveWindow.HWnd = {activeVisualStudioWindow}");
                if (activeVisualStudioWindow != IntPtr.Zero)
                {
                    if (TrySetForegroundWindow(activeVisualStudioWindow))
                    {
                        return;
                    }
                }

                nint mainWindow = dte.MainWindow.HWnd;
                activeVisualStudioWindow = (HWND)mainWindow;
                Debug.WriteLine($"DTE.MainWindow.HWnd = {activeVisualStudioWindow}");
                if (!TrySetForegroundWindow(activeVisualStudioWindow))
                {
                    throw new InvalidOperationException("Failed to set the foreground window.");
                }
            });
        }

        /// <summary>
        /// Get the Visual Studio in-memory activity log.
        /// This is always turned on and has a rolling buffer of the last 100 entries, and the first 10 entries, which have general configuration information.
        /// </summary>
        /// <returns>null if no data; error string if error; log info if valid data.</returns>
        internal static string? GetInMemoryActivityLog()
        {
            return InvokeOnUIThread(() =>
            {
                // We get an IVsActivityLogDumper interface by getting the SVsActivityLogService and type casting it to IVSActivityLogDumper
                if (GlobalServiceProvider.ServiceProvider.GetService(typeof(SVsActivityLog)) is not IVsActivityLogDumper vsActivityLogDumper)
                {
                    return null;
                }

                // And then using it, get a string that contains the VS in-memory activity log.
                // NOTE: this will return empty if the user explicitly enabled logging (in that case there is no in-memory activity log as it's written to a file)
                var vsActivityLogContents = vsActivityLogDumper.GetActivityLogBuffer();
                if (string.IsNullOrWhiteSpace(vsActivityLogContents))
                {
                    return null;
                }

                // The API returns the log with some 0x0 characters at the end that make it not a valid xml file
                var lastIndexOfClosingBracket = vsActivityLogContents.LastIndexOf('>');
                var vsActivityLogBuilder = new StringBuilder(vsActivityLogContents.Remove(lastIndexOfClosingBracket + 1, vsActivityLogContents.Length - lastIndexOfClosingBracket - 1));

                // We need to add a root element so that the it's valid xml and can easily be consumed using Xml.Linq
                vsActivityLogBuilder.Insert(0, $"<{VisualStudioActivityLogRoot}>{Environment.NewLine}");
                vsActivityLogBuilder.AppendLine($"{Environment.NewLine}</{VisualStudioActivityLogRoot}>");

                return vsActivityLogBuilder.ToString();
            });
        }

        internal static string GetIdeState()
        {
            return InvokeOnUIThread(() =>
            {
                var stateBuilder = new StringBuilder();

                /*
                 * Solution
                 */
                if (GlobalServiceProvider.ServiceProvider.GetService(typeof(SVsSolution)) is IVsSolution solution
                    && solution.GetSolutionInfo(out var solutionDirectory, out var solutionFile, out var userOptsFile) == VSConstants.S_OK)
                {
                    stateBuilder.AppendLine("Solution:");
                    stateBuilder.AppendLine($"  Solution Directory: {solutionDirectory}");
                    stateBuilder.AppendLine($"  Solution File:      {solutionFile}");
                    stateBuilder.AppendLine($"  User Opts File:     {userOptsFile}");
                }

                /*
                 * Error list
                 */
                if (GlobalServiceProvider.ServiceProvider.GetService(typeof(SVsTaskList)) is IVsTaskList taskList
                    && taskList.EnumTaskItems(out var enumTaskItems) == VSConstants.S_OK)
                {
                    stateBuilder.AppendLine("Error list:");

                    var index = 0;
                    var taskItems = new IVsTaskItem[10];
                    var actual = new uint[1];
                    while (enumTaskItems.Next((uint)taskItems.Length, taskItems, actual) == VSConstants.S_OK)
                    {
                        if (actual[0] == 0)
                        {
                            break;
                        }

                        for (var i = 0; i < actual[0]; i++)
                        {
                            var item = taskItems[i];
                            var text = item.get_Text(out var name) == VSConstants.S_OK ? name : string.Empty;
                            stateBuilder.AppendLine($"  {++index}: {name}");
                        }
                    }
                }

                return stateBuilder.ToString();
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

        private static bool TrySetForegroundWindow(HWND window)
        {
            var activeWindow = PInvoke.GetLastActivePopup(window);
            activeWindow = PInvoke.IsWindowVisible(activeWindow) ? activeWindow : window;
            PInvoke.SwitchToThisWindow(activeWindow, true);

            if (!PInvoke.SetForegroundWindow(activeWindow))
            {
                if (!PInvoke.AllocConsole())
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }

                try
                {
                    var consoleWindow = PInvoke.GetConsoleWindow();
                    if (consoleWindow == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("Failed to obtain the console window.");
                    }

                    if (!PInvoke.SetWindowPos(consoleWindow, hWndInsertAfter: (HWND)0, 0, 0, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOZORDER))
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }
                }
                finally
                {
                    if (!PInvoke.FreeConsole())
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }
                }

                if (!PInvoke.SetForegroundWindow(activeWindow))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
