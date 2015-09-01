﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Microsoft.DiaSymReader.PortablePdb
{
    // TODO:
    // ISymUnmanagedReaderSymbolSearchInfo?
    // ISymUnmanagedSourceServerModule?

    [ComVisible(false)]
    public sealed class SymReader : ISymUnmanagedReader3, ISymUnmanagedDispose
    {
        private readonly PortablePdbReader _pdbReader;
        private readonly Lazy<DocumentMap> _lazyDocumentMap;
        private readonly Lazy<bool> _lazyVbSemantics;
        private readonly Lazy<MethodMap> _lazyMethodMap;

        private int _version;

        // Takes ownership of <paramref name="pdbReader"/>.
        internal SymReader(PortablePdbReader pdbReader)
        {
            Debug.Assert(pdbReader != null);

            _pdbReader = pdbReader;
            _version = 1;

            _lazyDocumentMap = new Lazy<DocumentMap>(() => new DocumentMap(MetadataReader));
            _lazyVbSemantics = new Lazy<bool>(() => IsVisualBasicAssembly());
            _lazyMethodMap = new Lazy<MethodMap>(() => new MethodMap(MetadataReader));
        }

        internal MetadataReader MetadataReader => _pdbReader.MetadataReader;
        internal PortablePdbReader PdbReader => _pdbReader;
        internal Lazy<bool> VbSemantics => _lazyVbSemantics;

        public int Destroy()
        {
            if (_pdbReader.IsDisposed)
            {
                return HResult.S_OK;
            }

            _pdbReader.Dispose();
            return HResult.S_FALSE;
        }

        private bool IsVisualBasicAssembly()
        {
            var reader = MetadataReader;

            foreach (var cdiHandle in reader.GetCustomDebugInformation(Handle.ModuleDefinition))
            {
                if (reader.GetGuid(reader.GetCustomDebugInformation(cdiHandle).Kind) == MetadataUtilities.VbDefaultNamespaceId)
                {
                    return true;
                }
            }

            return false;
        }

        internal MethodMap GetMethodMap()
        {
            if (_pdbReader.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(SymReader));
            }

            return _lazyMethodMap.Value;
        }

        internal SymDocument AsSymDocument(ISymUnmanagedDocument document)
        {
            var symDocument = document as SymDocument;
            return (symDocument?.SymReader == this) ? symDocument : null;
        }

        public int GetDocument(
            [MarshalAs(UnmanagedType.LPWStr)]string url,
            Guid language,          
            Guid languageVendor,    
            Guid documentType,      
            [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedDocument document)
        {
            DocumentHandle documentHandle;

            // SymReader: language, vendor and type parameters are ignored.

            if (_pdbReader.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(SymReader));
            }

            if (_lazyDocumentMap.Value.TryGetDocument(url, out documentHandle))
            {
                document = new SymDocument(this, documentHandle);
                return HResult.S_OK;
            }

            document = null;
            return HResult.S_FALSE;
        }

        public int GetDocuments(
            int bufferLength, 
            out int count, 
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedDocument[] documents)
        {
            count = MetadataReader.Documents.Count;

            if (bufferLength == 0)
            {
                return HResult.S_OK;
            }

            int i = 0;
            foreach (var documentHandle in MetadataReader.Documents)
            {
                if (i >= bufferLength)
                {
                    break;
                }

                documents[i++] = new SymDocument(this, documentHandle);
            }

            return HResult.S_OK;
        }

        public int GetDocumentVersion(ISymUnmanagedDocument document, out int version, out bool isCurrent)
        {
            // SymReader always returns the same values
            version = 1;
            isCurrent = true;
            return HResult.E_NOTIMPL;
        }

        public int GetGlobalVariables(
            int bufferLength,
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedVariable[] variables)
        {
            // SymReader doesn't support.
            count = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetMethod(int methodToken, [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedMethod method)
        {
            return GetMethodByVersion(methodToken, _version, out method);
        }

        public int GetMethodByVersion(
            int methodToken, 
            int version,
            [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedMethod method)
        {
            if (version != _version)
            {
                method = null;
                return HResult.E_INVALIDARG;
            }

            var handle = MetadataTokens.Handle(methodToken);
            if (handle.Kind != HandleKind.MethodDefinition)
            {
                method = null;
                return HResult.E_INVALIDARG;
            }

            var methodBodyHandle = ((MethodDefinitionHandle)handle).ToMethodBodyHandle();

            var methodBody = MetadataReader.GetMethodBody(methodBodyHandle);
            if (methodBody.SequencePoints.IsNil)
            {
                // no debug info for the method
                method = null;
                return HResult.E_FAIL;
            }

            method = new SymMethod(this, methodBodyHandle);
            return HResult.S_OK;
        }

        public int GetMethodByVersionPreRemap(int methodToken, int version, [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedMethod method)
        {
            // TODO:
            throw new NotSupportedException();
        }

        public int GetMethodFromDocumentPosition(
            ISymUnmanagedDocument document, 
            int line, 
            int column, 
            [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedMethod method)
        {
            var symDocument = AsSymDocument(document);
            if (symDocument == null)
            {
                method = null;
                return HResult.E_INVALIDARG;
            }

            var methodBodyHandles = GetMethodMap().GetMethodsContainingLine(symDocument.Handle, line);
            if (methodBodyHandles == null)
            {
                method = null;
                return HResult.E_FAIL;
            }

            var comparer = HandleComparer.Default;
            var candidate = default(MethodBodyHandle);
            foreach (var methodBodyHandle in methodBodyHandles)
            {
                if (candidate.IsNil || comparer.Compare(methodBodyHandle, candidate) < 0)
                {
                    candidate = methodBodyHandle;
                }
            }

            if (candidate.IsNil)
            {
                method = null;
                return HResult.E_FAIL;
            }

            method = new SymMethod(this, candidate);
            return HResult.S_OK;
        }

        public int GetMethodsFromDocumentPosition(
            ISymUnmanagedDocument document, 
            int line, 
            int column, 
            int bufferLength,
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), Out]ISymUnmanagedMethod[] methods)
        {
            var symDocument = AsSymDocument(document);
            if (symDocument == null)
            {
                count = 0;
                return HResult.E_INVALIDARG;
            }

            var methodBodyHandles = GetMethodMap().GetMethodsContainingLine(symDocument.Handle, line);
            if (methodBodyHandles == null)
            {
                count = 0;
                return HResult.E_FAIL;
            }

            if (bufferLength > 0)
            {
                int i = 0;
                foreach (var methodBodyHandle in methodBodyHandles)
                {
                    if (i == bufferLength)
                    {
                        break;
                    }

                    methods[i++] = new SymMethod(this, methodBodyHandle);
                }

                count = i;

                if (i > 1)
                {
                    Array.Sort(methods, 0, i, SymMethod.ByHandleComparer.Default);
                }
            }
            else
            {
                count = methodBodyHandles.Count();
            }

            return HResult.S_OK;
        }

        public int GetMethodsInDocument(
            ISymUnmanagedDocument document,
            int bufferLength, 
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out]ISymUnmanagedMethod[] methods)
        {
            var symDocument = AsSymDocument(document);
            if (symDocument == null)
            {
                count = 0;
                return HResult.E_INVALIDARG;
            }

            var extentsByMethod = GetMethodMap().GetMethodExtents(symDocument.Handle);
            if (bufferLength > 0)
            {
                int actualCount = Math.Min(extentsByMethod.Length, bufferLength);
                for (int i = 0; i < actualCount; i++)
                {
                    methods[i] = new SymMethod(this, extentsByMethod[i].Method);
                }

                count = actualCount;
            }
            else
            {
                count = extentsByMethod.Length;
            }

            count = 0;
            return HResult.S_OK;
        }

        internal int GetMethodSourceExtentInDocument(ISymUnmanagedDocument document, SymMethod method, out int startLine, out int endLine)
        {
            var symDocument = AsSymDocument(document);
            if (symDocument == null)
            {
                startLine = endLine = 0;
                return HResult.E_INVALIDARG;
            }

            var map = GetMethodMap();
            if (!map.TryGetMethodSourceExtent(symDocument.Handle, method.BodyHandle, out startLine, out endLine))
            {
                startLine = endLine = 0;
                return HResult.E_FAIL;
            }

            return HResult.S_OK;
        }

        public int GetMethodVersion(ISymUnmanagedMethod method, out int version)
        {
            version = _version;
            return HResult.S_OK;
        }

        public int GetNamespaces(
            int bufferLength, 
            out int count, 
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedNamespace[] namespaces)
        {
            // SymReader doesn't support
            count = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetSymAttribute(int methodToken, 
            [MarshalAs(UnmanagedType.LPWStr)]string name, 
            int bufferLength,
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out]byte[] customDebugInformation)
        {
            return GetSymAttributeByVersion(methodToken, 1, name, bufferLength, out count, customDebugInformation);
        }

        public int GetSymAttributeByVersion(
            int methodToken, 
            int version, 
            [MarshalAs(UnmanagedType.LPWStr)]string name,
            int bufferLength,
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), Out]byte[] customDebugInformation)
        {
            if ((bufferLength != 0) != (customDebugInformation != null))
            {
                count = 0;
                return HResult.E_INVALIDARG;
            }

            if (version != _version)
            {
                count = 0;
                return HResult.E_INVALIDARG;
            }

            if (name == "<PortablePdbImage>")
            {
                count = _pdbReader.ImageSize;

                if (bufferLength == 0)
                {
                    return HResult.S_FALSE;
                }

                Marshal.Copy(_pdbReader.ImagePtr, customDebugInformation, 0, bufferLength);
                return HResult.S_OK;
            }

            count = 0;
            return HResult.S_FALSE;
        }

        public int GetSymAttributePreRemap(
            int methodToken,
            [MarshalAs(UnmanagedType.LPWStr)]string name,
            int bufferLength,
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out]byte[] customDebugInformation)
        {
            // TODO:
            throw new NotSupportedException();
        }

        public int GetSymAttributeByVersionPreRemap(
            int methodToken,
            int version,
            [MarshalAs(UnmanagedType.LPWStr)]string name,
            int bufferLength,
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), Out]byte[] customDebugInformation)
        {
            // TODO:
            throw new NotSupportedException();
        }

        public int GetSymbolStoreFileName(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]char[] name)
        {
            // TODO:
            throw new NotImplementedException();
        }

        public int GetUserEntryPoint(out int methodToken)
        {
            var handle = MetadataReader.DebugMetadataHeader.EntryPoint;
            if (!handle.IsNil)
            {
                methodToken = MetadataTokens.GetToken(handle);
                return HResult.S_OK;
            }

            methodToken = 0;
            return HResult.E_FAIL;
        }

        public int GetVariables(
            int methodToken, 
            int bufferLength, 
            out int count, 
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out]ISymUnmanagedVariable[] variables)
        {
            // SymReader doesn't support non-local variables.
            count = 0;
            return HResult.E_NOTIMPL;
        }

        public int Initialize(
            [MarshalAs(UnmanagedType.Interface)]object metadataImporter,
            [MarshalAs(UnmanagedType.LPWStr)]string fileName, 
            [MarshalAs(UnmanagedType.LPWStr)]string searchPath, 
            IStream stream)
        {
            return HResult.S_OK;
        }

        public int ReplaceSymbolStore([MarshalAs(UnmanagedType.LPWStr)]string fileName, IStream stream)
        {
            // TODO:
            throw new NotImplementedException();
        }

        public int UpdateSymbolStore([MarshalAs(UnmanagedType.LPWStr)]string fileName, IStream stream)
        {
            // TODO:
            throw new NotImplementedException();
        }
    }
}