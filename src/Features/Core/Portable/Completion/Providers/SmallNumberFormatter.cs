// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal static class SmallNumberFormatter
{
    private const int SmallNumberCacheLength = 32;

    /// <summary>
    /// Lazily-populated cache of strings for values in the range [0, <see cref="SmallNumberFormatter"/>).
    /// Inspired by corresponding field in core runtime's Number class. 
    /// </summary>
    private static readonly string[] s_smallNumberCache = new string[SmallNumberCacheLength];

    internal static string ToString(int value)
    {
        if (value >= SmallNumberCacheLength)
            return value.ToString();

        return s_smallNumberCache[value] ?? CreateAndCacheString(value);

        [MethodImpl(MethodImplOptions.NoInlining)] // keep rare usage out of fast path
        static string CreateAndCacheString(int value)
            => s_smallNumberCache[value] = value.ToString();
    }
}
