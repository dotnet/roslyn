// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

internal static class Int32Extensions
{
    public static int CountDigits(this int number)
    {
        // Avoid overflow when negating Int32.MinValue by using unsigned arithmetic
        var value = number < 0 ? (uint)-(number + 1) + 1 : (uint)number;

        // Binary search approach for better branch prediction
        if (value < 100_000)
        {
            if (value < 100)
            {
                return value < 10 ? 1 : 2;
            }

            if (value < 10_000)
            {
                return value < 1_000 ? 3 : 4;
            }

            return 5;
        }

        if (value < 10_000_000)
        {
            return value < 1_000_000 ? 6 : 7;
        }

        if (value < 1_000_000_000)
        {
            return value < 100_000_000 ? 8 : 9;
        }

        return 10;
    }
}
