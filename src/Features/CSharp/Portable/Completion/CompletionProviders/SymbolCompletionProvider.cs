// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(SymbolCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(SpeculativeTCompletionProvider))]
    [Shared]
    internal partial class SymbolCompletionProvider : AbstractRecommendationServiceBasedCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SymbolCompletionProvider()
        {
        }

        protected override Task<ImmutableArray<ISymbol>> GetSymbolsAsync(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            return Recommender.GetImmutableRecommendedSymbolsAtPositionAsync(
                context.SemanticModel, position, context.Workspace, options, cancellationToken);
        }

        protected override async Task<bool> ShouldProvidePreselectedItemsAsync(CompletionContext completionContext, SyntaxContext syntaxContext, Document document, int position, Lazy<ImmutableArray<ITypeSymbol>> inferredTypes, OptionSet options)
        {
            var sourceText = await document.GetTextAsync(CancellationToken.None).ConfigureAwait(false);
            if (ShouldTriggerInArgumentLists(sourceText, options))
            {
                // Avoid preselection & hard selection when triggered via insertion in an argument list.
                // If an item is hard selected, then a user trying to type MethodCall() will get
                // MethodCall(someVariable) instead. We need only soft selected items to prevent this.
                if (completionContext.Trigger.Kind == CompletionTriggerKind.Insertion &&
                    position > 0 &&
                    await IsTriggerInArgumentListAsync(document, position - 1, CancellationToken.None).ConfigureAwait(false) == true)
                {
                    return false;
                }
            }

            return true;
        }

        protected override bool IsInstrinsic(ISymbol s)
            => s is ITypeSymbol ts && ts.IsIntrinsicType();

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            return ShouldTriggerInArgumentLists(text, options)
                ? CompletionUtilities.IsTriggerCharacterOrArgumentListCharacter(text, characterPosition, options)
                : CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        internal override async Task<bool> IsSyntacticTriggerCharacterAsync(Document document, int caretPosition, CompletionTrigger trigger, OptionSet options, CancellationToken cancellationToken)
        {
            if (trigger.Kind == CompletionTriggerKind.Insertion && caretPosition > 0)
            {
                var result = await IsTriggerOnDotAsync(document, caretPosition - 1, cancellationToken).ConfigureAwait(false);
                if (result.HasValue)
                {
                    return result.Value;
                }

                if (ShouldTriggerInArgumentLists(document.Project.Solution.Workspace, await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false)))
                {
                    result = await IsTriggerInArgumentListAsync(document, caretPosition - 1, cancellationToken).ConfigureAwait(false);
                    if (result.HasValue)
                    {
                        return result.Value;
                    }
                }
            }

            // By default we want to proceed with triggering completion if we have items.
            return true;
        }

        internal override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharactersWithArgumentList;

        protected override async Task<bool> IsSemanticTriggerCharacterAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        {
            var result = await IsTriggerOnDotAsync(document, characterPosition, cancellationToken).ConfigureAwait(false);
            if (result.HasValue)
            {
                return result.Value;
            }

            return true;
        }

        private bool ShouldTriggerInArgumentLists(SourceText text, OptionSet options)
            => Workspace.TryGetWorkspace(text.Container, out var workspace) &&
                ShouldTriggerInArgumentLists(workspace, options);

        private bool? _shouldTriggerCompletionInArgumentListsExperiment = null;

        private bool ShouldTriggerInArgumentLists(Workspace workspace, OptionSet options)
        {
            var isTriggerInArgumentListOptionEnabled = options.GetOption(CompletionOptions.TriggerInArgumentLists, LanguageNames.CSharp);
            if (isTriggerInArgumentListOptionEnabled != null)
            {
                return isTriggerInArgumentListOptionEnabled.Value;
            }

            if (_shouldTriggerCompletionInArgumentListsExperiment == null)
            {
                var experimentationService = workspace.Services.GetService<IExperimentationService>();
                _shouldTriggerCompletionInArgumentListsExperiment =
                    experimentationService.IsExperimentEnabled(WellKnownExperimentNames.TriggerCompletionInArgumentLists);
            }

            return _shouldTriggerCompletionInArgumentListsExperiment.Value;
        }

        private static async Task<bool?> IsTriggerOnDotAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (text[characterPosition] != '.')
            {
                return null;
            }

            // don't want to trigger after a number.  All other cases after dot are ok.
            var tree = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = tree.FindToken(characterPosition);
            if (token.Kind() == SyntaxKind.DotToken)
            {
                token = token.GetPreviousToken();
            }

            return token.Kind() != SyntaxKind.NumericLiteralToken;
        }

        /// <returns><see langword="null"/> if not an argument list character, otherwise whether the trigger is in an argument list.</returns>
        private static async Task<bool?> IsTriggerInArgumentListAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (!CompletionUtilities.IsArgumentListCharacter(text[characterPosition]))
            {
                return null;
            }

            var tree = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = tree.FindToken(characterPosition);

            if (!token.Parent.IsKind(SyntaxKind.ArgumentList, SyntaxKind.BracketedArgumentList, SyntaxKind.AttributeArgumentList, SyntaxKind.ArrayRankSpecifier))
            {
                return false;
            }

            // Be careful, e.g. if we're in a comment before the token
            if (token.Span.End > characterPosition + 1)
            {
                return false;
            }

            // Only allow spaces between the end of the token and the trigger character
            for (var i = token.Span.End; i < characterPosition; i++)
            {
                if (text[i] != ' ')
                {
                    return false;
                }
            }

            return true;
        }

        protected override async Task<SyntaxContext> CreateContextAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            return CSharpSyntaxContext.CreateContext(workspace, semanticModel, position, cancellationToken);
        }

        protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(ISymbol symbol, SyntaxContext context)
            => CompletionUtilities.GetDisplayAndSuffixAndInsertionText(symbol, context);

        protected override CompletionItemRules GetCompletionItemRules(List<ISymbol> symbols, SyntaxContext context, bool preselect)
        {
            cachedRules.TryGetValue(ValueTuple.Create(((CSharpSyntaxContext)context).IsLeftSideOfImportAliasDirective, preselect, context.IsPossibleTupleContext), out var rule);

            return rule ?? CompletionItemRules.Default;
        }

        private static readonly Dictionary<ValueTuple<bool, bool, bool>, CompletionItemRules> cachedRules = InitCachedRules();

        private static Dictionary<ValueTuple<bool, bool, bool>, CompletionItemRules> InitCachedRules()
        {
            var result = new Dictionary<ValueTuple<bool, bool, bool>, CompletionItemRules>();

            for (var importDirective = 0; importDirective < 2; importDirective++)
            {
                for (var preselect = 0; preselect < 2; preselect++)
                {
                    for (var tupleLiteral = 0; tupleLiteral < 2; tupleLiteral++)
                    {
                        if (importDirective == 1 && tupleLiteral == 1)
                        {
                            // this combination doesn't make sense, we can skip it
                            continue;
                        }

                        var context = ValueTuple.Create(importDirective == 1, preselect == 1, tupleLiteral == 1);
                        result[context] = MakeRule(importDirective, preselect, tupleLiteral);
                    }
                }
            }

            return result;
        }

        private static CompletionItemRules MakeRule(int importDirective, int preselect, int tupleLiteral)
            => MakeRule(importDirective == 1, preselect == 1, tupleLiteral == 1);

        private static CompletionItemRules MakeRule(bool importDirective, bool preselect, bool tupleLiteral)
        {
            // '<' should not filter the completion list, even though it's in generic items like IList<>
            var generalBaseline = CompletionItemRules.Default.
                WithFilterCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, '<')).
                WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Add, '<'));

            var importDirectiveBaseline = CompletionItemRules.Create(commitCharacterRules:
                ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, '.', ';')));

            var rule = importDirective ? importDirectiveBaseline : generalBaseline;

            if (preselect)
            {
                rule = rule.WithSelectionBehavior(CompletionItemSelectionBehavior.HardSelection);
            }

            if (tupleLiteral)
            {
                rule = rule
                    .WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ':'));
            }

            return rule;
        }

        protected override CompletionItemSelectionBehavior PreselectedItemSelectionBehavior => CompletionItemSelectionBehavior.HardSelection;
    }
}
