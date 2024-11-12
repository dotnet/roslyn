// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    internal partial class IntelliSenseOptionPageControl : AbstractOptionPageControl
    {
        public IntelliSenseOptionPageControl(OptionStore optionStore) : base(optionStore)
        {
            InitializeComponent();

            BindToOption(Show_completion_list_after_a_character_is_typed, CompletionOptionsStorage.TriggerOnTypingLetters, LanguageNames.CSharp);
            BindToOption(Show_completion_list_after_a_character_is_deleted, CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, onNullValue: static () => false);
            Show_completion_list_after_a_character_is_deleted.IsEnabled = Show_completion_list_after_a_character_is_typed.IsChecked == true;

            BindToOption(Automatically_show_completion_list_in_argument_lists, CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp);
            BindToOption(Highlight_matching_portions_of_completion_list_items, CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems, LanguageNames.CSharp);
            BindToOption(Show_completion_item_filters, CompletionViewOptionsStorage.ShowCompletionItemFilters, LanguageNames.CSharp);

            BindToOption(Automatically_complete_statement_on_semicolon, CompleteStatementOptionsStorage.AutomaticallyCompleteStatementOnSemicolon);
            BindToOption(Never_include_snippets, CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.NeverInclude, LanguageNames.CSharp);
            BindToOption(Always_include_snippets, CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.AlwaysInclude, LanguageNames.CSharp);
            BindToOption(Include_snippets_when_question_Tab_is_typed_after_an_identifier, CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.IncludeAfterTypingIdentifierQuestionTab, LanguageNames.CSharp);
            SetSnippetsDefaultBehavior();

            BindToOption(Never_add_new_line_on_enter, CompletionOptionsStorage.EnterKeyBehavior, EnterKeyRule.Never, LanguageNames.CSharp);
            BindToOption(Only_add_new_line_on_enter_with_whole_word, CompletionOptionsStorage.EnterKeyBehavior, EnterKeyRule.AfterFullyTypedWord, LanguageNames.CSharp);
            BindToOption(Always_add_new_line_on_enter, CompletionOptionsStorage.EnterKeyBehavior, EnterKeyRule.Always, LanguageNames.CSharp);
            SetEnterKeyDefaultBehavior();

            BindToOption(Show_name_suggestions, CompletionOptionsStorage.ShowNameSuggestions, LanguageNames.CSharp);

            BindToOption(Show_items_from_unimported_namespaces, CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, onNullValue: static () => true);

            BindToOption(Tab_twice_to_insert_arguments, CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, LanguageNames.CSharp, onNullValue: static () => false);
            BindToOption(Show_new_snippet_experience, CompletionOptionsStorage.ShowNewSnippetExperienceUserOption, LanguageNames.CSharp,
                onNullValue: () => this.OptionStore.GetOption(CompletionOptionsStorage.ShowNewSnippetExperienceFeatureFlag));
        }

        private void SetSnippetsDefaultBehavior()
        {
            var snippetValue = this.OptionStore.GetOption(CompletionOptionsStorage.SnippetsBehavior, LanguageNames.CSharp);
            if (snippetValue == SnippetsRule.Default)
            {
                this.Always_include_snippets.IsChecked = true;
            }
        }

        private void SetEnterKeyDefaultBehavior()
        {
            var enterKeyBehavior = this.OptionStore.GetOption(CompletionOptionsStorage.EnterKeyBehavior, LanguageNames.CSharp);
            if (enterKeyBehavior == EnterKeyRule.Default)
            {
                this.Never_add_new_line_on_enter.IsChecked = true;
            }
        }

        private void Show_completion_list_after_a_character_is_typed_Checked(object sender, RoutedEventArgs e)
            => Show_completion_list_after_a_character_is_deleted.IsEnabled = Show_completion_list_after_a_character_is_typed.IsChecked == true;

        private void Show_completion_list_after_a_character_is_typed_Unchecked(object sender, RoutedEventArgs e)
        {
            Show_completion_list_after_a_character_is_deleted.IsEnabled = Show_completion_list_after_a_character_is_typed.IsChecked == true;
            Show_completion_list_after_a_character_is_deleted.IsChecked = false;
        }
    }
}
