// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Microsoft.DiaSymReader.PortablePdb
{
    // TODO:
    // ISymUnmanagedReaderSymbolSearchInfo?
    // ISymUnmanagedSourceServerModule?

    [ComVisible(true)]
    public sealed class SymReader : ISymUnmanagedReader3, ISymUnmanagedDispose
    {
        private readonly MetadataReader _reader;

        internal SymReader(MetadataReader reader)
        {
            _reader = reader;
        }

        public int Destroy()
        {
            return 0;
        }

        public int GetDocument(
            [MarshalAs(UnmanagedType.LPWStr)]string url,
            Guid language,          
            Guid languageVendor,    
            Guid documentType,      
            [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedDocument document)
        {
            throw new NotImplementedException();
        }

        public int GetDocuments(
            int bufferLength, 
            out int count, 
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedDocument[] documents)
        {
            throw new NotImplementedException();
        }

        public int GetDocumentVersion(ISymUnmanagedDocument document, out int version, out bool isCurrent)
        {
            throw new NotImplementedException();
        }

        public int GetGlobalVariables(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedVariable[] variables)
        {
            throw new NotImplementedException();
        }

        public int GetMethod(int methodToken, [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedMethod method)
        {
            throw new NotImplementedException();
        }

        public int GetMethodByVersion(int methodToken, int version, [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedMethod method)
        {
            throw new NotImplementedException();
        }

        public int GetMethodByVersionPreRemap(int methodToken, int version, [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedMethod method)
        {
            throw new NotImplementedException();
        }

        public int GetMethodFromDocumentPosition(ISymUnmanagedDocument document, int line, int column, [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedMethod method)
        {
            throw new NotImplementedException();
        }

        public int GetMethodsFromDocumentPosition(ISymUnmanagedDocument document, int line, int column, int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), Out]ISymUnmanagedMethod[] methods)
        {
            throw new NotImplementedException();
        }

        public int GetMethodsInDocument(ISymUnmanagedDocument document, int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out]ISymUnmanagedMethod[] methods)
        {
            throw new NotImplementedException();
        }

        public int GetMethodVersion(ISymUnmanagedMethod method, out int version)
        {
            throw new NotImplementedException();
        }

        public int GetNamespaces(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedNamespace[] namespaces)
        {
            throw new NotImplementedException();
        }

        public int GetSymAttribute(int methodToken, [MarshalAs(UnmanagedType.LPWStr)]string name, int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out]byte[] customDebugInformation)
        {
            throw new NotImplementedException();
        }

        public int GetSymAttributeByVersion(int methodToken, int version, [MarshalAs(UnmanagedType.LPWStr)]string name, int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), Out]byte[] customDebugInformation)
        {
            throw new NotImplementedException();
        }

        public int GetSymAttributeByVersionPreRemap(int methodToken, int version, [MarshalAs(UnmanagedType.LPWStr)]string name, int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), Out]byte[] customDebugInformation)
        {
            throw new NotImplementedException();
        }

        public int GetSymAttributePreRemap(int methodToken, [MarshalAs(UnmanagedType.LPWStr)]string name, int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out]byte[] customDebugInformation)
        {
            throw new NotImplementedException();
        }

        public int GetSymbolStoreFileName(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]char[] name)
        {
            throw new NotImplementedException();
        }

        public int GetUserEntryPoint(out int methodToken)
        {
            throw new NotImplementedException();
        }

        public int GetVariables(int methodToken, int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out]ISymUnmanagedVariable[] variables)
        {
            throw new NotImplementedException();
        }

        public int Initialize([MarshalAs(UnmanagedType.Interface)]object metadataImporter, [MarshalAs(UnmanagedType.LPWStr)]string fileName, [MarshalAs(UnmanagedType.LPWStr)]string searchPath, IStream stream)
        {
            throw new NotImplementedException();
        }

        public int ReplaceSymbolStore([MarshalAs(UnmanagedType.LPWStr)]string fileName, IStream stream)
        {
            throw new NotImplementedException();
        }

        public int UpdateSymbolStore([MarshalAs(UnmanagedType.LPWStr)]string fileName, IStream stream)
        {
            throw new NotImplementedException();
        }
    }
}
