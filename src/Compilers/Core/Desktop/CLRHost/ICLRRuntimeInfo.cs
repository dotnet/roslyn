// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.Runtime.Hosting.Interop
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;

    [System.Security.SecurityCritical]
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("BD39D1D2-BA2F-486A-89B0-B4B0CB466891")]
    internal interface IClrRuntimeInfo
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetVersionString(
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] StringBuilder buffer,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int bufferLength);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetRuntimeDirectory(
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] StringBuilder buffer,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int bufferLength);

        [return: MarshalAs(UnmanagedType.Bool)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        bool IsLoaded(
            [In] IntPtr processHandle);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), LCIDConversion(3)]
        [PreserveSig]
        int LoadErrorString(
            [In, MarshalAs(UnmanagedType.U4)] int resourceId,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)] StringBuilder buffer,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int bufferLength);

        //@TODO: SafeHandle?
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        IntPtr LoadLibrary(
            [In, MarshalAs(UnmanagedType.LPWStr)] string dllName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        IntPtr GetProcAddress(
            [In, MarshalAs(UnmanagedType.LPStr)] string procName);

        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        object GetInterface(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid coClassId,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId);

        [return: MarshalAs(UnmanagedType.Bool)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        bool IsLoadable();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDefaultStartupFlags(
            [In, MarshalAs(UnmanagedType.U4)] StartupFlags startupFlags,
            [In, MarshalAs(UnmanagedType.LPStr)] string hostConfigFile);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetDefaultStartupFlags(
            [Out, MarshalAs(UnmanagedType.U4)] out StartupFlags startupFlags,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)] StringBuilder hostConfigFile,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int hostConfigFileLength);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void BindAsLegacyV2Runtime();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void IsStarted(
            [Out, MarshalAs(UnmanagedType.Bool)] out bool started,
            [Out, MarshalAs(UnmanagedType.U4)] out StartupFlags startupFlags);
    }
}

