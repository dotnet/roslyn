// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal static class IVsShellExtensions
    {
        // tri-state: uninitialized (0), devenv is in command line mode (1), devenv is not in command line mode (-1)
        private static volatile int s_isInCommandLineMode;

        /// <summary>
        /// Returns true if devenv is invoked in command line mode for build, e.g. devenv /rebuild MySolution.sln
        /// </summary>
        public static bool IsInCommandLineMode(JoinableTaskFactory joinableTaskFactory)
        {
            var result = s_isInCommandLineMode;
            if (result == 0)
            {
                s_isInCommandLineMode = result = joinableTaskFactory.Run(async () =>
                {
                    await joinableTaskFactory.SwitchToMainThreadAsync();

                    var shell = ServiceProvider.GlobalProvider.GetService<SVsShell, IVsShell>(joinableTaskFactory);
                    return
                        (shell != null) &&
                        ErrorHandler.Succeeded(shell.GetProperty((int)__VSSPROPID.VSSPROPID_IsInCommandLineMode, out var result)) &&
                        (bool)result ? 1 : -1;
                });
            }

            return result == 1;
        }

        public static bool TryGetPropertyValue(this IVsShell shell, __VSSPROPID id, out IntPtr value)
        {
            var hresult = shell.GetProperty((int)id, out var objValue);
            if (ErrorHandler.Succeeded(hresult) && objValue != null)
            {
                value = (IntPtr.Size == 4) ? (IntPtr)(int)objValue : (IntPtr)(long)objValue;
                return true;
            }

            value = default;
            return false;
        }
    }
}
