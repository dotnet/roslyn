// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
