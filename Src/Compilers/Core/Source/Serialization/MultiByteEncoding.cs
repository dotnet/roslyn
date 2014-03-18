// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;

namespace Roslyn.Utilities
{
    internal class MultiByteEncoding : Encoding
    {
        internal static readonly MultiByteEncoding Instance = new MultiByteEncoding();

        private const byte DoubleByteEncoded = (byte)0xFF;

        public override int GetByteCount(char[] chars, int index, int count)
        {
            int byteCount = 0;

            for (int i = 0; i < count; i++)
            {
                ushort c = (ushort)chars[index + i];

                if (c >= DoubleByteEncoded)
                {
                    // marker + 2 byte value
                    byteCount += 3;
                }
                else
                {
                    // single byte value
                    byteCount += 1;
                }
            }

            return byteCount;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            int bi = byteIndex;
            for (int i = 0; i < charCount; i++)
            {
                ushort c = (ushort)chars[charIndex + i];

                if (c >= DoubleByteEncoded)
                {
                    bytes[bi] = DoubleByteEncoded;
                    bytes[bi + 1] = (byte)(c & 0xFF);
                    bytes[bi + 2] = (byte)((c >> 8) & 0xFF);
                    bi += 3;
                }
                else
                {
                    bytes[bi] = (byte)c;
                    bi++;
                }
            }

            return bi - byteIndex;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            throw new NotImplementedException();
        }

        public override int GetMaxByteCount(int charCount)
        {
            return charCount * 3;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount;
        }

        public override Decoder GetDecoder()
        {
            return new MultiByteDecoder();
        }

        private class MultiByteDecoder : Decoder
        {
            // The Decoder must figure out how to deal with broken character encoding fragments across multiple calls to GetChars.

            // since max # of bytes for a char is 3, at most 2 can be left over from a previous call to GetChars
            private int leftOverByteCount = 0;
            private byte[] leftOverBytes = new byte[2];

            // The total logical length of the buffer (including any left-over bytes from the last buffer)
            private int GetTotalBufferLength(int specifiedLength)
            {
                return this.leftOverByteCount + specifiedLength;
            }

            // Get the byte at the logical buffer position.  The head of the logical buffer includes any left over bytes
            // from the previous buffer.
            private byte GetBufferByte(byte[] buffer, int index)
            {
                if (index < this.leftOverByteCount)
                {
                    return this.leftOverBytes[index];
                }
                else
                {
                    return buffer[index - this.leftOverByteCount];
                }
            }

            public override void Reset()
            {
                this.leftOverByteCount = 0;
            }

            private void RecordLeftOverBytes(byte[] bytes, int index, int count)
            {
                if (count > 0)
                {
                    leftOverBytes[0] = GetBufferByte(bytes, index);

                    if (count > 1)
                    {
                        leftOverBytes[1] = GetBufferByte(bytes, index + 1);
                    }
                }

                leftOverByteCount = count;
            }

            public override int GetCharCount(byte[] bytes, int index, int count)
            {
                int charCount = 0;

                for (int i = 0; i < count; i++)
                {
                    byte b = GetBufferByte(bytes, index + 1);
                    if (b == DoubleByteEncoded)
                    {
                        i += 2; // skip double byte encoded character
                    }

                    // don't count character if we overshot the buffer
                    if (i <= count)
                    {
                        charCount++;
                    }
                }

                return charCount;
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            {
                int ci = charIndex;
                int totalBufferLength = GetTotalBufferLength(byteCount);

                for (int i = 0; i < totalBufferLength; i++)
                {
                    byte b = GetBufferByte(bytes, byteIndex + i);

                    if (b == DoubleByteEncoded)
                    {
                        // only decode if we all have the bytes to do it
                        int availableBytes = totalBufferLength - i;
                        if (availableBytes < 3)
                        {
                            RecordLeftOverBytes(bytes, byteIndex + i, availableBytes);
                            return ci - charIndex;
                        }

                        byte lo = GetBufferByte(bytes, byteIndex + i + 1);
                        byte hi = GetBufferByte(bytes, byteIndex + i + 2);
                        ushort c = (ushort)((hi << 8) | lo);
                        chars[ci] = (char)c;
                        i += 2;
                    }
                    else
                    {
                        chars[ci] = (char)b;
                    }

                    ci++;
                }

                leftOverByteCount = 0;
                return ci - charIndex;
            }
        }
    }
}