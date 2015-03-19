// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Microsoft.Cci
{
    internal struct BinaryWriter
    {
        internal readonly MemoryStream BaseStream;
        private readonly bool _utf8;

        internal BinaryWriter(MemoryStream output)
        {
            this.BaseStream = output;
            _utf8 = true;
        }

        internal BinaryWriter(MemoryStream output, bool unicode)
        {
            this.BaseStream = output;
            _utf8 = !unicode;
        }

        public bool IsDefault => BaseStream == null;

        internal void Align(uint alignment)
        {
            MemoryStream m = this.BaseStream;
            uint i = m.Position;
            while (i % alignment > 0)
            {
                m.Position = i + 1;
                m.Buffer[i] = 0;
                i++;
            }
        }

        internal void WriteBool(bool value)
        {
            MemoryStream m = this.BaseStream;
            uint i = m.Position;
            m.Position = i + 1;
            m.Buffer[i] = (byte)(value ? 1 : 0);
        }

        internal void WriteByte(byte value)
        {
            MemoryStream m = this.BaseStream;
            uint i = m.Position;
            m.Position = i + 1;
            m.Buffer[i] = value;
        }

        internal void Pad(int byteCount)
        {
            MemoryStream m = this.BaseStream;
            m.Position += (uint)byteCount;
        }

        internal void WriteSbyte(sbyte value)
        {
            MemoryStream m = this.BaseStream;
            uint i = m.Position;
            m.Position = i + 1;

            unchecked
            {
                m.Buffer[i] = (byte)value;
            }
        }

        internal void WriteBytes(byte[] buffer)
        {
            WriteBytes(buffer, 0, buffer.Length);
        }

        internal void WriteBytes(byte[] buffer, int offset, int count)
        {
            if (buffer == null || count == 0)
            {
                return;
            }

            this.BaseStream.Write(buffer, offset, count);
        }

        internal void WriteBytes(ImmutableArray<byte> buffer)
        {
            if (buffer.IsDefault)
            {
                return;
            }

            this.BaseStream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes the byte 'value' , 'count' number of times.
        /// </summary>
        /// <param name="value">value to be written</param>
        /// <param name="count">The number of times the value is going to be written</param>
        internal void WriteBytes(byte value, int count)
        {
            Debug.Assert(count > -1);
            MemoryStream m = this.BaseStream;

            uint i = m.Position;
            uint end = i + (uint)count;
            m.Position = end;

            while (i < end)
            {
                m.Buffer[i++] = value;
            }
        }

        internal void WriteChars(char[] chars)
        {
            if (chars == null)
            {
                return;
            }

            Debug.Assert(!_utf8, "WriteChars has a problem with unmatched surrogate pairs and does not support writing utf8");

            MemoryStream m = this.BaseStream;
            uint i = m.Position;

            m.Position = i + (uint)chars.Length * 2;
            byte[] buffer = m.Buffer;
            for (int j = 0; j < chars.Length; j++, i += 2)
            {
                char ch = chars[j];
                unchecked
                {
                    buffer[i] = (byte)ch;
                    buffer[i + 1] = (byte)(ch >> 8);
                }
            }
        }

        internal void WriteStringUtf16LE(string str)
        {
            if (str == null)
            {
                return;
            }

            MemoryStream m = this.BaseStream;
            uint i = m.Position;

            m.Position = i + (uint)str.Length * 2;
            byte[] buffer = m.Buffer;
            for (int j = 0; j < str.Length; j++, i += 2)
            {
                char ch = str[j];
                unchecked
                {
                    buffer[i] = (byte)ch;
                    buffer[i + 1] = (byte)(ch >> 8);
                }
            }
        }

        internal unsafe void WriteDouble(double value)
        {
            MemoryStream m = this.BaseStream;
            uint i = m.Position;
            m.Position = i + 8;
            fixed (byte* b = m.Buffer)
            {
                *((double*)(b + i)) = value;
            }
        }

        internal void WriteShort(short value)
        {
            MemoryStream m = this.BaseStream;
            uint i = m.Position;
            m.Position = i + 2;
            byte[] buffer = m.Buffer;

            unchecked
            {
                buffer[i] = (byte)value;
                buffer[i + 1] = (byte)(value >> 8);
            }
        }

        internal unsafe void WriteUshort(ushort value)
        {
            MemoryStream m = this.BaseStream;
            uint i = m.Position;
            m.Position = i + 2;
            byte[] buffer = m.Buffer;

            unchecked
            {
                buffer[i] = (byte)value;
                buffer[i + 1] = (byte)(value >> 8);
            }
        }

        internal void WriteInt(int value)
        {
            MemoryStream m = this.BaseStream;
            uint i = m.Position;
            m.Position = i + 4;
            byte[] buffer = m.Buffer;

            unchecked
            {
                buffer[i] = (byte)value;
                buffer[i + 1] = (byte)(value >> 8);
                buffer[i + 2] = (byte)(value >> 16);
                buffer[i + 3] = (byte)(value >> 24);
            }
        }

        internal void WriteUint(uint value)
        {
            MemoryStream m = this.BaseStream;
            uint i = m.Position;
            m.Position = i + 4;
            byte[] buffer = m.Buffer;

            unchecked
            {
                buffer[i] = (byte)value;
                buffer[i + 1] = (byte)(value >> 8);
                buffer[i + 2] = (byte)(value >> 16);
                buffer[i + 3] = (byte)(value >> 24);
            }
        }

        internal void WriteLong(long value)
        {
            MemoryStream m = this.BaseStream;
            uint i = m.Position;
            m.Position = i + 8;
            byte[] buffer = m.Buffer;

            unchecked
            {
                uint lo = (uint)value;
                uint hi = (uint)(value >> 32);
                buffer[i] = (byte)lo;
                buffer[i + 1] = (byte)(lo >> 8);
                buffer[i + 2] = (byte)(lo >> 16);
                buffer[i + 3] = (byte)(lo >> 24);
                buffer[i + 4] = (byte)hi;
                buffer[i + 5] = (byte)(hi >> 8);
                buffer[i + 6] = (byte)(hi >> 16);
                buffer[i + 7] = (byte)(hi >> 24);
            }
        }

        internal unsafe void WriteUlong(ulong value)
        {
            MemoryStream m = this.BaseStream;
            uint i = m.Position;
            m.Position = i + 8;
            byte[] buffer = m.Buffer;

            unchecked
            {
                uint lo = (uint)value;
                uint hi = (uint)(value >> 32);
                buffer[i] = (byte)lo;
                buffer[i + 1] = (byte)(lo >> 8);
                buffer[i + 2] = (byte)(lo >> 16);
                buffer[i + 3] = (byte)(lo >> 24);
                buffer[i + 4] = (byte)hi;
                buffer[i + 5] = (byte)(hi >> 8);
                buffer[i + 6] = (byte)(hi >> 16);
                buffer[i + 7] = (byte)(hi >> 24);
            }
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
                WriteUshort((ushort)reference);
            }
            else
            {
                WriteUint(reference);
            }
        }

        internal unsafe void WriteFloat(float value)
        {
            MemoryStream m = this.BaseStream;
            uint i = m.Position;
            m.Position = i + 4;
            fixed (byte* b = m.Buffer)
            {
                *((float*)(b + i)) = value;
            }
        }

        internal void WriteString(string str)
        {
            this.WriteString(str, false);
        }

        internal void WriteString(string str, bool emitNullTerminator)
        {
            if (str == null)
            {
                this.WriteByte(0xff);
                return;
            }

            int n = str.Length;
            uint size = _utf8 ? GetUTF8ByteCount(str) : (uint)n * 2;
            if (emitNullTerminator)
            {
                // No size recorded for null-terminated strings.
                //this.WriteUint(size);
            }
            else
            {
                this.WriteCompressedUInt(size);
            }

            MemoryStream m = this.BaseStream;
            uint i = m.Position;
            if (_utf8)
            {
                m.Position = i + (uint)n;
                byte[] buffer = m.Buffer;
                for (int j = 0; j < n; j++)
                {
                    char ch = str[j];
                    if (ch >= 0x80)
                    {
                        goto writeUTF8;
                    }

                    buffer[i++] = unchecked((byte)ch);
                }

                if (emitNullTerminator)
                {
                    m.Position = i + 1;
                    buffer = m.Buffer;
                    buffer[i] = 0;
                }

                return;
            writeUTF8:
                for (int j = n - (int)(m.Position - i); j < n; j++)
                {
                    if (IsHighSurrogateCharFollowedByLowSurrogateChar(str, j))
                    {
                        // High surrogate character followed by a low surrogate character encoded specially.
                        int highSurrogate = str[j++];
                        int lowSurrogate = str[j];
                        int codepoint = (((highSurrogate - 0xd800) << 10) + lowSurrogate - 0xdc00) + 0x10000;
                        m.Position = i + 4;
                        buffer = m.Buffer;
                        buffer[i++] = (byte)(((codepoint >> 18) & 0x7) | 0xF0);
                        buffer[i++] = (byte)(((codepoint >> 12) & 0x3F) | 0x80);
                        buffer[i++] = (byte)(((codepoint >> 6) & 0x3F) | 0x80);
                        buffer[i++] = (byte)((codepoint & 0x3F) | 0x80);
                    }
                    else
                    {
                        char ch = str[j];
                        if (ch < 0x80)
                        {
                            m.Position = i + 1;
                            buffer = m.Buffer;
                            buffer[i++] = (byte)ch;
                        }
                        else if (ch < 0x800)
                        {
                            m.Position = i + 2;
                            buffer = m.Buffer;
                            buffer[i++] = (byte)(((ch >> 6) & 0x1F) | 0xC0);
                            buffer[i++] = (byte)((ch & 0x3F) | 0x80);
                        }
                        else
                        {
                            m.Position = i + 3;
                            buffer = m.Buffer;
                            buffer[i++] = (byte)(((ch >> 12) & 0xF) | 0xE0);
                            buffer[i++] = (byte)(((ch >> 6) & 0x3F) | 0x80);
                            buffer[i++] = (byte)((ch & 0x3F) | 0x80);
                        }
                    }
                }

                if (emitNullTerminator)
                {
                    m.Position = i + 1;
                    buffer = m.Buffer;
                    buffer[i] = 0;
                }
            }
            else
            {
                m.Position = i + (uint)n * 2;
                byte[] buffer = m.Buffer;
                for (int j = 0; j < n; j++)
                {
                    char ch = str[j];
                    buffer[i++] = (byte)ch;
                    buffer[i++] = (byte)(ch >> 8);
                }

                if (emitNullTerminator)
                {
                    m.Position = i + 2;
                    buffer = m.Buffer;
                    buffer[i++] = 0;
                    buffer[i] = 0;
                }
            }
        }

        internal void WriteString(string str, Encoding encoding)
        {
            MemoryStream m = this.BaseStream;
            uint i = m.Position;
            m.Position = i + (uint)encoding.GetByteCount(str);
            encoding.GetBytes(str, 0, str.Length, m.Buffer, (int)i);
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
                    this.WriteByte((byte)n);
                }
                else if ((value & ~b13) == (signMask & ~b13))
                {
                    int n = ((value & b13) << 1) | (signMask & 1);
                    this.WriteByte((byte)(0x80 | (n >> 8)));
                    this.WriteByte((byte)n);
                }
                else
                {
                    Debug.Assert((value & ~b28) == (signMask & ~b28));

                    int n = ((value & b28) << 1) | (signMask & 1);
                    this.WriteByte((byte)(0xc0 | (n >> 24)));
                    this.WriteByte((byte)(n >> 16));
                    this.WriteByte((byte)(n >> 8));
                    this.WriteByte((byte)n);
                }
            }
        }

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
        internal void WriteCompressedUInt(uint val)
        {
            unchecked
            {
                if (val <= 0x7f)
                {
                    this.WriteByte((byte)val);
                }
                else if (val <= 0x3fff)
                {
                    this.WriteByte((byte)(0x80 | (val >> 8)));
                    this.WriteByte((byte)val);
                }
                else
                {
                    Debug.Assert(val <= 0x1fffffff);

                    this.WriteByte((byte)(0xc0 | (val >> 24)));
                    this.WriteByte((byte)(val >> 16));
                    this.WriteByte((byte)(val >> 8));
                    this.WriteByte((byte)val);
                }
            }
        }

        internal static uint GetUTF8ByteCount(string str)
        {
            uint count = 0;
            for (int i = 0, n = str.Length; i < n; i++)
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

        public void WriteConstantValueBlob(object value)
        {
            if (value == null)
            {
                // The encoding of Type for the nullref value for FieldInit is ELEMENT_TYPE_CLASS with a Value of a 32-bit.
                WriteUint(0);
                return;
            }

            var type = value.GetType();
            if (type.GetTypeInfo().IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            if (type == typeof(bool))
            {
                WriteBool((bool)value);
            }
            else if (type == typeof(int))
            {
                WriteInt((int)value);
            }
            else if (type == typeof(string))
            {
                WriteString((string)value);
            }
            else if (type == typeof(byte))
            {
                WriteByte((byte)value);
            }
            else if (type == typeof(char))
            {
                WriteUshort((char)value);
            }
            else if (type == typeof(double))
            {
                WriteDouble((double)value);
            }
            else if (type == typeof(short))
            {
                WriteShort((short)value);
            }
            else if (type == typeof(long))
            {
                WriteLong((long)value);
            }
            else if (type == typeof(sbyte))
            {
                WriteSbyte((sbyte)value);
            }
            else if (type == typeof(float))
            {
                WriteFloat((float)value);
            }
            else if (type == typeof(ushort))
            {
                WriteUshort((ushort)value);
            }
            else if (type == typeof(uint))
            {
                WriteUint((uint)value);
            }
            else if (type == typeof(ulong))
            {
                WriteUlong((ulong)value);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
