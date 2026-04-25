// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// The binding information for Tag Helpers resulted to a <see cref="RazorCodeDocument"/>. Represents the
/// Tag Helper information after processing by directives.
/// </summary>
internal sealed class TagHelperDocumentContext
{  
    private static readonly CleanableWeakCache<(string? Prefix, Checksum), TagHelperDocumentContext> s_cache = new(cleanUpThreshold: 20);

    public string? Prefix { get; }
    public TagHelperCollection TagHelpers { get; }

    private TagHelperBinder? _binder;

    private TagHelperDocumentContext(string? prefix, TagHelperCollection tagHelpers)
    {
        Prefix = prefix;
        TagHelpers = tagHelpers;
    }

    public static TagHelperDocumentContext GetOrCreate(TagHelperCollection tagHelpers)
        => GetOrCreate(prefix: null, tagHelpers);

    public static TagHelperDocumentContext GetOrCreate(string? prefix, TagHelperCollection tagHelpers)
    {
        ArgHelper.ThrowIfNull(tagHelpers);

        return s_cache.GetOrAdd(
            key: (prefix, tagHelpers.Checksum),
            arg: (prefix, tagHelpers),
            arg => new(arg.prefix, arg.tagHelpers));
    }

    public TagHelperBinder GetBinder()
        => _binder ?? InterlockedOperations.Initialize(ref _binder, new TagHelperBinder(Prefix, TagHelpers));
}
