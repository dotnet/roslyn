// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal sealed class MarkupChunkGenerator : SpanChunkGenerator
{
    public static readonly MarkupChunkGenerator Instance = new();

    private MarkupChunkGenerator() { }

    public override string ToString()
    {
        return "Markup";
    }

    public override bool Equals(object obj)
    {
        return ReferenceEquals(this, obj);
    }

    public override int GetHashCode()
    {
        return RuntimeHelpers.GetHashCode(this);
    }
}
