// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal sealed class MetadataSizes
    {
        private const int StreamAlignment = 4;

        public const ulong DebugMetadataTablesMask =
            1UL << (int)TableIndex.Document |
            1UL << (int)TableIndex.MethodDebugInformation |
            1UL << (int)TableIndex.LocalScope |
            1UL << (int)TableIndex.LocalVariable |
            1UL << (int)TableIndex.LocalConstant |
            1UL << (int)TableIndex.ImportScope |
            1UL << (int)TableIndex.StateMachineMethod |
            1UL << (int)TableIndex.CustomDebugInformation;

        public const ulong SortedDebugTables =
            1UL << (int)TableIndex.LocalScope |
            1UL << (int)TableIndex.StateMachineMethod |
            1UL << (int)TableIndex.CustomDebugInformation;

        public readonly bool IsMinimalDelta;

        // EnC delta tables are stored as uncompressed metadata table stream
        public bool IsMetadataTableStreamCompressed => !IsMinimalDelta;

        public readonly byte BlobIndexSize;
        public readonly byte StringIndexSize;
        public readonly byte GuidIndexSize;
        public readonly byte CustomAttributeTypeCodedIndexSize;
        public readonly byte DeclSecurityCodedIndexSize;
        public readonly byte EventDefIndexSize;
        public readonly byte FieldDefIndexSize;
        public readonly byte GenericParamIndexSize;
        public readonly byte HasConstantCodedIndexSize;
        public readonly byte HasCustomAttributeCodedIndexSize;
        public readonly byte HasFieldMarshalCodedIndexSize;
        public readonly byte HasSemanticsCodedIndexSize;
        public readonly byte ImplementationCodedIndexSize;
        public readonly byte MemberForwardedCodedIndexSize;
        public readonly byte MemberRefParentCodedIndexSize;
        public readonly byte MethodDefIndexSize;
        public readonly byte MethodDefOrRefCodedIndexSize;
        public readonly byte ModuleRefIndexSize;
        public readonly byte ParameterIndexSize;
        public readonly byte PropertyDefIndexSize;
        public readonly byte ResolutionScopeCodedIndexSize;
        public readonly byte TypeDefIndexSize;
        public readonly byte TypeDefOrRefCodedIndexSize;
        public readonly byte TypeOrMethodDefCodedIndexSize;

        public readonly byte DocumentIndexSize;
        public readonly byte LocalVariableIndexSize;
        public readonly byte LocalConstantIndexSize;
        public readonly byte ImportScopeIndexSize;
        public readonly byte HasCustomDebugInformationSize;

        /// <summary>
        /// Table row counts. 
        /// </summary>
        public readonly ImmutableArray<int> RowCounts;

        /// <summary>
        /// Non-empty tables that are emitted into the metadata table stream.
        /// </summary>
        public readonly ulong PresentTablesMask;

        /// <summary>
        /// Non-empty tables stored in an external metadata table stream that might be referenced from the metadata table stream being emitted.
        /// </summary>
        public readonly ulong ExternalTablesMask;

        /// <summary>
        /// Exact (unaligned) heap sizes.
        /// </summary>
        public readonly ImmutableArray<int> HeapSizes;

        /// <summary>
        /// Overall size of metadata stream storage (stream headers, table stream, heaps, additional streams).
        /// Aligned to <see cref="StreamAlignment"/>.
        /// </summary>
        public readonly int MetadataStreamStorageSize;

        /// <summary>
        /// The size of metadata stream (#- or #~). Aligned.
        /// Aligned to <see cref="StreamAlignment"/>.
        /// </summary>
        public readonly int MetadataTableStreamSize;

        /// <summary>
        /// The size of #Pdb stream. Aligned.
        /// </summary>
        public readonly int StandalonePdbStreamSize;

        /// <summary>
        /// The size of IL stream.
        /// </summary>
        public readonly int ILStreamSize;

        /// <summary>
        /// The size of mapped field data stream.
        /// Aligned to <see cref="MetadataWriter.MappedFieldDataAlignment"/>.
        /// </summary>
        public readonly int MappedFieldDataSize;

        /// <summary>
        /// The size of managed resource data stream.
        /// Aligned to <see cref="MetadataWriter.ManagedResourcesDataAlignment"/>.
        /// </summary>
        public readonly int ResourceDataSize;

        /// <summary>
        /// Size of strong name hash.
        /// </summary>
        public readonly int StrongNameSignatureSize;

        public MetadataSizes(
            ImmutableArray<int> rowCounts,
            ImmutableArray<int> heapSizes,
            int ilStreamSize,
            int mappedFieldDataSize,
            int resourceDataSize,
            int strongNameSignatureSize,
            bool isMinimalDelta,
            bool emitStandaloneDebugMetadata,
            bool isStandaloneDebugMetadata)
        {
            Debug.Assert(rowCounts.Length == MetadataTokens.TableCount);
            Debug.Assert(heapSizes.Length == MetadataTokens.HeapCount);
            Debug.Assert(!isStandaloneDebugMetadata || emitStandaloneDebugMetadata);

            const byte large = 4;
            const byte small = 2;

            this.RowCounts = rowCounts;
            this.HeapSizes = heapSizes;
            this.ResourceDataSize = resourceDataSize;
            this.ILStreamSize = ilStreamSize;
            this.MappedFieldDataSize = mappedFieldDataSize;
            this.StrongNameSignatureSize = strongNameSignatureSize;
            this.IsMinimalDelta = isMinimalDelta;

            this.BlobIndexSize = (isMinimalDelta || heapSizes[(int)HeapIndex.Blob] > ushort.MaxValue) ? large : small;
            this.StringIndexSize = (isMinimalDelta || heapSizes[(int)HeapIndex.String] > ushort.MaxValue) ? large : small;
            this.GuidIndexSize = (isMinimalDelta || heapSizes[(int)HeapIndex.Guid] > ushort.MaxValue) ? large : small;

            ulong allTables = ComputeNonEmptyTableMask(rowCounts);
            if (!emitStandaloneDebugMetadata)
            {
                // all tables
                PresentTablesMask = allTables;
                ExternalTablesMask = 0;
            }
            else if (isStandaloneDebugMetadata)
            {
                // debug tables:
                PresentTablesMask = allTables & DebugMetadataTablesMask;
                ExternalTablesMask = allTables & ~DebugMetadataTablesMask;
            }
            else
            {
                // type-system tables only:
                PresentTablesMask = allTables & ~DebugMetadataTablesMask;
                ExternalTablesMask = 0;
            }

            this.CustomAttributeTypeCodedIndexSize = this.GetReferenceByteSize(3, TableIndex.MethodDef, TableIndex.MemberRef);
            this.DeclSecurityCodedIndexSize = this.GetReferenceByteSize(2, TableIndex.MethodDef, TableIndex.TypeDef);
            this.EventDefIndexSize = this.GetReferenceByteSize(0, TableIndex.Event);
            this.FieldDefIndexSize = this.GetReferenceByteSize(0, TableIndex.Field);
            this.GenericParamIndexSize = this.GetReferenceByteSize(0, TableIndex.GenericParam);
            this.HasConstantCodedIndexSize = this.GetReferenceByteSize(2, TableIndex.Field, TableIndex.Param, TableIndex.Property);

            this.HasCustomAttributeCodedIndexSize = this.GetReferenceByteSize(5,
                TableIndex.MethodDef,
                TableIndex.Field,
                TableIndex.TypeRef,
                TableIndex.TypeDef,
                TableIndex.Param,
                TableIndex.InterfaceImpl,
                TableIndex.MemberRef,
                TableIndex.Module,
                TableIndex.DeclSecurity,
                TableIndex.Property,
                TableIndex.Event,
                TableIndex.StandAloneSig,
                TableIndex.ModuleRef,
                TableIndex.TypeSpec,
                TableIndex.Assembly,
                TableIndex.AssemblyRef,
                TableIndex.File,
                TableIndex.ExportedType,
                TableIndex.ManifestResource,
                TableIndex.GenericParam,
                TableIndex.GenericParamConstraint,
                TableIndex.MethodSpec);

            this.HasFieldMarshalCodedIndexSize = this.GetReferenceByteSize(1, TableIndex.Field, TableIndex.Param);
            this.HasSemanticsCodedIndexSize = this.GetReferenceByteSize(1, TableIndex.Event, TableIndex.Property);
            this.ImplementationCodedIndexSize = this.GetReferenceByteSize(2, TableIndex.File, TableIndex.AssemblyRef, TableIndex.ExportedType);
            this.MemberForwardedCodedIndexSize = this.GetReferenceByteSize(1, TableIndex.Field, TableIndex.MethodDef);
            this.MemberRefParentCodedIndexSize = this.GetReferenceByteSize(3, TableIndex.TypeDef, TableIndex.TypeRef, TableIndex.ModuleRef, TableIndex.MethodDef, TableIndex.TypeSpec);
            this.MethodDefIndexSize = this.GetReferenceByteSize(0, TableIndex.MethodDef);
            this.MethodDefOrRefCodedIndexSize = this.GetReferenceByteSize(1, TableIndex.MethodDef, TableIndex.MemberRef);
            this.ModuleRefIndexSize = this.GetReferenceByteSize(0, TableIndex.ModuleRef);
            this.ParameterIndexSize = this.GetReferenceByteSize(0, TableIndex.Param);
            this.PropertyDefIndexSize = this.GetReferenceByteSize(0, TableIndex.Property);
            this.ResolutionScopeCodedIndexSize = this.GetReferenceByteSize(2, TableIndex.Module, TableIndex.ModuleRef, TableIndex.AssemblyRef, TableIndex.TypeRef);
            this.TypeDefIndexSize = this.GetReferenceByteSize(0, TableIndex.TypeDef);
            this.TypeDefOrRefCodedIndexSize = this.GetReferenceByteSize(2, TableIndex.TypeDef, TableIndex.TypeRef, TableIndex.TypeSpec);
            this.TypeOrMethodDefCodedIndexSize = this.GetReferenceByteSize(1, TableIndex.TypeDef, TableIndex.MethodDef);

            this.DocumentIndexSize = this.GetReferenceByteSize(0, TableIndex.Document);
            this.LocalVariableIndexSize = this.GetReferenceByteSize(0, TableIndex.LocalVariable);
            this.LocalConstantIndexSize = this.GetReferenceByteSize(0, TableIndex.LocalConstant);
            this.ImportScopeIndexSize = this.GetReferenceByteSize(0, TableIndex.ImportScope);

            this.HasCustomDebugInformationSize = this.GetReferenceByteSize(5,
                TableIndex.MethodDef,
                TableIndex.Field,
                TableIndex.TypeRef,
                TableIndex.TypeDef,
                TableIndex.Param,
                TableIndex.InterfaceImpl,
                TableIndex.MemberRef,
                TableIndex.Module,
                TableIndex.DeclSecurity,
                TableIndex.Property,
                TableIndex.Event,
                TableIndex.StandAloneSig,
                TableIndex.ModuleRef,
                TableIndex.TypeSpec,
                TableIndex.Assembly,
                TableIndex.AssemblyRef,
                TableIndex.File,
                TableIndex.ExportedType,
                TableIndex.ManifestResource,
                TableIndex.GenericParam,
                TableIndex.GenericParamConstraint,
                TableIndex.MethodSpec,
                TableIndex.Document,
                TableIndex.LocalScope,
                TableIndex.LocalVariable,
                TableIndex.LocalConstant,
                TableIndex.ImportScope);

            int size = this.CalculateTableStreamHeaderSize();

            size += GetTableSize(TableIndex.Module, 2 + 3 * this.GuidIndexSize + this.StringIndexSize);
            size += GetTableSize(TableIndex.TypeRef, this.ResolutionScopeCodedIndexSize + this.StringIndexSize + this.StringIndexSize);
            size += GetTableSize(TableIndex.TypeDef, 4 + this.StringIndexSize + this.StringIndexSize + this.TypeDefOrRefCodedIndexSize + this.FieldDefIndexSize + this.MethodDefIndexSize);
            Debug.Assert(rowCounts[(int)TableIndex.FieldPtr] == 0);
            size += GetTableSize(TableIndex.Field, 2 + this.StringIndexSize + this.BlobIndexSize);
            Debug.Assert(rowCounts[(int)TableIndex.MethodPtr] == 0);
            size += GetTableSize(TableIndex.MethodDef, 8 + this.StringIndexSize + this.BlobIndexSize + this.ParameterIndexSize);
            Debug.Assert(rowCounts[(int)TableIndex.ParamPtr] == 0);
            size += GetTableSize(TableIndex.Param, 4 + this.StringIndexSize);
            size += GetTableSize(TableIndex.InterfaceImpl, this.TypeDefIndexSize + this.TypeDefOrRefCodedIndexSize);
            size += GetTableSize(TableIndex.MemberRef, this.MemberRefParentCodedIndexSize + this.StringIndexSize + this.BlobIndexSize);
            size += GetTableSize(TableIndex.Constant, 2 + this.HasConstantCodedIndexSize + this.BlobIndexSize);
            size += GetTableSize(TableIndex.CustomAttribute, this.HasCustomAttributeCodedIndexSize + this.CustomAttributeTypeCodedIndexSize + this.BlobIndexSize);
            size += GetTableSize(TableIndex.FieldMarshal, this.HasFieldMarshalCodedIndexSize + this.BlobIndexSize);
            size += GetTableSize(TableIndex.DeclSecurity, 2 + this.DeclSecurityCodedIndexSize + this.BlobIndexSize);
            size += GetTableSize(TableIndex.ClassLayout, 6 + this.TypeDefIndexSize);
            size += GetTableSize(TableIndex.FieldLayout, 4 + this.FieldDefIndexSize);
            size += GetTableSize(TableIndex.StandAloneSig, this.BlobIndexSize);
            size += GetTableSize(TableIndex.EventMap, this.TypeDefIndexSize + this.EventDefIndexSize);
            Debug.Assert(rowCounts[(int)TableIndex.EventPtr] == 0);
            size += GetTableSize(TableIndex.Event, 2 + this.StringIndexSize + this.TypeDefOrRefCodedIndexSize);
            size += GetTableSize(TableIndex.PropertyMap, this.TypeDefIndexSize + this.PropertyDefIndexSize);
            Debug.Assert(rowCounts[(int)TableIndex.PropertyPtr] == 0);
            size += GetTableSize(TableIndex.Property, 2 + this.StringIndexSize + this.BlobIndexSize);
            size += GetTableSize(TableIndex.MethodSemantics, 2 + this.MethodDefIndexSize + this.HasSemanticsCodedIndexSize);
            size += GetTableSize(TableIndex.MethodImpl, 0 + this.TypeDefIndexSize + this.MethodDefOrRefCodedIndexSize + this.MethodDefOrRefCodedIndexSize);
            size += GetTableSize(TableIndex.ModuleRef, 0 + this.StringIndexSize);
            size += GetTableSize(TableIndex.TypeSpec, 0 + this.BlobIndexSize);
            size += GetTableSize(TableIndex.ImplMap, 2 + this.MemberForwardedCodedIndexSize + this.StringIndexSize + this.ModuleRefIndexSize);
            size += GetTableSize(TableIndex.FieldRva, 4 + this.FieldDefIndexSize);
            size += GetTableSize(TableIndex.EncLog, 8);
            size += GetTableSize(TableIndex.EncMap, 4);
            size += GetTableSize(TableIndex.Assembly, 16 + this.BlobIndexSize + this.StringIndexSize + this.StringIndexSize);
            Debug.Assert(rowCounts[(int)TableIndex.AssemblyProcessor] == 0);
            Debug.Assert(rowCounts[(int)TableIndex.AssemblyOS] == 0);
            size += GetTableSize(TableIndex.AssemblyRef, 12 + this.BlobIndexSize + this.StringIndexSize + this.StringIndexSize + this.BlobIndexSize);
            Debug.Assert(rowCounts[(int)TableIndex.AssemblyRefProcessor] == 0);
            Debug.Assert(rowCounts[(int)TableIndex.AssemblyRefOS] == 0);
            size += GetTableSize(TableIndex.File, 4 + this.StringIndexSize + this.BlobIndexSize);
            size += GetTableSize(TableIndex.ExportedType, 8 + this.StringIndexSize + this.StringIndexSize + this.ImplementationCodedIndexSize);
            size += GetTableSize(TableIndex.ManifestResource, 8 + this.StringIndexSize + this.ImplementationCodedIndexSize);
            size += GetTableSize(TableIndex.NestedClass, this.TypeDefIndexSize + this.TypeDefIndexSize);
            size += GetTableSize(TableIndex.GenericParam, 4 + this.TypeOrMethodDefCodedIndexSize + this.StringIndexSize);
            size += GetTableSize(TableIndex.MethodSpec, this.MethodDefOrRefCodedIndexSize + this.BlobIndexSize);
            size += GetTableSize(TableIndex.GenericParamConstraint, this.GenericParamIndexSize + this.TypeDefOrRefCodedIndexSize);

            size += GetTableSize(TableIndex.Document, this.BlobIndexSize + this.GuidIndexSize + this.BlobIndexSize + this.GuidIndexSize);
            size += GetTableSize(TableIndex.MethodDebugInformation, this.DocumentIndexSize + this.BlobIndexSize);
            size += GetTableSize(TableIndex.LocalScope, this.MethodDefIndexSize + this.ImportScopeIndexSize + this.LocalVariableIndexSize + this.LocalConstantIndexSize + 4 + 4);
            size += GetTableSize(TableIndex.LocalVariable, 2 + 2 + this.StringIndexSize);
            size += GetTableSize(TableIndex.LocalConstant, this.StringIndexSize + this.BlobIndexSize);
            size += GetTableSize(TableIndex.ImportScope, this.ImportScopeIndexSize + this.BlobIndexSize);
            size += GetTableSize(TableIndex.StateMachineMethod, this.MethodDefIndexSize + this.MethodDefIndexSize);
            size += GetTableSize(TableIndex.CustomDebugInformation, this.HasCustomDebugInformationSize + this.GuidIndexSize + this.BlobIndexSize);

            // +1 for terminating 0 byte
            size = BitArithmeticUtilities.Align(size + 1, StreamAlignment);

            this.MetadataTableStreamSize = size;

            size += GetAlignedHeapSize(HeapIndex.String);
            size += GetAlignedHeapSize(HeapIndex.UserString);
            size += GetAlignedHeapSize(HeapIndex.Guid);
            size += GetAlignedHeapSize(HeapIndex.Blob);

            this.StandalonePdbStreamSize = isStandaloneDebugMetadata ? CalculateStandalonePdbStreamSize() : 0;
            size += this.StandalonePdbStreamSize;

            this.MetadataStreamStorageSize = size;
        }

        public bool IsStandaloneDebugMetadata => StandalonePdbStreamSize > 0;

        public bool IsPresent(TableIndex table) => (PresentTablesMask & (1UL << (int)table)) != 0;

        /// <summary>
        /// Metadata header size.
        /// Includes:
        /// - metadata storage signature
        /// - storage header
        /// - stream headers
        /// </summary>
        public int MetadataHeaderSize
        {
            get
            {
                const int RegularStreamHeaderSizes = 76;
                const int MinimalDeltaMarkerStreamHeaderSize = 16;
                const int StandalonePdbStreamHeaderSize = 16;

                Debug.Assert(RegularStreamHeaderSizes ==
                    GetMetadataStreamHeaderSize("#~") +
                    GetMetadataStreamHeaderSize("#Strings") +
                    GetMetadataStreamHeaderSize("#US") +
                    GetMetadataStreamHeaderSize("#GUID") +
                    GetMetadataStreamHeaderSize("#Blob"));

                Debug.Assert(MinimalDeltaMarkerStreamHeaderSize == GetMetadataStreamHeaderSize("#JTD"));
                Debug.Assert(StandalonePdbStreamHeaderSize == GetMetadataStreamHeaderSize("#Pdb"));

                return
                    sizeof(uint) +                 // signature
                    sizeof(ushort) +               // major version
                    sizeof(ushort) +               // minor version
                    sizeof(uint) +                 // reserved
                    sizeof(uint) +                 // padded metadata version length
                    MetadataVersionPaddedLength +  // metadata version
                    sizeof(ushort) +               // storage header: reserved
                    sizeof(ushort) +               // stream count
                    (IsStandaloneDebugMetadata ? StandalonePdbStreamHeaderSize : 0) +
                    RegularStreamHeaderSizes +
                    (IsMinimalDelta ? MinimalDeltaMarkerStreamHeaderSize : 0);
            }
        }

        // version must be 12 chars long, this observation is not supported by the standard
        public const int MetadataVersionPaddedLength = 12;

        public static int GetMetadataStreamHeaderSize(string streamName)
        {
            return
                sizeof(int) + // offset
                sizeof(int) + // size
                BitArithmeticUtilities.Align(streamName.Length + 1, 4); // zero-terminated name, padding
        }

        /// <summary>
        /// Total size of metadata (header and all streams).
        /// </summary>
        public int MetadataSize => MetadataHeaderSize + MetadataStreamStorageSize;

        public int GetAlignedHeapSize(HeapIndex index)
        {
            return BitArithmeticUtilities.Align(HeapSizes[(int)index], StreamAlignment);
        }

        internal int CalculateTableStreamHeaderSize()
        {
            int result = sizeof(int) +        // Reserved
                         sizeof(short) +      // Version (major, minor)      
                         sizeof(byte) +       // Heap index sizes
                         sizeof(byte) +       // Bit width of RowId
                         sizeof(long) +       // Valid table mask
                         sizeof(long);        // Sorted table mask

            // present table row counts
            for (int i = 0; i < RowCounts.Length; i++)
            {
                if (((1UL << i) & PresentTablesMask) != 0)
                {
                    result += sizeof(int);
                }
            }

            return result;
        }

        internal const int PdbIdSize = 20;

        internal int CalculateStandalonePdbStreamSize()
        {
            int result = PdbIdSize +          // PDB ID
                         sizeof(int) +        // EntryPoint
                         sizeof(long);        // ReferencedTypeSystemTables

            // external table row counts
            for (int i = 0; i < RowCounts.Length; i++)
            {
                if (((1UL << i) & ExternalTablesMask) != 0)
                {
                    result += sizeof(int);
                }
            }

            Debug.Assert(result % StreamAlignment == 0);
            return result;
        }

        private static ulong ComputeNonEmptyTableMask(ImmutableArray<int> rowCounts)
        {
            ulong mask = 0;
            for (int i = 0; i < rowCounts.Length; i++)
            {
                if (rowCounts[i] > 0)
                {
                    mask |= (1UL << i);
                }
            }

            return mask;
        }

        private int GetTableSize(TableIndex index, int rowSize)
        {
            return (PresentTablesMask & (1UL << (int)index)) != 0 ? RowCounts[(int)index] * rowSize : 0;
        }

        private byte GetReferenceByteSize(int tagBitSize, params TableIndex[] tables)
        {
            const byte large = 4;
            const byte small = 2;
            const int smallBitCount = 16;

            return (!IsMetadataTableStreamCompressed || !ReferenceFits(smallBitCount - tagBitSize, tables)) ? large : small;
        }

        private bool ReferenceFits(int bitCount, TableIndex[] tables)
        {
            int maxIndex = (1 << bitCount) - 1;
            foreach (TableIndex table in tables)
            {
                if (RowCounts[(int)table] > maxIndex)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
