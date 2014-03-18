// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.Cci
{
    internal struct BinaryWriter
    {
        internal readonly MemoryStream BaseStream;
        private readonly bool utf8;

        internal BinaryWriter(MemoryStream output)
        {
            this.BaseStream = output;
            this.utf8 = true;
        }

        internal BinaryWriter(MemoryStream output, bool unicode)
        {
            this.BaseStream = output;
            this.utf8 = !unicode;
        }

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
            if (buffer == null)
            {
                return;
            }

            this.BaseStream.Write(buffer, 0, (uint)buffer.Length);
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

            Debug.Assert(!this.utf8, "WriteChars has a problem with unmatches surrogate pairs and does not support writing utf8");

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
            uint size = this.utf8 ? GetUTF8ByteCount(str) : (uint)n * 2;
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
            if (this.utf8)
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

        internal void WriteCompressedInt(int val)
        {
            unchecked
            {
                if (val >= 0)
                {
                    val = val << 1;
                    this.WriteCompressedUInt((uint)val);
                }
                else
                {
                    if (val > -0x40)
                    {
                        val = 0x40 + val;
                        val = (val << 1) | 1;
                        this.WriteByte((byte)val);
                    }
                    else if (val >= -0x2000)
                    {
                        val = 0x2000 - val;
                        val = (val << 1) | 1;
                        this.WriteByte((byte)((val >> 8) | 0x80));
                        this.WriteByte((byte)(val & 0xff));
                    }
                    else if (val >= -0x20000000)
                    {
                        val = 0x20000000 - val;
                        val = (val << 1) | 1;
                        this.WriteByte((byte)((val >> 24) | 0xc0));
                        this.WriteByte((byte)((val & 0xff0000) >> 16));
                        this.WriteByte((byte)((val & 0xff00) >> 8));
                        this.WriteByte((byte)(val & 0xff));
                    }
                    else
                    {
                        // ^ assume false;
                    }
                }
            }
        }

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
                    this.WriteByte((byte)((val >> 8) | 0x80));
                    this.WriteByte((byte)(val & 0xff));
                }
                else if (val <= 0x1fffffff)
                {
                    this.WriteByte((byte)((val >> 24) | 0xc0));
                    this.WriteByte((byte)((val & 0xff0000) >> 16));
                    this.WriteByte((byte)((val & 0xff00) >> 8));
                    this.WriteByte((byte)(val & 0xff));
                }
                else
                {
                    // ^ assume false;
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
    }
}