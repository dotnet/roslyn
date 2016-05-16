// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
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

        internal bool IsTiny(int codeSize)
        {
            return codeSize < 64 && _maxStack <= 8 && _localVariablesSignature.IsNil && _exceptionRegionCount == 0;
        }

        private int WriteHeader(int codeSize)
        {
            Blob blob;
            return WriteHeader(codeSize, false, out blob);
        }

        private int WriteHeader(int codeSize, bool codeSizeFixup, out Blob codeSizeBlob)
        {
            const int TinyFormat = 2;
            const int FatFormat = 3;
            const int MoreSections = 8;
            const byte InitLocals = 0x10;

            int offset;

            if (IsTiny(codeSize))
            {
                offset = Builder.Count;
                Builder.WriteByte((byte)((codeSize << 2) | TinyFormat));

                Debug.Assert(!codeSizeFixup);
                codeSizeBlob = default(Blob);
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
                if (codeSizeFixup)
                {
                    codeSizeBlob = Builder.ReserveBytes(sizeof(int));
                }
                else
                {
                    codeSizeBlob = default(Blob);
                    Builder.WriteInt32(codeSize);
                }

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

        public ExceptionRegionEncoder WriteInstructions(ImmutableArray<byte> instructions, out int bodyOffset)
        {
            bodyOffset = WriteHeader(instructions.Length);
            Builder.WriteBytes(instructions);
            return CreateExceptionEncoder();
        }

        public ExceptionRegionEncoder WriteInstructions(ImmutableArray<byte> instructions, out int bodyOffset, out Blob instructionBlob)
        {
            bodyOffset = WriteHeader(instructions.Length);
            instructionBlob = Builder.ReserveBytes(instructions.Length);
            new BlobWriter(instructionBlob).WriteBytes(instructions);
            return CreateExceptionEncoder();
        }

        public ExceptionRegionEncoder WriteInstructions(BlobBuilder codeBuilder, out int bodyOffset)
        {
            bodyOffset = WriteHeader(codeBuilder.Count);
            codeBuilder.WriteContentTo(Builder);
            return CreateExceptionEncoder();
        }

        public ExceptionRegionEncoder WriteInstructions(BlobBuilder codeBuilder, BranchBuilder branchBuilder, out int bodyOffset)
        {
            if (branchBuilder == null || branchBuilder.BranchCount == 0)
            {
                return WriteInstructions(codeBuilder, out bodyOffset);
            }
            
            // When emitting branches we emitted short branches.
            int initialCodeSize = codeBuilder.Count;
            Blob codeSizeFixup;
            if (IsTiny(initialCodeSize))
            {
                // If the method is tiny so far then all branches have to be short 
                // (the max distance between any label and a branch instruction is < 64).
                bodyOffset = WriteHeader(initialCodeSize);
                codeSizeFixup = default(Blob);
            }
            else
            {
                // Otherwise, it's fat format and we can fixup the size later on:
                bodyOffset = WriteHeader(initialCodeSize, true, out codeSizeFixup);
            }

            int codeStartOffset = Builder.Count;
            branchBuilder.FixupBranches(codeBuilder, Builder);
            if (!codeSizeFixup.IsDefault)
            {
                new BlobWriter(codeSizeFixup).WriteInt32(Builder.Count - codeStartOffset);
            }
            else
            {
                Debug.Assert(initialCodeSize == Builder.Count - codeStartOffset);
            }

            return CreateExceptionEncoder();
        }
    }
}
