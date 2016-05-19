// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    internal static class CommonCompletionItem 
    {
        public static CompletionItem Create(
            string displayText,
            TextSpan span,
            Glyph? glyph = null,
            ImmutableArray<SymbolDisplayPart> description = default(ImmutableArray<SymbolDisplayPart>),
            string sortText = null,
            string filterText = null,
            bool preselect = false,
            bool showsWarningIcon = false,
            bool shouldFormatOnCommit = false,
            bool isArgumentName = false,
            ImmutableDictionary<string, string> properties = null,
            ImmutableArray<string> tags = default(ImmutableArray<string>),
            CompletionItemRules rules = null)
        {
            tags = tags.IsDefault ? ImmutableArray<string>.Empty : tags;

            if (glyph != null)
            {
                // put glyph tags first
                tags = GlyphTags.GetTags(glyph.Value).AddRange(tags);
            }

            if (showsWarningIcon)
            {
                tags = tags.Add(CompletionTags.Warning);
            }

            if (isArgumentName)
            {
                tags = tags.Add(CompletionTags.ArgumentName);
            }

            properties = properties ?? ImmutableDictionary<string, string>.Empty;
            if (!description.IsDefault && description.Length > 0)
            {
                properties = properties.Add(CommonCompletionUtilities.DescriptionKey, CommonCompletionUtilities.EncodeDescription(MapDescription(description)));
            }

            rules = rules ?? CompletionItemRules.Default;
            rules = rules.WithPreselect(preselect)
                         .WithFormatOnCommit(shouldFormatOnCommit);

            return CompletionItem.Create(
                displayText: displayText,
                filterText: filterText,
                sortText: sortText,
                span: span,
                properties: properties,
                tags: tags,
                rules: rules);
        }

        private static ImmutableArray<TaggedText> MapDescription(ImmutableArray<SymbolDisplayPart> description)
        {
            return description.Select(d => new TaggedText(SymbolDisplayPartKindTags.GetTag(d.Kind), d.ToString())).ToImmutableArray();
        }
    }
}
