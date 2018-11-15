// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Tags;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    internal static class CommonCompletionItem
    {
        [Obsolete("This is a compatibility shim for FSharp; please do not use it.", error: true)]
        public static CompletionItem Create(
            string displayText,
            CompletionItemRules rules,
            Glyph? glyph = null,
            ImmutableArray<SymbolDisplayPart> description = default,
            string sortText = null,
            string filterText = null,
            bool showsWarningIcon = false,
            ImmutableDictionary<string, string> properties = null,
            ImmutableArray<string> tags = default)
        {
            return Create(
                displayText, displayTextSuffix: string.Empty, rules,
                glyph, description, sortText, filterText, showsWarningIcon, properties, tags);
        }

        public static CompletionItem Create(
            string displayText,
            string displayTextSuffix,
            CompletionItemRules rules,
            Glyph? glyph = null,
            ImmutableArray<SymbolDisplayPart> description = default,
            string sortText = null,
            string filterText = null,
            bool showsWarningIcon = false,
            ImmutableDictionary<string, string> properties = null,
            ImmutableArray<string> tags = default)
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

            properties = properties ?? ImmutableDictionary<string, string>.Empty;
            if (!description.IsDefault && description.Length > 0)
            {
                properties = properties.Add("Description", EncodeDescription(description));
            }

            return CompletionItem.Create(
                displayText: displayText,
                displayTextSuffix: displayTextSuffix,
                filterText: filterText,
                sortText: sortText,
                properties: properties,
                tags: tags,
                rules: rules);
        }

        public static bool HasDescription(CompletionItem item)
        {
            return item.Properties.ContainsKey("Description");
        }

        public static CompletionDescription GetDescription(CompletionItem item)
        {
            if (item.Properties.TryGetValue("Description", out var encodedDescription))
            {
                return DecodeDescription(encodedDescription);
            }
            else
            {
                return CompletionDescription.Empty;
            }
        }

        private static char[] s_descriptionSeparators = new char[] { '|' };

        private static string EncodeDescription(ImmutableArray<SymbolDisplayPart> description)
        {
            return EncodeDescription(description.ToTaggedText());
        }

        private static string EncodeDescription(ImmutableArray<TaggedText> description)
        {
            if (description.Length > 0)
            {
                return string.Join("|",
                    description
                        .SelectMany(d => new string[] { d.Tag, d.Text })
                        .Select(t => t.Escape('\\', s_descriptionSeparators)));
            }
            else
            {
                return null;
            }
        }

        private static CompletionDescription DecodeDescription(string encoded)
        {
            var parts = encoded.Split(s_descriptionSeparators).Select(t => t.Unescape('\\')).ToArray();

            var builder = ImmutableArray<TaggedText>.Empty.ToBuilder();
            for (int i = 0; i < parts.Length; i += 2)
            {
                builder.Add(new TaggedText(parts[i], parts[i + 1]));
            }

            return CompletionDescription.Create(builder.ToImmutable());
        }
    }
}
