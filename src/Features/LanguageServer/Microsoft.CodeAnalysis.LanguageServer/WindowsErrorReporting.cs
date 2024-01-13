// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

internal class WindowsErrorReporting
{
    internal static void SetErrorModeOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        SetErrorMode(ErrorModes.SYSTEM_DEFAULT);

        // There have been reports that SetErrorMode wasn't working correctly, so double
        // check in Debug builds that it actually set
        Debug.Assert(GetErrorMode() == (uint)ErrorModes.SYSTEM_DEFAULT);
    }

    [DllImport("kernel32.dll")]
    private static extern ErrorModes SetErrorMode(ErrorModes uMode);

    [DllImport("kernel32.dll")]
    private static extern uint GetErrorMode();

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
