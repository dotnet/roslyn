// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.SymReaderInterop
{
    [ComVisible(false), Guid("AE932FBA-3FD8-4dba-8232-30A2309B02DB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface ISymUnmanagedScope2 : ISymUnmanagedScope
    {
        [PreserveSig]
        new int GetMethod([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod pRetVal);
        [PreserveSig]
        new int GetParent([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedScope pRetVal);
        [PreserveSig]
        new int GetChildren(int cChildren, out int pcChildren, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] ISymUnmanagedScope[] children);
        [PreserveSig]
        new int GetStartOffset(out int pRetVal);
        [PreserveSig]
        new int GetEndOffset(out int pRetVal);
        [PreserveSig]
        new int GetLocalCount(out int pRetVal);
        [PreserveSig]
        new int GetLocals(int cLocals, out int pcLocals, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] ISymUnmanagedVariable[] locals);
        [PreserveSig]
        new int GetNamespaces(int cNameSpaces, out int pcNameSpaces, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] ISymUnmanagedNamespace[] namespaces);
        [PreserveSig]
        int GetConstantCount(out int pRetVal);
        [PreserveSig]
        int GetConstants(int cConstants, out int pcConstants, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] ISymUnmanagedConstant[] constants);
    }
}
