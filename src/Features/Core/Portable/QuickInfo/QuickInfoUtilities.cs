// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.QuickInfo;

internal static class QuickInfoUtilities
{
    public static Task<QuickInfoItem> CreateQuickInfoItemAsync(SolutionServices services, SemanticModel semanticModel, TextSpan span, ImmutableArray<ISymbol> symbols, SymbolDescriptionOptions options, CancellationToken cancellationToken)
        => CreateQuickInfoItemAsync(services, semanticModel, span, symbols, supportedPlatforms: null, showAwaitReturn: false, nullabilityInfo: null, options, onTheFlyDocsInfo: null, cancellationToken);

    public static async Task<QuickInfoItem> CreateQuickInfoItemAsync(
        SolutionServices services,
        SemanticModel semanticModel,
        TextSpan span,
        ImmutableArray<ISymbol> symbols,
        SupportedPlatformData? supportedPlatforms,
        bool showAwaitReturn,
        string? nullabilityInfo,
        SymbolDescriptionOptions options,
        OnTheFlyDocsInfo? onTheFlyDocsInfo,
        CancellationToken cancellationToken)
    {
        var descriptionService = services.GetRequiredLanguageService<ISymbolDisplayService>(semanticModel.Language);
        var groups = await descriptionService.ToDescriptionGroupsAsync(semanticModel, span.Start, symbols, options, cancellationToken).ConfigureAwait(false);

        using var _1 = ArrayBuilder<QuickInfoSection>.GetInstance(out var sections);

        var symbol = symbols.First();
        if (showAwaitReturn)
        {
            // We show a special message if the Task being awaited has no return
            if (symbol is INamedTypeSymbol { SpecialType: SpecialType.System_Void })
            {
                var builder = ImmutableArray.CreateBuilder<TaggedText>();
                builder.AddText(FeaturesResources.Awaited_task_returns_no_value);
                AddSection(QuickInfoSectionKinds.Description, builder.ToImmutable());
                return QuickInfoItem.Create(span, sections: sections.ToImmutable());
            }

            if (TryGetGroupText(SymbolDescriptionGroups.MainDescription, out var mainDescriptionTaggedParts))
            {
                // We'll take the existing message and wrap it with a message saying this was returned from the task.
                var defaultSymbol = "{0}";
                var symbolIndex = FeaturesResources.Awaited_task_returns_0.IndexOf(defaultSymbol);

                var builder = ImmutableArray.CreateBuilder<TaggedText>();
                builder.AddText(FeaturesResources.Awaited_task_returns_0[..symbolIndex]);
                builder.AddRange(mainDescriptionTaggedParts);
                builder.AddText(FeaturesResources.Awaited_task_returns_0[(symbolIndex + defaultSymbol.Length)..]);

                AddSection(QuickInfoSectionKinds.Description, builder.ToImmutable());
            }
        }
        else if (TryGetGroupText(SymbolDescriptionGroups.MainDescription, out var mainDescriptionTaggedParts))
        {
            AddSection(QuickInfoSectionKinds.Description, mainDescriptionTaggedParts);
        }

        var hasDocumentationComments = false;
        ImmutableArray<TaggedText> docParts = default;
        if (TryCreateCharacterLiteralDocumentation(semanticModel, span, symbol, cancellationToken, out var characterLiteralDocumentation))
        {
            AddSection(QuickInfoSectionKinds.DocumentationComments, characterLiteralDocumentation);
            onTheFlyDocsInfo?.HasComments = true;
            hasDocumentationComments = true;
        }
        else if (groups.TryGetValue(SymbolDescriptionGroups.Documentation, out docParts) && !docParts.IsDefaultOrEmpty)
        {
            AddSection(QuickInfoSectionKinds.DocumentationComments, docParts);
            onTheFlyDocsInfo?.HasComments = true;
            hasDocumentationComments = true;
        }

        if (options.QuickInfoOptions.ShowRemarksInQuickInfo &&
            groups.TryGetValue(SymbolDescriptionGroups.RemarksDocumentation, out var remarksDocumentation) &&
            !remarksDocumentation.IsEmpty)
        {
            var builder = ImmutableArray.CreateBuilder<TaggedText>();
            if (hasDocumentationComments)
                builder.AddLineBreak();

            builder.AddRange(remarksDocumentation);
            AddSection(QuickInfoSectionKinds.RemarksDocumentationComments, builder.ToImmutable());
        }

        if (groups.TryGetValue(SymbolDescriptionGroups.ReturnsDocumentation, out var returnsDocumentation) &&
            !returnsDocumentation.IsDefaultOrEmpty)
        {
            var builder = ImmutableArray.CreateBuilder<TaggedText>();
            builder.AddLineBreak();
            builder.AddRange(returnsDocumentation);
            AddSection(QuickInfoSectionKinds.ReturnsDocumentationComments, builder.ToImmutable());
        }

        if (groups.TryGetValue(SymbolDescriptionGroups.ValueDocumentation, out var valueDocumentation) &&
            !valueDocumentation.IsDefaultOrEmpty)
        {
            var builder = ImmutableArray.CreateBuilder<TaggedText>();
            builder.AddLineBreak();
            builder.AddRange(valueDocumentation);
            AddSection(QuickInfoSectionKinds.ValueDocumentationComments, builder.ToImmutable());
        }

        if (TryGetGroupText(SymbolDescriptionGroups.TypeParameterMap, out var typeParameterMapText))
        {
            var builder = ImmutableArray.CreateBuilder<TaggedText>();
            builder.AddLineBreak();
            builder.AddRange(typeParameterMapText);
            AddSection(QuickInfoSectionKinds.TypeParameters, builder.ToImmutable());
        }

        if (TryGetGroupText(SymbolDescriptionGroups.StructuralTypes, out var anonymousTypesText))
        {
            var builder = ImmutableArray.CreateBuilder<TaggedText>();
            builder.AddLineBreak();
            builder.AddRange(anonymousTypesText);
            AddSection(QuickInfoSectionKinds.AnonymousTypes, builder.ToImmutable());
        }

        using var _ = ArrayBuilder<TaggedText>.GetInstance(out var usageTextBuilder);
        if (TryGetGroupText(SymbolDescriptionGroups.AwaitableUsageText, out var awaitableUsageText))
            usageTextBuilder.AddRange(awaitableUsageText);

        if (supportedPlatforms != null)
            usageTextBuilder.AddRange(supportedPlatforms.ToDisplayParts().ToTaggedText());

        if (usageTextBuilder.Count > 0)
            AddSection(QuickInfoSectionKinds.Usage, usageTextBuilder.ToImmutable());

        if (nullabilityInfo != null)
            AddSection(QuickInfoSectionKinds.NullabilityAnalysis, [new TaggedText(TextTags.Text, nullabilityInfo)]);

        if (TryGetGroupText(SymbolDescriptionGroups.Exceptions, out var exceptionsText))
            AddSection(QuickInfoSectionKinds.Exception, exceptionsText);

        if (TryGetGroupText(SymbolDescriptionGroups.Captures, out var capturesText))
            AddSection(QuickInfoSectionKinds.Captures, capturesText);

        var tags = ImmutableArray.CreateRange(GlyphTags.GetTags(symbol.GetGlyph()));
        if (supportedPlatforms?.HasValidAndInvalidProjects() == true)
            tags = tags.Add(WellKnownTags.Warning);

        return QuickInfoItem.Create(span, tags, sections.ToImmutable(), relatedSpans: default, onTheFlyDocsInfo);

        bool TryGetGroupText(SymbolDescriptionGroups group, out ImmutableArray<TaggedText> taggedParts)
            => groups.TryGetValue(group, out taggedParts) && !taggedParts.IsDefaultOrEmpty;

        void AddSection(string kind, ImmutableArray<TaggedText> taggedParts)
            => sections.Add(QuickInfoSection.Create(kind, taggedParts));
    }

    private static bool TryCreateCharacterLiteralDocumentation(
        SemanticModel semanticModel,
        TextSpan span,
        ISymbol symbol,
        CancellationToken cancellationToken,
        out ImmutableArray<TaggedText> documentation)
    {
        documentation = default;

        if (semanticModel.Language != LanguageNames.CSharp ||
            symbol is not INamedTypeSymbol { SpecialType: SpecialType.System_Char })
        {
            return false;
        }

        var token = semanticModel.SyntaxTree.GetRoot(cancellationToken).FindToken(span.Start);
        if (!token.Span.Contains(span))
            return false;

        if (token.Value is not char character)
            return false;

        if (!IsDisplayableCharacter(character))
            return false;

        if (!ContainsSupportedUnicodeEscape(token.Text))
            return false;

        documentation = ImmutableArray.Create(new TaggedText(TextTags.Text, CreateCharacterDocumentationText(character)));
        return true;
    }

    private static bool ContainsSupportedUnicodeEscape(string tokenText)
    {
        const int shortUnicodeEscapePrefixLength = 2;
        const int shortUnicodeEscapeHexDigitCount = 4;
        var maxStartIndex = tokenText.Length - 1;

        for (var i = 0; i < maxStartIndex; i++)
        {
            if (tokenText[i] != '\\')
                continue;

            if (tokenText[i + 1] == 'u' &&
                HasExactHexDigitSequence(tokenText, i + shortUnicodeEscapePrefixLength, count: shortUnicodeEscapeHexDigitCount))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasExactHexDigitSequence(string text, int start, int count)
    {
        if (start + count > text.Length)
            return false;

        for (var i = start; i < start + count; i++)
        {
            if (GetHexDigitValue(text[i]) < 0)
                return false;
        }

        if (start + count >= text.Length)
            return true;

        return GetHexDigitValue(text[start + count]) < 0;
    }

    private static int GetHexDigitValue(char c)
        => c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => -1,
        };

    private static bool IsDisplayableCharacter(char character)
        => !char.IsControl(character) && !char.IsSurrogate(character);

    private static string CreateCharacterDocumentationText(char character)
        => string.Format(FeaturesResources.Represents_the_character_0_as_a_UTF_16_code_unit, character);
}
