// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.Components;

public sealed record class ComponentMetadata() : MetadataObject(MetadataKind.Component)
{
    public static ComponentMetadata Default { get; } = new();

    public bool IsGeneric { get; init; }
    public bool HasRenderModeDirective { get; init; }

    internal override bool HasDefaultValue => Equals(Default);

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.Append(IsGeneric);
        builder.Append(HasRenderModeDirective);
    }

    public ref struct Builder
    {
        public bool IsGeneric { get; set; }
        public bool HasRenderModeDirective { get; set; }

        public readonly ComponentMetadata Build()
            => new()
            {
                IsGeneric = IsGeneric,
                HasRenderModeDirective = HasRenderModeDirective
            };
    }
}
