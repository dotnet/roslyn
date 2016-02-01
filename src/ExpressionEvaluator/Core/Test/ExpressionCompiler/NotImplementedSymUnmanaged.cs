// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.DiaSymReader;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class NotImplementedSymUnmanagedReader : ISymUnmanagedReader, ISymUnmanagedReader2, ISymUnmanagedReader3
    {
        public static readonly NotImplementedSymUnmanagedReader Instance = new NotImplementedSymUnmanagedReader();

        private NotImplementedSymUnmanagedReader() { }

        public int GetDocument(string url, Guid language, Guid languageVendor, Guid documentType, out ISymUnmanagedDocument retVal)
        {
            retVal = null;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetDocuments(int cDocs, out int pcDocs, ISymUnmanagedDocument[] pDocs)
        {
            pcDocs = 0;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetDocumentVersion(ISymUnmanagedDocument pDoc, out int version, out bool pbCurrent)
        {
            version = 0;
            pbCurrent = false;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetGlobalVariables(int cVars, out int pcVars, ISymUnmanagedVariable[] vars)
        {
            pcVars = 0;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetMethod(int methodToken, out ISymUnmanagedMethod retVal)
        {
            retVal = null;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetMethodByVersion(int methodToken, int version, out ISymUnmanagedMethod retVal)
        {
            retVal = null;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetMethodByVersionPreRemap(int methodToken, int version, out ISymUnmanagedMethod retVal)
        {
            retVal = null;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetMethodFromDocumentPosition(ISymUnmanagedDocument document, int line, int column, out ISymUnmanagedMethod retVal)
        {
            retVal = null;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetMethodsFromDocumentPosition(ISymUnmanagedDocument document, int line, int column, int cMethod, out int pcMethod, ISymUnmanagedMethod[] pRetVal)
        {
            pcMethod = 0;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetMethodsInDocument(ISymUnmanagedDocument document, int cMethod, out int pcMethod, ISymUnmanagedMethod[] methods)
        {
            pcMethod = 0;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetMethodVersion(ISymUnmanagedMethod pMethod, out int version)
        {
            version = 0;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetNamespaces(int cNameSpaces, out int pcNameSpaces, ISymUnmanagedNamespace[] namespaces)
        {
            pcNameSpaces = 0;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetSymAttribute(int parent, string name, int sizeBuffer, out int lengthBuffer, byte[] buffer)
        {
            lengthBuffer = 0;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetSymAttributeByVersion(int methodToken, int version, string name, int bufferLength, out int count, byte[] customDebugInformation)
        {
            count = 0;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetSymAttributeByVersionPreRemap(int methodToken, int version, string name, int bufferLength, out int count, byte[] customDebugInformation)
        {
            count = 0;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetSymAttributePreRemap(int parent, string name, int sizeBuffer, out int lengthBuffer, byte[] buffer)
        {
            lengthBuffer = 0;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetSymbolStoreFileName(int cchName, out int pcchName, char[] szName)
        {
            pcchName = 0;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetUserEntryPoint(out int EntryPoint)
        {
            EntryPoint = default(int);
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int GetVariables(int parent, int cVars, out int pcVars, ISymUnmanagedVariable[] vars)
        {
            pcVars = 0;
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int Initialize(object importer, string filename, string searchPath, IStream stream)
        {
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int ReplaceSymbolStore(string filename, IStream stream)
        {
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }

        public int UpdateSymbolStore(string filename, IStream stream)
        {
            return SymUnmanagedReaderExtensions.E_NOTIMPL;
        }
    }
}
