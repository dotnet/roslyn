using System;
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable RS0008 // Implement IEquatable<T> when overriding Object.Equals

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

#if !SRM
using PrimitiveTypeCode = Microsoft.Cci.PrimitiveTypeCode;
#endif

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
        InitLocals = 1
    }

#if SRM
    public
#endif
    class MethodBodyEncoder
    {
        public static int MethodBodyHeader(BlobBuilder builder, StandaloneSignatureHandle localVariablesSignature, int maxStack, int codeSize, int exceptionRegionCount, MethodBodyAttributes attributes)
        {
            if (unchecked((ushort)maxStack) > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStack));
            }

            if (codeSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(codeSize));
            }

            const int TinyFormat = 2;
            const int FatFormat = 3;
            const int MoreSections = 8;
            const int InitLocals = 0x10;

            int offset;

            bool isTiny = codeSize < 64 && maxStack <= 8 && localVariablesSignature.IsNil && exceptionRegionCount == 0;
            if (isTiny)
            {
                offset = builder.Count;
                builder.WriteByte((byte)((codeSize << 2) | TinyFormat));
            }
            else
            {
                builder.Align(4);

                offset = builder.Count;

                ushort flags = (3 << 12) | FatFormat;
                if (exceptionRegionCount > 0)
                {
                    flags |= MoreSections;
                }

                if ((attributes & MethodBodyAttributes.InitLocals) != 0)
                {
                    flags |= InitLocals;
                }

                builder.WriteUInt16(flags);
                builder.WriteUInt16((ushort)maxStack);
                builder.WriteInt32(codeSize);
                builder.WriteInt32(localVariablesSignature.IsNil ? 0 : MetadataTokens.GetToken(localVariablesSignature));
            }

            return offset;
        }

        public static void WriteExceptionTableHeader(BlobBuilder builder, bool isSmallFormat, int exceptionRegionCount)
        {
            const byte EHTableFlag = 0x01;
            const byte FatFormatFlag = 0x40;

            int dataSize = GetExceptionTableSize(exceptionRegionCount, isSmallFormat);

            builder.Align(4);
            if (isSmallFormat)
            {
                builder.WriteByte(EHTableFlag);
                builder.WriteByte((byte)(dataSize & 0xff));
                builder.WriteInt16(0);
            }
            else
            {
                builder.WriteByte(EHTableFlag | FatFormatFlag);
                builder.WriteByte((byte)(dataSize & 0xff));
                builder.WriteUInt16((ushort)((dataSize >> 8) & 0xffff));
            }
        }

        private static int GetExceptionTableSize(int exceptionRegionCount, bool isSmallFormat)
        {
            const int HeaderSize = 4;

            const int SmallRegionSize =
                sizeof(short) +  // Flags
                sizeof(short) +  // TryOffset
                sizeof(byte) +   // TryLength
                sizeof(short) +  // HandlerOffset
                sizeof(byte) +   // HandleLength
                sizeof(int);     // ClassToken | FilterOffset

            const int FatRegionSize =
                sizeof(int) +    // Flags
                sizeof(int) +    // TryOffset
                sizeof(int) +    // TryLength
                sizeof(int) +    // HandlerOffset
                sizeof(int) +    // HandleLength
                sizeof(int);     // ClassToken | FilterOffset

            return HeaderSize + exceptionRegionCount * (isSmallFormat ? SmallRegionSize : FatRegionSize);
        }

        public static void WriteExceptionRegion(
            BlobBuilder builder, 
            ExceptionRegionKind kind,
            int tryOffset,
            int tryLength,
            int handlerOffset,
            int handlerLength,
            int catchTypeTokenOrFilterOffset,
            bool isSmallFormat)
        {
            if (isSmallFormat)
            {
                builder.WriteUInt16((ushort)kind);
                builder.WriteUInt16((ushort)tryOffset);
                builder.WriteByte((byte)tryLength);
                builder.WriteUInt16((ushort)handlerOffset);
                builder.WriteByte((byte)handlerLength);
            }
            else
            {
                builder.WriteInt32((int)kind);
                builder.WriteInt32(tryOffset);
                builder.WriteInt32(tryLength);
                builder.WriteInt32(handlerOffset);
                builder.WriteInt32(handlerLength);
            }

            builder.WriteInt32(catchTypeTokenOrFilterOffset);
        }

        public static bool IsSmallRegionCount(int exceptionRegionCount)
        {
            return GetExceptionTableSize(exceptionRegionCount, isSmallFormat: true) <= 0xff;
        }

        public static bool IsSmallExceptionRegion(int startOffset, int length)
        {
            return startOffset <= 0xffff && length <= 0xff;
        }
    }
}
