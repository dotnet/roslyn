// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.Components;

public sealed record TypeParameterMetadata() : MetadataObject(MetadataKind.TypeParameter)
{
    public static TypeParameterMetadata Default { get; } = new();

    public bool IsCascading { get; init; }
    public string? Constraints { get; init; }

    /// <summary>
    /// If there are attributes that should be propagated into type inference method, the value of this metadata is the corresponding code for the type parameter such as
    /// <c>[global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] T</c>.
    /// </summary>
    public string? NameWithAttributes { get; init; }

    internal override bool HasDefaultValue => Equals(Default);

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.Append(IsCascading);
        builder.Append(Constraints);
        builder.Append(NameWithAttributes);
    }

    public ref struct Builder
    {
        public bool IsCascading { get; set; }
        public string? Constraints { get; set; }
        public string? NameWithAttributes { get; set; }

        public readonly TypeParameterMetadata Build()
            => new()
            {
                IsCascading = IsCascading,
                Constraints = Constraints,
                NameWithAttributes = NameWithAttributes
            };
    }
}
