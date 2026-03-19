// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Classification;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo;

[UseExportProvider]
public abstract class AbstractSemanticQuickInfoSourceTests
{
    protected static FormattedClassification Text(string text)
        => FormattedClassifications.Text(text);

    protected static string Lines(params string[] lines)
        => string.Join("\r\n", lines);

    protected static FormattedClassification[] ExpectedClassifications(
        params FormattedClassification[] expectedClassifications)
    {
        return expectedClassifications;
    }

    protected static Tuple<string, string>[] NoClassifications()
        => null;

    internal static Action<QuickInfoItem> SymbolGlyph(Glyph expectedGlyph)
        => qi => Assert.Contains(expectedGlyph, qi.Tags.GetGlyphs());

    internal static Action<QuickInfoItem> WarningGlyph(Glyph expectedGlyph)
        => SymbolGlyph(expectedGlyph);

    internal static void AssertSection(
        string expectedText,
        ImmutableArray<QuickInfoSection> sections,
        string textBlockKind,
        FormattedClassification[] expectedClassifications = null)
    {
        var textBlock = sections.FirstOrDefault(tb => tb.Kind == textBlockKind);
        var text = textBlock != null ? textBlock.TaggedParts : [];
        AssertTaggedText(expectedText, text, expectedClassifications);
    }

    protected static void AssertTaggedText(
        string expectedText,
        ImmutableArray<TaggedText> taggedText,
#pragma warning disable IDE0060 // Remove unused parameter - https://github.com/dotnet/roslyn/issues/45893
        FormattedClassification[] expectedClassifications = null)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        var actualText = string.Concat(taggedText.Select(tt => tt.Text));
        AssertEx.Equal(expectedText, actualText);
    }

    protected static Action<QuickInfoItem> MainDescription(
        string expectedText,
        FormattedClassification[] expectedClassifications = null)
    {
        return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.Description, expectedClassifications);
    }

    protected static Action<QuickInfoItem> Documentation(
        string expectedText,
        FormattedClassification[] expectedClassifications = null)
    {
        return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.DocumentationComments, expectedClassifications);
    }

    protected static Action<QuickInfoItem> Remarks(
        string expectedText,
        FormattedClassification[] expectedClassifications = null)
    {
        return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.RemarksDocumentationComments, expectedClassifications);
    }

    protected static Action<QuickInfoItem> Returns(
        string expectedText,
        FormattedClassification[] expectedClassifications = null)
    {
        return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.ReturnsDocumentationComments, expectedClassifications);
    }

    protected static Action<QuickInfoItem> Value(
        string expectedText,
        FormattedClassification[] expectedClassifications = null)
    {
        return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.ValueDocumentationComments, expectedClassifications);
    }

    protected static Action<QuickInfoItem> TypeParameterMap(
        string expectedText,
        FormattedClassification[] expectedClassifications = null)
    {
        return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.TypeParameters, expectedClassifications);
    }

    protected static Action<QuickInfoItem> AnonymousTypes(
        string expectedText,
        FormattedClassification[] expectedClassifications = null)
    {
        return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.AnonymousTypes, expectedClassifications);
    }

    protected static Action<QuickInfoItem> NullabilityAnalysis(
        string expectedText,
        FormattedClassification[] expectedClassifications = null)
    {
        return item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.NullabilityAnalysis, expectedClassifications);
    }

    protected static Action<QuickInfoItem> NoTypeParameterMap
        => item => AssertSection(string.Empty, item.Sections, QuickInfoSectionKinds.TypeParameters);

    protected static Action<QuickInfoItem> Usage(string expectedText, bool expectsWarningGlyph = false)
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

    protected static Action<QuickInfoItem> Exceptions(string expectedText)
        => item => AssertSection(expectedText, item.Sections, QuickInfoSectionKinds.Exception);

    protected static Action<QuickInfoItem> Captures(string capturesText)
        => item => AssertSection(capturesText, item.Sections, QuickInfoSectionKinds.Captures);

    protected static async Task<bool> CanUseSpeculativeSemanticModelAsync(Document document, int position)
    {
        var service = document.GetLanguageService<ISyntaxFactsService>();
        var node = (await document.GetSyntaxRootAsync()).FindToken(position).Parent;

        return !service.GetMemberBodySpanForSpeculativeBinding(node).IsEmpty;
    }

    protected abstract Task TestAsync(string markup, params Action<QuickInfoItem>[] expectedResults);
}
