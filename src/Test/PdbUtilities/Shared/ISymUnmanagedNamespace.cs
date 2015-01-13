// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.VisualStudio.SymReaderInterop
{
    [ComImport]
    [Guid("0DFF7289-54F8-11d3-BD28-0000F80849BD")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(false)]
    internal interface ISymUnmanagedNamespace
    {
        [PreserveSig]
        int GetName(
            int cchName,
            out int pcchName,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] char[] szName);

        int __GetNamespaces(
            int cNameSpaces,
            out int pcNameSpaces,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedNamespace[] namespaces);

        int __GetVariables(
            int cVars,
            out int pcVars,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] pVars);
    }
}