// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.SymReaderInterop
{
    [Guid("B20D55B3-532E-4906-87E7-25BD5734ABD2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface ISymUnmanagedAsyncMethod
    {
        [PreserveSig]
        int IsAsyncMethod(out bool result);

        [PreserveSig]
        int GetKickoffMethod(out uint kickoffMethod);

        [PreserveSig]
        int HasCatchHandlerILOffset(out bool pRetVal);

        [PreserveSig]
        int GetCatchHandlerILOffset(out uint result);

        [PreserveSig]
        int GetAsyncStepInfoCount(out uint result);

        [PreserveSig]
        int GetAsyncStepInfo(
            uint cStepInfo,
            out uint cStepInfoBack,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] uint[] yieldOffsets,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] uint[] breakpointOffset,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] uint[] breakpointMethod);
    }
}
