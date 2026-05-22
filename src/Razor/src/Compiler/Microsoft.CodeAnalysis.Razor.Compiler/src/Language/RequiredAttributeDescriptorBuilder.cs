// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class RequiredAttributeDescriptorBuilder : TagHelperObjectBuilder<RequiredAttributeDescriptor>
{
    [AllowNull]
    private TagMatchingRuleDescriptorBuilder _parent;
    private RequiredAttributeDescriptorFlags _flags;

    private RequiredAttributeDescriptorBuilder()
    {
    }

    internal RequiredAttributeDescriptorBuilder(TagMatchingRuleDescriptorBuilder parent)
    {
        _parent = parent;
    }

    public string? Name { get; set; }
    public RequiredAttributeNameComparison NameComparison { get; set; }
    public string? Value { get; set; }
    public RequiredAttributeValueComparison ValueComparison { get; set; }

    internal bool CaseSensitive => _parent.CaseSensitive;

    public bool IsDirectiveAttribute
    {
        get => _flags.IsFlagSet(RequiredAttributeDescriptorFlags.IsDirectiveAttribute);
        set => _flags.UpdateFlag(RequiredAttributeDescriptorFlags.IsDirectiveAttribute, value);
    }

    private protected override RequiredAttributeDescriptor BuildCore(ImmutableArray<RazorDiagnostic> diagnostics)
    {
        var flags = _flags;

        if (CaseSensitive)
        {
            flags |= RequiredAttributeDescriptorFlags.CaseSensitive;
        }

        return new RequiredAttributeDescriptor(
            flags,
            Name ?? string.Empty,
            NameComparison,
            Value,
            ValueComparison,
            diagnostics);
    }

    private protected override void CollectDiagnostics(ref PooledHashSet<RazorDiagnostic> diagnostics)
    {
        var name = Name;

        if (name.IsNullOrWhiteSpace())
        {
            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeNameNullOrWhitespace();

            diagnostics.Add(diagnostic);
            return;
        }

        var nameSpan = name.AsSpan();
        Debug.Assert(nameSpan.Length > 0, "Name should not be empty at this point.");

        if (IsDirectiveAttribute)
        {
            if (nameSpan[0] == '@')
            {
                nameSpan = nameSpan[1..];
            }
            else
            {
                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredDirectiveAttributeName(
                    RequiredAttributeDescriptor.GetDisplayName(name, NameComparison), name);

                diagnostics.Add(diagnostic);
            }
        }

        foreach (var ch in nameSpan)
        {
            if (char.IsWhiteSpace(ch) || HtmlConventions.IsInvalidNonWhitespaceHtmlCharacters(ch))
            {
                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeName(name, ch);

                diagnostics.Add(diagnostic);
            }
        }
    }
}
