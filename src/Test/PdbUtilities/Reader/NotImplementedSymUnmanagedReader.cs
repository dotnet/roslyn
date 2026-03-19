// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

extern alias DSR;
using System;
using System.Runtime.InteropServices.ComTypes;
using DSR::Microsoft.DiaSymReader;

namespace Roslyn.Test.Utilities
{
    public sealed class NotImplementedSymUnmanagedReader : ISymUnmanagedReader5
    {
        public static readonly NotImplementedSymUnmanagedReader Instance = new NotImplementedSymUnmanagedReader();

        private NotImplementedSymUnmanagedReader() { }

        public int GetDocument(string url, Guid language, Guid languageVendor, Guid documentType, out ISymUnmanagedDocument retVal)
        {
            retVal = null;
            return HResult.E_NOTIMPL;
        }

        public int GetDocuments(int cDocs, out int pcDocs, ISymUnmanagedDocument[] pDocs)
        {
            pcDocs = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetDocumentVersion(ISymUnmanagedDocument pDoc, out int version, out bool pbCurrent)
        {
            version = 0;
            pbCurrent = false;
            return HResult.E_NOTIMPL;
        }

        public int GetGlobalVariables(int cVars, out int pcVars, ISymUnmanagedVariable[] vars)
        {
            pcVars = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetMethod(int methodToken, out ISymUnmanagedMethod retVal)
        {
            retVal = null;
            return HResult.E_NOTIMPL;
        }

        public int GetMethodByVersion(int methodToken, int version, out ISymUnmanagedMethod retVal)
        {
            retVal = null;
            return HResult.E_NOTIMPL;
        }

        public int GetMethodByVersionPreRemap(int methodToken, int version, out ISymUnmanagedMethod retVal)
        {
            retVal = null;
            return HResult.E_NOTIMPL;
        }

        public int GetMethodFromDocumentPosition(ISymUnmanagedDocument document, int line, int column, out ISymUnmanagedMethod retVal)
        {
            retVal = null;
            return HResult.E_NOTIMPL;
        }

        public int GetMethodsFromDocumentPosition(ISymUnmanagedDocument document, int line, int column, int cMethod, out int pcMethod, ISymUnmanagedMethod[] pRetVal)
        {
            pcMethod = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetMethodsInDocument(ISymUnmanagedDocument document, int cMethod, out int pcMethod, ISymUnmanagedMethod[] methods)
        {
            pcMethod = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetMethodVersion(ISymUnmanagedMethod pMethod, out int version)
        {
            version = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetNamespaces(int cNameSpaces, out int pcNameSpaces, ISymUnmanagedNamespace[] namespaces)
        {
            pcNameSpaces = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetSymAttribute(int parent, string name, int sizeBuffer, out int lengthBuffer, byte[] buffer)
        {
            lengthBuffer = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetSymAttributeByVersion(int methodToken, int version, string name, int bufferLength, out int count, byte[] customDebugInformation)
        {
            count = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetSymAttributeByVersionPreRemap(int methodToken, int version, string name, int bufferLength, out int count, byte[] customDebugInformation)
        {
            count = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetSymAttributePreRemap(int parent, string name, int sizeBuffer, out int lengthBuffer, byte[] buffer)
        {
            lengthBuffer = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetSymbolStoreFileName(int cchName, out int pcchName, char[] szName)
        {
            pcchName = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetUserEntryPoint(out int EntryPoint)
        {
            EntryPoint = default(int);
            return HResult.E_NOTIMPL;
        }

        public int GetVariables(int parent, int cVars, out int pcVars, ISymUnmanagedVariable[] vars)
        {
            pcVars = 0;
            return HResult.E_NOTIMPL;
        }

        public int Initialize(object importer, string filename, string searchPath, IStream stream)
        {
            return HResult.E_NOTIMPL;
        }

        public int ReplaceSymbolStore(string filename, IStream stream)
        {
            return HResult.E_NOTIMPL;
        }

        public int UpdateSymbolStore(string filename, IStream stream)
        {
            return HResult.E_NOTIMPL;
        }

        public int MatchesModule(Guid guid, uint stamp, int age, out bool result)
        {
            result = false;
            return HResult.E_NOTIMPL;
        }

        public unsafe int GetPortableDebugMetadata(out byte* metadata, out int size)
        {
            metadata = null;
            size = 0;
            return HResult.E_NOTIMPL;
        }

        public unsafe int GetSourceServerData(out byte* data, out int size)
        {
            data = null;
            size = 0;
            return HResult.E_NOTIMPL;
        }

        public unsafe int GetPortableDebugMetadataByVersion(int version, out byte* metadata, out int size)
        {
            metadata = null;
            size = 0;
            return HResult.E_NOTIMPL;
        }
    }
}
