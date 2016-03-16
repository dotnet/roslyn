﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.Win32;
using Roslyn.VisualStudio.Test.Utilities.Interop;

using Process = System.Diagnostics.Process;

namespace Roslyn.VisualStudio.Test.Utilities
{
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

        public static async Task DownloadFileAsync(string downloadUrl, string fileName)
        {
            using (var webClient = new WebClient())
            {
                await webClient.DownloadFileTaskAsync(downloadUrl, fileName).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        public static IntPtr GetModalWindowFromParentWindow(IntPtr parentWindow)
        {
            foreach (var topLevelWindow in GetTopLevelWindows())
            {
                if ((User32.GetParent(topLevelWindow) == parentWindow) ||
                    (User32.GetWindow(topLevelWindow, User32.GW_OWNER) == parentWindow) ||
                    (User32.GetAncestor(topLevelWindow, User32.GA_PARENT) == parentWindow))
                {
                    return topLevelWindow;
                }
            }

            return IntPtr.Zero;
        }

        public static string GetTitleForWindow(IntPtr window)
        {
            var titleLength = User32.SendMessage(window, User32.WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);

            if (titleLength == IntPtr.Zero)
            {
                return string.Empty;
            }

            var title = new StringBuilder(titleLength.ToInt32() + 1);

            User32.SendMessage(window, User32.WM_GETTEXT, (IntPtr)(title.Capacity), title);
            return title.ToString();
        }

        public static IEnumerable<IntPtr> GetTopLevelWindows()
        {
            var topLevelWindows = new List<IntPtr>();

            var enumFunc = new User32.WNDENUMPROC((hWnd, lParam) => {
                topLevelWindows.Add(hWnd);
                return true;
            });
            User32.EnumWindows(enumFunc, IntPtr.Zero);

            return topLevelWindows;
        }

        public static void KillProcess(Process process)
        {
            if ((process != null) && (!process.HasExited))
            {
                process.Kill();
            }
        }

        public static void KillProcess(string processName)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                KillProcess(processName);
            }
        }

        public static DTE LocateDteForProcess(Process process)
        {
            object dte = null;
            IRunningObjectTable runningObjectTable = null;
            IEnumMoniker enumMoniker = null;
            IBindCtx bindContext = null;
            var monikers = new IMoniker[1];
            var vsProgId = IntegrationHost.VsProgId;

            Ole32.GetRunningObjectTable(0, out runningObjectTable);
            runningObjectTable.EnumRunning(out enumMoniker);
            Ole32.CreateBindCtx(0, out bindContext);

            do
            {
                monikers[0] = null;

                var monikersFetched = 0u;
                var result = enumMoniker.Next(1, monikers, out monikersFetched);

                if (result != 0)
                {
                    return null;
                }

                var moniker = monikers[0];
                var fullDisplayName = null as string;

                moniker.GetDisplayName(bindContext, null, out fullDisplayName);

                var displayNameProcessId = 0;

                if (!int.TryParse(fullDisplayName.Split(':').Last(), out displayNameProcessId))
                {
                    continue;
                }

                var displayName = fullDisplayName.Substring(0, (fullDisplayName.Length - (displayNameProcessId.ToString().Length + 1)));
                var fullProgId = vsProgId.StartsWith("!") ? vsProgId : $"!{vsProgId}";

                if (displayName.Equals(fullProgId, StringComparison.OrdinalIgnoreCase) &&
                    (displayNameProcessId == process.Id))
                {
                    runningObjectTable.GetObject(moniker, out dte);
                }
            }
            while (dte == null);

            return (DTE)(dte);
        }

        public static async Task<string> LocateFile(string fileName, string downloadUrl = "")
        {
            if (File.Exists(fileName))
            {
                return Path.GetFullPath(fileName);
            }

            var filePath = Path.Combine(Environment.CurrentDirectory, fileName);

            if (File.Exists(filePath))
            {
                return filePath;
            }

            filePath = Path.Combine(typeof(IntegrationHelper).Assembly.Location, fileName);

            if (File.Exists(filePath))
            {
                return filePath;
            }

            if (!string.IsNullOrWhiteSpace(downloadUrl))
            {
                await DownloadFileAsync(downloadUrl, fileName).ConfigureAwait(continueOnCapturedContext: false);
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Unable to locate the specified file. (FileName: '{fileName}')", fileName);
            }
            
            return filePath;
        }

        public static RegistryKey OpenRegistryKey(RegistryKey baseKey, string subKeyName)
        {
            using (var registryKey = baseKey.OpenSubKey(subKeyName))
            {
                if (registryKey == null)
                {
                    throw new Exception($"The specified registry key could not be found. Registry Key: '{registryKey}'");
                }

                return registryKey;
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
                IntegrationLog.Current.Warning($"Failed to recursively delete the specified directory. (Name: '{path}')");
                IntegrationLog.Current.WriteLine($"\t{e}");
                return false;
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

            while (result.Equals(null))
            {
                await Task.Yield();
                result = action();
            }

            return result;
        }
    }
}
