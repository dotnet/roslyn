// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.SymReaderInterop
{
    [ComImport]
    [Guid("68005D0F-B8E0-3B01-84D5-A11A94154942")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(false)]
    internal interface ISymUnmanagedScope
    {
        int __GetMethod([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod pRetVal);

        int __GetParent([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedScope pRetVal);

        [PreserveSig]
        int GetChildren(
            int cChildren,
            out int pcChildren,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedScope[] children);

        [PreserveSig]
        int GetStartOffset(out int pRetVal);

        [PreserveSig]
        int GetEndOffset(out int pRetVal);

        [PreserveSig]
        int GetLocalCount(out int pRetVal);

        [PreserveSig]
        int GetLocals(
            int cLocals,
            out int pcLocals,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] locals);

        [PreserveSig]
        int GetNamespaces(
            int cNameSpaces,
            out int pcNameSpaces,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedNamespace[] namespaces);
    }
}
