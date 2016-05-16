// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;

namespace Roslyn.VisualStudio.Test.Utilities.Interop
{
    internal static class Ole32
    {
        [DllImport("Ole32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "CreateBindCtx", PreserveSig = false, SetLastError = false)]
        public static extern void CreateBindCtx(
            [In] int reserved,
            [Out, MarshalAs(UnmanagedType.Interface)] out IBindCtx bindContext
        );

        [DllImport("Ole32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "GetRunningObjectTable", PreserveSig = false, SetLastError = false)]
        public static extern void GetRunningObjectTable(
            [In] int reserved,
            [Out, MarshalAs(UnmanagedType.Interface)] out IRunningObjectTable runningObjectTable
        );
    }
}
