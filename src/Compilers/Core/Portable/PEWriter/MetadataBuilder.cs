// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Cci
{
    partial class MetadataWriter
    {
        private void SerializeMetadata(
            BlobBuilder metadataWriter,
            MetadataSizes metadataSizes,
            int methodBodyStreamRva,
            int mappedFieldDataStreamRva,
            int debugEntryPointToken,
            out int guidHeapStartOffset,
            out int pdbIdOffset)
        {
            // header:
            SerializeMetadataHeader(metadataWriter, metadataSizes);

            // #Pdb stream
            if (metadataSizes.IsStandaloneDebugMetadata)
            {
                SerializeStandalonePdbStream(metadataWriter, metadataSizes, debugEntryPointToken, out pdbIdOffset);
            }
            else
            {
                pdbIdOffset = 0;
            }

            // #~ or #- stream:
            tables.SerializeMetadataTables(metadataWriter, metadataSizes, methodBodyStreamRva, mappedFieldDataStreamRva);

            // #Strings, #US, #Guid and #Blob streams:
            (metadataSizes.IsStandaloneDebugMetadata ? _debugHeapsOpt : heaps).WriteTo(metadataWriter, out guidHeapStartOffset);
        }

        private void SerializeMetadataHeader(BlobBuilder writer, MetadataSizes metadataSizes)
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

            string targetRuntimeVersion = metadataSizes.IsStandaloneDebugMetadata ? "PDB v1.0" : module.Properties.TargetRuntimeVersion;

            int n = Math.Min(MetadataSizes.MetadataVersionPaddedLength, targetRuntimeVersion.Length);
            for (int i = 0; i < n; i++)
            {
                writer.WriteByte((byte)targetRuntimeVersion[i]);
            }

            for (int i = n; i < MetadataSizes.MetadataVersionPaddedLength; i++)
            {
                writer.WriteByte(0);
            }

            // reserved
            writer.WriteUInt16(0);

            // number of streams
            writer.WriteUInt16((ushort)(5 + (metadataSizes.IsMinimalDelta ? 1 : 0) + (metadataSizes.IsStandaloneDebugMetadata ? 1 : 0)));

            // stream headers
            int offsetFromStartOfMetadata = metadataSizes.MetadataHeaderSize;

            // emit the #Pdb stream first so that only a single page has to be read in order to find out PDB ID
            if (metadataSizes.IsStandaloneDebugMetadata)
            {
                SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.StandalonePdbStreamSize, "#Pdb", writer);
            }

            // Spec: Some compilers store metadata in a #- stream, which holds an uncompressed, or non-optimized, representation of metadata tables;
            // this includes extra metadata -Ptr tables. Such PE files do not form part of ECMA-335 standard.
            //
            // Note: EnC delta is stored as uncompressed metadata stream.
            SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.MetadataTableStreamSize, (metadataSizes.IsMetadataTableStreamCompressed ? "#~" : "#-"), writer);

            SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.GetAlignedHeapSize(HeapIndex.String), "#Strings", writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.GetAlignedHeapSize(HeapIndex.UserString), "#US", writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.GetAlignedHeapSize(HeapIndex.Guid), "#GUID", writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.GetAlignedHeapSize(HeapIndex.Blob), "#Blob", writer);

            if (metadataSizes.IsMinimalDelta)
            {
                SerializeStreamHeader(ref offsetFromStartOfMetadata, 0, "#JTD", writer);
            }

            int endOffset = writer.Position;
            Debug.Assert(endOffset - startOffset == metadataSizes.MetadataHeaderSize);
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

        private static void SerializeStandalonePdbStream(BlobBuilder writer, MetadataSizes metadataSizes, int entryPointToken, out int pdbIdOffset)
        {
            int startPosition = writer.Position;

            // zero out and save position, will be filled in later
            pdbIdOffset = startPosition;
            writer.WriteBytes(0, MetadataSizes.PdbIdSize);

            writer.WriteUInt32((uint)entryPointToken);

            writer.WriteUInt64(metadataSizes.ExternalTablesMask);
            MetadataWriterUtilities.SerializeRowCounts(writer, metadataSizes.RowCounts, metadataSizes.ExternalTablesMask);

            int endPosition = writer.Position;
            Debug.Assert(metadataSizes.CalculateStandalonePdbStreamSize() == endPosition - startPosition);
        }
    }
}
