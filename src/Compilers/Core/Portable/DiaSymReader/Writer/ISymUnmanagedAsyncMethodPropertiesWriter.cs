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
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("FC073774-1739-4232-BD56-A027294BEC15")]
    [SuppressUnmanagedCodeSecurity]
    internal interface ISymUnmanagedAsyncMethodPropertiesWriter
    {
        void DefineKickoffMethod(int kickoffMethod);
        void DefineCatchHandlerILOffset(int catchHandlerOffset);

        unsafe void DefineAsyncStepInfo(int count, int* yieldOffsets, int* breakpointOffset, int* breakpointMethod);
    }
}
