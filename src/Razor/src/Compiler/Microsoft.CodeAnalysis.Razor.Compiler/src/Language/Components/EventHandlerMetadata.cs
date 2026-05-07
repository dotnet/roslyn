// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.Components;

public sealed record EventHandlerMetadata() : MetadataObject(MetadataKind.EventHandler)
{
    public required string EventArgsType { get; init; }

    internal override bool HasDefaultValue => false;

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.Append(EventArgsType);
    }

    public ref struct Builder
    {
        public string? EventArgsType { get; set; }

        public readonly EventHandlerMetadata Build()
            => new()
            {
                EventArgsType = EventArgsType.AssumeNotNull()
            };
    }
}
