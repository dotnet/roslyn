// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public sealed record ViewComponentMetadata : MetadataObject
{
    internal ViewComponentMetadata(string name, TypeNameObject originalTypeNameObject)
        : base(MetadataKind.ViewComponent)
    {
        Name = name;
        OriginalTypeNameObject = originalTypeNameObject;
    }

    public string Name { get; }
    internal TypeNameObject OriginalTypeNameObject { get; }

    public string? OriginalTypeName => OriginalTypeNameObject.FullName;

    internal override bool HasDefaultValue => false;

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.Append(Name);
    }

    public ref struct Builder
    {
        public string? Name { get; set; }
        internal TypeNameObject OriginalTypeNameObject { get; set; }

        public readonly ViewComponentMetadata Build()
            => new(Name.AssumeNotNull(), OriginalTypeNameObject);
    }
}
