// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader
{
    [ComVisible(false)]
    [Guid("AE932FBA-3FD8-4dba-8232-30A2309B02DB")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface ISymUnmanagedScope2 : ISymUnmanagedScope
    {
        #region ISymUnmanagedScope methods 

        [PreserveSig]
        new int GetMethod([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod method);

        [PreserveSig]
        new int GetParent([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedScope scope);

        [PreserveSig]
        new int GetChildren(
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedScope[] children);

        [PreserveSig]
        new int GetStartOffset(out int offset);

        [PreserveSig]
        new int GetEndOffset(out int offset);

        [PreserveSig]
        new int GetLocalCount(out int count);

        [PreserveSig]
        new int GetLocals(
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] locals);

        [PreserveSig]
        new int GetNamespaces(
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedNamespace[] namespaces);

        #endregion

        #region ISymUnmanagedScope2 methods

        [PreserveSig]
        int GetConstantCount(out int count);

        [PreserveSig]
        int GetConstants(
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedConstant[] constants);

        #endregion
    }
}
