// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Roslyn.Utilities.Pdb
{
    [ComImport]
    [Guid("B4CE6286-2A6B-3712-A3B7-1EE1DAD467B5")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(false)]
    internal interface ISymUnmanagedReader
    {
        void __GetDocument(/*...*/);

        void GetDocuments(
            int cDocs,
            out int pcDocs,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedDocument[] pDocs);

        // These methods will often return error HRs in common cases.
        // Using PreserveSig and manually handling error cases provides a big performance win.
        // Far fewer exceptions will be thrown and caught.
        // Exceptions should be reserved for truely "exceptional" cases.
        [PreserveSig]
        int __GetUserEntryPoint(/*...*/);

        [PreserveSig]
        int GetMethod(
            int methodToken,
            [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod retVal);

        [PreserveSig]
        int GetMethodByVersion(
            int methodToken,
            int version,
            [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod retVal);

        void __GetVariables(/*...*/);
        void __GetGlobalVariables(/*...*/);
        void __GetMethodFromDocumentPosition(/*...*/);

        [PreserveSig]
        int GetSymAttribute(
            int token,
            [MarshalAs(UnmanagedType.LPWStr)] string name,
            int sizeBuffer,
            out int lengthBuffer,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] buffer);

        void __GetNamespaces(/*...*/);

        void Initialize(
            [MarshalAs(UnmanagedType.Interface)] object importer,
            [MarshalAs(UnmanagedType.LPWStr)] string filename,
            [MarshalAs(UnmanagedType.LPWStr)] string searchPath,
            IStream stream);

        void __UpdateSymbolStore(/*...*/);
        void __ReplaceSymbolStore(/*...*/);
        void __GetSymbolStoreFileName(/*...*/);
        void __GetMethodsFromDocumentPosition(/*...*/);
        void __GetDocumentVersion(/*...*/);
        void __GetMethodVersion(/*...*/);
    }
}
