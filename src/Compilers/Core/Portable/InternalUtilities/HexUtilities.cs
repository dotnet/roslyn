// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Roslyn.Utilities
{
    internal static class HexUtilities
    {
        internal static string ToHexString(ReadOnlySpan<byte> bytes)
            => ToHexString(bytes, upperCase: true);

        internal static string ToHexStringLower(ReadOnlySpan<byte> bytes)
            => ToHexString(bytes, upperCase: false);

        private static string ToHexString(ReadOnlySpan<byte> bytes, bool upperCase)
        {
#if NET10_0_OR_GREATER
            return string.Create(bytes.Length * 2, bytes, upperCase
                ? static (destination, bytes) => toHex(bytes, destination, 'A')
                : static (destination, bytes) => toHex(bytes, destination, 'a'));
#else
            char[] chars = new char[bytes.Length * 2];
            toHex(bytes, chars, upperCase ? 'A' : 'a');
            return new string(chars);
#endif

            static void toHex(ReadOnlySpan<byte> source, Span<char> destination, char firstHexLetter)
            {
                int destinationIndex = 0;
                foreach (var value in source)
                {
                    destination[destinationIndex++] = hexChar(value >> 4, firstHexLetter);
                    destination[destinationIndex++] = hexChar(value & 0xF, firstHexLetter);
                }
            }

            static char hexChar(int value, char firstHexLetter)
                => (char)(value <= 9 ? value + '0' : value + (firstHexLetter - 10));
        }
    }
}
