// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

#if SRM
using System.Reflection.Internal;
#else
using Roslyn.Utilities;
#endif

#if SRM
namespace System.Reflection
#else
namespace Roslyn.Reflection
#endif
{
    internal unsafe static class BlobUtilities
    {
        public static void WriteBytes(this byte[] buffer, int start, byte value, int count)
        {
            Debug.Assert(buffer.Length > 0);

            fixed (byte* bufferPtr = &buffer[0])
            {
                byte* startPtr = bufferPtr + start;
                for (int i = 0; i < count; i++)
                {
                    startPtr[i] = value;
                }
            }
        }

        public static void WriteDouble(this byte[] buffer, int start, double value)
        {
            fixed (byte* ptr = &buffer[start])
            {
                *(double*)ptr = value;
            }
        }

        public static void WriteSingle(this byte[] buffer, int start, float value)
        {
            fixed (byte* ptr = &buffer[start])
            {
                *(float*)ptr = value;
            }
        }

        public static void WriteByte(this byte[] buffer, int start, byte value)
        {
            // Perf: The compiler emits a check when pinning the buffer. It's thus not worth doing so.
            buffer[start] = value;
        }

        public static void WriteUInt16(this byte[] buffer, int start, ushort value)
        {
            fixed (byte* ptr = &buffer[start])
            {
                *(ushort*)ptr = value;
            }
        }

        public static void WriteUInt16BE(this byte[] buffer, int start, ushort value)
        {
            fixed (byte* ptr = &buffer[start])
            {
                unchecked
                {
                    ptr[0] = (byte)(value >> 8);
                    ptr[1] = (byte)value;
                }
            }
        }

        public static void WriteUInt32BE(this byte[] buffer, int start, uint value)
        {
            fixed (byte* ptr = &buffer[start])
            {
                unchecked
                {
                    ptr[0] = (byte)(value >> 24);
                    ptr[1] = (byte)(value >> 16);
                    ptr[2] = (byte)(value >> 8);
                    ptr[3] = (byte)value;
                }
            }
        }

        public static void WriteUInt32(this byte[] buffer, int start, uint value)
        {
            fixed (byte* ptr = &buffer[start])
            {
                *(uint*)ptr = value;
            }
        }

        public static void WriteUInt64(this byte[] buffer, int start, ulong value)
        {
            fixed (byte* ptr = &buffer[start])
            {
                *(ulong*)ptr = value;
            }
        }

        public const int SizeOfSerializedDecimal = sizeof(byte) + 3 * sizeof(uint);

        public static void WriteDecimal(this byte[] buffer, int start, decimal value)
        {
            bool isNegative;
            byte scale;
            uint low, mid, high;
            value.GetBits(out isNegative, out scale, out low, out mid, out high);

            fixed (byte* ptr = &buffer[start])
            {
                *ptr = (byte)(scale | (isNegative ? 0x80 : 0x00));
                *(uint*)(ptr + 1) = low;
                *(uint*)(ptr + 5) = mid;
                *(uint*)(ptr + 9) = high;
            }
        }

        // TODO: Use UTF8Encoding https://github.com/dotnet/corefx/issues/2217
        public static void WriteUTF8(this byte[] buffer, int start, char* charPtr, int charCount, int byteCount, bool allowUnpairedSurrogates)
        {
            Debug.Assert(byteCount >= charCount);
            const char ReplacementCharacter = '\uFFFD';

            char* strEnd = charPtr + charCount;
            fixed (byte* bufferPtr = &buffer[0])
            {
                byte* ptr = bufferPtr + start;

                if (byteCount == charCount)
                {
                    while (charPtr < strEnd)
                    {
                        Debug.Assert(*charPtr <= 0x7f);
                        *ptr++ = unchecked((byte)*charPtr++);
                    }
                }
                else
                {
                    while (charPtr < strEnd)
                    {
                        char c = *charPtr++;

                        if (c < 0x80)
                        {
                            *ptr++ = (byte)c;
                            continue;
                        }

                        if (c < 0x800)
                        {
                            ptr[0] = (byte)(((c >> 6) & 0x1F) | 0xC0);
                            ptr[1] = (byte)((c & 0x3F) | 0x80);
                            ptr += 2;
                            continue;
                        }

                        if (IsSurrogateChar(c))
                        {
                            // surrogate pair
                            if (IsHighSurrogateChar(c) && charPtr < strEnd && IsLowSurrogateChar(*charPtr))
                            {
                                int highSurrogate = c;
                                int lowSurrogate = *charPtr++;
                                int codepoint = (((highSurrogate - 0xd800) << 10) + lowSurrogate - 0xdc00) + 0x10000;
                                ptr[0] = (byte)(((codepoint >> 18) & 0x7) | 0xF0);
                                ptr[1] = (byte)(((codepoint >> 12) & 0x3F) | 0x80);
                                ptr[2] = (byte)(((codepoint >> 6) & 0x3F) | 0x80);
                                ptr[3] = (byte)((codepoint & 0x3F) | 0x80);
                                ptr += 4;
                                continue;
                            }

                            // unpaired high/low surrogate
                            if (!allowUnpairedSurrogates)
                            {
                                c = ReplacementCharacter;
                            }
                        }

                        ptr[0] = (byte)(((c >> 12) & 0xF) | 0xE0);
                        ptr[1] = (byte)(((c >> 6) & 0x3F) | 0x80);
                        ptr[2] = (byte)((c & 0x3F) | 0x80);
                        ptr += 3;
                    }
                }

                Debug.Assert(ptr == bufferPtr + start + byteCount);
                Debug.Assert(charPtr == strEnd);
            }
        }

        internal unsafe static int GetUTF8ByteCount(string str)
        {
            fixed (char* ptr = str)
            {
                return GetUTF8ByteCount(ptr, str.Length);
            }
        }

        internal unsafe static int GetUTF8ByteCount(char* str, int charCount)
        {
            char* remainder;
            return GetUTF8ByteCount(str, charCount, int.MaxValue, out remainder);
        }

        internal static int GetUTF8ByteCount(char* str, int charCount, int byteLimit, out char* remainder)
        {
            char* end = str + charCount;

            char* ptr = str;
            int byteCount = 0;
            while (ptr < end)
            {
                int characterSize;
                char c = *ptr++;
                if (c < 0x80)
                {
                    characterSize = 1;
                }
                else if (c < 0x800)
                {
                    characterSize = 2;
                }
                else if (IsHighSurrogateChar(c) && ptr < end && IsLowSurrogateChar(*ptr))
                {
                    // surrogate pair:
                    characterSize = 4;
                    ptr++;
                }
                else
                {
                    characterSize = 3;
                }

                if (byteCount + characterSize > byteLimit)
                {
                    ptr -= (characterSize < 4) ? 1 : 2;
                    break;
                }

                byteCount += characterSize;
            }

            remainder = ptr;
            return byteCount;
        }

        internal static bool IsSurrogateChar(int c)
        {
            return unchecked((uint)(c - 0xD800)) <= 0xDFFF - 0xD800;
        }

        internal static bool IsHighSurrogateChar(int c)
        {
            return unchecked((uint)(c - 0xD800)) <= 0xDBFF - 0xD800;
        }

        internal static bool IsLowSurrogateChar(int c)
        {
            return unchecked((uint)(c - 0xDC00)) <= 0xDFFF - 0xDC00;
        }

        internal static void ValidateRange(int bufferLength, int start, int byteCount)
        {
            if (start < 0 || start > bufferLength)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if (byteCount < 0 || byteCount > bufferLength - start)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }
        }

        internal static int GetUserStringByteLength(int characterCount)
        {
            return characterCount * 2 + 1;
        }

        internal static byte GetUserStringTrailingByte(string str)
        {
            // Write out a trailing byte indicating if the string is really quite simple
            foreach (char ch in str)
            {
                if (ch >= 0x7F)
                {
                    return 1;
                }

                switch ((int)ch)
                {
                    case 0x1:
                    case 0x2:
                    case 0x3:
                    case 0x4:
                    case 0x5:
                    case 0x6:
                    case 0x7:
                    case 0x8:
                    case 0xE:
                    case 0xF:
                    case 0x10:
                    case 0x11:
                    case 0x12:
                    case 0x13:
                    case 0x14:
                    case 0x15:
                    case 0x16:
                    case 0x17:
                    case 0x18:
                    case 0x19:
                    case 0x1A:
                    case 0x1B:
                    case 0x1C:
                    case 0x1D:
                    case 0x1E:
                    case 0x1F:
                    case 0x27:
                    case 0x2D:
                        return 1;

                    default:
                        continue;
                }
            }

            return 0;
        }
    }
}
