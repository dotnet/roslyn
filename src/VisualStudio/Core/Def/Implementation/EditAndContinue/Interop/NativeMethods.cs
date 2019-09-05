// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop
{
    internal static class NativeMethods
    {
        private static Guid s_IID_IUnknown = new Guid(0x00000000, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);

        [DllImport("ole32.dll")]
        private static extern int CoMarshalInterThreadInterfaceInStream([In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] object pUnk, out IntPtr ppStm);

        [DllImport("ole32.dll")]
        private static extern int CoGetInterfaceAndReleaseStream(IntPtr pStm, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

        public static int GetStreamForObject(object pUnk, out IntPtr stream)
        {
            return CoMarshalInterThreadInterfaceInStream(ref s_IID_IUnknown, pUnk, out stream);
        }

        public static object GetObjectAndRelease(IntPtr stream)
        {
            Marshal.ThrowExceptionForHR(CoGetInterfaceAndReleaseStream(stream, ref s_IID_IUnknown, out var pUnk));
            return pUnk;
        }
    }
}
