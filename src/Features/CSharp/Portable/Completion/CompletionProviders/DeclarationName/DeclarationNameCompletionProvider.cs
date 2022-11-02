// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers.DeclarationName;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(DeclarationNameCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(TupleNameCompletionProvider))]
    [Shared]
    internal partial class DeclarationNameCompletionProvider : LSPCompletionProvider
    {
        private ImmutableArray<DeclarationNameRecommender> Recommenders { get; }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DeclarationNameCompletionProvider()
        {
            Recommenders = ImmutableArray.Create(new DeclarationNameRecommender());
        }

        internal override string Language => LanguageNames.CSharp;

        public override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, CompletionOptions options)
            => CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, insertedCharacterPosition, options);

        public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.SpaceTriggerCharacter;

        public override async Task ProvideCompletionsAsync(CompletionContext completionContext)
        {
            try
            {
                var position = completionContext.Position;
                var document = completionContext.Document;
                var cancellationToken = completionContext.CancellationToken;

                if (!completionContext.CompletionOptions.ShowNameSuggestions)
                {
                    return;
                }

                var context = (CSharpSyntaxContext)await completionContext.GetSyntaxContextWithExistingSpeculativeModelAsync(document, cancellationToken).ConfigureAwait(false);
                if (context.IsInNonUserCode)
                {
                    return;
                }

                // Do not show name suggestions for unbound "async" or "yield" identifier.
                // Most likely user is using it as keyword, so name suggestion will just interfere them
                if (context.TargetToken.IsKindOrHasMatchingText(SyntaxKind.AsyncKeyword) ||
                    context.TargetToken.IsKindOrHasMatchingText(SyntaxKind.YieldKeyword))
                {
                    if (context.SemanticModel.GetSymbolInfo(context.TargetToken).GetAnySymbol() is null)
                    {
                        return;
                    }
                }

                var nameInfo = await NameDeclarationInfo.GetDeclarationInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
                using var _ = ArrayBuilder<(string name, Glyph glyph)>.GetInstance(out var result);

                foreach (var recommender in Recommenders)
                {
                    var names = await recommender.ProvideRecommendedNamesAsync(completionContext, document, context, nameInfo, cancellationToken).ConfigureAwait(false);
                    result.AddRange(names);
                }

                if (result.Count == 0)
                    return;

                var sortValue = 0;
                foreach (var (name, glyph) in result)
                {
                    // We've produced items in the desired order, add a sort text to each item to prevent alphabetization
                    completionContext.AddItem(CreateCompletionItem(name, glyph, sortValue.ToString("D8")));
                    sortValue++;
                }

                completionContext.SuggestionModeItem = CommonCompletionItem.Create(
                    CSharpFeaturesResources.Name, displayTextSuffix: "", CompletionItemRules.Default);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
            {
                // nop
            }
        }

        private static CompletionItem CreateCompletionItem(string name, Glyph glyph, string sortText)
        {
            return CommonCompletionItem.Create(
                name,
                displayTextSuffix: "",
                CompletionItemRules.Default,
                glyph: glyph,
                sortText: sortText,
                description: CSharpFeaturesResources.Suggested_name.ToSymbolDisplayParts());
        }
    }
}
