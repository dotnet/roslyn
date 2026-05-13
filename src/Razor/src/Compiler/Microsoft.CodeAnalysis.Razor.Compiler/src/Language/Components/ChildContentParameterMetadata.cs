// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.Components;

public sealed record class ChildContentParameterMetadata : MetadataObject
{
    public static ChildContentParameterMetadata Default { get; } = new();

    private ChildContentParameterMetadata()
        : base(MetadataKind.ChildContentParameter)
    {
    }

    internal override bool HasDefaultValue => true;

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
    }
}
