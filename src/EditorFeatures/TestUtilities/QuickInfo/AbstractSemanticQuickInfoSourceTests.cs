// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Classification;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo
{
    [UseExportProvider]
    public abstract class AbstractSemanticQuickInfoSourceTests
    {
        protected FormattedClassification Text(string text)
            => FormattedClassifications.Text(text);

        protected string Lines(params string[] lines)
        {
            return string.Join("\r\n", lines);
        }

        protected FormattedClassification[] ExpectedClassifications(
            params FormattedClassification[] expectedClassifications)
        {
            return expectedClassifications;
        }

        protected Tuple<string, string>[] NoClassifications()
        {
            return null;
        }

        internal Action<QuickInfoItem> SymbolGlyph(Glyph expectedGlyph)
        {
            return qi =>
            {
                Assert.Contains(expectedGlyph, qi.Tags.GetGlyphs());
            };
        }

        internal Action<QuickInfoItem> WarningGlyph(Glyph expectedGlyph)
        {
            return SymbolGlyph(expectedGlyph);
        }

        internal void AssertSection(
            string expectedText,
            ImmutableArray<QuickInfoSection> sections,
            string textBlockKind,
            FormattedClassification[] expectedClassifications = null)
        {
            var textBlock = sections.FirstOrDefault(tb => tb.Kind == textBlockKind);
            var taggedText = textBlock != null ? textBlock.TaggedParts : ImmutableArray<TaggedText>.Empty;

            Assert.Equal(expectedText, taggedText.GetFullText());
        }

        internal void AssertSection(
            ImmutableArray<(string text, string tag)> expectedTextWithTags,
            ImmutableArray<QuickInfoSection> sections,
            string textBlockKind)
        {
            var textBlock = sections.FirstOrDefault(tb => tb.Kind == textBlockKind);
            var taggedText = textBlock != null ? textBlock.TaggedParts : ImmutableArray<TaggedText>.Empty;

            var expectedTaggedText = expectedTextWithTags.Select(t => new TaggedText(t.tag, t.text));
            Assert.Equal(expectedTaggedText.GetFullText(), taggedText.GetFullText());

            // For better failure messages, use AssertEx and assert equality of tuples
            // instead of TaggedText because TaggedText.ToString() just returns the Text.
            AssertEx.Equal(expectedTextWithTags, taggedText.Select(t => (t.Text, t.Tag)));
        }

        protected Action<QuickInfoItem> MainDescription(
            string expectedText,
            FormattedClassification[] expectedClassifications = null)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.Description, expectedClassifications);
        }

        protected Action<QuickInfoItem> Documentation(
            string expectedText,
            FormattedClassification[] expectedClassifications = null)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.DocumentationComments, expectedClassifications);
        }

        protected Action<QuickInfoItem> TypeParameterMap(
            string expectedText,
            FormattedClassification[] expectedClassifications = null)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.TypeParameters, expectedClassifications);
        }

        protected Action<QuickInfoItem> AnonymousTypes(
            string expectedText,
            FormattedClassification[] expectedClassifications = null)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.AnonymousTypes, expectedClassifications);
        }

        protected Action<QuickInfoItem> NoTypeParameterMap
        {
            get
            {
                return item => AssertSection(string.Empty, item.Sections, QuickInfoSectionKinds.TypeParameters);
            }
        }

        protected Action<QuickInfoItem> Usage(string expectedText, bool expectsWarningGlyph = false)
        {
            return item =>
            {
                AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.Usage);

                if (expectsWarningGlyph)
                {
                    WarningGlyph(Glyph.CompletionWarning)(item);
                }
                else
                {
                    Assert.DoesNotContain(Glyph.CompletionWarning, item.Tags.GetGlyphs());
                }
            };
        }

        protected Action<QuickInfoItem> Exceptions(string expectedText)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.Exception);
        }

        protected Action<QuickInfoItem> Captures(string capturesText)
        {
            return item => AssertSection(capturesText, item.Sections, QuickInfoSectionKinds.Captures);
        }

        protected Action<QuickInfoItem> ConstantValue(string expectedText)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.ConstantValue);
        }

        protected Action<QuickInfoItem> ConstantValueContent(params (string text, string tag)[] expectedContentWithTags)
        {
            var expectedTextWithTags = expectedContentWithTags.ToImmutableArray().InsertRange(0, new[]
            {
                ("\r\n", TextTags.LineBreak),
                (FeaturesResources.Constant_value_colon, TextTags.Text),
                (" ", TextTags.Space),
            });

            return item => AssertSection(expectedTextWithTags, item.Sections, QuickInfoSectionKinds.ConstantValue);
        }

        protected static async Task<bool> CanUseSpeculativeSemanticModelAsync(Document document, int position)
        {
            var service = document.GetLanguageService<ISyntaxFactsService>();
            var node = (await document.GetSyntaxRootAsync()).FindToken(position).Parent;

            return !service.GetMemberBodySpanForSpeculativeBinding(node).IsEmpty;
        }

        protected abstract Task TestAsync(string markup, params Action<QuickInfoItem>[] expectedResults);
    }
}
