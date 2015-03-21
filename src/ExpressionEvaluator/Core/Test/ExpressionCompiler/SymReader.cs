// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
extern alias PDB;


using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.DiaSymReader;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class SymReader : ISymUnmanagedReader
    {
        private readonly ISymUnmanagedReader _reader;
        private readonly ImmutableDictionary<string, byte[]> _constantSignaturesOpt;

        internal SymReader(byte[] pdbBytes, ImmutableDictionary<string, byte[]> constantSignaturesOpt = null)
        {
            _reader = SymUnmanagedReaderTestExtensions.CreateReader(
                new MemoryStream(pdbBytes),
                PDB::Roslyn.Test.PdbUtilities.DummyMetadataImport.Instance);

            _constantSignaturesOpt = constantSignaturesOpt;
        }

        public int GetDocuments(int cDocs, out int pcDocs, ISymUnmanagedDocument[] pDocs)
        {
            throw new NotImplementedException();
        }

        public int GetMethod(int methodToken, out ISymUnmanagedMethod retVal)
        {
            // The EE should never be calling ISymUnmanagedReader.GetMethod.  In order to account
            // for EnC updates, it should always be calling GetMethodByVersion instead.
            throw ExceptionUtilities.Unreachable;
        }

        public int GetMethodByVersion(int methodToken, int version, out ISymUnmanagedMethod retVal)
        {
            var hr = _reader.GetMethodByVersion(methodToken, version, out retVal);
            if (retVal != null)
            {
                retVal = new SymMethod(this, retVal);
            }
            return hr;
        }

        public int GetSymAttribute(int token, string name, int sizeBuffer, out int lengthBuffer, byte[] buffer)
        {
            return _reader.GetSymAttribute(token, name, sizeBuffer, out lengthBuffer, buffer);
        }

        public int Initialize(object importer, string filename, string searchPath, IStream stream)
        {
            throw new NotImplementedException();
        }

        public int GetDocument(string url, Guid language, Guid languageVendor, Guid documentType, out ISymUnmanagedDocument retVal)
        {
            throw new NotImplementedException();
        }

        public int GetDocumentVersion(ISymUnmanagedDocument pDoc, out int version, out bool pbCurrent)
        {
            throw new NotImplementedException();
        }

        public int GetGlobalVariables(int cVars, out int pcVars, ISymUnmanagedVariable[] vars)
        {
            throw new NotImplementedException();
        }

        public int GetMethodFromDocumentPosition(ISymUnmanagedDocument document, int line, int column, out ISymUnmanagedMethod retVal)
        {
            throw new NotImplementedException();
        }

        public int GetMethodsFromDocumentPosition(ISymUnmanagedDocument document, int line, int column, int cMethod, out int pcMethod, ISymUnmanagedMethod[] pRetVal)
        {
            throw new NotImplementedException();
        }

        public int GetMethodVersion(ISymUnmanagedMethod pMethod, out int version)
        {
            throw new NotImplementedException();
        }

        public int GetNamespaces(int cNameSpaces, out int pcNameSpaces, ISymUnmanagedNamespace[] namespaces)
        {
            throw new NotImplementedException();
        }

        public int GetSymbolStoreFileName(int cchName, out int pcchName, char[] szName)
        {
            throw new NotImplementedException();
        }

        public int GetUserEntryPoint(out int EntryPoint)
        {
            throw new NotImplementedException();
        }

        public int GetVariables(int parent, int cVars, out int pcVars, ISymUnmanagedVariable[] vars)
        {
            throw new NotImplementedException();
        }

        public int ReplaceSymbolStore(string filename, IStream stream)
        {
            throw new NotImplementedException();
        }

        public int UpdateSymbolStore(string filename, IStream stream)
        {
            throw new NotImplementedException();
        }

        private sealed class SymMethod : ISymUnmanagedMethod
        {
            private readonly SymReader _reader;
            private readonly ISymUnmanagedMethod _method;

            internal SymMethod(SymReader reader, ISymUnmanagedMethod method)
            {
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
