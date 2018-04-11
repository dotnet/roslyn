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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.Win32;
using Roslyn.Utilities;
using Process = System.Diagnostics.Process;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    /// <summary>
    /// Provides some helper functions used by the other classes in the project.
    /// </summary>
    internal static class IntegrationHelper
    {
        public static bool AttachThreadInput(uint idAttach, uint idAttachTo)
        {
            var success = NativeMethods.AttachThreadInput(idAttach, idAttachTo, true);

            if (!success)
            {
                var hresult = Marshal.GetHRForLastWin32Error();
                Marshal.ThrowExceptionForHR(hresult);
            }

            return success;
        }

        public static bool BlockInput()
        {
            var success = NativeMethods.BlockInput(true);

            if (!success)
            {
                var hresult = Marshal.GetHRForLastWin32Error();

                if (hresult == VSConstants.E_ACCESSDENIED)
                {
                    Debug.WriteLine("Input cannot be blocked because the system requires Administrative privileges.");
                }
                else
                {
                    Debug.WriteLine("Input cannot be blocked because another thread has blocked the input.");
                }
            }

            return success;
        }

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
            return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        public static bool DetachThreadInput(uint idAttach, uint idAttachTo)
        {
            var success = NativeMethods.AttachThreadInput(idAttach, idAttachTo, false);

            if (!success)
            {
                var hresult = Marshal.GetHRForLastWin32Error();
                Marshal.ThrowExceptionForHR(hresult);
            }

            return success;
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
            // in certain circumstances, such as when a window is losing activation.

            var foregroundWindow = IntPtr.Zero;

            do
            {
                foregroundWindow = NativeMethods.GetForegroundWindow();
            }
            while (foregroundWindow == IntPtr.Zero);

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

            var enumFunc = new NativeMethods.WNDENUMPROC((hWnd, lParam) => {
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

        public static void SetForegroundWindow(IntPtr window, bool skipAttachingThread = false)
        {
            var foregroundWindow = GetForegroundWindow();

            if (window == foregroundWindow)
            {
                return;
            }

            var activeThreadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
            var currentThreadId = NativeMethods.GetCurrentThreadId();

            var threadInputsAttached = false;

            try
            {
                // No need to re-attach threads in case when VS initializaed an UI thread for a debugged application.
                if (!skipAttachingThread && activeThreadId != currentThreadId)
                {
                    // Attach the thread inputs so that 'SetActiveWindow' and 'SetFocus' work
                    threadInputsAttached = AttachThreadInput(currentThreadId, activeThreadId);
                }

                // Make the window a top-most window so it will appear above any existing top-most windows
                NativeMethods.SetWindowPos(window, (IntPtr)NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, (NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE));

                // Move the window into the foreground as it may not have been achieved by the 'SetWindowPos' call
                var success = NativeMethods.SetForegroundWindow(window);

                if (!success)
                {
                    throw new InvalidOperationException("Setting the foreground window failed.");
                }

                // Ensure the window is 'Active' as it may not have been achieved by 'SetForegroundWindow'
                NativeMethods.SetActiveWindow(window);

                // Give the window the keyboard focus as it may not have been achieved by 'SetActiveWindow'
                NativeMethods.SetFocus(window);

                // Remove the 'Top-Most' qualification from the window
                NativeMethods.SetWindowPos(window, (IntPtr)NativeMethods.HWND_NOTOPMOST, 0, 0, 0, 0, (NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE));
            }
            finally
            {
                if (threadInputsAttached)
                {
                    // Finally, detach the thread inputs from eachother
                    DetachThreadInput(currentThreadId, activeThreadId);
                }
            }
        }

        public static void SendInput(NativeMethods.INPUT[] inputs)
        {
            // NOTE: This assumes that Visual Studio is the active foreground window.

            LogKeyboardInputs(inputs);

            var eventsInserted = NativeMethods.SendInput((uint)inputs.Length, inputs, NativeMethods.SizeOf_INPUT);

            if (eventsInserted == 0)
            {
                var hresult = Marshal.GetHRForLastWin32Error();
                throw new ExternalException("Sending input failed because input was blocked by another thread.", hresult);
            }
        }

        [Conditional("DEBUG")]
        private static void LogKeyboardInputs(NativeMethods.INPUT[] inputs)
        {
            foreach (var input in inputs)
            {
                switch (input.Type)
                {
                    case NativeMethods.INPUT_KEYBOARD:
                        LogKeyboardInput(input.ki);
                        break;
                    case NativeMethods.INPUT_MOUSE:
                        Debug.WriteLine("UNEXPECTED: Encountered mouse input");
                        break;
                    case NativeMethods.INPUT_HARDWARE:
                        Debug.WriteLine("UNEXPECTED: Encountered hardware input");
                        break;
                    default:
                        Debug.WriteLine($"ERROR: Encountered illegal input type: {input.Type}");
                        break;
                }
            }
        }

        [Conditional("DEBUG")]
        private static void LogKeyboardInput(NativeMethods.KEYBDINPUT input)
        {
            var isExtendedKey = (input.dwFlags & NativeMethods.KEYEVENTF_EXTENDEDKEY) != 0;
            var isKeyUp = (input.dwFlags & NativeMethods.KEYEVENTF_KEYUP) != 0;
            var isUnicode = (input.dwFlags & NativeMethods.KEYEVENTF_UNICODE) != 0;
            var isScanCode = (input.dwFlags & NativeMethods.KEYEVENTF_SCANCODE) != 0;

            if (isUnicode && input.wVk != 0)
            {
                Debug.WriteLine("UNEXPECTED: if KEYEVENTF_UNICODE flag is specified then wVk must be 0.");
                return;
            }

            var builder = SharedPools.Default<StringBuilder>().AllocateAndClear();

            builder.Append("Send Key: ");

            char ch;
            if (isUnicode || isScanCode)
            {
                builder.Append(input.wScan.ToString("x4"));
                ch = (char)input.wScan;
            }
            else
            {
                builder.Append(input.wVk.ToString("x4"));
                ch = (char)(NativeMethods.MapVirtualKey(input.wVk, NativeMethods.MAPVK_VK_TO_CHAR) & 0x0000ffff);
            }

            // Append code and printable character
            builder.Append(' ');
            AppendPrintableChar(ch, builder);

            if (!isUnicode && !isScanCode && input.wVk <= byte.MaxValue)
            {
                AppendVirtualKey((byte)input.wVk, builder);
            }

            // Append flags
            if (input.dwFlags == 0)
            {
                builder.Append("[none]");
            }
            else
            {
                builder.Append('[');

                if (isExtendedKey)
                {
                    AppendFlag("extended", builder);
                }

                if (isKeyUp)
                {
                    AppendFlag("key up", builder);
                }

                if (isUnicode)
                {
                    AppendFlag("unicode", builder);
                }

                if (isScanCode)
                {
                    AppendFlag("scan code", builder);
                }

                builder.Append(']');
            }

            Debug.WriteLine(builder.ToString());

            SharedPools.Default<StringBuilder>().ClearAndFree(builder);
        }

        private static void AppendPrintableChar(char ch, StringBuilder builder)
        {
            string text = GetPrintableCharText(ch);

            if (text != null)
            {
                builder.Append("'");
                builder.Append(text);
                builder.Append("' ");
            }
        }

        private static string GetPrintableCharText(char ch)
        {
            switch (ch)
            {
                case '\r':
                    return @"\r";
                case '\n':
                    return @"\n";
                case '\t':
                    return @"\t";
                case '\f':
                    return @"\f";
                case '\v':
                    return @"\v";
                default:
                    return !char.IsControl(ch)
                        ? new string(ch, 1)
                        : null;
            }
        }

        private static void AppendVirtualKey(byte virtualKey, StringBuilder builder)
        {
            if (Enum.IsDefined(typeof(VirtualKey), virtualKey))
            {
                builder.Append('(');
                builder.Append(Enum.GetName(typeof(VirtualKey), virtualKey));
                builder.Append(") ");
            }
        }

        [Conditional("DEBUG")]
        private static void AppendFlag(string flagText, StringBuilder builder)
        {
            if (builder.Length > 0 && builder[builder.Length - 1] != '[')
            {
                builder.Append(", ");
            }

            builder.Append(flagText);
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

        public static void UnblockInput()
        {
            var success = NativeMethods.BlockInput(false);

            if (!success)
            {
                var hresult = Marshal.GetHRForLastWin32Error();
                throw new ExternalException("Input cannot be unblocked because it was blocked by another thread.", hresult);
            }
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
