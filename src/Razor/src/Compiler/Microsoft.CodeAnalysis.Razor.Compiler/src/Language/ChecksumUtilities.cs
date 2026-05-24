// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class ChecksumUtilities
{
    public static string BytesToString(ImmutableArray<byte> bytes)
    {
        if (bytes.IsDefault)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

#if NET9_0_OR_GREATER
        return Convert.ToHexStringLower(bytes.AsSpan());
#else
        return string.Create(bytes.Length * 2, bytes, static (span, bytes) =>
        {
             foreach (var b in bytes)
             {
                 // Write hex chars directly
                 span[0] = GetHexChar(b >> 4);
                 span[1] = GetHexChar(b & 0xf);
                 span = span[2..];
             }
        });

        static char GetHexChar(int value)
            => (char)(value < 10 ? '0' + value : 'a' + (value - 10));
#endif
    }
}
