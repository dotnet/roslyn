// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test.TestData;

public sealed class StringHolder(string? text) : IComparable<StringHolder>
{
    public string? Text => text;

    public int CompareTo(StringHolder? other)
    {
        if (other is null)
        {
            return 1;
        }

        return string.CompareOrdinal(Text, other.Text);
    }

    public override string ToString() => Text ?? string.Empty;

    public static implicit operator StringHolder(string? text)
        => new(text);

    public sealed class Comparer : IComparer<StringHolder?>
    {
        public static readonly IComparer<StringHolder?> Ordinal = new Comparer(StringComparer.Ordinal);
        public static readonly IComparer<StringHolder?> OrdinalIgnoreCase = new Comparer(StringComparer.OrdinalIgnoreCase);

        private readonly StringComparer _comparer;

        private Comparer(StringComparer comparer)
        {
            _comparer = comparer;
        }

        public int Compare(StringHolder? x, StringHolder? y)
        {
            if (x is null)
            {
                if (y is not null)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            else if (y is null)
            {
                return -1;
            }

            return _comparer.Compare(x.Text, y.Text);
        }
    }
}
