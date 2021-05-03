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
            get { return GetBooleanOption(CompletionOptions.TriggerOnTypingLetters2); }
            set { SetBooleanOption(CompletionOptions.TriggerOnTypingLetters2, value); }
        }

        public int HighlightMatchingPortionsOfCompletionListItems
        {
            get { return GetBooleanOption(CompletionOptions.HighlightMatchingPortionsOfCompletionListItems); }
            set { SetBooleanOption(CompletionOptions.HighlightMatchingPortionsOfCompletionListItems, value); }
        }

        public int ShowCompletionItemFilters
        {
            get { return GetBooleanOption(CompletionOptions.ShowCompletionItemFilters); }
            set { SetBooleanOption(CompletionOptions.ShowCompletionItemFilters, value); }
        }

        public int ShowItemsFromUnimportedNamespaces
        {
            get { return GetBooleanOption(CompletionOptions.ShowItemsFromUnimportedNamespaces); }
            set { SetBooleanOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, value); }
        }

        public int InsertNewlineOnEnterWithWholeWord
        {
            get { return (int)GetOption(CompletionOptions.EnterKeyBehavior); }
            set { SetOption(CompletionOptions.EnterKeyBehavior, (EnterKeyRule)value); }
        }

        public int EnterKeyBehavior
        {
            get { return (int)GetOption(CompletionOptions.EnterKeyBehavior); }
            set { SetOption(CompletionOptions.EnterKeyBehavior, (EnterKeyRule)value); }
        }

        public int SnippetsBehavior
        {
            get { return (int)GetOption(CompletionOptions.SnippetsBehavior); }
            set { SetOption(CompletionOptions.SnippetsBehavior, (SnippetsRule)value); }
        }

        public int TriggerInArgumentLists
        {
            get { return GetBooleanOption(CompletionOptions.TriggerInArgumentLists); }
            set { SetBooleanOption(CompletionOptions.TriggerInArgumentLists, value); }
        }

        public int EnableArgumentCompletionSnippets
        {
            get { return GetBooleanOption(CompletionOptions.EnableArgumentCompletionSnippets); }
            set { SetBooleanOption(CompletionOptions.EnableArgumentCompletionSnippets, value); }
        }
    }
}
