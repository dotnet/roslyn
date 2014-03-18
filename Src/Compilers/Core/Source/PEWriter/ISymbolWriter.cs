// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable 436 // SuppressUnmanagedCodeSecurityAttribute defined in source and mscorlib 

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Cci
{
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("B01FAFEB-C450-3A4D-BEEC-B4CEEC01E006"), SuppressUnmanagedCodeSecurity]
    internal interface ISymUnmanagedDocumentWriter
    {
        void SetSource(uint sourceSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] source);
        void SetCheckSum(Guid algorithmId, uint checkSumSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] checkSum);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("0B97726E-9E6D-4f05-9A26-424022093CAA"), SuppressUnmanagedCodeSecurity]
    internal interface ISymUnmanagedWriter2
    {
        ISymUnmanagedDocumentWriter DefineDocument(string url, ref Guid language, ref Guid languageVendor, ref Guid documentType);
        void SetUserEntryPoint(uint entryMethod);
        void OpenMethod(uint method);
        void CloseMethod();
        uint OpenScope(uint startOffset);
        void CloseScope(uint endOffset);
        void SetScopeRange(uint scopeID, uint startOffset, uint endOffset);
        void DefineLocalVariable(string name, uint attributes, uint sig, IntPtr signature, uint addrKind, uint addr1, uint addr2, uint startOffset, uint endOffset);
        void DefineParameter(string name, uint attributes, uint sequence, uint addrKind, uint addr1, uint addr2, uint addr3);
        void DefineField(uint parent, string name, uint attributes, uint sig, IntPtr signature, uint addrKind, uint addr1, uint addr2, uint addr3);
        void DefineGlobalVariable(string name, uint attributes, uint sig, IntPtr signature, uint addrKind, uint addr1, uint addr2, uint addr3);
        void Close();
        void SetSymAttribute(uint parent, string name, uint data, IntPtr signature);
        void OpenNamespace(string name);
        void CloseNamespace();
        void UsingNamespace(string fullName);
        void SetMethodSourceRange(ISymUnmanagedDocumentWriter startDoc, uint startLine, uint startColumn, object endDoc, uint endLine, uint endColumn);
        void Initialize([MarshalAs(UnmanagedType.IUnknown)] object emitter, string filename, [MarshalAs(UnmanagedType.IUnknown)] object ptrIStream, bool fullBuild);
        void GetDebugInfo(ref ImageDebugDirectory ptrIDD, uint dataCount, out uint dataCountPtr, IntPtr data);
        void DefineSequencePoints(ISymUnmanagedDocumentWriter document, uint count,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] offsets,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] lines,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] columns,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] endLines,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] endColumns);
        void RemapToken(uint oldToken, uint newToken);
        void Initialize2([MarshalAs(UnmanagedType.IUnknown)] object emitter, string tempfilename, [MarshalAs(UnmanagedType.IUnknown)] object ptrIStream, bool fullBuild, string finalfilename);
        void DefineConstant(string name, object value, uint sig, IntPtr signature);
        void Abort();
        void DefineLocalVariable2(string name, uint attributes, uint sigToken, uint addrKind, uint addr1, uint addr2, uint addr3, uint startOffset, uint endOffset);
        void DefineGlobalVariable2(string name, uint attributes, uint sigToken, uint addrKind, uint addr1, uint addr2, uint addr3);
        void DefineConstant2(string name, object value, uint sigToken);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("DCF7780D-BDE9-45DF-ACFE-21731A32000C"), SuppressUnmanagedCodeSecurity]
    internal interface ISymUnmanagedWriter5
    {
        //  ISymUnmanagedWriter, ISymUnmanagedWriter2, ISymUnmanagedWriter3, ISymUnmanagedWriter4
        void _VtblGap1_30();

        //  ISymUnmanagedWriter5
        void OpenMapTokensToSourceSpans();
        void CloseMapTokensToSourceSpans();
        void MapTokenToSourceSpan(uint token, ISymUnmanagedDocumentWriter document, uint startLine, uint startColumn, uint endLine, uint endColumn);
    }

    internal struct ImageDebugDirectory
    {
        internal int Characteristics;
        internal int TimeDateStamp;
        internal short MajorVersion;
        internal short MinorVersion;
        internal int Type;
        internal int SizeOfData;
        internal int AddressOfRawData;
        internal int PointerToRawData;

        // only here to shut up warnings
        internal ImageDebugDirectory(object dummy)
        {
            this.Characteristics = 0;
            this.TimeDateStamp = 0;
            this.MajorVersion = 0;
            this.MinorVersion = 0;
            this.Type = 0;
            this.SizeOfData = 0;
            this.AddressOfRawData = 0;
            this.PointerToRawData = 0;
        }
    }
}