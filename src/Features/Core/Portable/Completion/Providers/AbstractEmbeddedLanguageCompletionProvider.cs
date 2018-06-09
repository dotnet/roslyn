// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractEmbeddedLanguageCompletionProvider : CompletionProvider
    {
        private static readonly ConditionalWeakTable<CompletionItem, EmbeddedCompletionItem> s_itemMap =
            new ConditionalWeakTable<CompletionItem, EmbeddedCompletionItem>();

        private readonly IEmbeddedLanguageProvider _languageProvider;

        protected AbstractEmbeddedLanguageCompletionProvider(IEmbeddedLanguageProvider languageProvider)
        {
            _languageProvider = languageProvider;
        }

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {
            foreach (var language in _languageProvider.GetEmbeddedLanguages())
            {
                var completionProvider = language.CompletionProvider;
                if (completionProvider != null)
                {
                    if (completionProvider.ShouldTriggerCompletion(
                            text, caretPosition, Convert(trigger), options))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private EmbeddedCompletionTrigger Convert(CompletionTrigger trigger)
            => new EmbeddedCompletionTrigger((EmbeddedCompletionTriggerKind)trigger.Kind, trigger.Character);

        private static readonly CompletionItemRules s_rules = CompletionItemRules.Default.WithSelectionBehavior(CompletionItemSelectionBehavior.SoftSelection);

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            foreach (var language in _languageProvider.GetEmbeddedLanguages())
            {
                var completionProvider = language.CompletionProvider;
                if (completionProvider != null)
                {
                    var embeddedContext = new EmbeddedCompletionContext(
                        context.Document,
                        context.Position,
                        context.CompletionListSpan,
                        Convert(context.Trigger),
                        context.Options,
                        context.CancellationToken);
                    await completionProvider.ProvideCompletionsAsync(embeddedContext).ConfigureAwait(false);

                    if (embeddedContext.Items.Count > 0)
                    {
                        foreach (var embeddedItem in embeddedContext.Items)
                        {
                            var item = CompletionItem.Create(embeddedItem.DisplayText, rules: s_rules);

                            context.AddItem(item);
                            s_itemMap.Add(item, embeddedItem);
                        }

                        context.CompletionListSpan = embeddedContext.CompletionListSpan;
                        context.IsExclusive = true;
                        return;
                    }
                }
            }
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SpecializedTasks.Default<CompletionDescription>();

        public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
        {
            if (!s_itemMap.TryGetValue(item, out var embeddedItem))
            {
                return SpecializedTasks.Default<CompletionChange>();
            }

            return Task.FromResult(Convert(embeddedItem.Change));
        }

        private CompletionChange Convert(EmbeddedCompletionChange change)
            => CompletionChange.Create(change.TextChange, change.NewPosition, change.IncludesCommitCharacter);
    }
}
