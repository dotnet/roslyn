// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

internal static class IVsShellExtensions
{
    // tri-state: uninitialized (0), devenv is in command line mode (1), devenv is not in command line mode (-1)
    private static volatile int s_isInCommandLineMode;

    /// <summary>
    /// Returns true if devenv is invoked in command line mode for build, e.g. devenv /rebuild MySolution.sln
    /// </summary>
    public static bool IsInCommandLineMode(this IVsShell shell)
    {
        if (s_isInCommandLineMode == 0)
        {
            s_isInCommandLineMode =
                ErrorHandler.Succeeded(shell.GetProperty((int)__VSSPROPID.VSSPROPID_IsInCommandLineMode, out var result)) &&
                (bool)result ? 1 : -1;
        }

        return s_isInCommandLineMode == 1;
    }

    public static bool TryGetPropertyValue(this IVsShell shell, __VSSPROPID id, out IntPtr value)
    {
        if (ErrorHandler.Succeeded(shell.GetProperty((int)id, out var objValue)) && objValue != null)
        {
            value = (IntPtr.Size == 4) ? (IntPtr)(int)objValue : (IntPtr)(long)objValue;
            return true;
        }

        value = default;
        return false;
    }
}
