// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Unicode;
using Internal.Runtime.CompilerServices;

namespace System
{
    internal static partial class Marvin
    {
        /// <summary>
        /// Compute a Marvin OrdinalIgnoreCase hash and collapse it into a 32-bit hash.
        /// n.b. <paramref name="count"/> is specified as char count, not byte count.
        /// </summary>
        public static int ComputeHash32OrdinalIgnoreCase(ref char data, int count, uint p0, uint p1)
        {
            uint ucount = (uint)count; // in chars
            nuint byteOffset = 0; // in bytes
            uint tempValue;

            // We operate on 32-bit integers (two chars) at a time.

            while (ucount >= 2)
            {
                tempValue = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref data, byteOffset)));
                if (!Utf16Utility.AllCharsInUInt32AreAscii(tempValue))
                {
                    goto NotAscii;
                }
                p0 += Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase(tempValue);
                Block(ref p0, ref p1);

                byteOffset += 4;
                ucount -= 2;
            }

            // We have either one char (16 bits) or zero chars left over.
            Debug.Assert(ucount < 2);

            if (ucount > 0)
            {
                tempValue = Unsafe.AddByteOffset(ref data, byteOffset);
                if (tempValue > 0x7Fu)
                {
                    goto NotAscii;
                }

                // addition is written with -0x80u to allow fall-through to next statement rather than jmp past it
                p0 += Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase(tempValue) + (0x800000u - 0x80u);
            }
            p0 += 0x80u;

            Block(ref p0, ref p1);
            Block(ref p0, ref p1);

            return (int)(p1 ^ p0);

        NotAscii:
            Debug.Assert(ucount <= int.MaxValue); // this should fit into a signed int
            return ComputeHash32OrdinalIgnoreCaseSlow(ref Unsafe.AddByteOffset(ref data, byteOffset), (int)ucount, p0, p1);
        }

        private static int ComputeHash32OrdinalIgnoreCaseSlow(ref char data, int count, uint p0, uint p1)
        {
            Debug.Assert(count > 0);

            char[]? borrowedArr = null;
            Span<char> scratch = (uint)count <= 64 ? stackalloc char[64] : (borrowedArr = ArrayPool<char>.Shared.Rent(count));

            int charsWritten = System.Globalization.Ordinal.ToUpperOrdinal(new ReadOnlySpan<char>(ref data, count), scratch);
            Debug.Assert(charsWritten == count); // invariant case conversion should involve simple folding; preserve code unit count

            // Slice the array to the size returned by ToUpperInvariant.
            // Multiplication below will not overflow since going from positive Int32 to UInt32.
            int hash = ComputeHash32(ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(scratch)), (uint)charsWritten * 2, p0, p1);

            // Return the borrowed array if necessary.
            if (borrowedArr != null)
            {
                ArrayPool<char>.Shared.Return(borrowedArr);
            }

            return hash;
        }
    }
}
