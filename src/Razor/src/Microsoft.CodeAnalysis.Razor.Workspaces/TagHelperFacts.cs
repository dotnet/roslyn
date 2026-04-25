// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.VisualStudio.Editor.Razor;

internal static class TagHelperFacts
{
    public static TagHelperBinding? GetTagHelperBinding(
        TagHelperDocumentContext documentContext,
        string? tagName,
        ImmutableArray<KeyValuePair<string, string>> attributes,
        string? parentTag,
        bool parentIsTagHelper)
    {
        ArgHelper.ThrowIfNull(documentContext);

        if (attributes.IsDefault)
        {
            throw new ArgumentNullException(nameof(attributes));
        }

        if (tagName is null)
        {
            return null;
        }

        if (documentContext.TagHelpers.Count == 0)
        {
            return null;
        }

        var binder = documentContext.GetBinder();

        return binder.GetBinding(tagName, attributes, parentTag, parentIsTagHelper);
    }

    public static ImmutableArray<BoundAttributeDescriptor> GetBoundTagHelperAttributes(
        TagHelperDocumentContext documentContext,
        string attributeName,
        TagHelperBinding binding)
    {
        ArgHelper.ThrowIfNull(documentContext);
        ArgHelper.ThrowIfNull(attributeName);
        ArgHelper.ThrowIfNull(binding);

        using var matchingBoundAttributes = new PooledArrayBuilder<BoundAttributeDescriptor>();

        foreach (var tagHelper in binding.TagHelpers)
        {
            foreach (var boundAttribute in tagHelper.BoundAttributes)
            {
                if (TagHelperMatchingConventions.CanSatisfyBoundAttribute(attributeName, boundAttribute))
                {
                    matchingBoundAttributes.Add(boundAttribute);

                    // Only one bound attribute can match an attribute
                    break;
                }
            }
        }

        return matchingBoundAttributes.ToImmutableAndClear();
    }

    public static TagHelperCollection GetTagHelpersGivenTag(
        TagHelperDocumentContext documentContext,
        string tagName,
        string? parentTag)
    {
        ArgHelper.ThrowIfNull(documentContext);
        ArgHelper.ThrowIfNull(tagName);

        if (documentContext.TagHelpers.IsEmpty)
        {
            return [];
        }

        var tagNameWithoutPrefix = tagName.AsMemory();

        if (documentContext.Prefix is { Length: > 0 } prefix)
        {
            if (!tagNameWithoutPrefix.Span.StartsWith(prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                // 'tagName' can't possibly match TagHelpers if it doesn't start with the provided prefix.
                return [];
            }

            tagNameWithoutPrefix = tagNameWithoutPrefix[prefix.Length..];
        }

        return documentContext.TagHelpers.Where(state: (tagNameWithoutPrefix, parentTag), static (tagHelper, state) =>
        {
            foreach (var rule in tagHelper.TagMatchingRules)
            {
                if (TagHelperMatchingConventions.SatisfiesTagName(rule, state.tagNameWithoutPrefix.Span) &&
                    TagHelperMatchingConventions.SatisfiesParentTag(rule, state.parentTag.AsSpan()))
                {
                    return true;
                }
            }

            return false;
        });
    }

    public static TagHelperCollection GetTagHelpersGivenParent(TagHelperDocumentContext documentContext, string? parentTag)
    {
        ArgHelper.ThrowIfNull(documentContext);

        if (documentContext.TagHelpers.IsEmpty)
        {
            return [];
        }

        return documentContext.TagHelpers.Where(parentTag, static (tagHelper, parentTag) =>
        {
            foreach (var rule in tagHelper.TagMatchingRules)
            {
                if (TagHelperMatchingConventions.SatisfiesParentTag(rule, parentTag.AsSpan()))
                {
                    return true;
                }
            }

            return false;
        });
    }

    public static ImmutableArray<KeyValuePair<string, string>> StringifyAttributes(SyntaxList<RazorSyntaxNode> attributes)
    {
        using var builder = new PooledArrayBuilder<KeyValuePair<string, string>>();

        foreach (var attribute in attributes)
        {
            switch (attribute)
            {
                case MarkupTagHelperAttributeSyntax tagHelperAttribute:
                    {
                        var name = tagHelperAttribute.Name.GetContent();
                        var value = tagHelperAttribute.Value?.GetContent() ?? string.Empty;
                        builder.Add(KeyValuePair.Create(name, value));
                        break;
                    }

                case MarkupMinimizedTagHelperAttributeSyntax minimizedTagHelperAttribute:
                    {
                        var name = minimizedTagHelperAttribute.Name.GetContent();
                        builder.Add(KeyValuePair.Create(name, string.Empty));
                        break;
                    }

                case MarkupAttributeBlockSyntax markupAttribute:
                    {
                        var name = markupAttribute.Name.GetContent();
                        var value = markupAttribute.Value?.GetContent() ?? string.Empty;
                        builder.Add(KeyValuePair.Create(name, value));
                        break;
                    }

                case MarkupMinimizedAttributeBlockSyntax minimizedMarkupAttribute:
                    {
                        var name = minimizedMarkupAttribute.Name.GetContent();
                        builder.Add(KeyValuePair.Create(name, string.Empty));
                        break;
                    }

                case MarkupTagHelperDirectiveAttributeSyntax directiveAttribute:
                    {
                        var name = directiveAttribute.FullName;
                        var value = directiveAttribute.Value?.GetContent() ?? string.Empty;
                        builder.Add(KeyValuePair.Create(name, value));
                        break;
                    }

                case MarkupMinimizedTagHelperDirectiveAttributeSyntax minimizedDirectiveAttribute:
                    {
                        var name = minimizedDirectiveAttribute.FullName;
                        builder.Add(KeyValuePair.Create(name, string.Empty));
                        break;
                    }
            }
        }

        return builder.ToImmutableAndClear();
    }

    public static (string? ancestorTagName, bool ancestorIsTagHelper) GetNearestAncestorTagInfo(IEnumerable<SyntaxNode> ancestors)
    {
        foreach (var ancestor in ancestors)
        {
            if (ancestor is BaseMarkupElementSyntax { StartTag: var startTag })
            {
                // It's possible for start tag to be null in malformed cases.
                var name = startTag?.Name.Content ?? string.Empty;
                return (name, ancestorIsTagHelper: ancestor is MarkupTagHelperElementSyntax);
            }
        }

        return (ancestorTagName: null, ancestorIsTagHelper: false);
    }
}
