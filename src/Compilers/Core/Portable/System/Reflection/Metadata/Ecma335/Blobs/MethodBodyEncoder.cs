// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

#if SRM
namespace System.Reflection.Metadata.Ecma335.Blobs
#else
namespace Roslyn.Reflection.Metadata.Ecma335.Blobs
#endif
{
    [Flags]
#if SRM
    public
#endif
    enum MethodBodyAttributes
    {
        None = 0,
        InitLocals = 1,
        LargeExceptionRegions = 2,
    }

#if SRM
    public
#endif
    struct MethodBodiesEncoder
    {
        public BlobBuilder Builder { get; }

        public MethodBodiesEncoder(BlobBuilder builder = null)
        {
            if (builder == null)
            {
                builder = new BlobBuilder();
            }

            // Fat methods are 4-byte aligned. We calculate the alignment relative to the start of the ILStream.
            //
            // See ECMA-335 paragraph 25.4.5, Method data section:
            // "At the next 4-byte boundary following the method body can be extra method data sections."
            if ((builder.Count % 4) != 0)
            {
                // TODO: error message
                throw new ArgumentException("Builder has to be aligned to 4 byte boundary", nameof(builder));
            }

            Builder = builder;
        }

        public MethodBodyEncoder AddMethodBody(
            int maxStack = 8,
            int exceptionRegionCount = 0,
            StandaloneSignatureHandle localVariablesSignature = default(StandaloneSignatureHandle),
            MethodBodyAttributes attributes = MethodBodyAttributes.InitLocals)
        {
            if (unchecked((ushort)maxStack) > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStack));
            }

            if (exceptionRegionCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exceptionRegionCount));
            }

            return new MethodBodyEncoder(Builder, (ushort)maxStack, exceptionRegionCount, localVariablesSignature, attributes);
        }
    }

#if SRM
    public
#endif
    struct MethodBodyEncoder
    {
        public BlobBuilder Builder { get; }

        private readonly ushort _maxStack;
        private readonly int _exceptionRegionCount;
        private readonly StandaloneSignatureHandle _localVariablesSignature;
        private readonly byte _attributes;

        internal MethodBodyEncoder(
            BlobBuilder builder,
            ushort maxStack,
            int exceptionRegionCount,
            StandaloneSignatureHandle localVariablesSignature,
            MethodBodyAttributes attributes)
        {
            Builder = builder;
            _maxStack = maxStack;
            _localVariablesSignature = localVariablesSignature;
            _attributes = (byte)attributes;
            _exceptionRegionCount = exceptionRegionCount;
        }

        private int WriteHeader(int codeSize)
        {
            const int TinyFormat = 2;
            const int FatFormat = 3;
            const int MoreSections = 8;
            const byte InitLocals = 0x10;

            int offset;

            bool isTiny = codeSize < 64 && _maxStack <= 8 && _localVariablesSignature.IsNil && _exceptionRegionCount == 0;
            if (isTiny)
            {
                offset = Builder.Count;
                Builder.WriteByte((byte)((codeSize << 2) | TinyFormat));
            }
            else
            {
                Builder.Align(4);

                offset = Builder.Count;

                ushort flags = (3 << 12) | FatFormat;
                if (_exceptionRegionCount > 0)
                {
                    flags |= MoreSections;
                }

                if ((_attributes & (int)MethodBodyAttributes.InitLocals) != 0)
                {
                    flags |= InitLocals;
                }

                Builder.WriteUInt16((ushort)(_attributes | flags));
                Builder.WriteUInt16(_maxStack);
                Builder.WriteInt32(codeSize);
                Builder.WriteInt32(_localVariablesSignature.IsNil ? 0 : MetadataTokens.GetToken(_localVariablesSignature));
            }

            return offset;
        }

        private ExceptionRegionEncoder CreateExceptionEncoder()
        {
            return new ExceptionRegionEncoder(
                Builder, 
                _exceptionRegionCount,
                hasLargeRegions: (_attributes & (int)MethodBodyAttributes.LargeExceptionRegions) != 0);
        }

        public ExceptionRegionEncoder WriteInstructions(ImmutableArray<byte> buffer, out int offset)
        {
            offset = WriteHeader(buffer.Length);
            Builder.WriteBytes(buffer);
            return CreateExceptionEncoder();
        }

        public ExceptionRegionEncoder WriteInstructions(ImmutableArray<byte> buffer, out int offset, out Blob instructionBlob)
        {
            offset = WriteHeader(buffer.Length);
            instructionBlob = Builder.ReserveBytes(buffer.Length);
            new BlobWriter(instructionBlob).WriteBytes(buffer);
            return CreateExceptionEncoder();
        }

        public ExceptionRegionEncoder WriteInstructions(BlobBuilder buffer, out int offset)
        {
            offset = WriteHeader(buffer.Count);
            buffer.WriteContentTo(Builder);
            return CreateExceptionEncoder();
        }
    }
}
