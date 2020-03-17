// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
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
        protected string Lines(params string[] lines)
        {
            return string.Join("\r\n", lines);
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

        private void AssertSection(
            string expectedText,
            ImmutableArray<QuickInfoSection> sections,
            string textBlockKind)
        {
            var textBlock = sections.FirstOrDefault(tb => tb.Kind == textBlockKind);
            var taggedText = textBlock != null ? textBlock.TaggedParts : ImmutableArray<TaggedText>.Empty;

            Assert.Equal(expectedText, taggedText.GetFullText());
        }

        private void AssertSection(
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

        protected Action<QuickInfoItem> MainDescription(string expectedText)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.Description);
        }

        protected Action<QuickInfoItem> MainDescription(params (string text, string tag)[] expectedTextWithTags)
        {
            return item => AssertSection(expectedTextWithTags.ToImmutableArray(), item.Sections, QuickInfoSectionKinds.Description);
        }

        protected Action<QuickInfoItem> Documentation(string expectedText)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.DocumentationComments);
        }

        protected Action<QuickInfoItem> Documentation(params (string text, string tag)[] expectedTextWithTags)
        {
            return item => AssertSection(expectedTextWithTags.ToImmutableArray(), item.Sections, QuickInfoSectionKinds.DocumentationComments);
        }

        protected Action<QuickInfoItem> Remarks(string expectedText)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.RemarksDocumentationComments);
        }

        protected Action<QuickInfoItem> Returns(string expectedText)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.ReturnsDocumentationComments);
        }

        protected Action<QuickInfoItem> Value(string expectedText)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.ValueDocumentationComments);
        }

        protected Action<QuickInfoItem> TypeParameterMap(string expectedText)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.TypeParameters);
        }

        protected Action<QuickInfoItem> TypeParameterMap(params (string text, string tag)[] expectedTextWithTags)
        {
            return item => AssertSection(expectedTextWithTags.ToImmutableArray(), item.Sections, QuickInfoSectionKinds.TypeParameters);
        }

        protected Action<QuickInfoItem> AnonymousTypes(string expectedText)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.AnonymousTypes);
        }

        protected Action<QuickInfoItem> NullabilityAnalysis(string expectedText)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.NullabilityAnalysis);
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

        protected Action<QuickInfoItem> ConstantValue(params (string text, string tag)[] expectedTextWithTags)
        {
            return item => AssertSection(expectedTextWithTags.ToImmutableArray(), item.Sections, QuickInfoSectionKinds.ConstantValue);
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

        protected Action<QuickInfoItem> ConstantValueContentNoLineBreak(params (string text, string tag)[] expectedContentWithTags)
        {
            var expectedTextWithTags = expectedContentWithTags.ToImmutableArray().InsertRange(0, new[]
            {
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
