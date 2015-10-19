// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader
{
    [ComImport]
    [Guid("B20D55B3-532E-4906-87E7-25BD5734ABD2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(false)]
    public interface ISymUnmanagedAsyncMethod
    {
        [PreserveSig]
        int IsAsyncMethod([MarshalAs(UnmanagedType.Bool)]out bool value);

        [PreserveSig]
        int GetKickoffMethod(out int kickoffMethodToken);

        [PreserveSig]
        int HasCatchHandlerILOffset([MarshalAs(UnmanagedType.Bool)]out bool offset);

        [PreserveSig]
        int GetCatchHandlerILOffset(out int offset);

        [PreserveSig]
        int GetAsyncStepInfoCount(out int count);

        [PreserveSig]
        int GetAsyncStepInfo(
            int bufferLength,
            out int count,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] yieldOffsets,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] breakpointOffset,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] breakpointMethod);
    }
}
