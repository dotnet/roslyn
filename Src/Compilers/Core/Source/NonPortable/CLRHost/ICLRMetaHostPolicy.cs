// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft.Runtime.Hosting.Interop
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using System.Text;

    [System.Security.SecurityCritical]
    [ComImport, Guid("E2190695-77B2-492E-8E14-C4B3A7FDD593"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IClrMetaHostPolicy
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int GetRequestedRuntime(
            [In, ComAliasName("System.Runtime.Hosting.MetaHostPolicyFlags")] MetaHostPolicyFlags policyFlags,
            [In, MarshalAs(UnmanagedType.LPWStr)] string binaryPath,
            [In, MarshalAs(UnmanagedType.Interface)] IStream configStream,
            [In, Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex=4)] StringBuilder version,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int versionLength,
            [Out, MarshalAs(UnmanagedType.LPWStr,SizeParamIndex=6)] StringBuilder imageVersion,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int imageVersionLength,
            [Out, MarshalAs(UnmanagedType.U4)] out MetaHostConfigFlags configFlags,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId,
            [Out, MarshalAs(UnmanagedType.IUnknown)] out object runtimeInfo);
    }
}

