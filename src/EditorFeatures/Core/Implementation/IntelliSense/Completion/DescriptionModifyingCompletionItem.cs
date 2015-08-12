// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Snippets;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    /// <summary>
    /// DescriptionModifyingCompletionItems are used to lazily update other types of completion 
    /// items just before they are presented. They can alter the description if necessary, which
    /// we do when the insertion text of a VB completion item matches a known snippet shortcut. 
    /// In this case, we add an extra note to the description so users know how to expand the 
    /// snippet.
    /// </summary>
    internal sealed class DescriptionModifyingCompletionItem : CompletionItem
    {
        private ICompletionService _completionService;
        private Workspace _workspace;

        public CompletionItem CompletionItem { get; }

        public DescriptionModifyingCompletionItem(CompletionItem completionItem, ICompletionService completionService, Workspace workspace)
            : base(default(CompletionListProvider),
                   completionItem.DisplayText,
                   completionItem.FilterSpan,
                   descriptionFactory: null,
                   glyph: completionItem.Glyph,
                   sortText: completionItem.SortText,
                   filterText: completionItem.FilterText,
                   preselect: completionItem.Preselect,
                   isBuilder: completionItem.IsBuilder,
                   showsWarningIcon: completionItem.ShowsWarningIcon,
                   shouldFormatOnCommit: completionItem.ShouldFormatOnCommit)
        {
            this.CompletionItem = completionItem;
            _completionService = completionService;
            _workspace = workspace;
        }

        public override CompletionListProvider CompletionProvider
        {
            get
            {
                Debug.Assert(false, "The CompletionProvider must be accessed through the underlying CompletionItem");
                return CompletionItem.CompletionProvider;
            }
        }

        /// <summary>
        /// Returns a string that should be added to the description of the given completion item
        /// if a snippet exists with a shortcut that matches the completion item's insertion text.
        /// </summary>
        private Task<string> GetSnippetExpansionNoteForCompletionItemAsync(CompletionItem completionItem)
        {
            var completionRules = _completionService.GetCompletionRules();
            var insertionText = completionRules.GetTextChange(completionItem, '\t').NewText;

            var snippetInfoService = _workspace.Services.GetLanguageServices(_completionService.LanguageName).GetService<ISnippetInfoService>();
            if (snippetInfoService != null && snippetInfoService.SnippetShortcutExists_NonBlocking(insertionText))
            {
                return Task.FromResult(string.Format(FeaturesResources.NoteTabTwiceToInsertTheSnippet, insertionText));
            }

            return SpecializedTasks.Default<string>();
        }

        public override Task<ImmutableArray<SymbolDisplayPart>> GetDescriptionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var descriptionTask = CompletionItem.GetDescriptionAsync(cancellationToken);

            var updatedDescriptionTask = descriptionTask.SafeContinueWithFromAsync(async task =>
                {
                    var parts = task.Result;
                    var note = await GetSnippetExpansionNoteForCompletionItemAsync(CompletionItem).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(note))
                    {
                        if (parts.Any())
                        {
                            parts = parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.LineBreak, null, Environment.NewLine));
                        }

                        parts = parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, note));
                    }

                    return parts;
                },
                cancellationToken,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);

            return updatedDescriptionTask;
        }
    }
}
