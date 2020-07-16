// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal abstract class PythiaCompletionProviderBase : CommonCompletionProvider
    {
        public static PerLanguageOption2<bool> HideAdvancedMembersOption => CompletionOptions.HideAdvancedMembers;

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
            => CommonCompletionItem.Create(displayText, displayTextSuffix, rules, (Glyph?)glyph, description, sortText, filterText, showsWarningIcon, properties, tags, inlineDescription);

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
            => SymbolCompletionItem.CreateWithSymbolId(displayText, displayTextSuffix: null, symbols, rules, contextPosition, sortText, insertionText, filterText, supportedPlatforms, properties, tags);

        public static ImmutableArray<SymbolDisplayPart> CreateRecommendedKeywordDisplayParts(string keyword, string toolTip)
            => RecommendedKeyword.CreateDisplayParts(keyword, toolTip);

        public static Task<CompletionDescription> GetDescriptionAsync(CompletionItem item, Document document, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

        public static CompletionDescription GetDescription(CompletionItem item)
            => CommonCompletionItem.GetDescription(item);

        public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
            => base.GetChangeAsync(document, item, commitKey, cancellationToken);
    }
}
