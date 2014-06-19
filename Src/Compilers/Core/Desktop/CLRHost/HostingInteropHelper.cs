// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Runtime.Hosting
{
    internal static class HostingInteropHelper
    {

        [SecurityCritical]
        [DllImport("mscoree.dll", PreserveSig = false, EntryPoint = "CLRCreateInstance")]
        [return: MarshalAs(UnmanagedType.Interface)]
        private static extern object nCreateInterface(
                [MarshalAs(UnmanagedType.LPStruct)] Guid clsid,
                [MarshalAs(UnmanagedType.LPStruct)] Guid riid);

        private static Guid _metaHostClsIdGuid =
            new Guid(0x9280188D, 0xE8E, 0x4867, 0xB3, 0xC, 0x7F, 0xA8, 0x38, 0x84, 0xE8, 0xDE);

        private static Guid _metaHostGuid =
            new Guid(0xD332DB9E, 0xB9B3, 0x4125, 0x82, 0x07, 0xA1, 0x48, 0x84, 0xF5, 0x32, 0x16);


        [SecurityCritical]
        internal static T GetClrMetaHost<T>()
        {
            return (T)nCreateInterface(_metaHostClsIdGuid, _metaHostGuid);
        }
    }
}
