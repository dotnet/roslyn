// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class AllowedChildTagDescriptor : TagHelperObject<AllowedChildTagDescriptor>
{
    private TagHelperDescriptor? _parent;

    public string Name { get; }
    public string DisplayName { get; }

    internal AllowedChildTagDescriptor(string name, string displayName, ImmutableArray<RazorDiagnostic> diagnostics)
        : base(diagnostics)
    {
        Name = name;
        DisplayName = displayName;
    }

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.Append(Name);
        builder.Append(DisplayName);
    }

    public TagHelperDescriptor Parent
        => _parent ?? ThrowHelper.ThrowInvalidOperationException<TagHelperDescriptor>(Resources.Parent_has_not_been_set);

    internal void SetParent(TagHelperDescriptor parent)
    {
        Debug.Assert(parent != null);
        Debug.Assert(_parent == null);

        _parent = parent;
    }

    public override string ToString()
        => DisplayName ?? base.ToString()!;
}
