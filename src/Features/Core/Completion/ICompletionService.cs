// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Completion.Rules;
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
        IEnumerable<ICompletionProvider> GetDefaultCompletionProviders();

        /// <summary>
        /// Returns the default set of completion rules for this completion service.
        /// </summary>
        ICompletionRules GetDefaultCompletionRules();

        /// <summary>
        /// Returns the CompletionItemGroups for the specified position in the document.
        /// </summary>
        Task<IEnumerable<CompletionItemGroup>> GetGroupsAsync(Document document, int position, CompletionTriggerInfo triggerInfo, IEnumerable<ICompletionProvider> completionProviders, CancellationToken cancellationToken);

        /// <summary>
        /// Returns true if the character at the specific position in the document should trigger
        /// completion.
        /// </summary>   
        bool IsTriggerCharacter(SourceText text, int characterPosition, IEnumerable<ICompletionProvider> completionProviders, OptionSet optionSet);

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
        /// Called when a completion item is committed.  The completion service can then use this to
        /// help inform future completion requests.  For example, it can use this information to 
        /// help decide if this item is 'better' than another when queried.
        /// </summary>
        void CompletionItemComitted(CompletionItem item);

        /// <summary>
        /// Returns true if 'item1' is a better match for the filter text typed so far versus 
        /// 'item2'.  The better match will be selected after text is typed.
        /// </summary>
        bool IsBetterFilterMatch(CompletionItem item1, CompletionItem item2);

        /// <summary>
        /// Used to allow the completion service to indicate if this item should be preselected.
        /// Only called when no filter text has been provided.
        /// </summary>
        bool ShouldPreselect(CompletionItem item);

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
