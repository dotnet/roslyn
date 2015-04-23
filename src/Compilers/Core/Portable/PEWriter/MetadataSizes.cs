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

        private readonly bool _isMinimalDelta;

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

        public readonly byte LocalVariableIndexSize;
        public readonly byte LocalConstantIndexSize;
        public readonly byte ImportScopeIndexSize;
        public readonly byte HasCustomDebugInformationSize;

        /// <summary>
        /// Table row counts. 
        /// </summary>
        public readonly ImmutableArray<int> RowCounts;

        /// <summary>
        /// Exact (unaligned) heap sizes.
        /// </summary>
        public readonly ImmutableArray<int> HeapSizes;

        /// <summary>
        /// Overall size of metadata stream storage (stream headers, streams: heaps + tables).
        /// </summary>
        public readonly int MetadataStreamStorageSize;

        /// <summary>
        /// The size of metadata stream (#- or #~). Aligned.
        /// </summary>
        public readonly int MetadataTableStreamSize;

        /// <summary>
        /// The size of IL stream.
        /// </summary>
        public readonly int ILStreamSize;

        /// <summary>
        /// The size of mapped field data stream.
        /// </summary>
        public readonly int MappedFieldDataSize;

        /// <summary>
        /// The size of managed resource data stream.
        /// </summary>
        public readonly int ResourceDataSize;

        public MetadataSizes(
            ImmutableArray<int> rowCounts,
            ImmutableArray<int> heapSizes,
            int ilStreamSize,
            int mappedFieldDataSize,
            int resourceDataSize,
            bool isMinimalDelta)
        {
            Debug.Assert(rowCounts.Length == MetadataTokens.TableCount);
            Debug.Assert(heapSizes.Length == MetadataTokens.HeapCount);

            const byte large = 4;
            const byte small = 2;

            this.RowCounts = rowCounts;
            this.HeapSizes = heapSizes;
            this.ResourceDataSize = resourceDataSize;
            this.ILStreamSize = ilStreamSize;
            this.MappedFieldDataSize = mappedFieldDataSize;
            _isMinimalDelta = isMinimalDelta;

            this.BlobIndexSize = (isMinimalDelta || heapSizes[(int)HeapIndex.Blob] > ushort.MaxValue) ? large : small;
            this.StringIndexSize = (isMinimalDelta || heapSizes[(int)HeapIndex.String] > ushort.MaxValue) ? large : small;
            this.GuidIndexSize = (isMinimalDelta || heapSizes[(int)HeapIndex.Guid] > ushort.MaxValue) ? large : small;

            this.CustomAttributeTypeCodedIndexSize = this.GetIndexByteSize(3, TableIndex.MethodDef, TableIndex.MemberRef);
            this.DeclSecurityCodedIndexSize = this.GetIndexByteSize(2, TableIndex.MethodDef, TableIndex.TypeDef);
            this.EventDefIndexSize = this.GetIndexByteSize(0, TableIndex.Event);
            this.FieldDefIndexSize = this.GetIndexByteSize(0, TableIndex.Field);
            this.GenericParamIndexSize = this.GetIndexByteSize(0, TableIndex.GenericParam);
            this.HasConstantCodedIndexSize = this.GetIndexByteSize(2, TableIndex.Field, TableIndex.Param, TableIndex.Property);

            this.HasCustomAttributeCodedIndexSize = this.GetIndexByteSize(5,
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

            this.HasFieldMarshalCodedIndexSize = this.GetIndexByteSize(1, TableIndex.Field, TableIndex.Param);
            this.HasSemanticsCodedIndexSize = this.GetIndexByteSize(1, TableIndex.Event, TableIndex.Property);
            this.ImplementationCodedIndexSize = this.GetIndexByteSize(2, TableIndex.File, TableIndex.AssemblyRef, TableIndex.ExportedType);
            this.MemberForwardedCodedIndexSize = this.GetIndexByteSize(1, TableIndex.Field, TableIndex.MethodDef);
            this.MemberRefParentCodedIndexSize = this.GetIndexByteSize(3, TableIndex.TypeDef, TableIndex.TypeRef, TableIndex.ModuleRef, TableIndex.MethodDef, TableIndex.TypeSpec);
            this.MethodDefIndexSize = this.GetIndexByteSize(0, TableIndex.MethodDef);
            this.MethodDefOrRefCodedIndexSize = this.GetIndexByteSize(1, TableIndex.MethodDef, TableIndex.MemberRef);
            this.ModuleRefIndexSize = this.GetIndexByteSize(0, TableIndex.ModuleRef);
            this.ParameterIndexSize = this.GetIndexByteSize(0, TableIndex.Param);
            this.PropertyDefIndexSize = this.GetIndexByteSize(0, TableIndex.Property);
            this.ResolutionScopeCodedIndexSize = this.GetIndexByteSize(2, TableIndex.Module, TableIndex.ModuleRef, TableIndex.AssemblyRef, TableIndex.TypeRef);
            this.TypeDefIndexSize = this.GetIndexByteSize(0, TableIndex.TypeDef);
            this.TypeDefOrRefCodedIndexSize = this.GetIndexByteSize(2, TableIndex.TypeDef, TableIndex.TypeRef, TableIndex.TypeSpec);
            this.TypeOrMethodDefCodedIndexSize = this.GetIndexByteSize(1, TableIndex.TypeDef, TableIndex.MethodDef);

            this.LocalVariableIndexSize = this.GetIndexByteSize(0, TableIndex.LocalVariable);
            this.LocalConstantIndexSize = this.GetIndexByteSize(0, TableIndex.LocalConstant);
            this.ImportScopeIndexSize = this.GetIndexByteSize(0, TableIndex.ImportScope);

            this.HasCustomDebugInformationSize = this.GetIndexByteSize(5,
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
            Debug.Assert(RowCounts[(int)TableIndex.FieldPtr] == 0);
            size += GetTableSize(TableIndex.Field, 2 + this.StringIndexSize + this.BlobIndexSize);
            Debug.Assert(RowCounts[(int)TableIndex.MethodPtr] == 0);
            size += GetTableSize(TableIndex.MethodDef, 8 + this.StringIndexSize + this.BlobIndexSize + this.ParameterIndexSize);
            Debug.Assert(RowCounts[(int)TableIndex.ParamPtr] == 0);
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
            Debug.Assert(RowCounts[(int)TableIndex.EventPtr] == 0);
            size += GetTableSize(TableIndex.Event, 2 + this.StringIndexSize + this.TypeDefOrRefCodedIndexSize);
            size += GetTableSize(TableIndex.PropertyMap, this.TypeDefIndexSize + this.PropertyDefIndexSize);
            Debug.Assert(RowCounts[(int)TableIndex.PropertyPtr] == 0);
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
            Debug.Assert(RowCounts[(int)TableIndex.AssemblyProcessor] == 0);
            Debug.Assert(RowCounts[(int)TableIndex.AssemblyOS] == 0);
            size += GetTableSize(TableIndex.AssemblyRef, 12 + this.BlobIndexSize + this.StringIndexSize + this.StringIndexSize + this.BlobIndexSize);
            Debug.Assert(RowCounts[(int)TableIndex.AssemblyRefProcessor] == 0);
            Debug.Assert(RowCounts[(int)TableIndex.AssemblyRefOS] == 0);
            size += GetTableSize(TableIndex.File, 4 + this.StringIndexSize + this.BlobIndexSize);
            size += GetTableSize(TableIndex.ExportedType, 8 + this.StringIndexSize + this.StringIndexSize + this.ImplementationCodedIndexSize);
            size += GetTableSize(TableIndex.ManifestResource, 8 + this.StringIndexSize + this.ImplementationCodedIndexSize);
            size += GetTableSize(TableIndex.NestedClass, this.TypeDefIndexSize + this.TypeDefIndexSize);
            size += GetTableSize(TableIndex.GenericParam, 4 + this.TypeOrMethodDefCodedIndexSize + this.StringIndexSize);
            size += GetTableSize(TableIndex.MethodSpec, this.MethodDefOrRefCodedIndexSize + this.BlobIndexSize);
            size += GetTableSize(TableIndex.GenericParamConstraint, this.GenericParamIndexSize + this.TypeDefOrRefCodedIndexSize);

            size += GetTableSize(TableIndex.Document, this.BlobIndexSize + this.GuidIndexSize + this.BlobIndexSize + this.GuidIndexSize);
            size += GetTableSize(TableIndex.MethodBody, this.BlobIndexSize);
            size += GetTableSize(TableIndex.LocalScope, this.MethodDefIndexSize + this.ImportScopeIndexSize + this.LocalVariableIndexSize + this.LocalConstantIndexSize + 4 + 4);
            size += GetTableSize(TableIndex.LocalVariable, 2 + 2 + this.StringIndexSize);
            size += GetTableSize(TableIndex.LocalConstant, this.StringIndexSize + this.BlobIndexSize);
            size += GetTableSize(TableIndex.ImportScope, this.ImportScopeIndexSize + this.BlobIndexSize);
            size += GetTableSize(TableIndex.AsyncMethod, this.MethodDefIndexSize + 4 + this.BlobIndexSize);
            size += GetTableSize(TableIndex.CustomDebugInformation, this.HasCustomDebugInformationSize + this.GuidIndexSize + this.BlobIndexSize);

            // +1 for terminating 0 byte
            size = BitArithmeticUtilities.Align(size + 1, StreamAlignment);

            this.MetadataTableStreamSize = size;

            size += GetAlignedHeapSize(HeapIndex.String);
            size += GetAlignedHeapSize(HeapIndex.UserString);
            size += GetAlignedHeapSize(HeapIndex.Guid);
            size += GetAlignedHeapSize(HeapIndex.Blob);

            this.MetadataStreamStorageSize = size;
        }

        public bool IsEmpty(TableIndex table) => RowCounts[(int)table] == 0;

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
                return _isMinimalDelta ? 124 : 108;
            }
        }

        /// <summary>
        /// Total size of metadata (header and all streams).
        /// </summary>
        public int MetadataSize
        {
            get
            {
                return MetadataHeaderSize + MetadataStreamStorageSize;
            }
        }

        public int GetAlignedHeapSize(HeapIndex index)
        {
            return BitArithmeticUtilities.Align(HeapSizes[(int)index], StreamAlignment);
        }

        private int GetTableSize(TableIndex index, int rowSize)
        {
            return RowCounts[(int)index] * rowSize;
        }

        internal int CalculateTableStreamHeaderSize()
        {
            int result = sizeof(int) +        // Reserved
                         sizeof(short) +      // Version (major, minor)      
                         sizeof(byte) +       // Heap index sizes
                         sizeof(byte) +       // Bit width of RowId
                         sizeof(long) +       // Valid table mask
                         sizeof(long);        // Sorted table mask

            foreach (int rowCount in RowCounts)
            {
                if (rowCount > 0)
                {
                    // present table row count
                    result += sizeof(int);
                }
            }

            return result;
        }

        private byte GetIndexByteSize(int discriminatingBits, params TableIndex[] tables)
        {
            const int BitsPerShort = 16;
            return (byte)(_isMinimalDelta || IndexDoesNotFit(BitsPerShort - discriminatingBits, tables) ? 4 : 2);
        }

        private bool IndexDoesNotFit(int numberOfBits, params TableIndex[] tables)
        {
            int maxIndex = (1 << numberOfBits) - 1;
            foreach (TableIndex table in tables)
            {
                if (RowCounts[(int)table] > maxIndex)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
