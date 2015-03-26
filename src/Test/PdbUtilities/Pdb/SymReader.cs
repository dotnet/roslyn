// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.DiaSymReader;
using Roslyn.Utilities;

namespace Roslyn.Test.PdbUtilities
{
    public sealed class SymReader : ISymUnmanagedReader, ISymUnmanagedReader2, ISymUnmanagedReader3, IDisposable
    {
        /// <summary>
        /// Mock implementation: instead of a single reader with multiple versions, we'll use an array
        /// of readers - each with a single version.  We do this so that we can implement 
        /// <see cref="ISymUnmanagedReader3.GetSymAttributeByVersion"/> on top of 
        /// <see cref="ISymUnmanagedReader.GetSymAttribute"/>.
        /// </summary>
        private readonly ISymUnmanagedReader[] _readerVersions;
        private readonly ImmutableDictionary<string, byte[]> _constantSignaturesOpt;

        private bool _isDisposed;

        public SymReader(byte[] pdbBytes, ImmutableDictionary<string, byte[]> constantSignaturesOpt = null)
            : this(new[] { new MemoryStream(pdbBytes) }, constantSignaturesOpt)
        {
        }

        public SymReader(Stream pdbStream, ImmutableDictionary<string, byte[]> constantSignaturesOpt = null)
            : this(new[] { pdbStream }, constantSignaturesOpt)
        {
        }

        public SymReader(Stream[] pdbStreamsByVersion, ImmutableDictionary<string, byte[]> constantSignaturesOpt = null)
        {
            _readerVersions = pdbStreamsByVersion.Select(
                pdbStream =>
                    SymUnmanagedReaderTestExtensions.CreateReader(pdbStream, DummyMetadataImport.Instance)).ToArray();

            // If ISymUnmanagedReader3 is available, then we shouldn't be passing in multiple byte streams - one should suffice.
            Debug.Assert(!(UnversionedReader is ISymUnmanagedReader3) || _readerVersions.Length == 1);

            _constantSignaturesOpt = constantSignaturesOpt;
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
            var hr = reader.GetMethodByVersion(methodToken, version, out retVal);
            if (retVal != null)
            {
                var asyncMethod = retVal as ISymUnmanagedAsyncMethod;
                retVal = asyncMethod == null 
                    ? (ISymUnmanagedMethod)new SymMethod(this, retVal)
                    : new SymAsyncMethod(this, asyncMethod);
            }
            return hr;
        }

        public int GetSymAttribute(int token, string name, int sizeBuffer, out int lengthBuffer, byte[] buffer)
        {
            // The EE should never be calling ISymUnmanagedReader.GetSymAttribute.  
            // In order to account for EnC updates, it should always be calling 
            // ISymUnmanagedReader3.GetSymAttributeByVersion instead.
            // TODO (DevDiv #1145183): throw ExceptionUtilities.Unreachable;

            return UnversionedReader.GetSymAttribute(token, name, sizeBuffer, out lengthBuffer, buffer);
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

        private sealed class SymMethod : ISymUnmanagedMethod
        {
            private readonly SymReader _reader;
            private readonly ISymUnmanagedMethod _method;

            internal SymMethod(SymReader reader, ISymUnmanagedMethod method)
            {
                Debug.Assert(!(method is ISymUnmanagedAsyncMethod), "Use SymAsyncMethod.");
                _reader = reader;
                _method = method;
            }

            public int GetRootScope(out ISymUnmanagedScope retVal)
            {
                _method.GetRootScope(out retVal);
                if (retVal != null)
                {
                    retVal = new SymScope(_reader, retVal);
                }
                return SymUnmanagedReaderExtensions.S_OK;
            }

            public int GetScopeFromOffset(int offset, out ISymUnmanagedScope retVal)
            {
                throw new NotImplementedException();
            }

            public int GetSequencePointCount(out int retVal)
            {
                return _method.GetSequencePointCount(out retVal);
            }

            public int GetToken(out int token)
            {
                throw new NotImplementedException();
            }

            public int GetNamespace(out ISymUnmanagedNamespace retVal)
            {
                throw new NotImplementedException();
            }

            public int GetOffset(ISymUnmanagedDocument document, int line, int column, out int retVal)
            {
                throw new NotImplementedException();
            }

            public int GetParameters(int cParams, out int pcParams, ISymUnmanagedVariable[] parms)
            {
                throw new NotImplementedException();
            }

            public int GetRanges(ISymUnmanagedDocument document, int line, int column, int cRanges, out int pcRanges, int[] ranges)
            {
                throw new NotImplementedException();
            }

            public int GetSequencePoints(
                int cPoints,
                out int pcPoints,
                int[] offsets,
                ISymUnmanagedDocument[] documents,
                int[] lines,
                int[] columns,
                int[] endLines,
                int[] endColumns)
            {
                _method.GetSequencePoints(cPoints, out pcPoints, offsets, documents, lines, columns, endLines, endColumns);
                return SymUnmanagedReaderExtensions.S_OK;
            }

            public int GetSourceStartEnd(ISymUnmanagedDocument[] docs, int[] lines, int[] columns, out bool retVal)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class SymAsyncMethod : ISymUnmanagedMethod, ISymUnmanagedAsyncMethod
        {
            private readonly SymReader _reader;
            private readonly ISymUnmanagedAsyncMethod _method;

            internal SymAsyncMethod(SymReader reader, ISymUnmanagedAsyncMethod method)
            {
                _reader = reader;
                _method = method;
            }

            public int GetRootScope(out ISymUnmanagedScope retVal)
            {
                ((ISymUnmanagedMethod)_method).GetRootScope(out retVal);
                if (retVal != null)
                {
                    retVal = new SymScope(_reader, retVal);
                }
                return SymUnmanagedReaderExtensions.S_OK;
            }

            public int GetScopeFromOffset(int offset, out ISymUnmanagedScope retVal)
            {
                throw new NotImplementedException();
            }

            public int GetSequencePointCount(out int retVal)
            {
                return ((ISymUnmanagedMethod)_method).GetSequencePointCount(out retVal);
            }

            public int GetToken(out int token)
            {
                throw new NotImplementedException();
            }

            public int GetNamespace(out ISymUnmanagedNamespace retVal)
            {
                throw new NotImplementedException();
            }

            public int GetOffset(ISymUnmanagedDocument document, int line, int column, out int retVal)
            {
                throw new NotImplementedException();
            }

            public int GetParameters(int cParams, out int pcParams, ISymUnmanagedVariable[] parms)
            {
                throw new NotImplementedException();
            }

            public int GetRanges(ISymUnmanagedDocument document, int line, int column, int cRanges, out int pcRanges, int[] ranges)
            {
                throw new NotImplementedException();
            }

            public int GetSequencePoints(
                int cPoints,
                out int pcPoints,
                int[] offsets,
                ISymUnmanagedDocument[] documents,
                int[] lines,
                int[] columns,
                int[] endLines,
                int[] endColumns)
            {
                ((ISymUnmanagedMethod)_method).GetSequencePoints(cPoints, out pcPoints, offsets, documents, lines, columns, endLines, endColumns);
                return SymUnmanagedReaderExtensions.S_OK;
            }

            public int GetSourceStartEnd(ISymUnmanagedDocument[] docs, int[] lines, int[] columns, out bool retVal)
            {
                throw new NotImplementedException();
            }

            public int IsAsyncMethod(out bool value)
            {
                return _method.IsAsyncMethod(out value);
            }

            public int GetKickoffMethod(out int kickoffMethodToken)
            {
                return _method.GetKickoffMethod(out kickoffMethodToken);
            }

            public int HasCatchHandlerILOffset(out bool offset)
            {
                return _method.HasCatchHandlerILOffset(out offset);
            }

            public int GetCatchHandlerILOffset(out int offset)
            {
                return _method.GetCatchHandlerILOffset(out offset);
            }

            public int GetAsyncStepInfoCount(out int count)
            {
                return _method.GetAsyncStepInfoCount(out count);
            }

            public int GetAsyncStepInfo(int bufferLength, out int count, int[] yieldOffsets, int[] breakpointOffset, int[] breakpointMethod)
            {
                return _method.GetAsyncStepInfo(bufferLength, out count, yieldOffsets, breakpointOffset, breakpointMethod);
            }
        }

        private sealed class SymScope : ISymUnmanagedScope, ISymUnmanagedScope2
        {
            private readonly SymReader _reader;
            private readonly ISymUnmanagedScope _scope;

            internal SymScope(SymReader reader, ISymUnmanagedScope scope)
            {
                _reader = reader;
                _scope = scope;
            }

            public int GetChildren(int cChildren, out int pcChildren, ISymUnmanagedScope[] children)
            {
                _scope.GetChildren(cChildren, out pcChildren, children);
                if (children != null)
                {
                    for (int i = 0; i < pcChildren; i++)
                    {
                        children[i] = new SymScope(_reader, children[i]);
                    }
                }
                return SymUnmanagedReaderExtensions.S_OK;
            }

            public int GetConstantCount(out int pRetVal)
            {
                throw new NotImplementedException();
            }

            public int GetConstants(int cConstants, out int pcConstants, ISymUnmanagedConstant[] constants)
            {
                ((ISymUnmanagedScope2)_scope).GetConstants(cConstants, out pcConstants, constants);
                if (constants != null)
                {
                    for (int i = 0; i < pcConstants; i++)
                    {
                        var c = constants[i];
                        var signaturesOpt = _reader._constantSignaturesOpt;
                        byte[] signature = null;
                        if (signaturesOpt != null)
                        {
                            int length;
                            int hresult = c.GetName(0, out length, null);
                            SymUnmanagedReaderExtensions.ThrowExceptionForHR(hresult);
                            var chars = new char[length];
                            hresult = c.GetName(length, out length, chars);
                            SymUnmanagedReaderExtensions.ThrowExceptionForHR(hresult);
                            var name = new string(chars, 0, length - 1);
                            signaturesOpt.TryGetValue(name, out signature);
                        }
                        constants[i] = new SymConstant(c, signature);
                    }
                }
                return SymUnmanagedReaderExtensions.S_OK;
            }

            public int GetEndOffset(out int pRetVal)
            {
                return _scope.GetEndOffset(out pRetVal);
            }

            public int GetLocalCount(out int pRetVal)
            {
                return _scope.GetLocalCount(out pRetVal);
            }

            public int GetLocals(int cLocals, out int pcLocals, ISymUnmanagedVariable[] locals)
            {
                return _scope.GetLocals(cLocals, out pcLocals, locals);
            }

            public int GetMethod(out ISymUnmanagedMethod pRetVal)
            {
                throw new NotImplementedException();
            }

            public int GetNamespaces(int cNameSpaces, out int pcNameSpaces, ISymUnmanagedNamespace[] namespaces)
            {
                return _scope.GetNamespaces(cNameSpaces, out pcNameSpaces, namespaces);
            }

            public int GetParent(out ISymUnmanagedScope pRetVal)
            {
                throw new NotImplementedException();
            }

            public int GetStartOffset(out int pRetVal)
            {
                return _scope.GetStartOffset(out pRetVal);
            }

            public void _VtblGap1_9()
            {
                throw new NotImplementedException();
            }
        }

        private sealed class SymConstant : ISymUnmanagedConstant
        {
            private readonly ISymUnmanagedConstant _constant;
            private readonly byte[] _signatureOpt;

            internal SymConstant(ISymUnmanagedConstant constant, byte[] signatureOpt)
            {
                _constant = constant;
                _signatureOpt = signatureOpt;
            }

            public int GetName(int cchName, out int pcchName, char[] name)
            {
                return _constant.GetName(cchName, out pcchName, name);
            }

            public int GetSignature(int cSig, out int pcSig, byte[] sig)
            {
                if (_signatureOpt == null)
                {
                    pcSig = 1;
                    if (sig != null)
                    {
                        object value;
                        _constant.GetValue(out value);
                        sig[0] = (byte)GetSignatureTypeCode(value);
                    }
                }
                else
                {
                    pcSig = _signatureOpt.Length;
                    if (sig != null)
                    {
                        Array.Copy(_signatureOpt, sig, cSig);
                    }
                }
                return SymUnmanagedReaderExtensions.S_OK;
            }

            public int GetValue(out object value)
            {
                return _constant.GetValue(out value);
            }

            private static SignatureTypeCode GetSignatureTypeCode(object value)
            {
                if (value == null)
                {
                    // Note: We never reach here since PdbWriter uses
                    // (int)0 for (object)null. This is just an issue with
                    // this implementation of GetSignature however.
                    return SignatureTypeCode.Object;
                }
                var typeCode = Type.GetTypeCode(value.GetType());
                switch (typeCode)
                {
                    case TypeCode.Int32:
                        return SignatureTypeCode.Int32;
                    case TypeCode.String:
                        return SignatureTypeCode.String;
                    case TypeCode.Double:
                        return SignatureTypeCode.Double;
                    default:
                        // Only a few TypeCodes handled currently.
                        throw new NotImplementedException();
                }
            }
        }
    }
}
