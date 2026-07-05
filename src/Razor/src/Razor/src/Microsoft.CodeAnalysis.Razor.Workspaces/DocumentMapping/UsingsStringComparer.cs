// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor.DocumentMapping;

internal sealed class UsingsStringComparer : IComparer<string>
{
    public static readonly UsingsStringComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (x is null)
        {
            return y is null ? 0 : 1;
        }

        if (y is null)
        {
            return -1;
        }

        var xIsSystem = x.StartsWith("System", StringComparison.Ordinal);
        var yIsSystem = y.StartsWith("System", StringComparison.Ordinal);

        if (xIsSystem)
        {
            return yIsSystem
                ? string.Compare(x, y, StringComparison.Ordinal)
                : -1;
        }

        if (yIsSystem)
        {
            return 1;
        }

        return string.Compare(x, y, StringComparison.Ordinal);
    }
}
