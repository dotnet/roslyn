// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class ReadOnlySpanExtensions
{
#if !NET
    public static bool Contains<T>(this ReadOnlySpan<T> values, T value)
    {
        foreach (var v in values)
        {
            if (EqualityComparer<T>.Default.Equals(v, value))
                return true;
        }

        return false;
    }
#endif
}
