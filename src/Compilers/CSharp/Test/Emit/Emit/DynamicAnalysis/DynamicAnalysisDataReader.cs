// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Review all exception messages, range checks, public API, TODOs: https://github.com/dotnet/testimpact/issues/47

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal struct DynamicAnalysisDocument
    {
        public readonly BlobHandle Name;
        public readonly GuidHandle HashAlgorithm;
        public readonly BlobHandle Hash;

        public DynamicAnalysisDocument(BlobHandle name, GuidHandle hashAlgorithm, BlobHandle hash)
        {
            Name = name;
            HashAlgorithm = hashAlgorithm;
            Hash = hash;
        }
    }

    internal struct DynamicAnalysisMethod
    {
        public readonly BlobHandle Blob;

        public DynamicAnalysisMethod(BlobHandle blob)
        {
            Blob = blob;
        }
    }

    internal unsafe sealed class DynamicAnalysisDataReader
    {
        public ImmutableArray<DynamicAnalysisDocument> Documents { get; }
        public ImmutableArray<DynamicAnalysisMethod> Methods { get; }

        private readonly Blob _guidHeapBlob;
        private readonly Blob _blobHeapBlob;

        private const int GuidSize = 16;

        public DynamicAnalysisDataReader(byte* buffer, int size)
        {
            var reader = new BlobReader(buffer, size);

            // header:
            if (reader.ReadByte() != 'D' || reader.ReadByte() != 'A' || reader.ReadByte() != 'M' || reader.ReadByte() != 'D')
            {
                throw new BadImageFormatException();
            }

            // version
            byte major = reader.ReadByte();
            byte minor = reader.ReadByte();
            if (major != 0 && minor != 1)
            {
                throw new NotSupportedException();
            }

            // table sizes:
            int documentRowCount = reader.ReadInt32();
            int methodSpanRowCount = reader.ReadInt32();

            // blob heap sizes:
            int stringHeapSize = reader.ReadInt32();
            int userStringHeapSize = reader.ReadInt32();
            int guidHeapSize = reader.ReadInt32();
            int blobHeapSize = reader.ReadInt32();

            // TODO: check size ranges

            bool isBlobHeapSmall = blobHeapSize <= ushort.MaxValue;
            bool isGuidHeapSmall = guidHeapSize / GuidSize <= ushort.MaxValue;

            var documentsBuilder = ArrayBuilder<DynamicAnalysisDocument>.GetInstance(documentRowCount);

            for (int i = 0; i < documentRowCount; i++)
            {
                var name = MetadataTokens.BlobHandle(ReadReference(ref reader, isBlobHeapSmall));
                var hashAlgorithm = MetadataTokens.GuidHandle(ReadReference(ref reader, isGuidHeapSmall));
                var hash = MetadataTokens.BlobHandle(ReadReference(ref reader, isBlobHeapSmall));

                documentsBuilder.Add(new DynamicAnalysisDocument(name, hashAlgorithm, hash));
            }

            Documents = documentsBuilder.ToImmutableAndFree();

            var methodsBuilder = ArrayBuilder<DynamicAnalysisMethod>.GetInstance(methodSpanRowCount);

            for (int i = 0; i < methodSpanRowCount; i++)
            {
                methodsBuilder.Add(new DynamicAnalysisMethod(MetadataTokens.BlobHandle(ReadReference(ref reader, isBlobHeapSmall))));
            }

            Methods = methodsBuilder.ToImmutableAndFree();

            int stringHeapOffset = reader.Offset;
            int userStringHeapOffset = stringHeapOffset + stringHeapSize;
            int guidHeapOffset = userStringHeapOffset + userStringHeapSize;
            int blobHeapOffset = guidHeapOffset + guidHeapSize;

            if (reader.Length != blobHeapOffset + blobHeapSize)
            {
                throw new BadImageFormatException();
            }

            _guidHeapBlob = new Blob(buffer + guidHeapOffset, guidHeapSize);
            _blobHeapBlob = new Blob(buffer + blobHeapOffset, blobHeapSize);
        }

        public static DynamicAnalysisDataReader TryCreateFromPE(PEReader peReader)
        {
            // TODO: review all range checks, better error messages

            var mdReader = peReader.GetMetadataReader();
            long offset = -1;
            foreach (var resourceHandle in mdReader.ManifestResources)
            {
                var resource = mdReader.GetManifestResource(resourceHandle);
                if (resource.Implementation.IsNil &&
                    resource.Attributes == ManifestResourceAttributes.Private &&
                    mdReader.StringComparer.Equals(resource.Name, "<DynamicAnalysisData>"))
                {
                    offset = resource.Offset;
                }
            }

            if (offset < 0)
            {
                return null;
            }

            var resourcesDir = peReader.PEHeaders.CorHeader.ResourcesDirectory;
            if (resourcesDir.Size < 0)
            {
                throw new BadImageFormatException();
            }

            int start;
            if (!peReader.PEHeaders.TryGetDirectoryOffset(resourcesDir, out start))
            {
                return null;
            }

            var peImage = peReader.GetEntireImage();
            if (start >= peImage.Length - resourcesDir.Size)
            {
                throw new BadImageFormatException();
            }

            byte* resourceStart = peImage.Pointer + start;
            int resourceSize = *(int*)resourceStart;
            if (resourceSize > resourcesDir.Size - sizeof(int))
            {
                throw new BadImageFormatException();
            }

            return new DynamicAnalysisDataReader(resourceStart + sizeof(int), resourceSize);
        }

        private static int ReadReference(ref BlobReader reader, bool smallRefSize)
        {
            return smallRefSize ? reader.ReadUInt16() : reader.ReadInt32();
        }

        public ImmutableArray<LinePositionSpan> GetSpans(BlobHandle handle)
        {
            var builder = ArrayBuilder<LinePositionSpan>.GetInstance();

            var reader = GetBlobReader(handle);

            // header:
            int documentRowId = ReadDocumentRowId(ref reader);
            int previousStartLine = -1;
            ushort previousStartColumn = 0;

            // records:
            while (reader.RemainingBytes > 0)
            {
                int deltaLines, deltaColumns;
                ReadDeltaLinesAndColumns(ref reader, out deltaLines, out deltaColumns);

                // document:
                if (deltaLines == 0 && deltaColumns == 0)
                {
                    documentRowId = ReadDocumentRowId(ref reader);
                    continue;
                }

                int startLine;
                ushort startColumn;

                // delta Start Line & Column:
                if (previousStartLine < 0)
                {
                    Debug.Assert(previousStartColumn == 0);

                    startLine = ReadLine(ref reader);
                    startColumn = ReadColumn(ref reader);
                }
                else
                {
                    startLine = AddLines(previousStartLine, reader.ReadCompressedSignedInteger());
                    startColumn = AddColumns(previousStartColumn, reader.ReadCompressedSignedInteger());
                }

                previousStartLine = startLine;
                previousStartColumn = startColumn;

                int endLine = AddLines(startLine, deltaLines);
                int endColumn = AddColumns(startColumn, deltaColumns);
                builder.Add(new LinePositionSpan(new LinePosition(startLine, startColumn), new LinePosition(endLine, endColumn)));
            }

            return builder.ToImmutableAndFree();
        }

        //TODO: some of the helpers below should be provided by System.Reflection.Metadata

        private unsafe struct Blob
        {
            public readonly byte* Pointer;
            public readonly int Length;

            public Blob(byte* pointer, int length)
            {
                Pointer = pointer;
                Length = length;
            }
        }

        public Guid GetGuid(GuidHandle handle)
        {
            if (handle.IsNil)
            {
                return default(Guid);
            }

            int offset = (MetadataTokens.GetHeapOffset(handle) - 1) * GuidSize;
            if (offset + GuidSize > _guidHeapBlob.Length)
            {
                throw new BadImageFormatException();
            }

            return *(Guid*)(_guidHeapBlob.Pointer + offset);
        }

        internal byte[] GetBytes(BlobHandle handle)
        {
            var reader = GetBlobReader(handle);
            return reader.ReadBytes(reader.Length);
        }

        private BlobReader GetBlobReader(BlobHandle handle)
        {
            int offset = MetadataTokens.GetHeapOffset(handle);
            byte* start = _blobHeapBlob.Pointer + offset;
            var reader = new BlobReader(start, _blobHeapBlob.Length - offset);
            int size = reader.ReadCompressedInteger();
            return new BlobReader(start + reader.Offset, size);
        }

        public string GetDocumentName(BlobHandle handle)
        {
            var blobReader = GetBlobReader(handle);

            // Spec: separator is an ASCII encoded character in range [0x01, 0x7F], or byte 0 to represent an empty separator.
            int separator = blobReader.ReadByte();
            if (separator > 0x7f)
            {
                throw new BadImageFormatException(string.Format("Invalid document name", separator));
            }

            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;
            bool isFirstPart = true;
            while (blobReader.RemainingBytes > 0)
            {
                if (separator != 0 && !isFirstPart)
                {
                    builder.Append((char)separator);
                }

                var partReader = GetBlobReader(blobReader.ReadBlobHandle());

                // TODO: avoid allocating temp string (https://github.com/dotnet/corefx/issues/2102)
                builder.Append(partReader.ReadUTF8(partReader.Length));
                isFirstPart = false;
            }

            return pooledBuilder.ToStringAndFree();
        }

        private void ReadDeltaLinesAndColumns(ref BlobReader reader, out int deltaLines, out int deltaColumns)
        {
            deltaLines = reader.ReadCompressedInteger();
            deltaColumns = (deltaLines == 0) ? reader.ReadCompressedInteger() : reader.ReadCompressedSignedInteger();
        }

        private int ReadLine(ref BlobReader reader)
        {
            return reader.ReadCompressedInteger();
        }

        private ushort ReadColumn(ref BlobReader reader)
        {
            int column = reader.ReadCompressedInteger();
            if (column > ushort.MaxValue)
            {
                throw new BadImageFormatException("SequencePointValueOutOfRange");
            }

            return (ushort)column;
        }

        private int AddLines(int value, int delta)
        {
            int result = unchecked(value + delta);
            if (result < 0)
            {
                throw new BadImageFormatException("SequencePointValueOutOfRange");
            }

            return result;
        }

        private ushort AddColumns(ushort value, int delta)
        {
            int result = unchecked(value + delta);
            if (result < 0 || result >= ushort.MaxValue)
            {
                throw new BadImageFormatException("SequencePointValueOutOfRange");
            }

            return (ushort)result;
        }

        private int ReadDocumentRowId(ref BlobReader reader)
        {
            int rowId = reader.ReadCompressedInteger();
            if (rowId == 0)
            {
                throw new BadImageFormatException("Invalid handle");
            }

            return rowId;
        }
    }
}
