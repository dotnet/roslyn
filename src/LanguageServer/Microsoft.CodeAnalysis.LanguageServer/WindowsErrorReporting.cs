// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

internal sealed class WindowsErrorReporting
{
    internal static void SetErrorModeOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var oldErrorMode = SetErrorMode(ErrorModes.SYSTEM_DEFAULT);

        // There have been reports that SetErrorMode wasn't working correctly, so double
        // check in Debug builds that it actually set. The SEM_NOALIGNMENTFAULTEXCEPT mode
        // is special because it cannot be cleared once it is set.
        // Refer to https://learn.microsoft.com/en-us/windows/win32/api/errhandlingapi/nf-errhandlingapi-seterrormode
        Debug.Assert(GetErrorMode() == (oldErrorMode & ErrorModes.SEM_NOALIGNMENTFAULTEXCEPT));
    }

    [DllImport("kernel32.dll")]
    private static extern ErrorModes SetErrorMode(ErrorModes uMode);

    [DllImport("kernel32.dll")]
    private static extern ErrorModes GetErrorMode();

    [Flags]
    private enum ErrorModes : uint
    {
        SYSTEM_DEFAULT = 0x0,
        SEM_FAILCRITICALERRORS = 0x0001,
        SEM_NOGPFAULTERRORBOX = 0x0002,
        SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
        SEM_NOOPENFILEERRORBOX = 0x8000
    }
}
