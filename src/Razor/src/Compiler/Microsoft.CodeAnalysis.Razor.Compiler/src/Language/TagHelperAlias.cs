// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Describes how a using alias changes a component's tag name while retaining its original descriptor.
/// For example, <c>UI = Library</c> maps <c>Library.Controls.Button</c> to <c>UI.Controls.Button</c>.
/// </summary>
/// <remarks>
/// <see cref="NamespaceSuffixLength"/> stores the length of the namespace suffix, such as <c>.Controls</c>.
/// </remarks>
internal readonly record struct TagHelperAlias(
    TagHelperDescriptor Descriptor,
    string Alias,
    int NamespaceSuffixLength,
    bool IsTypeAlias)
{
    public void AppendToChecksum(in Checksum.Builder builder)
    {
        builder.Append(Descriptor.Checksum);
        builder.Append(Alias);
        builder.Append(NamespaceSuffixLength);
        builder.Append(IsTypeAlias);
    }

    public bool MatchesTagName(ReadOnlySpan<char> tagName)
    {
        var comparison = Descriptor.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        if (IsTypeAlias)
        {
            return tagName.Equals(Alias.AsSpan(), comparison);
        }

        return tagName.Length > Alias.Length &&
            tagName.StartsWith(Alias.AsSpan(), comparison) &&
            tagName[Alias.Length] == '.';
    }

    public string GetTagName()
    {
        if (IsTypeAlias)
        {
            return Alias;
        }

        var componentTagName = Descriptor.TagMatchingRules[0].ParentTag ?? Descriptor.TagMatchingRules[0].TagName;
        // The suffix is at the end of the descriptor's namespace, so only its length needs to be stored.
        var typeNamespace = Descriptor.TypeNamespace.AsMemory();
        return string.Create(
            Alias.Length + NamespaceSuffixLength + 1 + componentTagName.Length,
            (Alias, NamespaceSuffixLength, typeNamespace, componentTagName),
            static (span, state) =>
            {
                state.Alias.AsSpan().CopyTo(span);
                span = span[state.Alias.Length..];
                state.typeNamespace.Span.Slice(state.typeNamespace.Length - state.NamespaceSuffixLength, state.NamespaceSuffixLength).CopyTo(span);
                span = span[state.NamespaceSuffixLength..];
                span[0] = '.';
                state.componentTagName.AsSpan().CopyTo(span[1..]);
            });
    }
}
