// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Tags;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion;

internal static class CommonCompletionItem
{
    public const string DescriptionProperty = nameof(DescriptionProperty);

    public static CompletionItem Create(
        string displayText,
        string? displayTextSuffix,
        CompletionItemRules rules,
        Glyph? glyph = null,
        ImmutableArray<SymbolDisplayPart> description = default,
        string? sortText = null,
        string? filterText = null,
        bool showsWarningIcon = false,
        ImmutableArray<KeyValuePair<string, object>> properties = default,
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
            properties = properties.NullToEmpty().Add(KeyValuePairUtil.Create<string, object>(DescriptionProperty, CompletionDescription.Create(description.ToTaggedText())));
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
        return item.TryGetObjectProperty<CompletionDescription>(DescriptionProperty, out var description)
            ? description
            : CompletionDescription.Empty;
    }
}
