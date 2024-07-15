// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Interop;

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

        public static async Task WaitForResultAsync<T>(Func<T> action, T expectedResult)
            where T : notnull
        {
            while (!action().Equals(expectedResult))
            {
                await Task.Yield();
            }
        }
    }
}
