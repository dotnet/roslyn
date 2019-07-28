// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    internal abstract class CommonCompletionProvider : CompletionProvider
    {
        public override bool ShouldTriggerCompletion(SourceText text, int position, CompletionTrigger trigger, OptionSet options)
        {
            switch (trigger.Kind)
            {
                case CompletionTriggerKind.Insertion when position > 0:
                    var insertedCharacterPosition = position - 1;
                    return IsInsertionTrigger(text, insertedCharacterPosition, options);

                default:
                    return false;
            }
        }

        internal virtual bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
        {
            return false;
        }

        public sealed override async Task<CompletionDescription> GetDescriptionAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            // Get the actual description provided by whatever subclass we are.
            // Then, if we would commit text that could be expanded as a snippet, 
            // put that information in the description so that the user knows.
            var description = await GetDescriptionWorkerAsync(document, item, cancellationToken).ConfigureAwait(false);
            var parts = await TryAddSnippetInvocationPart(document, item, description.TaggedParts, cancellationToken).ConfigureAwait(false);

            return description.WithTaggedParts(parts);
        }

        private async Task<ImmutableArray<TaggedText>> TryAddSnippetInvocationPart(
            Document document, CompletionItem item,
            ImmutableArray<TaggedText> parts, CancellationToken cancellationToken)
        {
            var languageServices = document.Project.LanguageServices;
            var snippetService = languageServices.GetService<ISnippetInfoService>();
            if (snippetService != null)
            {
                var change = await GetTextChangeAsync(document, item, ch: '\t', cancellationToken: cancellationToken).ConfigureAwait(false) ??
                    new TextChange(item.Span, item.DisplayText);
                var insertionText = change.NewText;

                if (snippetService != null && snippetService.SnippetShortcutExists_NonBlocking(insertionText))
                {
                    var note = string.Format(FeaturesResources.Note_colon_Tab_twice_to_insert_the_0_snippet, insertionText);

                    if (parts.Any())
                    {
                        parts = parts.Add(new TaggedText(TextTags.LineBreak, Environment.NewLine));
                    }

                    parts = parts.Add(new TaggedText(TextTags.Text, note));
                }
            }

            return parts;
        }

        protected virtual Task<CompletionDescription> GetDescriptionWorkerAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            return CommonCompletionItem.HasDescription(item)
                ? Task.FromResult(CommonCompletionItem.GetDescription(item))
                : Task.FromResult(CompletionDescription.Empty);
        }


        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
        {
            var change = (await GetTextChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false))
                ?? new TextChange(item.Span, item.DisplayText);
            return CompletionChange.Create(change);
        }

        public virtual Task<TextChange?> GetTextChangeAsync(Document document, CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            return GetTextChangeAsync(selectedItem, ch, cancellationToken);
        }

        protected virtual Task<TextChange?> GetTextChangeAsync(CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            return Task.FromResult<TextChange?>(null);
        }

        private static readonly CompletionItemRules s_suggestionItemRules = CompletionItemRules.Create(enterKeyRule: EnterKeyRule.Never);

        protected CompletionItem CreateSuggestionModeItem(string displayText, string description)
        {
            return CommonCompletionItem.Create(
                displayText: displayText ?? string.Empty,
                displayTextSuffix: "",
                description: description != null ? description.ToSymbolDisplayParts() : default,
                rules: s_suggestionItemRules);
        }
    }
}
