// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Tags;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion;

internal static class CommonCompletionItem
{
    public const string DescriptionProperty = nameof(DescriptionProperty);

    /// <summary>
    /// Mark CompletionItem with this property to indicate the intention of keeping otherwise similar items separated in the completion list.
    /// By default, when two items with identical texts are added to the completion list, unless both are marked with this property, 
    /// only one of them will be shown (the one with this property will win, if exist). 
    /// For example, when there are two items, a property `MyClass.MyMember` and an exntension method `ExtensionClass.MyMember(this MyClass c)`
    /// they have same display text `MyMember` but we want to show both of them in the completion list.
    /// </summary>
    public const string DoNotMergeProperty = nameof(DoNotMergeProperty);

    public static CompletionItem Create(
        string displayText,
        string? displayTextSuffix,
        CompletionItemRules rules,
        Glyph? glyph = null,
        ImmutableArray<SymbolDisplayPart> description = default,
        string? sortText = null,
        string? filterText = null,
        bool showsWarningIcon = false,
        ImmutableArray<KeyValuePair<string, string>> properties = default,
        ImmutableArray<string> tags = default,
        string? inlineDescription = null,
        string? displayTextPrefix = null,
        bool isComplexTextEdit = false)
    {
        tags = tags.NullToEmpty();

        if (glyph != null)
        {
            // put glyph tags first
            tags = GlyphTags.GetTags(glyph.Value).AddRange(tags);
        }

        if (showsWarningIcon)
        {
            tags = tags.Add(WellKnownTags.Warning);
        }

        if (!description.IsDefault && description.Length > 0)
        {
            properties = properties.NullToEmpty().Add(KeyValuePair.Create(DescriptionProperty, EncodeDescription(description.ToTagsAndText())));
        }

        return CompletionItem.CreateInternal(
            displayText: displayText,
            displayTextSuffix: displayTextSuffix,
            displayTextPrefix: displayTextPrefix,
            filterText: filterText,
            sortText: sortText,
            properties: properties,
            tags: tags,
            rules: rules,
            inlineDescription: inlineDescription,
            isComplexTextEdit: isComplexTextEdit);
    }

    public static bool HasDescription(CompletionItem item)
        => item.TryGetProperty(DescriptionProperty, out var _);

    public static CompletionDescription GetDescription(CompletionItem item)
    {
        if (item.TryGetProperty(DescriptionProperty, out var encodedDescription))
        {
            return DecodeDescription(encodedDescription);
        }
        else
        {
            return CompletionDescription.Empty;
        }
    }

    private static readonly char[] s_descriptionSeparators = ['|'];

    private static string EncodeDescription(ImmutableArray<(string tag, string text)> description)
    {
        using var _ = PooledStringBuilder.GetInstance(out var builder);

        foreach (var (tag, text) in description)
        {
            var escapedTag = tag.Escape('\\', s_descriptionSeparators);
            var escapedText = text.Escape('\\', s_descriptionSeparators);

            if (builder.Length > 0)
                builder.Append('|');

            builder.Append(escapedTag);
            builder.Append('|');
            builder.Append(escapedText);
        }

        return builder.ToString();
    }

    private static CompletionDescription DecodeDescription(string encoded)
    {
        var parts = encoded.Split(s_descriptionSeparators).Select(t => t.Unescape('\\')).ToArray();

        var builder = ImmutableArray<TaggedText>.Empty.ToBuilder();
        for (var i = 0; i < parts.Length; i += 2)
        {
            builder.Add(new TaggedText(parts[i], parts[i + 1]));
        }

        return CompletionDescription.Create(builder.ToImmutable());
    }
}
