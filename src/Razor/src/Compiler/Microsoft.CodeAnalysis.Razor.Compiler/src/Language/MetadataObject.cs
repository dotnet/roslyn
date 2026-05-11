// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public enum MetadataKind : byte
{
    None,
    TypeParameter,
    Property,
    ChildContentParameter,
    Bind,
    Component,
    EventHandler,
    ViewComponent
}

public abstract record MetadataObject
{
    public static readonly MetadataObject None = new NoMetadataObject();

    public MetadataKind Kind { get; }

    protected MetadataObject(MetadataKind kind)
    {
        Kind = kind;
    }

    internal abstract bool HasDefaultValue { get; }

    internal void AppendToChecksum(in Checksum.Builder builder)
    {
        builder.Append((byte)Kind);

        BuildChecksum(in builder);
    }

    private protected abstract void BuildChecksum(in Checksum.Builder builder);

    private sealed record NoMetadataObject() : MetadataObject(MetadataKind.None)
    {
        internal override bool HasDefaultValue => true;

        private protected override void BuildChecksum(in Checksum.Builder builder)
        {
            // No more data to append.
        }
    }
}
