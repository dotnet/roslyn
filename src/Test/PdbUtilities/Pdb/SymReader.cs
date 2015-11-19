// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.DiaSymReader;
using Roslyn.Utilities;

namespace Roslyn.Test.PdbUtilities
{
    // TODO: remove, use SymReaderFactory instead
    public sealed class SymReader : ISymUnmanagedReader, ISymUnmanagedReader2, ISymUnmanagedReader3, IDisposable
    {
        /// <summary>
        /// Mock implementation: instead of a single reader with multiple versions, we'll use an array
        /// of readers - each with a single version.  We do this so that we can implement 
        /// <see cref="ISymUnmanagedReader3.GetSymAttributeByVersion"/> on top of 
        /// <see cref="ISymUnmanagedReader.GetSymAttribute"/>.
        /// </summary>
        private readonly ISymUnmanagedReader[] _readerVersions;

        private readonly DummyMetadataImport _metadataImport;
        private readonly PEReader _peReaderOpt;

        private bool _isDisposed;

        public SymReader(Stream pdbStream)
            : this(new[] { pdbStream }, null, null)
        {
        }

        public SymReader(byte[] pdbImage)
           : this(new[] { new MemoryStream(pdbImage) }, null, null)
        {
        }

        public SymReader(byte[] pdbImage, byte[] peImage)
            : this(new[] { new MemoryStream(pdbImage) }, new MemoryStream(peImage), null)
        {
        }

        public SymReader(Stream pdbStream, Stream peStream)
            : this(new[] { pdbStream }, peStream, null)
        {
        }

        public SymReader(Stream pdbStream, MetadataReader metadataReader)
            : this(new[] { pdbStream }, null, metadataReader)
        {
        }

        public SymReader(Stream[] pdbStreamsByVersion, Stream peStreamOpt, MetadataReader metadataReaderOpt)
        {
            if (peStreamOpt != null)
            {
                _peReaderOpt = new PEReader(peStreamOpt);
                _metadataImport = new DummyMetadataImport(_peReaderOpt.GetMetadataReader());
            }
            else
            {
                _metadataImport = new DummyMetadataImport(metadataReaderOpt);
            }

            _readerVersions = pdbStreamsByVersion.Select(
                pdbStream => CreateReader(pdbStream, _metadataImport)).ToArray();

            // If ISymUnmanagedReader3 is available, then we shouldn't be passing in multiple byte streams - one should suffice.
            Debug.Assert(!(UnversionedReader is ISymUnmanagedReader3) || _readerVersions.Length == 1);
        }

        private static ISymUnmanagedReader CreateReader(Stream pdbStream, object metadataImporter)
        {
            // NOTE: The product uses a different GUID (Microsoft.CodeAnalysis.ExpressionEvaluator.DkmUtilities.s_symUnmanagedReaderClassId).
            Guid corSymReaderSxS = new Guid("0A3976C5-4529-4ef8-B0B0-42EED37082CD");
            var reader = (ISymUnmanagedReader)Activator.CreateInstance(Marshal.GetTypeFromCLSID(corSymReaderSxS));
            int hr = reader.Initialize(metadataImporter, null, null, new ComStreamWrapper(pdbStream));
            SymUnmanagedReaderExtensions.ThrowExceptionForHR(hr);
            return reader;
        }

        private ISymUnmanagedReader UnversionedReader => _readerVersions[0];

        public int GetDocuments(int cDocs, out int pcDocs, ISymUnmanagedDocument[] pDocs)
        {
            return UnversionedReader.GetDocuments(cDocs, out pcDocs, pDocs);
        }

        public int GetMethod(int methodToken, out ISymUnmanagedMethod retVal)
        {
            // The EE should never be calling ISymUnmanagedReader.GetMethod.  In order to account
            // for EnC updates, it should always be calling GetMethodByVersion instead.
            throw ExceptionUtilities.Unreachable;
        }

        public int GetMethodByVersion(int methodToken, int version, out ISymUnmanagedMethod retVal)
        {
            // Versions are 1-based.
            Debug.Assert(version >= 1);
            var reader = _readerVersions[version - 1];
            version = _readerVersions.Length > 1 ? 1 : version;
            return reader.GetMethodByVersion(methodToken, version, out retVal);
        }

        public int GetSymAttribute(int token, string name, int sizeBuffer, out int lengthBuffer, byte[] buffer)
        {
            // The EE should never be calling ISymUnmanagedReader.GetSymAttribute.  
            // In order to account for EnC updates, it should always be calling 
            // ISymUnmanagedReader3.GetSymAttributeByVersion instead.
            throw ExceptionUtilities.Unreachable;
        }

        public int GetSymAttributeByVersion(int methodToken, int version, string name, int bufferLength, out int count, byte[] customDebugInformation)
        {
            // Versions are 1-based.
            Debug.Assert(version >= 1);
            return _readerVersions[version - 1].GetSymAttribute(methodToken, name, bufferLength, out count, customDebugInformation);
        }

        public int GetUserEntryPoint(out int entryPoint)
        {
            return UnversionedReader.GetUserEntryPoint(out entryPoint);
        }

        void IDisposable.Dispose()
        {
            if (!_isDisposed)
            {
                for (int i = 0; i < _readerVersions.Length; i++)
                {
                    int hr = (_readerVersions[i] as ISymUnmanagedDispose).Destroy();
                    SymUnmanagedReaderExtensions.ThrowExceptionForHR(hr);
                    _readerVersions[i] = null;
                }

                _peReaderOpt?.Dispose();
                _metadataImport.Dispose();

                _isDisposed = true;
            }
        }

        public int GetDocument(string url, Guid language, Guid languageVendor, Guid documentType, out ISymUnmanagedDocument document)
        {
            return UnversionedReader.GetDocument(url, language, languageVendor, documentType, out document);
        }

        public int GetVariables(int methodToken, int bufferLength, out int count, ISymUnmanagedVariable[] variables)
        {
            return UnversionedReader.GetVariables(methodToken, bufferLength, out count, variables);
        }

        public int GetGlobalVariables(int bufferLength, out int count, ISymUnmanagedVariable[] variables)
        {
            return UnversionedReader.GetGlobalVariables(bufferLength, out count, variables);
        }

        public int GetMethodFromDocumentPosition(ISymUnmanagedDocument document, int line, int column, out ISymUnmanagedMethod method)
        {
            return UnversionedReader.GetMethodFromDocumentPosition(document, line, column, out method);
        }

        public int GetNamespaces(int bufferLength, out int count, ISymUnmanagedNamespace[] namespaces)
        {
            return UnversionedReader.GetNamespaces(bufferLength, out count, namespaces);
        }

        public int Initialize(object metadataImporter, string fileName, string searchPath, IStream stream)
        {
            return UnversionedReader.Initialize(metadataImporter, fileName, searchPath, stream);
        }

        public int UpdateSymbolStore(string fileName, IStream stream)
        {
            return UnversionedReader.UpdateSymbolStore(fileName, stream);
        }

        public int ReplaceSymbolStore(string fileName, IStream stream)
        {
            return UnversionedReader.ReplaceSymbolStore(fileName, stream);
        }

        public int GetSymbolStoreFileName(int bufferLength, out int count, char[] name)
        {
            return UnversionedReader.GetSymbolStoreFileName(bufferLength, out count, name);
        }

        public int GetMethodsFromDocumentPosition(ISymUnmanagedDocument document, int line, int column, int bufferLength, out int count, ISymUnmanagedMethod[] methods)
        {
            return UnversionedReader.GetMethodsFromDocumentPosition(document, line, column, bufferLength, out count, methods);
        }

        public int GetDocumentVersion(ISymUnmanagedDocument document, out int version, out bool isCurrent)
        {
            return UnversionedReader.GetDocumentVersion(document, out version, out isCurrent);
        }

        public int GetMethodVersion(ISymUnmanagedMethod method, out int version)
        {
            return UnversionedReader.GetMethodVersion(method, out version);
        }

        public int GetMethodByVersionPreRemap(int methodToken, int version, out ISymUnmanagedMethod method)
        {
            throw new NotImplementedException();
        }

        public int GetSymAttributePreRemap(int methodToken, string name, int bufferLength, out int count, byte[] customDebugInformation)
        {
            throw new NotImplementedException();
        }

        public int GetMethodsInDocument(ISymUnmanagedDocument document, int bufferLength, out int count, ISymUnmanagedMethod[] methods)
        {
            throw new NotImplementedException();
        }

        public int GetSymAttributeByVersionPreRemap(int methodToken, int version, string name, int bufferLength, out int count, byte[] customDebugInformation)
        {
            throw new NotImplementedException();
        }
    }
}
