// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Tags;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
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
            ImmutableDictionary<string, string>? properties = null,
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

            properties ??= ImmutableDictionary<string, string>.Empty;
            if (!description.IsDefault && description.Length > 0)
            {
                properties = properties.Add(DescriptionProperty, EncodeDescription(description.ToTaggedText()));
            }

            return CompletionItem.Create(
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
            => item.Properties.ContainsKey(DescriptionProperty);

        public static CompletionDescription GetDescription(CompletionItem item)
        {
            if (item.Properties.TryGetValue(DescriptionProperty, out var encodedDescription))
            {
                return DecodeDescription(encodedDescription);
            }
            else
            {
                return CompletionDescription.Empty;
            }
        }

        private static readonly char[] s_descriptionSeparators = new char[] { '|' };

        private static string EncodeDescription(ImmutableArray<TaggedText> description)
            => string.Join("|", description.SelectMany(d => new[] { d.Tag, d.Text }).Select(t => t.Escape('\\', s_descriptionSeparators)));

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
}
