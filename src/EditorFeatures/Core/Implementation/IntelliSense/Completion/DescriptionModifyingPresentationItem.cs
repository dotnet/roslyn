// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal class DescriptionModifyingPresentationItem : PresentationItem
    {
        public override CompletionItem Item { get; }
        public override bool IsSuggestionModeItem { get; }
        public override CompletionService CompletionService { get; }

        public DescriptionModifyingPresentationItem(CompletionItem item, CompletionService completionService, bool isSuggestionModeItem = false)
        {
            Debug.Assert(item != null);
            Debug.Assert(completionService != null);

            this.Item = item;
            this.CompletionService = completionService;
            this.IsSuggestionModeItem = isSuggestionModeItem;
        }

        public override async Task<CompletionDescription> GetDescriptionAsync(Document document, CancellationToken cancellationToken)
        {
            var languageServices = document.Project.LanguageServices;
            var snippetService = languageServices.GetService<ISnippetInfoService>();

            var description = await this.CompletionService.GetDescriptionAsync(document, this.Item, cancellationToken).ConfigureAwait(false);
            var parts = description.TaggedParts;

            var change = await GetTextChangeAsync(this.CompletionService, document, this.Item, '\t').ConfigureAwait(false);
            var insertionText = change.NewText;

            var note = string.Empty;
            if (snippetService != null && snippetService.SnippetShortcutExists_NonBlocking(insertionText))
            {
                note = string.Format(FeaturesResources.Note_colon_Tab_twice_to_insert_the_0_snippet, insertionText);
            }

            if (!string.IsNullOrEmpty(note))
            {
                if (parts.Any())
                {
                    parts = parts.Add(new TaggedText(TextTags.LineBreak, Environment.NewLine));
                }

                parts = parts.Add(new TaggedText(TextTags.Text, note));
            }

            return description.WithTaggedParts(parts);
        }

        /// <summary>
        /// Internal for testing purposes only.
        /// </summary>
        internal static async Task<TextChange> GetTextChangeAsync(
            CompletionService service, Document document, CompletionItem item,
            char? commitKey = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var change = await service.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);

            // normally the items that produce multiple changes are not expecting to trigger the behaviors that rely on looking at the text
            return change.TextChange;
        }
    }
}