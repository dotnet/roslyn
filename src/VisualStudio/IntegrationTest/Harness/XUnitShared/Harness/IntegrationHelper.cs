// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Windows.Win32;
    using DTE = EnvDTE.DTE;
    using IMoniker = Windows.Win32.System.Com.IMoniker;

    /// <summary>
    /// Provides some helper functions used by the other classes in the project.
    /// </summary>
    internal static class IntegrationHelper
    {
        /// <summary>
        /// Kills the specified process if it is not <see langword="null"/> and has not already exited.
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

        /// <summary>Locates the DTE object for the specified process.</summary>
        public static DTE? TryLocateDteForProcess(Process process)
        {
            object? dte = null;
            var monikers = new IMoniker?[1];

            PInvoke.GetRunningObjectTable(0, out var runningObjectTable);
            runningObjectTable.EnumRunning(out var enumMoniker);
            PInvoke.CreateBindCtx(0, out var bindContext);

            do
            {
                monikers[0] = null;

                uint monikersFetched;
                unsafe
                {
                    enumMoniker.Next(1, monikers, &monikersFetched);
                }

                if (monikersFetched == 0)
                {
                    // There's nothing further to enumerate, so fail
                    return null;
                }

                var moniker = monikers[0]!;
                moniker.GetDisplayName(bindContext, null, out var fullDisplayName);

                // FullDisplayName will look something like: <ProgID>:<ProccessId>
                var displayNameParts = fullDisplayName.ToString().Split(':');
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

        public static async Task<T> WaitForNotNullAsync<T>(Func<T?> action)
            where T : class
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
