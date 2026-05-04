// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api;

internal abstract class PythiaCompletionProviderBase : CommonCompletionProvider, INotifyCommittingItemCompletionProvider
{
    public static CompletionItem CreateCommonCompletionItem(
        string displayText,
        string displayTextSuffix,
        CompletionItemRules rules,
        PythiaGlyph? glyph,
        ImmutableArray<SymbolDisplayPart> description,
        string sortText,
        string filterText,
        bool showsWarningIcon = false,
        ImmutableDictionary<string, string>? properties = null,
        ImmutableArray<string> tags = default,
        string? inlineDescription = null)
        => CommonCompletionItem.Create(displayText, displayTextSuffix, rules, (Glyph?)glyph, description, sortText, filterText, showsWarningIcon, properties.AsImmutableOrNull(), tags, inlineDescription);

    public static CompletionItem CreateSymbolCompletionItem(
        string displayText,
        IReadOnlyList<ISymbol> symbols,
        CompletionItemRules rules,
        int contextPosition,
        string? sortText = null,
        string? insertionText = null,
        string? filterText = null,
        SupportedPlatformData? supportedPlatforms = null,
        ImmutableDictionary<string, string>? properties = null,
        ImmutableArray<string> tags = default)
        => SymbolCompletionItem.CreateWithSymbolId(displayText, displayTextSuffix: null, [.. symbols], rules, contextPosition, sortText, insertionText,
            filterText, displayTextPrefix: null, inlineDescription: null, glyph: null, supportedPlatforms, properties.AsImmutableOrNull(), tags);

    public static ImmutableArray<SymbolDisplayPart> CreateRecommendedKeywordDisplayParts(string keyword, string toolTip)
        => RecommendedKeyword.CreateDisplayParts(keyword, toolTip);

    public static Task<CompletionDescription> GetDescriptionAsync(CompletionItem item, Document document, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        => SymbolCompletionItem.HasSymbols(item)
        ? SymbolCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken)
        : Task.FromResult(CommonCompletionItem.GetDescription(item));

    public static CompletionDescription GetDescription(CompletionItem item)
        => CommonCompletionItem.GetDescription(item);

    internal sealed override async Task<CompletionDescription> GetDescriptionWorkerAsync(
        Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
    {
        var description = await GetDescriptionAsync(item, document, displayOptions, cancellationToken).ConfigureAwait(false);
        return UpdateDescription(description);
    }

    protected virtual CompletionDescription UpdateDescription(CompletionDescription completionDescription)
        => completionDescription;

    public static bool TryGetInsertionText(CompletionItem item, [NotNullWhen(true)] out string? insertionText)
        => SymbolCompletionItem.TryGetInsertionText(item, out insertionText);

    public sealed override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, CompletionOptions options)
        => IsInsertionTriggerWorker(text, insertedCharacterPosition);

    protected virtual bool IsInsertionTriggerWorker(SourceText text, int insertedCharacterPosition)
        => text[insertedCharacterPosition] == '.';

    public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
        => base.GetChangeAsync(document, item, commitKey, cancellationToken);

    public virtual Task NotifyCommittingItemAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
