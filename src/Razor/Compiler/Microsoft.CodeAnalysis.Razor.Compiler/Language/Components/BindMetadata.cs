// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.Components;

public sealed record class BindMetadata() : MetadataObject(MetadataKind.Bind)
{
    public static BindMetadata Default { get; } = new();

    public bool IsFallback { get; init; }
    public string? ValueAttribute { get; init; }
    public string? ChangeAttribute { get; init; }
    public string? ExpressionAttribute { get; init; }
    public string? TypeAttribute { get; init; }
    public bool IsInvariantCulture { get; init; }
    public string? Format { get; init; }

    internal override bool HasDefaultValue => Equals(Default);

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.Append(IsFallback);
        builder.Append(ValueAttribute);
        builder.Append(ChangeAttribute);
        builder.Append(ExpressionAttribute);
        builder.Append(TypeAttribute);
        builder.Append(IsInvariantCulture);
        builder.Append(Format);
    }

    public ref struct Builder
    {
        public bool IsFallback { get; set; }
        public string? ValueAttribute { get; set; }
        public string? ChangeAttribute { get; set; }
        public string? ExpressionAttribute { get; set; }
        public string? TypeAttribute { get; set; }
        public bool IsInvariantCulture { get; set; }
        public string? Format { get; set; }

        public readonly BindMetadata Build()
            => new()
            {
                IsFallback = IsFallback,
                ValueAttribute = ValueAttribute,
                ChangeAttribute = ChangeAttribute,
                ExpressionAttribute = ExpressionAttribute,
                TypeAttribute = TypeAttribute,
                IsInvariantCulture = IsInvariantCulture,
                Format = Format
            };
    }
}
