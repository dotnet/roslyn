// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Note: This was refactored from http://github.com/dotnet/runtime. The original algorithm is located at
// https://github.com/dotnet/runtime/blob/876f763a8ff8345b61897ff6297876445b2b484f/src/libraries/System.Private.CoreLib/src/System/Net/WebUtility.cs#L483-L543

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Razor;

namespace Microsoft.CodeAnalysis.Razor.Utilities;

internal static class UrlDecoder
{
    /// <summary>
    ///  Map from an ASCII char to its hex value, e.g. arr['b'] == 11. 0xFF means it's not a hex digit.
    /// </summary>
    private static ReadOnlySpan<byte> CharToHexLookup => new byte[]
    {
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 15
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 31
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 47
        0x0,  0x1,  0x2,  0x3,  0x4,  0x5,  0x6,  0x7,  0x8,  0x9,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 63
        0xFF, 0xA,  0xB,  0xC,  0xD,  0xE,  0xF,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 79
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 95
        0xFF, 0xa,  0xb,  0xc,  0xd,  0xe,  0xf,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 111
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 127
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 143
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 159
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 175
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 191
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 207
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 223
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 239
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF  // 255
    };

    private static int CharToHexValue(int ch)
        => ch >= CharToHexLookup.Length ? 0xff : CharToHexLookup[ch];

    private ref struct ByteBuffer(int size)
    {
        private readonly int _size = size;

        private byte[]? _bytes;
        private int _bytesWritten;

        public readonly bool HasBytes => _bytesWritten > 0;

        public readonly void Dispose()
        {
            if (_bytes is byte[] bytes)
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        public void Add(byte b)
        {
            _bytes ??= ArrayPool<byte>.Shared.Rent(_size);
            _bytes[_bytesWritten++] = b;
        }

        public unsafe int Flush(Span<char> chars)
        {
            if (_bytesWritten == 0)
            {
                return 0;
            }

            fixed (byte* bytesPtr = _bytes)
            fixed (char* charsPtr = chars)
            {
                var charsWritten = Encoding.UTF8.GetChars(bytesPtr, _bytesWritten, charsPtr, chars.Length);
                _bytesWritten = 0;

                return charsWritten;
            }
        }
    }

    public static void Decode(ReadOnlySpan<char> source, Span<char> destination, out int charsWritten)
    {
        charsWritten = 0;

        if (source.IsEmpty)
        {
            return;
        }

        if (destination.Length < source.Length)
        {
            throw new ArgumentException("Destination length must be greater or equal to the source length.", nameof(destination));
        }

        var count = source.Length;
        ref var src = ref MemoryMarshal.GetReference(source);

        using var buffer = new ByteBuffer(count);

        // Go through the string's chars collapsing %XX and appending each char
        // as char, with exception %XX constructs that are appended as bytes
        var needsDecodingUnsafe = false;
        var needsDecodingSpaces = false;

        // Walk through chars, collapsing %XX
        for (var i = 0; i < count; i++)
        {
            var ch = Unsafe.Add(ref src, i);

            if (ch == '+')
            {
                needsDecodingSpaces = true;
                ch = ' ';
            }
            else if (ch == '%' && i < count - 2)
            {
                // Get the hex values of the next two characters. These are 'nibble' values (i.e. half-bytes).
                // So, we need to construct the real byte out of them.
                var h1 = CharToHexValue(Unsafe.Add(ref src, i + 1));
                var h2 = CharToHexValue(Unsafe.Add(ref src, i + 2));

                if ((h1 | h2) != 0xff)
                {
                    // Valid 2 hex character
                    var b = (byte)(h1 << 4 | h2);
                    i += 2;

                    // Add to our byte buffer.
                    buffer.Add(b);
                    needsDecodingUnsafe = true;
                    continue;
                }
            }

            if ((ch & 0xff80) == 0)
            {
                // 7-bit chars have to be handled as bytes.
                buffer.Add((byte)ch);
            }
            else
            {
                if (buffer.HasBytes)
                {
                    charsWritten += buffer.Flush(destination[charsWritten..]);
                }

                destination[charsWritten++] = ch;
            }
        }

        if (buffer.HasBytes)
        {
            charsWritten += buffer.Flush(destination[charsWritten..]);
        }

        if (!needsDecodingUnsafe)
        {
            // If we didn't do any significant decoding we should have written the entire source
            // to the destination buffer.
            Debug.Assert(charsWritten == source.Length);

            if (needsDecodingSpaces)
            {
                // It's possible that we still need to decode +'s as spaces. However, be sure to
                // only replace chars in the range that was written.
                destination[..charsWritten].Replace('+', ' ');
            }
        }
    }

    public static ReadOnlyMemory<char> Decode(ReadOnlyMemory<char> value)
    {
        if (value.IsEmpty)
        {
            return value;
        }

        var source = value.Span;
        using var _ = ArrayPool<char>.Shared.GetPooledArray(source.Length, out var destination);

        Decode(source, destination, out var charsWritten);

        // Go ahead and create a new string so that ReadOnlyMemory<char>.ToString() is non-allocating.
        return new string(destination, 0, charsWritten).AsMemory();
    }

    public static string Decode(string value)
    {
        return value.Length > 0
            ? Decode(value.AsMemory()).ToString()
            : value;
    }
}
