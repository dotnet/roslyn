// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int BringUpOnIdentifier
        {
            get { return GetBooleanOption(CompletionOptions.Metadata.TriggerOnTypingLetters); }
            set { SetBooleanOption(CompletionOptions.Metadata.TriggerOnTypingLetters, value); }
        }

        public int HighlightMatchingPortionsOfCompletionListItems
        {
            get { return GetBooleanOption(CompletionViewOptions.HighlightMatchingPortionsOfCompletionListItems); }
            set { SetBooleanOption(CompletionViewOptions.HighlightMatchingPortionsOfCompletionListItems, value); }
        }

        public int ShowCompletionItemFilters
        {
            get { return GetBooleanOption(CompletionViewOptions.ShowCompletionItemFilters); }
            set { SetBooleanOption(CompletionViewOptions.ShowCompletionItemFilters, value); }
        }

        public int ShowItemsFromUnimportedNamespaces
        {
            get { return GetBooleanOption(CompletionOptions.Metadata.ShowItemsFromUnimportedNamespaces); }
            set { SetBooleanOption(CompletionOptions.Metadata.ShowItemsFromUnimportedNamespaces, value); }
        }

        public int InsertNewlineOnEnterWithWholeWord
        {
            get { return (int)GetOption(CompletionOptions.Metadata.EnterKeyBehavior); }
            set { SetOption(CompletionOptions.Metadata.EnterKeyBehavior, (EnterKeyRule)value); }
        }

        public int EnterKeyBehavior
        {
            get { return (int)GetOption(CompletionOptions.Metadata.EnterKeyBehavior); }
            set { SetOption(CompletionOptions.Metadata.EnterKeyBehavior, (EnterKeyRule)value); }
        }

        public int SnippetsBehavior
        {
            get { return (int)GetOption(CompletionOptions.Metadata.SnippetsBehavior); }
            set { SetOption(CompletionOptions.Metadata.SnippetsBehavior, (SnippetsRule)value); }
        }

        public int TriggerInArgumentLists
        {
            get { return GetBooleanOption(CompletionOptions.Metadata.TriggerInArgumentLists); }
            set { SetBooleanOption(CompletionOptions.Metadata.TriggerInArgumentLists, value); }
        }

        public int EnableArgumentCompletionSnippets
        {
            get { return GetBooleanOption(CompletionViewOptions.EnableArgumentCompletionSnippets); }
            set { SetBooleanOption(CompletionViewOptions.EnableArgumentCompletionSnippets, value); }
        }
    }
}
