// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    [ComVisible(true)]
    public sealed class SymDocument : ISymUnmanagedDocument
    {
        public int FindClosestLine(int line, out int closestLine)
        {
            throw new NotImplementedException();
        }

        public int GetChecksum(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]byte[] checksum)
        {
            throw new NotImplementedException();
        }

        public int GetChecksumAlgorithmId(ref Guid algorithm)
        {
            throw new NotImplementedException();
        }

        public int GetDocumentType(ref Guid documentType)
        {
            throw new NotImplementedException();
        }

        public int GetLanguage(ref Guid language)
        {
            throw new NotImplementedException();
        }

        public int GetLanguageVendor(ref Guid vendor)
        {
            throw new NotImplementedException();
        }

        public int GetSourceLength(out int length)
        {
            throw new NotImplementedException();
        }

        public int GetSourceRange(int startLine, int startColumn, int endLine, int endColumn, int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4), Out]byte[] source)
        {
            throw new NotImplementedException();
        }

        public int GetUrl(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]char[] url)
        {
            throw new NotImplementedException();
        }

        public int HasEmbeddedSource(out bool value)
        {
            throw new NotImplementedException();
        }
    }
}
