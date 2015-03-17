// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Microsoft.VisualStudio.SymReaderInterop
{
    [Guid("A09E53B2-2A57-4cca-8F63-B84F7C35D4AA")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISymUnmanagedReader2
    {
        [PreserveSig]
        int GetDocument([MarshalAs(UnmanagedType.LPWStr)] string url, Guid language, Guid languageVendor, Guid documentType, [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedDocument retVal);
        [PreserveSig]
        int GetDocuments(int cDocs, out int pcDocs, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] ISymUnmanagedDocument[] pDocs);
        [PreserveSig]
        int GetUserEntryPoint(out SymbolToken entryPoint);
        [PreserveSig]
        int GetMethod(SymbolToken methodToken, [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod retVal);
        [PreserveSig]
        int GetMethodByVersion(SymbolToken methodToken, int version, [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod retVal);
        [PreserveSig]
        int GetVariables(SymbolToken parent, int cVars, out int pcVars, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] [In] [Out] ISymUnmanagedVariable[] vars);
        [PreserveSig]
        int GetGlobalVariables(int cVars, out int pcVars, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] ISymUnmanagedVariable[] vars);
        [PreserveSig]
        int GetMethodFromDocumentPosition(ISymUnmanagedDocument document, int line, int column, [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod retVal);
        [PreserveSig]
        int GetSymAttribute(SymbolToken parent, [MarshalAs(UnmanagedType.LPWStr)] string name, int sizeBuffer, out int lengthBuffer, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] [In] [Out] byte[] buffer);
        [PreserveSig]
        int GetNamespaces(int cNameSpaces, out int pcNameSpaces, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] ISymUnmanagedNamespace[] namespaces);
        [PreserveSig]
        int Initialize([MarshalAs(UnmanagedType.Interface)] object importer, [MarshalAs(UnmanagedType.LPWStr)] string filename, [MarshalAs(UnmanagedType.LPWStr)] string searchPath, IStream stream);
        [PreserveSig]
        int UpdateSymbolStore([MarshalAs(UnmanagedType.LPWStr)] string filename, IStream stream);
        [PreserveSig]
        int ReplaceSymbolStore([MarshalAs(UnmanagedType.LPWStr)] string filename, IStream stream);
        [PreserveSig]
        int GetSymbolStoreFileName(int cchName, out int pcchName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] char[] szName);
        [PreserveSig]
        int GetMethodsFromDocumentPosition(ISymUnmanagedDocument document, int line, int column, int cMethod, out int pcMethod, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] [In] [Out] ISymUnmanagedMethod[] pRetVal);
        [PreserveSig]
        int GetDocumentVersion(ISymUnmanagedDocument pDoc, out int version, out bool pbCurrent);
        [PreserveSig]
        int GetMethodVersion(ISymUnmanagedMethod pMethod, out int version);
        [PreserveSig]
        int GetMethodByVersionPreRemap(SymbolToken methodToken, int version, [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod retVal);
        [PreserveSig]
        int GetSymAttributePreRemap(SymbolToken parent, [MarshalAs(UnmanagedType.LPWStr)] string name, int sizeBuffer, out int lengthBuffer, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] [In] [Out] byte[] buffer);
        [PreserveSig]
        int GetMethodsInDocument(ISymUnmanagedDocument document, uint cMethod, out uint pcMethod, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] [In] [Out] ISymUnmanagedMethod[] methods);
    }
}
