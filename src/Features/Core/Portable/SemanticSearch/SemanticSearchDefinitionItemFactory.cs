// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal static class SemanticSearchDefinitionItemFactory
{
    private static readonly FindReferencesSearchOptions s_findReferencesSearchOptions = new()
    {
        DisplayAllDefinitions = true,
    };

    public static DefinitionItem Create(string text)
    {
        var displayStr = Clip(text, maxLength: 100);
        var displayText = new TaggedText(TextTags.Text, displayStr);

        return DefinitionItem.CreateNonNavigableItem(
            tags: [],
            displayParts: text.Length == displayStr.Length ? [displayText] : [displayText, new TaggedText(TextTags.Punctuation, "…")]);
    }

    public static ValueTask<DefinitionItem> CreateAsync(Solution solution, ISymbol symbol, OptionsProvider<ClassificationOptions> classificationOptions, CancellationToken cancellationToken)
        => symbol.ToClassifiedDefinitionItemAsync(
            classificationOptions, solution, s_findReferencesSearchOptions, isPrimary: true, includeHiddenLocations: false, cancellationToken);

    public static ValueTask<DefinitionItem> CreateAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        => CreateItemAsync(document, node.FullSpan, cancellationToken);

    public static async ValueTask<DefinitionItem?> CreateAsync(Solution solution, Location location, CancellationToken cancellationToken)
    {
        if (location.MetadataModule is { } module)
        {
            var metadataLocation = DefinitionItemFactory.GetMetadataLocation(module.ContainingAssembly, solution, out var originatingProjectId);

            return DefinitionItem.Create(
                tags: [],
                displayParts: [],
                sourceSpans: [],
                classifiedSpans: [],
                metadataLocations: [metadataLocation],
                properties: ImmutableDictionary<string, string>.Empty.WithMetadataSymbolProperties(module.ContainingAssembly, originatingProjectId));
        }

        if (solution.GetDocument(location.SourceTree) is { } document)
        {
            return await CreateItemAsync(document, location.SourceSpan, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static async ValueTask<DefinitionItem> CreateItemAsync(Document document, TextSpan span, CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var displaySpan = Clip(span, maxLength: 100);
        var displayText = new TaggedText(TextTags.Text, text.ToString(displaySpan));

        return DefinitionItem.Create(
            tags: [],
            displayParts: displaySpan.Length == span.Length ? [displayText] : [displayText, new TaggedText(TextTags.Punctuation, "…")],
            sourceSpans: [new DocumentSpan(document, span)],
            classifiedSpans: [],
            metadataLocations: []);
    }

    private static TextSpan Clip(TextSpan span, int maxLength)
        => new(span.Start, Math.Min(span.Length, maxLength));

    private static string Clip(string str, int maxLength)
        => str[0..Math.Min(str.Length, maxLength)];
}
