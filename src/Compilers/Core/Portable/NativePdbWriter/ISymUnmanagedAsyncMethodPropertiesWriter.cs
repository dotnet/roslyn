// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable 436 // SuppressUnmanagedCodeSecurityAttribute defined in source and mscorlib 

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Cci
{
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("FC073774-1739-4232-BD56-A027294BEC15"), SuppressUnmanagedCodeSecurity]
    internal interface ISymUnmanagedAsyncMethodPropertiesWriter
    {
        void DefineKickoffMethod(uint kickoffMethod);
        void DefineCatchHandlerILOffset(uint catchHandlerOffset);
        void DefineAsyncStepInfo(uint count,
                                 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] uint[] yieldOffsets,
                                 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] uint[] breakpointOffset,
                                 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] uint[] breakpointMethod);
    }
}
