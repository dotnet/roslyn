// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Roslyn.VisualStudio.Test.Utilities.Interop
{
    internal static class Kernel32
    {
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "GetCurrentThreadId", PreserveSig = true, SetLastError = false)]
        public static extern uint GetCurrentThreadId();
    }
}
