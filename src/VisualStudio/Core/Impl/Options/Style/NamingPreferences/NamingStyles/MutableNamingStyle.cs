// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.NamingStyles;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

internal sealed class MutableNamingStyle(NamingStyle namingStyle)
{
    public NamingStyle NamingStyle { get; private set; } = namingStyle;

    public Guid ID => NamingStyle.ID;

    public string Name
    {
        get => NamingStyle.Name;
        set => NamingStyle = NamingStyle with { Name = value };
    }

    public string Prefix
    {
        get => NamingStyle.Prefix;
        set => NamingStyle = NamingStyle with { Prefix = value };
    }

    public string Suffix
    {
        get => NamingStyle.Suffix;
        set => NamingStyle = NamingStyle with { Suffix = value };
    }

    public string WordSeparator
    {
        get => NamingStyle.WordSeparator;
        set => NamingStyle = NamingStyle with { WordSeparator = value };
    }

    public Capitalization CapitalizationScheme
    {
        get => NamingStyle.CapitalizationScheme;
        set => NamingStyle = NamingStyle with { CapitalizationScheme = value };
    }

    public MutableNamingStyle()
        : this(new NamingStyle(Guid.NewGuid()))
    {
    }

    internal MutableNamingStyle Clone()
        => new(NamingStyle);
}
