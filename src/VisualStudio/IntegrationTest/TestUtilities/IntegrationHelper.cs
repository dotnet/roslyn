// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.Win32;
using Roslyn.Utilities;
using WindowsInput;
using WindowsInput.Native;
using Process = System.Diagnostics.Process;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    /// <summary>
    /// Provides some helper functions used by the other classes in the project.
    /// </summary>
    internal static class IntegrationHelper
    {
        public static void CreateDirectory(string path, bool deleteExisting = false)
        {
            if (deleteExisting)
            {
                DeleteDirectoryRecursively(path);
            }

            Directory.CreateDirectory(path);
        }

        public static void DeleteDirectoryRecursively(string path)
        {
            if (Directory.Exists(path))
            {
                DirectoryExtensions.DeleteRecursively(path);
            }
        }

        public static string CreateTemporaryPath()
        {
            return Path.Combine(TempRoot.Root, Path.GetRandomFileName());
        }

        public static async Task DownloadFileAsync(string downloadUrl, string fileName)
        {
            using (var webClient = new WebClient())
            {
                await webClient.DownloadFileTaskAsync(downloadUrl, fileName).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        public static IntPtr GetForegroundWindow()
        {
            // Attempt to get the foreground window in a loop, as the NativeMethods function can return IntPtr.Zero
            // in certain circumstances, such as when a window is losing activation. If no foreground window is
            // identified after a short timeout, none is returned. This only impacts the ability of the test to restore
            // focus to a previous window, which is fine.

            var foregroundWindow = IntPtr.Zero;
            var stopwatch = Stopwatch.StartNew();

            do
            {
                foregroundWindow = NativeMethods.GetForegroundWindow();
            }
            while (foregroundWindow == IntPtr.Zero && stopwatch.Elapsed < TimeSpan.FromMilliseconds(250));

            return foregroundWindow;
        }

        /// <summary>Gets the Modal Window that is currently blocking interaction with the specified window or <see cref="IntPtr.Zero"/> if none exists.</summary>
        public static IntPtr GetModalWindowFromParentWindow(IntPtr parentWindow)
        {
            foreach (var topLevelWindow in GetTopLevelWindows())
            {
                // GetParent will return the parent or owner of the specified window, unless:
                //  * The window is a top-level window that is unowned
                //  * The window is a top-level does not have the WS_POPUP style
                //  * The owner window has the WS_POPUP style
                // GetWindow with GW_OWNER specified will return the owner window, but not the parent window
                // GetAncestor with GA_PARENT specified will return the parent window, but not the owner window
                if ((NativeMethods.GetParent(topLevelWindow) == parentWindow) ||
                    (NativeMethods.GetWindow(topLevelWindow, NativeMethods.GW_OWNER) == parentWindow) ||
                    (NativeMethods.GetAncestor(topLevelWindow, NativeMethods.GA_PARENT) == parentWindow))
                {
                    return topLevelWindow;
                }
            }

            return IntPtr.Zero;
        }

        public static object GetRegistryKeyValue(RegistryKey baseKey, string subKeyName, string valueName)
        {
            using (var registryKey = baseKey.OpenSubKey(subKeyName))
            {
                if (registryKey == null)
                {
                    throw new Exception($@"The specified registry key could not be found. Registry Key: '{baseKey}\{subKeyName}'");
                }

                return registryKey.GetValue(valueName);
            }
        }

        /// <summary>
        /// Gets the title text for the specified window.
        /// </summary>
        /// <remarks>
        /// GetWindowText() does not work across the process boundary.
        /// </remarks>
        public static string GetTitleForWindow(IntPtr window)
        {
            var titleLength = NativeMethods.SendMessage(window, NativeMethods.WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);

            if (titleLength == IntPtr.Zero)
            {
                return string.Empty;
            }

            var title = new StringBuilder(titleLength.ToInt32() + 1);

            NativeMethods.SendMessage(window, NativeMethods.WM_GETTEXT, (IntPtr)(title.Capacity), title);
            return title.ToString();
        }

        public static IEnumerable<IntPtr> GetTopLevelWindows()
        {
            var topLevelWindows = new List<IntPtr>();

            var enumFunc = new NativeMethods.WNDENUMPROC((hWnd, lParam) =>
            {
                topLevelWindows.Add(hWnd);
                return true;
            });

            var success = NativeMethods.EnumWindows(enumFunc, IntPtr.Zero);

            if (!success)
            {
                var hresult = Marshal.GetHRForLastWin32Error();
                Marshal.ThrowExceptionForHR(hresult);
            }

            return topLevelWindows;
        }

        /// <summary>
        /// Kills the specified process if it is not <c>null</c> and has not already exited.
        /// </summary>
        public static void KillProcess(Process process)
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
            }
        }

        /// <summary>
        /// Kills all processes matching the specified name.
        /// </summary>
        public static void KillProcess(string processName)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                KillProcess(process);
            }
        }

        public static void SetForegroundWindow(IntPtr window)
        {
            var activeWindow = NativeMethods.GetLastActivePopup(window);
            NativeMethods.SwitchToThisWindow(activeWindow, true);
        }

        public static void SendInput(KeyPress[] inputs)
        {
            // NOTE: This assumes that Visual Studio is the active foreground window.
            var simulator = new InputSimulator();

            ResetKeyState(VirtualKeyCode.LCONTROL);
            ResetKeyState(VirtualKeyCode.RCONTROL);
            ResetKeyState(VirtualKeyCode.LSHIFT);
            ResetKeyState(VirtualKeyCode.RSHIFT);
            ResetKeyState(VirtualKeyCode.LMENU);
            ResetKeyState(VirtualKeyCode.RMENU);

            foreach (var input in inputs)
            {
                if (!input.IsTextEntry && (input.VirtualKey == VirtualKeyCode.ESCAPE || input.VirtualKey == VirtualKeyCode.TAB))
                {
                    // Slight delay on Esc to ensure the Ctrl key isn't registered as down.
                    // Slight delay on Tab to ensure the Alt key isn't registered as down.
                    simulator.Keyboard.Sleep(TimeSpan.FromMilliseconds(10));
                }

                if (input.IsTextEntry)
                {
                    simulator.Keyboard.TextEntry(input.Character);
                }
                else if (!input.Modifiers.IsEmpty)
                {
                    simulator.Keyboard.ModifiedKeyStroke(input.Modifiers, input.VirtualKey);
                }
                else
                {
                    simulator.Keyboard.KeyPress(input.VirtualKey);
                }
            }

            return;

            void ResetKeyState(VirtualKeyCode keyCode)
            {
                if (simulator.InputDeviceState.IsHardwareKeyDown(keyCode))
                {
                    simulator.Keyboard.KeyUp(keyCode);
                }
            }
        }

        public static bool TryDeleteDirectoryRecursively(string path)
        {
            try
            {
                DeleteDirectoryRecursively(path);
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Warning: Failed to recursively delete the specified directory. (Name: '{path}')");
                Debug.WriteLine($"\t{e}");
                return false;
            }
        }

        /// <summary>Locates the DTE object for the specified process.</summary>
        public static DTE TryLocateDteForProcess(Process process)
        {
            object dte = null;
            var monikers = new IMoniker[1];

            NativeMethods.GetRunningObjectTable(0, out var runningObjectTable);
            runningObjectTable.EnumRunning(out var enumMoniker);
            NativeMethods.CreateBindCtx(0, out var bindContext);

            do
            {
                monikers[0] = null;

                var hresult = enumMoniker.Next(1, monikers, out var monikersFetched);

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

                // FullDisplayName will look something like: <ProgID>:<ProccessId>
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

            return (DTE)dte;
        }

        public static async Task WaitForResultAsync<T>(Func<T> action, T expectedResult)
        {
            while (!action().Equals(expectedResult))
            {
                await Task.Yield();
            }
        }

        public static async Task<T> WaitForNotNullAsync<T>(Func<T> action) where T : class
        {
            var result = action();

            while (result == null)
            {
                await Task.Yield();
                result = action();
            }

            return result;
        }
    }
}
