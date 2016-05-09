// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

#if SRM
namespace System.Reflection.Metadata.Ecma335
#else
namespace Roslyn.Reflection.Metadata.Ecma335
#endif
{
#if SRM
using System.Reflection.PortableExecutable;
#else
    using Roslyn.Reflection.PortableExecutable;
#endif

#if SRM
    public
#endif
    sealed class StandaloneDebugMetadataSerializer : MetadataSerializer
    {
        private const string DebugMetadataVersionString = "PDB v1.0";

        private Blob _pdbIdBlob;
        private readonly MethodDefinitionHandle _entryPoint;

        public StandaloneDebugMetadataSerializer(
            MetadataBuilder builder, 
            ImmutableArray<int> typeSystemRowCounts,
            MethodDefinitionHandle entryPoint,
            bool isMinimalDelta)
            : base(builder, CreateSizes(builder, typeSystemRowCounts, isMinimalDelta, isStandaloneDebugMetadata: true), DebugMetadataVersionString)
        {
            _entryPoint = entryPoint;
        }

        /// <summary>
        /// Serialized #Pdb stream.
        /// </summary>
        protected override void SerializeStandalonePdbStream(BlobBuilder builder)
        {
            int startPosition = builder.Position;

            // the id will be filled in later
            _pdbIdBlob = builder.ReserveBytes(MetadataSizes.PdbIdSize);
            
            builder.WriteInt32(_entryPoint.IsNil ? 0 : MetadataTokens.GetToken(_entryPoint));

            builder.WriteUInt64(MetadataSizes.ExternalTablesMask);
            MetadataWriterUtilities.SerializeRowCounts(builder, MetadataSizes.ExternalRowCounts);

            int endPosition = builder.Position;
            Debug.Assert(MetadataSizes.CalculateStandalonePdbStreamSize() == endPosition - startPosition);
        }

        public void SerializeMetadata(BlobBuilder builder, Func<BlobBuilder, ContentId> idProvider, out ContentId contentId)
        {
            SerializeMetadataImpl(builder, methodBodyStreamRva: 0, mappedFieldDataStreamRva: 0);

            contentId = idProvider(builder);

            // fill in the id:
            var idWriter = new BlobWriter(_pdbIdBlob);
            idWriter.WriteBytes(contentId.Guid);
            idWriter.WriteBytes(contentId.Stamp);
            Debug.Assert(idWriter.RemainingBytes == 0);
        }
    }

#if SRM
    public
#endif
    sealed class TypeSystemMetadataSerializer : MetadataSerializer
    {
        private static readonly ImmutableArray<int> EmptyRowCounts = ImmutableArray.CreateRange(Enumerable.Repeat(0, MetadataTokens.TableCount));

        public TypeSystemMetadataSerializer(
            MetadataBuilder tables, 
            string metadataVersion,
            bool isMinimalDelta)
            : base(tables, CreateSizes(tables, EmptyRowCounts, isMinimalDelta, isStandaloneDebugMetadata: false), metadataVersion)
        {
            
        }

        protected override void SerializeStandalonePdbStream(BlobBuilder writer)
        {
            // nop
        }

        public void SerializeMetadata(BlobBuilder metadataWriter, int methodBodyStreamRva, int mappedFieldDataStreamRva)
        {
            SerializeMetadataImpl(metadataWriter, methodBodyStreamRva, mappedFieldDataStreamRva);
        }
    }

#if SRM
    public
#endif
    abstract class MetadataSerializer
    {
        protected readonly MetadataBuilder _tables;
        private readonly MetadataSizes _sizes;
        private readonly string _metadataVersion;

        public MetadataSerializer(MetadataBuilder tables, MetadataSizes sizes, string metadataVersion)
        {
            _tables = tables;
            _sizes = sizes;
            _metadataVersion = metadataVersion;
        }

        internal static MetadataSizes CreateSizes(MetadataBuilder tables, ImmutableArray<int> externalRowCounts, bool isMinimalDelta, bool isStandaloneDebugMetadata)
        {
            tables.CompleteHeaps();

            return new MetadataSizes(
                tables.GetRowCounts(),
                externalRowCounts,
                tables.GetHeapSizes(),
                isMinimalDelta,
                isStandaloneDebugMetadata);
        }

        protected abstract void SerializeStandalonePdbStream(BlobBuilder writer);

        public MetadataSizes MetadataSizes => _sizes;

        protected void SerializeMetadataImpl(BlobBuilder metadataWriter, int methodBodyStreamRva, int mappedFieldDataStreamRva)
        {
            // header:
            SerializeMetadataHeader(metadataWriter);

            // #Pdb stream
            SerializeStandalonePdbStream(metadataWriter);
            
            // #~ or #- stream:
            _tables.SerializeMetadataTables(metadataWriter, _sizes, methodBodyStreamRva, mappedFieldDataStreamRva);

            // #Strings, #US, #Guid and #Blob streams:
            _tables.WriteHeapsTo(metadataWriter);
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
