// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public sealed class TagMatchingRuleDescriptor : TagHelperObject<TagMatchingRuleDescriptor>
{
    private TagHelperDescriptor? _parent;

    public string TagName { get; }
    public string? ParentTag { get; }
    public TagStructure TagStructure { get; }
    public bool CaseSensitive { get; }
    public ImmutableArray<RequiredAttributeDescriptor> Attributes { get; }

    internal TagMatchingRuleDescriptor(
        string tagName,
        string? parentTag,
        TagStructure tagStructure,
        bool caseSensitive,
        ImmutableArray<RequiredAttributeDescriptor> attributes,
        ImmutableArray<RazorDiagnostic> diagnostics)
        : base(diagnostics)
    {
        TagName = tagName;
        ParentTag = parentTag;
        TagStructure = tagStructure;
        CaseSensitive = caseSensitive;
        Attributes = attributes.NullToEmpty();

        foreach (var attribute in Attributes)
        {
            attribute.SetParent(this);
        }
    }

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.Append(TagName);
        builder.Append(ParentTag);
        builder.Append((int)TagStructure);

        builder.Append(CaseSensitive);

        foreach (var descriptor in Attributes)
        {
            builder.Append(descriptor.Checksum);
        }
    }

    public TagHelperDescriptor Parent
        => _parent ?? ThrowHelper.ThrowInvalidOperationException<TagHelperDescriptor>(Resources.Parent_has_not_been_set);

    internal void SetParent(TagHelperDescriptor parent)
    {
        Debug.Assert(parent != null);
        Debug.Assert(_parent == null);

        _parent = parent;
    }

    public IEnumerable<RazorDiagnostic> GetAllDiagnostics()
    {
        foreach (var attribute in Attributes)
        {
            foreach (var diagnostic in attribute.Diagnostics)
            {
                yield return diagnostic;
            }
        }

        foreach (var diagnostic in Diagnostics)
        {
            yield return diagnostic;
        }
    }

    internal string GetDebuggerDisplay()
    {
        var tagName = TagName ?? "*";
        tagName += TagStructure == TagStructure.WithoutEndTag ? "/" : "";
        return $"{TagName ?? "*"}[{string.Join(", ", Attributes.Select(a => DescribeAttribute(a)))}]";
        static string DescribeAttribute(RequiredAttributeDescriptor attribute)
        {
            var name = attribute.Name switch
            {
                null => "*",
                var prefix when attribute.NameComparison == RequiredAttributeNameComparison.PrefixMatch => $"^{prefix}",
                var full => full,
            };

            var value = attribute.Value switch
            {
                null => "",
                var prefix when attribute.ValueComparison == RequiredAttributeValueComparison.PrefixMatch => $"^={prefix}",
                var suffix when attribute.ValueComparison == RequiredAttributeValueComparison.SuffixMatch => $"$={suffix}",
                var full => $"={full}",
            };
            return name + value;
        }
    }
}
