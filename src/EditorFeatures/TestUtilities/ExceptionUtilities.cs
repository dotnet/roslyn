// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class ExceptionUtilities
    {
        [DllImport("kernel32")]
        private static extern void RaiseFailFastException(IntPtr exceptionRecord, IntPtr contextRecord, uint flags);

        [DebuggerHidden]
        public static void FailFast(Exception exception)
        {
            RaiseFailFastException(IntPtr.Zero, IntPtr.Zero, 0);

            // the RaiseFailFastException above may have not actually killed the process if a
            // debugger was attached. Fall back to the CLR FailFast.
            Environment.FailFast("FailFast was issued from devenv.exe.  A crash dump should be available.");
        }
    }
}
