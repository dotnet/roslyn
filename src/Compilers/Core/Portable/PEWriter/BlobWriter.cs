// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal unsafe sealed class BlobWriter
    {
        private byte[] _buffer;
        private int _length;
        private int _position;

        internal BlobWriter(int initialSize = 64)
        {
            // the writer assumes little-endian architecture:
            Debug.Assert(BitConverter.IsLittleEndian);

            _buffer = new byte[initialSize];
        }

        internal BlobWriter(ObjectPool<BlobWriter> pool)
            : this()
        {
            _pool = pool;
        }

        public byte[] Buffer => _buffer;
        public int Length => _length;

        private void Resize(int capacity)
        {
            Array.Resize(ref _buffer, Math.Max(Math.Min(_length, int.MaxValue / 2) * 2, capacity));
        }

        internal int Position => _position;

        internal void SetPosition(int newPosition)
        {
            if (newPosition > _length)
            {
                if (newPosition > _buffer.Length)
                {
                    Resize(newPosition);
                }

                _length = newPosition;
            }

            _position = newPosition;
        }

        private int Advance(int value)
        {
            int position = _position;
            SetPosition(position + value);
            return position;
        }

        internal byte[] ToArray()
        {
            return ToArray(0, _length);
        }

        internal byte[] ToArray(int start, int length)
        {
            if (_length == 0)
            {
                return SpecializedCollections.EmptyArray<byte>();
            }

            byte[] result = new byte[length];
            Array.Copy(_buffer, start, result, 0, result.Length);
            return result;
        }

        internal ImmutableArray<byte> ToImmutableArray()
        {
            return ToImmutableArray(0, _length);
        }

        internal ImmutableArray<byte> ToImmutableArray(int start, int length)
        {
            return ImmutableArray.Create(_buffer, start, length);
        }

        internal void WriteBytes(byte value, int count)
        {
            int start = Advance(count);
            fixed (byte* buffer = _buffer)
            {
                byte* ptr = buffer + start;
                for (int i = 0; i < count; i++)
                {
                    ptr[i] = value;
                }
            }
        }

        internal void WriteBytes(byte[] buffer)
        {
            WriteBytes(buffer, 0, buffer.Length);
        }

        internal void WriteBytes(byte[] buffer, int index, int length)
        {
            int start = Advance(length);
            System.Buffer.BlockCopy(buffer, index, _buffer, start, length);
        }

        internal void WriteBytes(ImmutableArray<byte> buffer)
        {
            WriteBytes(buffer, 0, buffer.Length);
        }

        internal void WriteBytes(ImmutableArray<byte> buffer, int index, int length)
        {
            int start = Advance(length);
            buffer.CopyTo(index, _buffer, start, length);
        }

        internal void Write(Stream stream, int length)
        {
            int start = Advance(length);
            int bytesRead = stream.Read(_buffer, start, length);
            _position = start + bytesRead;
        }

        internal void PadTo(int position)
        {
            WriteBytes(0, position - _position);
        }

        internal void Align(int alignment)
        {
            int position = _position;
            WriteBytes(0, BitArithmeticUtilities.Align(position, alignment) - position);
        }

        internal void WriteBoolean(bool value)
        {
            WriteByte((byte)(value ? 1 : 0));
        }

        internal void WriteByte(byte value)
        {
            int start = Advance(sizeof(byte));
            _buffer[start] = value;
        }

        internal void WriteSByte(sbyte value)
        {
            WriteByte(unchecked((byte)value));
        }

        internal void WriteDouble(double value)
        {
            int start = Advance(sizeof(double));
            fixed (byte* ptr = _buffer)
            {
                *((double*)(ptr + start)) = value;
            }
        }

        internal void WriteSingle(float value)
        {
            int start = Advance(sizeof(float));
            fixed (byte* ptr = _buffer)
            {
                *((float*)(ptr + start)) = value;
            }
        }

        internal void WriteInt16(short value)
        {
            WriteUInt16(unchecked((ushort)value));
        }

        internal void WriteUInt16(ushort value)
        {
            int start = Advance(sizeof(ushort));
            fixed (byte* ptr = _buffer)
            {
                *((ushort*)(ptr + start)) = value;
            }
        }

        private void WriteUInt16BE(ushort value)
        {
            int start = Advance(sizeof(ushort));

            fixed (byte* ptr = _buffer)
            {
                unchecked
                {
                    ptr[start] = (byte)(value >> 8);
                    ptr[start + 1] = (byte)value;
                }
            }
        }

        private void WriteUInt32BE(uint value)
        {
            int start = Advance(sizeof(uint));

            fixed (byte* ptr = _buffer)
            {
                unchecked
                {
                    ptr[start] = (byte)(value >> 24);
                    ptr[start + 1] = (byte)(value >> 16);
                    ptr[start + 2] = (byte)(value >> 8);
                    ptr[start + 3] = (byte)value;
                }
            }
        }

        internal void WriteInt32(int value)
        {
            WriteUInt32(unchecked((uint)value));
        }

        internal void WriteUInt32(uint value)
        {
            int start = Advance(sizeof(uint));
            fixed (byte* ptr = _buffer)
            {
                *((uint*)(ptr + start)) = value;
            }
        }

        internal void WriteInt64(long value)
        {
            WriteUInt64(unchecked((ulong)value));
        }

        internal void WriteUInt64(ulong value)
        {
            int start = Advance(sizeof(ulong));
            fixed (byte* ptr = _buffer)
            {
                *((ulong*)(ptr + start)) = value;
            }
        }

        internal void WriteDecimal(decimal value)
        {
            bool isNegative;
            byte scale;
            uint low, mid, high;
            value.GetBits(out isNegative, out scale, out low, out mid, out high);

            WriteByte((byte)(scale | (isNegative ? 0x80 : 0x00)));
            WriteUInt32(low);
            WriteUInt32(mid);
            WriteUInt32(high);
        }

        internal void WriteDateTime(DateTime value)
        {
            WriteInt64(value.Ticks);
        }

        /// <summary>
        /// Writes a reference to a heap (heap index) or a table (row id).
        /// </summary>
        /// <remarks>
        /// References may be small (2B) or large (4B).
        /// </remarks>
        internal void WriteReference(uint reference, int size)
        {
            Debug.Assert(size == 2 || size == 4);

            if (size == 2)
            {
                Debug.Assert((ushort)reference == reference);
                WriteUInt16((ushort)reference);
            }
            else
            {
                WriteUInt32(reference);
            }
        }

        /// <summary>
        /// Writes UTF16 (little-endian) encoded string at the current position.
        /// </summary>
        public void WriteUTF16(char[] value)
        {
            if (value == null)
            {
                return;
            }

            int size = value.Length * sizeof(char);
            int start = Advance(size);
            System.Buffer.BlockCopy(value, 0, _buffer, start, size);
        }

        /// <summary>
        /// Writes UTF16 (little-endian) encoded string at the current position.
        /// </summary>
        public void WriteUTF16(string value)
        {
            if (value == null)
            {
                return;
            }

            int size = value.Length * sizeof(char);
            int start = Advance(size);

            fixed (char* ptr = value)
            {
                Marshal.Copy((IntPtr)ptr, _buffer, start, size);
            }
        }

        /// <summary>
        /// Writes string in SerString format (see ECMA-335-II 23.3 Custom attributes): 
        /// The string is UTF8 encoded and prefixed by the its size in bytes. 
        /// Null string is represented as a single byte 0xFF.
        /// </summary>
        public void WriteSerializedString(string str)
        {
            if (str == null)
            {
                WriteByte(0xff);
                return;
            }

            int byteCount = GetUTF8ByteCount(str);
            WriteCompressedInteger((uint)byteCount);
            WriteUTF8(str, byteCount);
        }

        internal void WriteString(string str, Encoding encoding)
        {
            int start = Advance(encoding.GetByteCount(str));
            encoding.GetBytes(str, 0, str.Length, _buffer, start);
        }

        /// <summary>
        /// Writes UTF8 encoded string at the current position.
        /// </summary>
        public void WriteUTF8(string str)
        {
            WriteUTF8(str, GetUTF8ByteCount(str));
        }

        // TODO: Use UTF8Encoding https://github.com/dotnet/corefx/issues/2217
        public void WriteUTF8(string str, int byteCount)
        {
            Debug.Assert(byteCount >= str.Length);

            int start = Advance(byteCount);
            fixed (byte* buffer = _buffer)
            {
                byte* ptr = buffer + start;

                if (byteCount == str.Length)
                {
                    for (int j = 0; j < str.Length; j++)
                    {
                        Debug.Assert(str[j] <= 0x7f);
                        *ptr++ = unchecked((byte)str[j]);
                    }
                }
                else
                {
                    for (int j = 0; j < str.Length; j++)
                    {
                        if (IsHighSurrogateCharFollowedByLowSurrogateChar(str, j))
                        {
                            // High surrogate character followed by a low surrogate character encoded specially.
                            int highSurrogate = str[j++];
                            int lowSurrogate = str[j];
                            int codepoint = (((highSurrogate - 0xd800) << 10) + lowSurrogate - 0xdc00) + 0x10000;
                            ptr[0] = (byte)(((codepoint >> 18) & 0x7) | 0xF0);
                            ptr[1] = (byte)(((codepoint >> 12) & 0x3F) | 0x80);
                            ptr[2] = (byte)(((codepoint >> 6) & 0x3F) | 0x80);
                            ptr[3] = (byte)((codepoint & 0x3F) | 0x80);
                            ptr += 4;
                        }
                        else
                        {
                            char ch = str[j];
                            if (ch < 0x80)
                            {
                                *ptr++ = (byte)ch;
                            }
                            else if (ch < 0x800)
                            {
                                ptr[0] = (byte)(((ch >> 6) & 0x1F) | 0xC0);
                                ptr[1] = (byte)((ch & 0x3F) | 0x80);
                                ptr += 2;
                            }
                            else
                            {
                                ptr[0] = (byte)(((ch >> 12) & 0xF) | 0xE0);
                                ptr[1] = (byte)(((ch >> 6) & 0x3F) | 0x80);
                                ptr[2] = (byte)((ch & 0x3F) | 0x80);
                                ptr += 3;
                            }
                        }
                    }
                }
            }
        }

        internal static int GetUTF8ByteCount(string str)
        {
            int count = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (IsHighSurrogateCharFollowedByLowSurrogateChar(str, i))
                {
                    // High surrogate character followed by a Low surrogate character encoded specially.
                    count += 4;
                    i++;
                }
                else
                {
                    char ch = str[i];
                    if (ch < 0x80)
                    {
                        count += 1;
                    }
                    else if (ch < 0x800)
                    {
                        count += 2;
                    }
                    else
                    {
                        count += 3;
                    }
                }
            }

            return count;
        }

        private static bool IsHighSurrogateCharFollowedByLowSurrogateChar(string str, int index)
        {
            if (!IsHighSurrogateChar(str[index++]))
            {
                return false;
            }

            return index < str.Length && IsLowSurrogateChar(str[index]);
        }

        private static bool IsHighSurrogateChar(char ch)
        {
            return 0xD800 <= ch && ch <= 0xDBFF;
        }

        private static bool IsLowSurrogateChar(char ch)
        {
            return 0xDC00 <= ch && ch <= 0xDFFF;
        }

        /// <summary>
        /// Implements compressed signed integer encoding as defined by ECMA-335-II chapter 23.2: Blobs and signatures.
        /// </summary>
        /// <remarks>
        /// If the value lies between -64 (0xFFFFFFC0) and 63 (0x3F), inclusive, encode as a one-byte integer: 
        /// bit 7 clear, value bits 5 through 0 held in bits 6 through 1, sign bit (value bit 31) in bit 0.
        /// 
        /// If the value lies between -8192 (0xFFFFE000) and 8191 (0x1FFF), inclusive, encode as a two-byte integer: 
        /// 15 set, bit 14 clear, value bits 12 through 0 held in bits 13 through 1, sign bit(value bit 31) in bit 0.
        /// 
        /// If the value lies between -268435456 (0xF000000) and 268435455 (0x0FFFFFFF), inclusive, encode as a four-byte integer: 
        /// 31 set, 30 set, bit 29 clear, value bits 27 through 0 held in bits 28 through 1, sign bit(value bit 31) in bit 0.
        /// </remarks>
        internal void WriteCompressedSignedInteger(int value)
        {
            unchecked
            {
                const int b6 = (1 << 6) - 1;
                const int b13 = (1 << 13) - 1;
                const int b28 = (1 << 28) - 1;

                // 0xffffffff for negative value
                // 0x00000000 for non-negative
                int signMask = value >> 31;

                if ((value & ~b6) == (signMask & ~b6))
                {
                    int n = ((value & b6) << 1) | (signMask & 1);
                    WriteByte((byte)n);
                }
                else if ((value & ~b13) == (signMask & ~b13))
                {
                    int n = ((value & b13) << 1) | (signMask & 1);
                    WriteUInt16BE((ushort)(0x8000 | n));
                }
                else
                {
                    Debug.Assert((value & ~b28) == (signMask & ~b28));

                    int n = ((value & b28) << 1) | (signMask & 1);
                    WriteUInt32BE(0xc0000000 | (uint)n);
                }
            }
        }

        private const int SingleByteCompressedIntegerMaxValue = 0x7f;
        private const int TwoByteCompressedIntegerMaxValue = 0x3fff;
        private const int MaxCompressedIntegerValue = 0x1fffffff;

        /// <summary>
        /// Implements compressed unsigned integer encoding as defined by ECMA-335-II chapter 23.2: Blobs and signatures.
        /// </summary>
        /// <remarks>
        /// If the value lies between 0 (0x00) and 127 (0x7F), inclusive, 
        /// encode as a one-byte integer (bit 7 is clear, value held in bits 6 through 0).
        /// 
        /// If the value lies between 28 (0x80) and 214 – 1 (0x3FFF), inclusive, 
        /// encode as a 2-byte integer with bit 15 set, bit 14 clear(value held in bits 13 through 0).
        /// 
        /// Otherwise, encode as a 4-byte integer, with bit 31 set, bit 30 set, bit 29 clear (value held in bits 28 through 0).
        /// </remarks>
        internal void WriteCompressedInteger(uint val)
        {
            Debug.Assert(val <= MaxCompressedIntegerValue);

            unchecked
            {
                if (val <= SingleByteCompressedIntegerMaxValue)
                {
                    WriteByte((byte)val);
                }
                else if (val <= TwoByteCompressedIntegerMaxValue)
                {
                    WriteUInt16BE((ushort)(0x8000 | val));
                }
                else
                {
                    
                    WriteUInt32BE(0xc0000000 | val);
                }
            }
        }

        internal static int GetCompressedIntegerSize(int value)
        {
            Debug.Assert(value <= MaxCompressedIntegerValue);

            if (value <= SingleByteCompressedIntegerMaxValue)
            {
                return 1;
            }

            if (value <= TwoByteCompressedIntegerMaxValue)
            {
                return 2;
            }

            return 4;
        }

        /// <summary>
        /// Writes a constant value (see ECMA-335 Partition II section 22.9) at the current position.
        /// </summary>
        public void WriteConstant(object value)
        {
            if (value == null)
            {
                // The encoding of Type for the nullref value for FieldInit is ELEMENT_TYPE_CLASS with a Value of a 32-bit.
                WriteUInt32(0);
                return;
            }

            var type = value.GetType();
            if (type.GetTypeInfo().IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            if (type == typeof(bool))
            {
                WriteBoolean((bool)value);
            }
            else if (type == typeof(int))
            {
                WriteInt32((int)value);
            }
            else if (type == typeof(string))
            {
                WriteUTF16((string)value);
            }
            else if (type == typeof(byte))
            {
                WriteByte((byte)value);
            }
            else if (type == typeof(char))
            {
                WriteUInt16((char)value);
            }
            else if (type == typeof(double))
            {
                WriteDouble((double)value);
            }
            else if (type == typeof(short))
            {
                WriteInt16((short)value);
            }
            else if (type == typeof(long))
            {
                WriteInt64((long)value);
            }
            else if (type == typeof(sbyte))
            {
                WriteSByte((sbyte)value);
            }
            else if (type == typeof(float))
            {
                WriteSingle((float)value);
            }
            else if (type == typeof(ushort))
            {
                WriteUInt16((ushort)value);
            }
            else if (type == typeof(uint))
            {
                WriteUInt32((uint)value);
            }
            else if (type == typeof(ulong))
            {
                WriteUInt64((ulong)value);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal void WriteTo(BlobWriter stream)
        {
            stream.WriteBytes(_buffer, 0, _length);
        }

        internal void WriteTo(Stream stream)
        {
            stream.Write(_buffer, 0, _length);
        }

        // Reset to zero-length, but don't reduce or free the array.
        internal void Clear()
        {
            _position = 0;
            _length = 0;
        }

        #region Poolable

        private readonly ObjectPool<BlobWriter> _pool;

        //
        // To implement Poolable, you need two things:
        // 1) Expose Freeing primitive. 
        public void Free()
        {
            // Note that poolables are not finalizable. If one gets collected - no big deal.
            Clear();
            if (_pool != null)
            {
                if (_buffer.Length < 1024)
                {
                    _pool.Free(this);
                }
                else
                {
                    _pool.ForgetTrackedObject(this);
                }
            }
        }

        //2) Expose  the way to get an instance.
        private static readonly ObjectPool<BlobWriter> s_poolInstance = CreatePool();

        public static BlobWriter GetInstance()
        {
            var stream = s_poolInstance.Allocate();
            return stream;
        }

        public static ObjectPool<BlobWriter> CreatePool()
        {
            return CreatePool(32);
        }

        public static ObjectPool<BlobWriter> CreatePool(int size)
        {
            ObjectPool<BlobWriter> pool = null;
            pool = new ObjectPool<BlobWriter>(() => new BlobWriter(pool), size);
            return pool;
        }

        #endregion
    }
}
