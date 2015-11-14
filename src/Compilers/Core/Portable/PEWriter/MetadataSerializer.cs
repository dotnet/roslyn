// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Cci
{
    internal sealed class StandaloneDebugMetadataSerializer : MetadataSerializer
    {
        private const string DebugMetadataVersionString = "PDB v1.0";

        private int _pdbIdOffset;
        private readonly int _entryPointToken;

        public StandaloneDebugMetadataSerializer(
            MetadataTablesBuilder tables, 
            MetadataHeapsBuilder heaps, 
            ImmutableArray<int> typeSystemRowCounts,
            int entryPointToken,
            bool isMinimalDelta)
            : base(tables, heaps, CreateSizes(tables, heaps, typeSystemRowCounts, isMinimalDelta, isStandaloneDebugMetadata: true), DebugMetadataVersionString)
        {
            _entryPointToken = entryPointToken;
        }

        public int PdbIdOffset => _pdbIdOffset;

        protected override void SerializeStandalonePdbStream(BlobBuilder writer)
        {
            int startPosition = writer.Position;

            // zero out and save position, will be filled in later
            _pdbIdOffset = startPosition;
            writer.WriteBytes(0, MetadataSizes.PdbIdSize);

            writer.WriteUInt32((uint)_entryPointToken);

            writer.WriteUInt64(MetadataSizes.ExternalTablesMask);
            MetadataWriterUtilities.SerializeRowCounts(writer, MetadataSizes.ExternalRowCounts);

            int endPosition = writer.Position;
            Debug.Assert(MetadataSizes.CalculateStandalonePdbStreamSize() == endPosition - startPosition);
        }

        public void SerializeMetadata(BlobBuilder metadataWriter)
        {
            int guidHeapStartOffset;
            SerializeMetadataImpl(metadataWriter, methodBodyStreamRva: 0, mappedFieldDataStreamRva: 0, guidHeapStartOffset: out guidHeapStartOffset);
        }
    }

    internal sealed class TypeSystemMetadataSerializer : MetadataSerializer
    {
        private static readonly ImmutableArray<int> EmptyRowCounts = ImmutableArray.CreateRange(Enumerable.Repeat(0, MetadataTokens.TableCount));

        public int ModuleVersionIdOffset => _moduleVersionIdOffset;
        private int _moduleVersionIdOffset;

        public TypeSystemMetadataSerializer(
            MetadataTablesBuilder tables, 
            MetadataHeapsBuilder heaps, 
            string metadataVersion,
            bool isMinimalDelta)
            : base(tables, heaps, CreateSizes(tables, heaps, EmptyRowCounts, isMinimalDelta, isStandaloneDebugMetadata: false), metadataVersion)
        {
            
        }

        protected override void SerializeStandalonePdbStream(BlobBuilder writer)
        {
            // nop
        }

        public void SerializeMetadata(BlobBuilder metadataWriter, int methodBodyStreamRva, int mappedFieldDataStreamRva)
        {
            int guidHeapStartOffset;
            SerializeMetadataImpl(metadataWriter, methodBodyStreamRva, mappedFieldDataStreamRva, out guidHeapStartOffset);
            _moduleVersionIdOffset = _tables.GetModuleVersionGuidOffsetInMetadataStream(guidHeapStartOffset);
        }
    }

    internal abstract class MetadataSerializer
    {
        protected readonly MetadataTablesBuilder _tables;
        private readonly MetadataHeapsBuilder _heaps;
        private readonly MetadataSizes _sizes;
        private readonly string _metadataVersion;

        public MetadataSerializer(MetadataTablesBuilder tables, MetadataHeapsBuilder heaps, MetadataSizes sizes, string metadataVersion)
        {
            _tables = tables;
            _heaps = heaps;
            _sizes = sizes;
            _metadataVersion = metadataVersion;
        }

        internal static MetadataSizes CreateSizes(MetadataTablesBuilder tables, MetadataHeapsBuilder heaps, ImmutableArray<int> externalRowCounts, bool isMinimalDelta, bool isStandaloneDebugMetadata)
        {
            heaps.Complete();

            return new MetadataSizes(
                tables.GetRowCounts(),
                externalRowCounts,
                heaps.GetHeapSizes(),
                isMinimalDelta,
                isStandaloneDebugMetadata);
        }

        protected abstract void SerializeStandalonePdbStream(BlobBuilder writer);

        public MetadataSizes MetadataSizes => _sizes;

        protected void SerializeMetadataImpl(BlobBuilder metadataWriter, int methodBodyStreamRva, int mappedFieldDataStreamRva, out int guidHeapStartOffset)
        {
            // header:
            SerializeMetadataHeader(metadataWriter);

            // #Pdb stream
            SerializeStandalonePdbStream(metadataWriter);
            
            // #~ or #- stream:
            _tables.SerializeMetadataTables(metadataWriter, _sizes, methodBodyStreamRva, mappedFieldDataStreamRva);

            // #Strings, #US, #Guid and #Blob streams:
            _heaps.WriteTo(metadataWriter, out guidHeapStartOffset);
        }

        private void SerializeMetadataHeader(BlobBuilder writer)
        {
            int startOffset = writer.Position;

            // signature
            writer.WriteUInt32(0x424A5342);

            // major version
            writer.WriteUInt16(1);

            // minor version
            writer.WriteUInt16(1);

            // reserved
            writer.WriteUInt32(0);

            // metadata version length
            writer.WriteUInt32(MetadataSizes.MetadataVersionPaddedLength);

            int n = Math.Min(MetadataSizes.MetadataVersionPaddedLength, _metadataVersion.Length);
            for (int i = 0; i < n; i++)
            {
                writer.WriteByte((byte)_metadataVersion[i]);
            }

            for (int i = n; i < MetadataSizes.MetadataVersionPaddedLength; i++)
            {
                writer.WriteByte(0);
            }

            // reserved
            writer.WriteUInt16(0);

            // number of streams
            writer.WriteUInt16((ushort)(5 + (_sizes.IsMinimalDelta ? 1 : 0) + (_sizes.IsStandaloneDebugMetadata ? 1 : 0)));

            // stream headers
            int offsetFromStartOfMetadata = _sizes.MetadataHeaderSize;

            // emit the #Pdb stream first so that only a single page has to be read in order to find out PDB ID
            if (_sizes.IsStandaloneDebugMetadata)
            {
                SerializeStreamHeader(ref offsetFromStartOfMetadata, _sizes.StandalonePdbStreamSize, "#Pdb", writer);
            }

            // Spec: Some compilers store metadata in a #- stream, which holds an uncompressed, or non-optimized, representation of metadata tables;
            // this includes extra metadata -Ptr tables. Such PE files do not form part of ECMA-335 standard.
            //
            // Note: EnC delta is stored as uncompressed metadata stream.
            SerializeStreamHeader(ref offsetFromStartOfMetadata, _sizes.MetadataTableStreamSize, (_sizes.IsMetadataTableStreamCompressed ? "#~" : "#-"), writer);

            SerializeStreamHeader(ref offsetFromStartOfMetadata, _sizes.GetAlignedHeapSize(HeapIndex.String), "#Strings", writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, _sizes.GetAlignedHeapSize(HeapIndex.UserString), "#US", writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, _sizes.GetAlignedHeapSize(HeapIndex.Guid), "#GUID", writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, _sizes.GetAlignedHeapSize(HeapIndex.Blob), "#Blob", writer);

            if (_sizes.IsMinimalDelta)
            {
                SerializeStreamHeader(ref offsetFromStartOfMetadata, 0, "#JTD", writer);
            }

            int endOffset = writer.Position;
            Debug.Assert(endOffset - startOffset == _sizes.MetadataHeaderSize);
        }

        private static void SerializeStreamHeader(ref int offsetFromStartOfMetadata, int alignedStreamSize, string streamName, BlobBuilder writer)
        {
            // 4 for the first uint (offset), 4 for the second uint (padded size), length of stream name + 1 for null terminator (then padded)
            int sizeOfStreamHeader = MetadataSizes.GetMetadataStreamHeaderSize(streamName);
            writer.WriteInt32(offsetFromStartOfMetadata);
            writer.WriteInt32(alignedStreamSize);
            foreach (char ch in streamName)
            {
                writer.WriteByte((byte)ch);
            }

            // After offset, size, and stream name, write 0-bytes until we reach our padded size.
            for (uint i = 8 + (uint)streamName.Length; i < sizeOfStreamHeader; i++)
            {
                writer.WriteByte(0);
            }

            offsetFromStartOfMetadata += alignedStreamSize;
        }
    }
}
