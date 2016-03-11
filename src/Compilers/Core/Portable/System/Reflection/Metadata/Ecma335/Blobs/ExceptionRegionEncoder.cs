// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

#if SRM
namespace System.Reflection.Metadata.Ecma335.Blobs
#else
namespace Roslyn.Reflection.Metadata.Ecma335.Blobs
#endif
{
#if SRM
    public
#endif
    struct ExceptionRegionEncoder
    {
        private readonly int _exceptionRegionCount;
        private readonly bool _isSmallFormat;
        public BlobBuilder Builder { get; }

        internal ExceptionRegionEncoder(BlobBuilder builder, int exceptionRegionCount, bool hasLargeRegions)
        {
            Builder = builder;
            _exceptionRegionCount = exceptionRegionCount;
            _isSmallFormat = !hasLargeRegions && IsSmallRegionCount(exceptionRegionCount);
        }

        public void StartRegions()
        {
            if (_exceptionRegionCount == 0)
            {
                return;
            }

            const byte EHTableFlag = 0x01;
            const byte FatFormatFlag = 0x40;

            int dataSize = GetExceptionTableSize(_exceptionRegionCount, _isSmallFormat);

            Builder.Align(4);
            if (_isSmallFormat)
            {
                Builder.WriteByte(EHTableFlag);
                Builder.WriteByte((byte)(dataSize & 0xff));
                Builder.WriteInt16(0);
            }
            else
            {
                Builder.WriteByte(EHTableFlag | FatFormatFlag);
                Builder.WriteByte((byte)(dataSize & 0xff));
                Builder.WriteUInt16((ushort)((dataSize >> 8) & 0xffff));
            }
        }

        public static bool IsSmallRegionCount(int exceptionRegionCount)
        {
            return GetExceptionTableSize(exceptionRegionCount, isSmallFormat: true) <= 0xff;
        }

        public static bool IsSmallExceptionRegion(int startOffset, int length)
        {
            return startOffset <= 0xffff && length <= 0xff;
        }

        internal static int GetExceptionTableSize(int exceptionRegionCount, bool isSmallFormat)
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

        public void AddFinally(int tryOffset, int tryLength, int handlerOffset, int handlerLength)
        {
            AddRegion(ExceptionRegionKind.Finally, tryOffset, tryLength, handlerOffset, handlerLength, default(EntityHandle), 0);
        }

        public void AddFault(int tryOffset, int tryLength, int handlerOffset, int handlerLength)
        {
            AddRegion(ExceptionRegionKind.Fault, tryOffset, tryLength, handlerOffset, handlerLength, default(EntityHandle), 0);
        }

        public void AddCatch(int tryOffset, int tryLength, int handlerOffset, int handlerLength, EntityHandle catchType)
        {
            AddRegion(ExceptionRegionKind.Catch, tryOffset, tryLength, handlerOffset, handlerLength, catchType, 0);
        }

        public void AddFilter(int tryOffset, int tryLength, int handlerOffset, int handlerLength, int filterOffset)
        {
            AddRegion(ExceptionRegionKind.Filter, tryOffset, tryLength, handlerOffset, handlerLength, default(EntityHandle), filterOffset);
        }

        public void AddRegion(
            ExceptionRegionKind kind,
            int tryOffset,
            int tryLength,
            int handlerOffset,
            int handlerLength,
            EntityHandle catchType,
            int filterOffset)
        {
            if (_isSmallFormat)
            {
                Builder.WriteUInt16((ushort)kind);
                Builder.WriteUInt16((ushort)tryOffset);
                Builder.WriteByte((byte)tryLength);
                Builder.WriteUInt16((ushort)handlerOffset);
                Builder.WriteByte((byte)handlerLength);
            }
            else
            {
                Builder.WriteInt32((int)kind);
                Builder.WriteInt32(tryOffset);
                Builder.WriteInt32(tryLength);
                Builder.WriteInt32(handlerOffset);
                Builder.WriteInt32(handlerLength);
            }

            switch (kind)
            {
                case ExceptionRegionKind.Catch:
                    Builder.WriteInt32(MetadataTokens.GetToken(catchType));
                    break;

                case ExceptionRegionKind.Filter:
                    Builder.WriteInt32(filterOffset);
                    break;

                default:
                    Builder.WriteInt32(0);
                    break;
            }
        }

        public void EndRegions()
        {
        }
    }
}
