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
            get { return GetBooleanOption(CompletionOptionsStorage.TriggerOnTypingLetters); }
            set { SetBooleanOption(CompletionOptionsStorage.TriggerOnTypingLetters, value); }
        }

        public int HighlightMatchingPortionsOfCompletionListItems
        {
            get { return GetBooleanOption(CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems); }
            set { SetBooleanOption(CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems, value); }
        }

        public int ShowCompletionItemFilters
        {
            get { return GetBooleanOption(CompletionViewOptionsStorage.ShowCompletionItemFilters); }
            set { SetBooleanOption(CompletionViewOptionsStorage.ShowCompletionItemFilters, value); }
        }

        public int ShowItemsFromUnimportedNamespaces
        {
            get { return GetBooleanOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces); }
            set { SetBooleanOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, value); }
        }

        public int InsertNewlineOnEnterWithWholeWord
        {
            get { return (int)GetOption(CompletionOptionsStorage.EnterKeyBehavior); }
            set { SetOption(CompletionOptionsStorage.EnterKeyBehavior, (EnterKeyRule)value); }
        }

        public int EnterKeyBehavior
        {
            get { return (int)GetOption(CompletionOptionsStorage.EnterKeyBehavior); }
            set { SetOption(CompletionOptionsStorage.EnterKeyBehavior, (EnterKeyRule)value); }
        }

        public int SnippetsBehavior
        {
            get { return (int)GetOption(CompletionOptionsStorage.SnippetsBehavior); }
            set { SetOption(CompletionOptionsStorage.SnippetsBehavior, (SnippetsRule)value); }
        }

        public int TriggerInArgumentLists
        {
            get { return GetBooleanOption(CompletionOptionsStorage.TriggerInArgumentLists); }
            set { SetBooleanOption(CompletionOptionsStorage.TriggerInArgumentLists, value); }
        }

        public int EnableArgumentCompletionSnippets
        {
            get { return GetBooleanOption(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets); }
            set { SetBooleanOption(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, value); }
        }
    }
}
