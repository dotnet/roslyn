// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
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
        private const string StartKey = nameof(StartKey);
        private const string LengthKey = nameof(LengthKey);
        private const string NewTextKey = nameof(NewTextKey);
        private const string NewPositionKey = nameof(NewPositionKey);
        private const string DescriptionKey = nameof(DescriptionKey);

        // Always soft-select these completion items.  Also, never filter down.
        private static readonly CompletionItemRules s_rules =
            CompletionItemRules.Default.WithSelectionBehavior(CompletionItemSelectionBehavior.SoftSelection)
                                       .WithFilterCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, new char[] { }));                                    


        private readonly IEmbeddedLanguagesProvider _languagesProvider;

        protected AbstractEmbeddedLanguageCompletionProvider(IEmbeddedLanguagesProvider languagesProvider)
        {
            _languagesProvider = languagesProvider;
        }

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {
            if (_languagesProvider != null)
            {
                foreach (var language in _languagesProvider.GetEmbeddedLanguages())
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
            }

            return false;
        }

        private EmbeddedCompletionTrigger Convert(CompletionTrigger trigger)
            => new EmbeddedCompletionTrigger((EmbeddedCompletionTriggerKind)trigger.Kind, trigger.Character);

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            if (_languagesProvider != null)
            {
                foreach (var language in _languagesProvider.GetEmbeddedLanguages())
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
                            var index = 0;
                            foreach (var embeddedItem in embeddedContext.Items)
                            {
                                var change = embeddedItem.Change;
                                var textChange = change.TextChange;

                                var properties = ImmutableDictionary.CreateBuilder<string, string>();
                                properties.Add(StartKey, textChange.Span.Start.ToString());
                                properties.Add(LengthKey, textChange.Span.Length.ToString());
                                properties.Add(NewTextKey, textChange.NewText);
                                properties.Add(DescriptionKey, embeddedItem.Description);

                                if (change.NewPosition != null)
                                {
                                    properties.Add(NewPositionKey, change.NewPosition.ToString());
                                }

                                // Keep everything sorted in the order the underlying embedded
                                // language provided it.
                                var sortText = index.ToString("0000");

                                var item = CompletionItem.Create(
                                    embeddedItem.DisplayText,
                                    sortText: sortText,
                                    properties: properties.ToImmutable(),
                                    rules: s_rules);

                                context.AddItem(item);
                                index++;
                            }

                            context.CompletionListSpan = embeddedContext.CompletionListSpan;
                            context.IsExclusive = true;
                            return;
                        }
                    }
                }
            }
        }

        public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
        {
            if (!item.Properties.TryGetValue(StartKey, out var startString) ||
                !item.Properties.TryGetValue(LengthKey, out var lengthString) ||
                !item.Properties.TryGetValue(NewTextKey, out var newText))
            {
                return SpecializedTasks.Default<CompletionChange>();
            }

            item.Properties.TryGetValue(NewPositionKey, out var newPositionString);

            return Task.FromResult(CompletionChange.Create(
                new TextChange(new TextSpan(int.Parse(startString), int.Parse(lengthString)), newText),
                newPositionString == null ? default(int?) : int.Parse(newPositionString)));
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            if (!item.Properties.TryGetValue(DescriptionKey, out var description))
            {
                return SpecializedTasks.Default<CompletionDescription>();
            }

            return Task.FromResult(CompletionDescription.Create(
                ImmutableArray.Create(new TaggedText(TextTags.Text, description))));
        }

        private CompletionChange Convert(EmbeddedCompletionChange change)
            => CompletionChange.Create(change.TextChange, change.NewPosition);
    }
}
