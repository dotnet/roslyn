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

        var xName = x.AsSpan();
        var yName = y.AsSpan();
        var xAliasEnd = xName.IndexOf('=');
        var yAliasEnd = yName.IndexOf('=');
        if (xAliasEnd >= 0 || yAliasEnd >= 0)
        {
            // Like C#, put aliases after namespace and static imports, regardless of their targets.
            if (xAliasEnd < 0)
            {
                return -1;
            }

            if (yAliasEnd < 0)
            {
                return 1;
            }

            // Order by alias name rather than the target namespace or type.
            return xName[..xAliasEnd].TrimEnd().CompareTo(yName[..yAliasEnd].TrimEnd(), StringComparison.Ordinal);
        }

        var xIsStatic = xName.Length > 6 && xName.StartsWith("static".AsSpan(), StringComparison.Ordinal) && char.IsWhiteSpace(xName[6]);
        var yIsStatic = yName.Length > 6 && yName.StartsWith("static".AsSpan(), StringComparison.Ordinal) && char.IsWhiteSpace(yName[6]);

        // Keep static imports in their own group, after namespace imports and before aliases.
        if (xIsStatic != yIsStatic)
        {
            return xIsStatic ? 1 : -1;
        }

        if (xIsStatic)
        {
            // Apply System-first ordering to the targets, not the shared static prefix.
            xName = xName[6..].TrimStart();
            yName = yName[6..].TrimStart();
        }

        var xIsSystem = xName.StartsWith("System".AsSpan(), StringComparison.Ordinal);
        var yIsSystem = yName.StartsWith("System".AsSpan(), StringComparison.Ordinal);

        if (xIsSystem)
        {
            return yIsSystem
                ? xName.CompareTo(yName, StringComparison.Ordinal)
                : -1;
        }

        if (yIsSystem)
        {
            return 1;
        }

        return xName.CompareTo(yName, StringComparison.Ordinal);
    }
}
