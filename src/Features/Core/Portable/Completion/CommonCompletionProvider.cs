// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    internal abstract class CommonCompletionProvider : CompletionProvider
    {
        private static readonly CompletionItemRules s_suggestionItemRules = CompletionItemRules.Create(enterKeyRule: EnterKeyRule.Never);

        /// <summary>
        /// Language used to retrieve <see cref="CompletionOptions"/> from <see cref="OptionSet"/>.
        /// Null for language agnostic values.
        /// </summary>
        internal abstract string Language { get; }

        /// <summary>
        /// For backwards API compat only, should not be called.
        /// </summary>
        public sealed override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {
            Debug.Fail("For backwards API compat only, should not be called");

            // Publicly available options do not affect this API.
            return ShouldTriggerCompletionImpl(text, caretPosition, trigger, CompletionOptions.Default);
        }

        internal override bool ShouldTriggerCompletion(HostLanguageServices languageServices, SourceText text, int caretPosition, CompletionTrigger trigger, CompletionOptions options, OptionSet passThroughOptions)
            => ShouldTriggerCompletionImpl(text, caretPosition, trigger, options);

        private bool ShouldTriggerCompletionImpl(SourceText text, int caretPosition, CompletionTrigger trigger, in CompletionOptions options)
            => trigger.Kind == CompletionTriggerKind.Insertion &&
               caretPosition > 0 &&
               IsInsertionTrigger(text, insertedCharacterPosition: caretPosition - 1, options);

        public virtual bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, CompletionOptions options)
            => false;

        /// <summary>
        /// For backwards API compat only, should not be called.
        /// </summary>
        public sealed override Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            Debug.Fail("For backwards API compat only, should not be called");

            // Publicly available options do not affect this API.
            return GetDescriptionAsync(document, item, CompletionOptions.Default, SymbolDescriptionOptions.Default, cancellationToken);
        }

        internal override async Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        {
            // Get the actual description provided by whatever subclass we are.
            // Then, if we would commit text that could be expanded as a snippet, 
            // put that information in the description so that the user knows.
            var description = await GetDescriptionWorkerAsync(document, item, options, displayOptions, cancellationToken).ConfigureAwait(false);
            var parts = await TryAddSnippetInvocationPartAsync(document, item, description.TaggedParts, cancellationToken).ConfigureAwait(false);

            return description.WithTaggedParts(parts);
        }

        private async Task<ImmutableArray<TaggedText>> TryAddSnippetInvocationPartAsync(
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

        internal virtual Task<CompletionDescription> GetDescriptionWorkerAsync(
            Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
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
            => GetTextChangeAsync(selectedItem, ch, cancellationToken);

        protected virtual Task<TextChange?> GetTextChangeAsync(CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
            => SpecializedTasks.Default<TextChange?>();

        protected static CompletionItem CreateSuggestionModeItem(string? displayText, string? description)
        {
            return CommonCompletionItem.Create(
                displayText: displayText ?? string.Empty,
                displayTextSuffix: "",
                description: description == null ? default : description.ToSymbolDisplayParts(),
                rules: s_suggestionItemRules);
        }
    }
}
