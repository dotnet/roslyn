// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Json;

// Json.net uses Convert.ToInt64 when checking if numbers are legal (see
// https://github.com/JamesNK/Newtonsoft.Json/blob/993215529562866719689206e27e413013d4439c/Src/Newtonsoft.Json/JsonTextReader.cs#L1926).
// However, this is very expensive when it fails as it throws an exception (see
// https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1871418).  As such, to avoid that, we have a copy of
// Convert.ToInt64 here which uses `bool Try(out)` semantics instead of exception failure semantics.  Not ideal, but
// necessary as long as the runtime exposes no other way to do this validation with the same semantics in a non-throwing
// fashion.
//
// If this code is updated, it should follow the original BCL code as closely as possible, with the only changes being
// to make it not throw, and that the result is returned in an out-position.  It should otherwise be kept the same.
//
// The normal json parser does not need this as we simply follow the json spec exactly wrt what is or is not legal with
// numbers.  This code is only needed to match json.net's "quirks".

/// <summary>
/// Copy of helpers we need from https://github.com/dotnet/runtime/blob/2bfa26cebc917d05a3363078fa277ab5fee2651b/src/libraries/System.Private.CoreLib/src/System/Convert.cs#L2178
/// that do not throw.
/// </summary>
internal static class RoslynConvert
{
    public static bool TryToInt64(string? value, int fromBase, out long result)
    {
        if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
        {
            result = 0;
            return false;
        }

        if (value is null)
        {
            result = 0;
            return true;
        }

        return RoslynParseNumbers.TryStringToLong(value.AsSpan(), fromBase, RoslynParseNumbers.IsTight, out result);
    }
}

/// <summary>
/// Copy of helpers we need from https://github.com/dotnet/runtime/blob/2bfa26cebc917d05a3363078fa277ab5fee2651b/src/libraries/System.Private.CoreLib/src/System/ParseNumbers.cs#L10
/// that do not throw.
/// </summary>
internal static class RoslynParseNumbers
{
    private const int TreatAsUnsigned = 0x0200;
    public const int IsTight = 0x1000;

    public static unsafe bool TryStringToLong(ReadOnlySpan<char> s, int radix, int flags, out long result)
    {
        var pos = 0;
        return TryStringToLong(s, radix, flags, ref pos, out result);
    }

    private static bool TryStringToLong(ReadOnlySpan<char> s, int radix, int flags, ref int currPos, out long finalResult)
    {
        finalResult = 0;
        var i = currPos;

        // Do some radix checking.
        // A radix of -1 says to use whatever base is spec'd on the number.
        // Parse in Base10 until we figure out what the base actually is.
        var r = (-1 == radix) ? 10 : radix;

        if (r != 2 && r != 10 && r != 8 && r != 16)
            return false;

        var length = s.Length;

        if (i < 0 || i >= length)
            return false;

        // Get rid of the whitespace and then check that we've still got some digits to parse.
        if ((flags & IsTight) == 0)
        {
            EatWhiteSpace(s, ref i);
            if (i == length)
                return false;
        }

        // Check for a sign
        var sign = 1;
        if (s[i] == '-')
        {
            if (r != 10)
                return false;

            if ((flags & TreatAsUnsigned) != 0)
                return false;

            sign = -1;
            i++;
        }
        else if (s[i] == '+')
        {
            i++;
        }

        if ((radix == -1 || radix == 16) && (i + 1 < length) && s[i] == '0')
        {
            if (s[i + 1] == 'x' || s[i + 1] == 'X')
            {
                r = 16;
                i += 2;
            }
        }

        var grabNumbersStart = i;
        if (!TryGrabLongs(r, s, ref i, (flags & TreatAsUnsigned) != 0, out var result))
            return false;

        // Check if they passed us a string with no parsable digits.
        if (i == grabNumbersStart)
            return false;

        if ((flags & IsTight) != 0)
        {
            // If we've got effluvia left at the end of the string, complain.
            if (i < length)
                return false;
        }

        // Put the current index back into the correct place.
        currPos = i;

        // Return the value properly signed.
        if ((ulong)result == 0x8000000000000000 && sign == 1 && r == 10 && ((flags & TreatAsUnsigned) == 0))
            return false;

        if (r == 10)
            result *= sign;

        finalResult = result;
        return true;
    }

    private static void EatWhiteSpace(ReadOnlySpan<char> s, ref int i)
    {
        var localIndex = i;
        for (; localIndex < s.Length && char.IsWhiteSpace(s[localIndex]); localIndex++)
            ;
        i = localIndex;
    }

    private static bool TryGrabLongs(int radix, ReadOnlySpan<char> s, ref int i, bool isUnsigned, out long finalResult)
    {
        finalResult = 0;

        ulong result = 0;
        ulong maxVal;

        // Allow all non-decimal numbers to set the sign bit.
        if (radix == 10 && !isUnsigned)
        {
            maxVal = 0x7FFFFFFFFFFFFFFF / 10;

            // Read all of the digits and convert to a number
            while (i < s.Length && IsDigit(s[i], radix, out var value))
            {
                // Check for overflows - this is sufficient & correct.
                if (result > maxVal || ((long)result) < 0)
                    return false;

                result = result * (ulong)radix + (ulong)value;
                i++;
            }

            if ((long)result < 0 && result != 0x8000000000000000)
                return false;
        }
        else
        {
            RoslynDebug.Assert(radix == 2 || radix == 8 || radix == 10 || radix == 16);
            maxVal =
                radix == 10 ? 0xffffffffffffffff / 10 :
                radix == 16 ? 0xffffffffffffffff / 16 :
                radix == 8 ? 0xffffffffffffffff / 8 :
                0xffffffffffffffff / 2;

            // Read all of the digits and convert to a number
            while (i < s.Length && IsDigit(s[i], radix, out var value))
            {
                // Check for overflows - this is sufficient & correct.
                if (result > maxVal)
                    return false;

                var temp = result * (ulong)radix + (ulong)value;

                if (temp < result) // this means overflow as well
                    return false;

                result = temp;
                i++;
            }
        }

        finalResult = (long)result;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDigit(char c, int radix, out int result)
    {
        int tmp;
        if ((uint)(c - '0') <= 9)
        {
            result = tmp = c - '0';
        }
        else if ((uint)(c - 'A') <= 'Z' - 'A')
        {
            result = tmp = c - 'A' + 10;
        }
        else if ((uint)(c - 'a') <= 'z' - 'a')
        {
            result = tmp = c - 'a' + 10;
        }
        else
        {
            result = -1;
            return false;
        }

        return tmp < radix;
    }
}
