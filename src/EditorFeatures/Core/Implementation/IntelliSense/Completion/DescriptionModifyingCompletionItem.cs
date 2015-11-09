// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
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

        public override Task<ImmutableArray<SymbolDisplayPart>> GetDescriptionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var descriptionTask = CompletionItem.GetDescriptionAsync(cancellationToken);

            var updatedDescriptionTask = descriptionTask.SafeContinueWithFromAsync(async task =>
                {
                    var parts = task.Result;
                    var note = await _completionService.GetSnippetExpansionNoteForCompletionItemAsync(CompletionItem, _workspace).ConfigureAwait(false);

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
