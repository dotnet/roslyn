// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    internal interface ICompletionService : ILanguageService
    {
        /// <summary>
        /// Returns the default set of completion providers for this completion service.
        /// </summary>
        IEnumerable<CompletionListProvider> GetDefaultCompletionProviders();

        /// <summary>
        /// Returns the set of completion rules for this completion service.
        /// </summary>
        CompletionRules GetCompletionRules();

        /// <summary>
        /// Clears the most-recently-used cache used by completion.
        /// </summary>
        void ClearMRUCache();

        /// <summary>
        /// Returns the <see cref="CompletionList"/> for the specified position in the document.
        /// </summary>
        Task<CompletionList> GetCompletionListAsync(
            Document document,
            int position,
            CompletionTriggerInfo triggerInfo,
            OptionSet options,
            IEnumerable<CompletionListProvider> providers,
            CancellationToken cancellationToken);

        /// <summary>
        /// Returns true if the character at the specific position in the document should trigger
        /// completion.
        /// </summary>   
        bool IsTriggerCharacter(SourceText text, int characterPosition, IEnumerable<CompletionListProvider> completionProviders, OptionSet optionSet);

        /// <summary>
        /// Get the default tracking span, based on the language, that providers are likely to use.
        /// </summary>
        Task<TextSpan> GetDefaultTrackingSpanAsync(Document document, int position, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a string that should be added to the description of the given completion item
        /// if a snippet exists with a shortcut that matches the completion item's insertion text.
        /// </summary>
        Task<string> GetSnippetExpansionNoteForCompletionItemAsync(CompletionItem completionItem, Workspace workspace);

        /// <summary>
        /// True if the completion list should be dismissed if the user's typing causes it to filter
        /// and display no items.
        /// </summary>
        bool DismissIfEmpty { get; }

        /// <summary>
        /// True if typing ?[tab] should try to show the list of available snippets.
        /// </summary>
        bool SupportSnippetCompletionListOnTab { get; }

        /// <summary>
        /// True if the list should be dismissed when the user deletes the last character in the filter span.
        /// </summary>
        bool DismissIfLastFilterCharacterDeleted { get; }
    }
}
