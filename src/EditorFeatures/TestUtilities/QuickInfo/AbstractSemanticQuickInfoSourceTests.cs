// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo
{
    [UseExportProvider]
    public abstract class AbstractSemanticQuickInfoSourceTests
    {
        protected FormattedClassification Text(string text)
            => FormattedClassifications.Text(text);

        protected string Lines(params string[] lines)
            => string.Join("\r\n", lines);

        protected FormattedClassification[] ExpectedClassifications(
            params FormattedClassification[] expectedClassifications)
        {
            return expectedClassifications;
        }

        protected Tuple<string, string>[] NoClassifications()
            => null;

        internal Action<QuickInfoItem> SymbolGlyph(Glyph expectedGlyph)
        {
            return qi =>
            {
                Assert.Contains(expectedGlyph, qi.Tags.GetGlyphs());
            };
        }

        internal Action<QuickInfoItem> WarningGlyph(Glyph expectedGlyph)
            => SymbolGlyph(expectedGlyph);

        internal void AssertSection(
            string expectedText,
            ImmutableArray<QuickInfoSection> sections,
            string textBlockKind,
            FormattedClassification[] expectedClassifications = null)
        {
            var textBlock = sections.FirstOrDefault(tb => tb.Kind == textBlockKind);
            var text = textBlock != null ? textBlock.TaggedParts : ImmutableArray<TaggedText>.Empty;
            AssertTaggedText(expectedText, text, expectedClassifications);
        }

        protected void AssertTaggedText(
            string expectedText,
            ImmutableArray<TaggedText> taggedText,
            FormattedClassification[] expectedClassifications = null)
        {
            var actualText = string.Concat(taggedText.Select(tt => tt.Text));
            Assert.Equal(expectedText, actualText);
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

        protected Action<QuickInfoItem> Remarks(
            string expectedText,
            FormattedClassification[] expectedClassifications = null)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.RemarksDocumentationComments, expectedClassifications);
        }

        protected Action<QuickInfoItem> Returns(
            string expectedText,
            FormattedClassification[] expectedClassifications = null)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.ReturnsDocumentationComments);
        }

        protected Action<QuickInfoItem> Value(
            string expectedText,
            FormattedClassification[] expectedClassifications = null)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.ValueDocumentationComments);
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

        protected Action<QuickInfoItem> NullabilityAnalysis(
            string expectedText,
            FormattedClassification[] expectedClassifications = null)
        {
            return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.NullabilityAnalysis, expectedClassifications);
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
            => item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.Exception);

        protected Action<QuickInfoItem> Captures(string capturesText)
            => item => AssertSection(capturesText, item.Sections, QuickInfoSectionKinds.Captures);

        protected static async Task<bool> CanUseSpeculativeSemanticModelAsync(Document document, int position)
        {
            var service = document.GetLanguageService<ISyntaxFactsService>();
            var node = (await document.GetSyntaxRootAsync()).FindToken(position).Parent;

            return !service.GetMemberBodySpanForSpeculativeBinding(node).IsEmpty;
        }

        protected abstract Task TestAsync(string markup, params Action<QuickInfoItem>[] expectedResults);
    }
}
