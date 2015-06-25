﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    [ComVisible(false)]
    public sealed class SymDocument : ISymUnmanagedDocument
    {
        private static Guid CSharpGuid = new Guid("3f5162f8-07c6-11d3-9053-00c04fa302a1");
        private static Guid VisualBasicGuid = new Guid("3a12d0b8-c26c-11d0-b442-00a0244a1dd2");
        private static Guid FSharpGuid = new Guid("ab4f38c9-b6e6-43ba-be3b-58080b2ccce3");
        private static Guid Sha1Guid = new Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460");
        private static Guid Sha256Guid = new Guid("8829d00f-11b8-4213-878b-770e8597ac16");

        private static Guid VendorMicrosoftGuid = new Guid("994b45c4-e6e9-11d2-903f-00c04fa302a1");
        private static Guid DocumentTypeGuid = new Guid("5a869d0b-6611-11d3-bd2a-0000f80849bd");

        private readonly DocumentHandle _handle;
        private readonly SymReader _symReader;

        internal SymDocument(SymReader symReader, DocumentHandle documentHandle)
        {
            Debug.Assert(symReader != null);
            _symReader = symReader;
            _handle = documentHandle;
        }

        internal DocumentHandle Handle => _handle;

        public int FindClosestLine(int line, out int closestLine)
        {
            // TODO:
            throw new NotImplementedException();
        }

        public int GetChecksum(
            int bufferLength,
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]byte[] checksum)
        {
            var document = _symReader.MetadataReader.GetDocument(_handle);
            if (document.Hash.IsNil)
            {
                count = 0;
                return HResult.S_FALSE;
            }

            var hash = _symReader.MetadataReader.GetBlobBytes(document.Hash);
            return InteropUtilities.BytesToBuffer(hash, bufferLength, out count, checksum);
        }

        public int GetChecksumAlgorithmId(ref Guid algorithm)
        {
            var document = _symReader.MetadataReader.GetDocument(_handle);
            algorithm = _symReader.MetadataReader.GetGuid(document.HashAlgorithm);
            return HResult.S_OK;
        }

        public int GetDocumentType(ref Guid documentType)
        {
            documentType = DocumentTypeGuid;
            return HResult.S_OK;
        }

        public int GetLanguage(ref Guid language)
        {
            var document = _symReader.MetadataReader.GetDocument(_handle);
            language = _symReader.MetadataReader.GetGuid(document.Language);
            return HResult.S_OK;
        }

        public int GetLanguageVendor(ref Guid vendor)
        {
            var document = _symReader.MetadataReader.GetDocument(_handle);
            Guid languageId = _symReader.MetadataReader.GetGuid(document.Language);
            vendor = VendorMicrosoftGuid;
            return HResult.S_OK;
        }

        public int GetSourceLength(out int length)
        {
            // SymReader doesn't support embedded source.
            length = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetSourceRange(
            int startLine,
            int startColumn, 
            int endLine,
            int endColumn, 
            int bufferLength, 
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4), Out]byte[] source)
        {
            // SymReader doesn't support embedded source.
            count = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetUrl(
            int bufferLength, 
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]char[] url)
        {
            string name = _symReader.MetadataReader.GetString(_symReader.MetadataReader.GetDocument(_handle).Name);
            return InteropUtilities.StringToBuffer(name, bufferLength, out count, url);
        }

        public int HasEmbeddedSource(out bool value)
        {
            // SymReader doesn't support embedded source.
            value = false;
            return HResult.E_NOTIMPL;
        }
    }
}
