// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.Cci
{
    internal class DynamicAnalysisDataWriter
    {
        private struct DocumentRow
        {
            public BlobHandle Name;
            public GuidHandle HashAlgorithm;
            public BlobHandle Hash;
        }

        private struct MethodRow
        {
            public BlobHandle Spans;
        }

        // #Blob heap
        private readonly Dictionary<ImmutableArray<byte>, BlobHandle> _blobs;
        private int _blobHeapSize;

        // #GUID heap
        private readonly Dictionary<Guid, GuidHandle> _guids;
        private readonly BlobBuilder _guidWriter;

        // tables:
        private readonly List<DocumentRow> _documentTable;
        private readonly List<MethodRow> _methodTable;

        private readonly Dictionary<DebugSourceDocument, int> _documentIndex;

        public DynamicAnalysisDataWriter(int documentCountEstimate, int methodCountEstimate)
        {
            // Most methods will have a span blob, each document has a hash blob and at least two blobs encoding the name 
            // (adding one more blob per document to account for all directory names):
            _blobs = new Dictionary<ImmutableArray<byte>, BlobHandle>(1 + methodCountEstimate + 4 * documentCountEstimate, ByteSequenceComparer.Instance);

            // Each document has a unique guid:
            const int guidSize = 16;
            _guids = new Dictionary<Guid, GuidHandle>(documentCountEstimate);
            _guidWriter = new BlobBuilder(guidSize * documentCountEstimate);

            _documentTable = new List<DocumentRow>(documentCountEstimate);
            _documentIndex = new Dictionary<DebugSourceDocument, int>(documentCountEstimate);
            _methodTable = new List<MethodRow>(methodCountEstimate);

            _blobs.Add(ImmutableArray<byte>.Empty, default(BlobHandle));
            _blobHeapSize = 1;
        }

#nullable enable
        internal void SerializeMethodCodeCoverageData(IMethodBody? body)
        {
            var spans = body?.CodeCoverageSpans ?? ImmutableArray<SourceSpan>.Empty;
            BlobHandle spanBlob = SerializeSpans(spans, _documentIndex);
            _methodTable.Add(new MethodRow { Spans = spanBlob });
        }
#nullable disable

        #region Heaps

        private BlobHandle GetOrAddBlob(BlobBuilder builder)
        {
            // TODO: avoid making a copy if the blob exists in the index
            return GetOrAddBlob(builder.ToImmutableArray());
        }

        private BlobHandle GetOrAddBlob(ImmutableArray<byte> blob)
        {
            BlobHandle index;
            if (!_blobs.TryGetValue(blob, out index))
            {
                index = MetadataTokens.BlobHandle(_blobHeapSize);
                _blobs.Add(blob, index);

                _blobHeapSize += GetCompressedIntegerLength(blob.Length) + blob.Length;
            }

            return index;
        }

        private static int GetCompressedIntegerLength(int length)
        {
            return (length <= 0x7f) ? 1 : ((length <= 0x3fff) ? 2 : 4);
        }

        private GuidHandle GetOrAddGuid(Guid guid)
        {
            if (guid == Guid.Empty)
            {
                return default(GuidHandle);
            }

            GuidHandle result;
            if (_guids.TryGetValue(guid, out result))
            {
                return result;
            }

            result = MetadataTokens.GuidHandle((_guidWriter.Count >> 4) + 1);
            _guids.Add(guid, result);
            _guidWriter.WriteBytes(guid.ToByteArray());
            return result;
        }

        #endregion

        #region Spans

        private BlobHandle SerializeSpans(
            ImmutableArray<SourceSpan> spans,
            Dictionary<DebugSourceDocument, int> documentIndex)
        {
            if (spans.Length == 0)
            {
                return default(BlobHandle);
            }

            // 4 bytes per span plus a header, the builder expands by the same amount.
            var writer = new BlobBuilder(4 + spans.Length * 4);

            int previousStartLine = -1;
            int previousStartColumn = -1;
            DebugSourceDocument previousDocument = spans[0].Document;

            // header:
            writer.WriteCompressedInteger(GetOrAddDocument(previousDocument, documentIndex));

            for (int i = 0; i < spans.Length; i++)
            {
                var currentDocument = spans[i].Document;
                if (previousDocument != currentDocument)
                {
                    writer.WriteInt16(0);
                    writer.WriteCompressedInteger(GetOrAddDocument(currentDocument, documentIndex));
                    previousDocument = currentDocument;
                }

                // Delta Lines & Columns:
                SerializeDeltaLinesAndColumns(writer, spans[i]);

                // delta Start Lines & Columns:
                if (previousStartLine < 0)
                {
                    Debug.Assert(previousStartColumn < 0);
                    writer.WriteCompressedInteger(spans[i].StartLine);
                    writer.WriteCompressedInteger(spans[i].StartColumn);
                }
                else
                {
                    writer.WriteCompressedSignedInteger(spans[i].StartLine - previousStartLine);
                    writer.WriteCompressedSignedInteger(spans[i].StartColumn - previousStartColumn);
                }

                previousStartLine = spans[i].StartLine;
                previousStartColumn = spans[i].StartColumn;
            }

            return GetOrAddBlob(writer);
        }

        private void SerializeDeltaLinesAndColumns(BlobBuilder writer, SourceSpan span)
        {
            int deltaLines = span.EndLine - span.StartLine;
            int deltaColumns = span.EndColumn - span.StartColumn;

            // spans can't have zero width
            Debug.Assert(deltaLines != 0 || deltaColumns != 0);

            writer.WriteCompressedInteger(deltaLines);

            if (deltaLines == 0)
            {
                writer.WriteCompressedInteger(deltaColumns);
            }
            else
            {
                writer.WriteCompressedSignedInteger(deltaColumns);
            }
        }

        #endregion

        #region Documents

        internal int GetOrAddDocument(DebugSourceDocument document)
        {
            return GetOrAddDocument(document, _documentIndex);
        }

        private int GetOrAddDocument(DebugSourceDocument document, Dictionary<DebugSourceDocument, int> index)
        {
            int documentRowId;
            if (!index.TryGetValue(document, out documentRowId))
            {
                documentRowId = _documentTable.Count + 1;
                index.Add(document, documentRowId);

                var sourceInfo = document.GetSourceInfo();
                _documentTable.Add(new DocumentRow
                {
                    Name = SerializeDocumentName(document.Location),
                    HashAlgorithm = (sourceInfo.Checksum.IsDefault ? default(GuidHandle) : GetOrAddGuid(sourceInfo.ChecksumAlgorithmId)),
                    Hash = (sourceInfo.Checksum.IsDefault) ? default(BlobHandle) : GetOrAddBlob(sourceInfo.Checksum)
                });
            }

            return documentRowId;
        }

        private static readonly char[] s_separator1 = { '/' };
        private static readonly char[] s_separator2 = { '\\' };

        private BlobHandle SerializeDocumentName(string name)
        {
            Debug.Assert(name != null);

            int c1 = Count(name, s_separator1[0]);
            int c2 = Count(name, s_separator2[0]);
            char[] separator = (c1 >= c2) ? s_separator1 : s_separator2;

            // Estimate 2 bytes per part, if the blob heap gets big we expand the builder once.
            var writer = new BlobBuilder(1 + Math.Max(c1, c2) * 2);

            writer.WriteByte((byte)separator[0]);

            // TODO: avoid allocations
            foreach (var part in name.Split(separator))
            {
                BlobHandle partIndex = GetOrAddBlob(ImmutableArray.Create(MetadataWriter.s_utf8Encoding.GetBytes(part)));
                writer.WriteCompressedInteger(MetadataTokens.GetHeapOffset(partIndex));
            }

            return GetOrAddBlob(writer);
        }

        private static int Count(string str, char c)
        {
            int count = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == c)
                {
                    count++;
                }
            }

            return count;
        }

        #endregion

        #region Table Serialization

        private readonly struct Sizes
        {
            public readonly int BlobHeapSize;
            public readonly int GuidHeapSize;

            public readonly int BlobIndexSize;
            public readonly int GuidIndexSize;

            public Sizes(int blobHeapSize, int guidHeapSize)
            {
                BlobHeapSize = blobHeapSize;
                GuidHeapSize = guidHeapSize;
                BlobIndexSize = (blobHeapSize <= ushort.MaxValue) ? 2 : 4;
                GuidIndexSize = (guidHeapSize <= ushort.MaxValue) ? 2 : 4;
            }
        }

        internal void SerializeMetadataTables(BlobBuilder writer)
        {
            var sizes = new Sizes(_blobHeapSize, _guidWriter.Count);

            SerializeHeader(writer, sizes);

            // tables:
            SerializeDocumentTable(writer, sizes);
            SerializeMethodTable(writer, sizes);

            // heaps:
            writer.LinkSuffix(_guidWriter);
            WriteBlobHeap(writer);
        }

        private void WriteBlobHeap(BlobBuilder builder)
        {
            var writer = new BlobWriter(builder.ReserveBytes(_blobHeapSize));

            // Perf consideration: With large heap the following loop may cause a lot of cache misses 
            // since the order of entries in _blobs dictionary depends on the hash of the array values, 
            // which is not correlated to the heap index. If we observe such issue we should order 
            // the entries by heap position before running this loop.
            foreach (var entry in _blobs)
            {
                int heapOffset = MetadataTokens.GetHeapOffset(entry.Value);
                var blob = entry.Key;

                writer.Offset = heapOffset;
                writer.WriteCompressedInteger(blob.Length);
                writer.WriteBytes(blob);
            }
        }

        private void SerializeHeader(BlobBuilder writer, Sizes sizes)
        {
            // signature:
            writer.WriteByte((byte)'D');
            writer.WriteByte((byte)'A');
            writer.WriteByte((byte)'M');
            writer.WriteByte((byte)'D');

            // version: 0.2
            writer.WriteByte(0);
            writer.WriteByte(2);

            // table sizes:
            writer.WriteInt32(_documentTable.Count);
            writer.WriteInt32(_methodTable.Count);

            // blob heap sizes:
            writer.WriteInt32(sizes.GuidHeapSize);
            writer.WriteInt32(sizes.BlobHeapSize);
        }

        private void SerializeDocumentTable(BlobBuilder writer, Sizes sizes)
        {
            foreach (var row in _documentTable)
            {
                writer.WriteReference(MetadataTokens.GetHeapOffset(row.Name), isSmall: (sizes.BlobIndexSize == 2));
                writer.WriteReference(MetadataTokens.GetHeapOffset(row.HashAlgorithm), isSmall: (sizes.GuidIndexSize == 2));
                writer.WriteReference(MetadataTokens.GetHeapOffset(row.Hash), isSmall: (sizes.BlobIndexSize == 2));
            }
        }

        private void SerializeMethodTable(BlobBuilder writer, Sizes sizes)
        {
            foreach (var row in _methodTable)
            {
                writer.WriteReference(MetadataTokens.GetHeapOffset(row.Spans), isSmall: (sizes.BlobIndexSize == 2));
            }
        }

        #endregion
    }
}
