// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.Components;

/// <summary>
/// Descriptor-level metadata that records a single element/attribute pair for which the runtime
/// has opted into '~/' asset-path expansion via <c>[AcceptsAssetPath(element, attribute)]</c>.
/// </summary>
public sealed record AssetPathMetadata() : MetadataObject(MetadataKind.AssetPath)
{
    public required string Element { get; init; }
    public required string Attribute { get; init; }

    internal override bool HasDefaultValue => false;

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.Append(Element);
        builder.Append(Attribute);
    }

    public ref struct Builder
    {
        public string? Element { get; set; }
        public string? Attribute { get; set; }

        public readonly AssetPathMetadata Build()
            => new()
            {
                Element = Element.AssumeNotNull(),
                Attribute = Attribute.AssumeNotNull(),
            };
    }
}
