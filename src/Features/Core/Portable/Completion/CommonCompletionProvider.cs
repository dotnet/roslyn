// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    internal abstract class CommonCompletionProvider : CompletionProvider
    {
        private static readonly CompletionItemRules s_suggestionItemRules = CompletionItemRules.Create(enterKeyRule: EnterKeyRule.Never);

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

        public virtual bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
            => false;

        public override async Task<CompletionDescription?> GetDescriptionAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            // Get the actual description provided by whatever subclass we are.
            // Then, if we would commit text that could be expanded as a snippet, 
            // put that information in the description so that the user knows.
            var description = await GetDescriptionWorkerAsync(document, item, cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// Computes, in parallel, <paramref name="getItemsWorker"/> against all <see
        /// cref="Solution.GetRelatedDocumentIds(DocumentId)"/> of <paramref name="document"/>.  The results of each
        /// call are then merged all together in the end, using <paramref name="comparer"/> to remove duplicates if any.
        /// </summary>
        protected static async Task<ImmutableArray<T>> ForkJoinItemsFromAllRelatedDocumentsAsync<T>(
            Document document,
            IEqualityComparer<T> comparer,
            Func<Document, Task<ImmutableArray<T>>> getItemsWorker,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<Task<ImmutableArray<T>>>.GetInstance(out var tasks);

            // Place the initial document as the first item in the queue.  This will give all the values returned by it
            // priority over any values computed from any of its related documents.
            tasks.Add(Task.Run(() => getItemsWorker(document), cancellationToken));

            foreach (var linkedDocumentId in document.GetLinkedDocumentIds())
                tasks.Add(Task.Run(() => getItemsWorker(document.Project.Solution.GetRequiredDocument(linkedDocumentId)), cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            var totalItems = new HashSet<T>(comparer);
            foreach (var task in tasks)
            {
                var items = await task.ConfigureAwait(false);
                totalItems.AddRange(items.NullToEmpty());
            }

            return totalItems.ToImmutableArray();
        }
    }
}
