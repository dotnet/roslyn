// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;
using static System.Buffers.Binary.BinaryPrimitives;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A Span-compatible version of <see cref="System.IO.BinaryReader"/>.
    /// </summary>
    internal ref struct LittleEndianReader
    {
        private ReadOnlySpan<byte> _span;

        public LittleEndianReader(ReadOnlySpan<byte> span)
        {
            _span = span;
        }

        internal uint ReadUInt32()
        {
            var result = ReadUInt32LittleEndian(_span);
            _span = _span.Slice(sizeof(uint));
            return result;
        }

        internal byte ReadByte()
        {
            var result = _span[0];
            _span = _span.Slice(sizeof(byte));
            return result;
        }

        internal ushort ReadUInt16()
        {
            var result = ReadUInt16LittleEndian(_span);
            _span = _span.Slice(sizeof(ushort));
            return result;
        }

        internal ReadOnlySpan<byte> ReadBytes(int byteCount)
        {
            var result = _span.Slice(0, byteCount);
            _span = _span.Slice(byteCount);
            return result;
        }

        internal int ReadInt32()
        {
            var result = ReadInt32LittleEndian(_span);
            _span = _span.Slice(sizeof(int));
            return result;
        }

        internal byte[] ReadReversed(int byteCount)
        {
            var result = ReadBytes(byteCount).ToArray();
            result.ReverseContents();
            return result;
        }
    }
}
