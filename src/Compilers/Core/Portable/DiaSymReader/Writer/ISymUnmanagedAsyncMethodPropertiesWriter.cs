// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#pragma warning disable 436 // SuppressUnmanagedCodeSecurityAttribute defined in source and mscorlib

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.DiaSymReader
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("FC073774-1739-4232-BD56-A027294BEC15")]
    [SuppressUnmanagedCodeSecurity]
    [GeneratedWhenPossibleComInterface]
    internal partial interface ISymUnmanagedAsyncMethodPropertiesWriter
    {
        void DefineKickoffMethod(int kickoffMethod);
        void DefineCatchHandlerILOffset(int catchHandlerOffset);

        void DefineAsyncStepInfo(
            int count,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] yieldOffsets,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] breakpointOffset,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] breakpointMethod);
    }
}
