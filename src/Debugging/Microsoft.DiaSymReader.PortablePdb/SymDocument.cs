// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    [ComVisible(false)]
    public sealed class SymDocument : ISymUnmanagedDocument
    {
        private static Guid s_CSharpGuid = new Guid("3f5162f8-07c6-11d3-9053-00c04fa302a1");
        private static Guid s_visualBasicGuid = new Guid("3a12d0b8-c26c-11d0-b442-00a0244a1dd2");
        private static Guid s_FSharpGuid = new Guid("ab4f38c9-b6e6-43ba-be3b-58080b2ccce3");
        private static Guid s_sha1Guid = new Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460");
        private static Guid s_sha256Guid = new Guid("8829d00f-11b8-4213-878b-770e8597ac16");

        private static Guid s_vendorMicrosoftGuid = new Guid("994b45c4-e6e9-11d2-903f-00c04fa302a1");
        private static Guid s_documentTypeGuid = new Guid("5a869d0b-6611-11d3-bd2a-0000f80849bd");

        internal DocumentHandle Handle { get; }
        internal SymReader SymReader { get; }

        internal SymDocument(SymReader symReader, DocumentHandle documentHandle)
        {
            Debug.Assert(symReader != null);
            SymReader = symReader;
            Handle = documentHandle;
        }

        public int FindClosestLine(int line, out int closestLine)
        {
            // Find a minimal sequence point start line in this document 
            // that is greater than or equal to the given line.

            int result = int.MaxValue;
            var map = SymReader.GetMethodMap();
            var mdReader = SymReader.MetadataReader;

            // Note DiaSymReader searches across all documents with the same file name in CDiaWrapper::FindClosestLineAcrossFileIDs. We don't.
            foreach (var extent in map.EnumerateContainingOrClosestFollowingMethodExtents(Handle, line))
            {
                Debug.Assert(extent.MaxLine >= line);

                // extent is further than a sequence point we already found:
                if (extent.MinLine >= result)
                {
                    continue;
                }

                // enumerate method sequence points:
                var body = mdReader.GetMethodDebugInformation(extent.Method);
                foreach (var sequencePoint in body.GetSequencePoints())
                {
                    if (sequencePoint.IsHidden || sequencePoint.Document != Handle)
                    {
                        continue;
                    }

                    int startLine = sequencePoint.StartLine;
                    if (startLine >= line && startLine < result)
                    {
                        result = startLine;
                    }
                }
            }

            if (result < int.MaxValue)
            {
                closestLine = result;
                return HResult.S_OK;
            }

            closestLine = 0;
            return HResult.E_FAIL;
        }

        public int GetChecksum(
            int bufferLength,
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]byte[] checksum)
        {
            var document = SymReader.MetadataReader.GetDocument(Handle);
            if (document.Hash.IsNil)
            {
                count = 0;
                return HResult.S_FALSE;
            }

            var hash = SymReader.MetadataReader.GetBlobBytes(document.Hash);
            return InteropUtilities.BytesToBuffer(hash, bufferLength, out count, checksum);
        }

        public int GetChecksumAlgorithmId(ref Guid algorithm)
        {
            var document = SymReader.MetadataReader.GetDocument(Handle);
            algorithm = SymReader.MetadataReader.GetGuid(document.HashAlgorithm);
            return HResult.S_OK;
        }

        public int GetDocumentType(ref Guid documentType)
        {
            documentType = s_documentTypeGuid;
            return HResult.S_OK;
        }

        public int GetLanguage(ref Guid language)
        {
            var document = SymReader.MetadataReader.GetDocument(Handle);
            language = SymReader.MetadataReader.GetGuid(document.Language);
            return HResult.S_OK;
        }

        public int GetLanguageVendor(ref Guid vendor)
        {
            var document = SymReader.MetadataReader.GetDocument(Handle);
            Guid languageId = SymReader.MetadataReader.GetGuid(document.Language);
            vendor = s_vendorMicrosoftGuid;
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
            string name = SymReader.MetadataReader.GetString(SymReader.MetadataReader.GetDocument(Handle).Name);
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