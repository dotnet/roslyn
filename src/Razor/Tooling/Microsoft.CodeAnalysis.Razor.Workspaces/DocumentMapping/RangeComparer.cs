// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.DocumentMapping;

internal sealed class RangeComparer : IComparer<LspRange>
{
    public static readonly RangeComparer Instance = new();

    public int Compare(LspRange? x, LspRange? y)
    {
        if (x is null)
        {
            return y is null ? 0 : 1;
        }

        if (y is null)
        {
            return -1;
        }

        return x.CompareTo(y);
    }
}
