// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    internal partial class IntelliSenseOptionPageControl : AbstractOptionPageControl
    {
        public IntelliSenseOptionPageControl(OptionStore optionStore) : base(optionStore)
        {
            InitializeComponent();

            BindToOption(Automatically_complete_statement_on_semicolon, CompleteStatementOptionsStorage.AutomaticallyCompleteStatementOnSemicolon);

            BindToOption(Show_completion_item_filters, CompletionViewOptionsStorage.ShowCompletionItemFilters, LanguageNames.CSharp);
            BindToOption(Highlight_matching_portions_of_completion_list_items, CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems, LanguageNames.CSharp);

            BindToOption(Show_completion_list_after_a_character_is_typed, CompletionOptionsStorage.TriggerOnTypingLetters, LanguageNames.CSharp);
            Show_completion_list_after_a_character_is_deleted.IsChecked = this.OptionStore.GetOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp) == true;
            Show_completion_list_after_a_character_is_deleted.IsEnabled = Show_completion_list_after_a_character_is_typed.IsChecked == true;
            AddSearchHandler(Show_completion_list_after_a_character_is_deleted);

            BindToOption(Never_include_snippets, CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.NeverInclude, LanguageNames.CSharp);
            BindToOption(Always_include_snippets, CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.AlwaysInclude, LanguageNames.CSharp);
            BindToOption(Include_snippets_when_question_Tab_is_typed_after_an_identifier, CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.IncludeAfterTypingIdentifierQuestionTab, LanguageNames.CSharp);

            BindToOption(Never_add_new_line_on_enter, CompletionOptionsStorage.EnterKeyBehavior, EnterKeyRule.Never, LanguageNames.CSharp);
            BindToOption(Only_add_new_line_on_enter_with_whole_word, CompletionOptionsStorage.EnterKeyBehavior, EnterKeyRule.AfterFullyTypedWord, LanguageNames.CSharp);
            BindToOption(Always_add_new_line_on_enter, CompletionOptionsStorage.EnterKeyBehavior, EnterKeyRule.Always, LanguageNames.CSharp);

            BindToOption(Show_name_suggestions, CompletionOptionsStorage.ShowNameSuggestions, LanguageNames.CSharp);
            BindToOption(Automatically_show_completion_list_in_argument_lists, CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp);

            Show_items_from_unimported_namespaces.IsChecked = this.OptionStore.GetOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp);
            AddSearchHandler(Show_items_from_unimported_namespaces);

            Tab_twice_to_insert_arguments.IsChecked = this.OptionStore.GetOption(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, LanguageNames.CSharp);
            AddSearchHandler(Tab_twice_to_insert_arguments);

            Show_new_snippet_experience.IsChecked = this.OptionStore.GetOption(CompletionOptionsStorage.ShowNewSnippetExperienceUserOption, LanguageNames.CSharp);
            AddSearchHandler(Show_new_snippet_experience);
        }

        private void Show_completion_list_after_a_character_is_typed_Checked(object sender, RoutedEventArgs e)
            => Show_completion_list_after_a_character_is_deleted.IsEnabled = Show_completion_list_after_a_character_is_typed.IsChecked == true;

        private void Show_completion_list_after_a_character_is_typed_Unchecked(object sender, RoutedEventArgs e)
        {
            Show_completion_list_after_a_character_is_deleted.IsEnabled = Show_completion_list_after_a_character_is_typed.IsChecked == true;
            Show_completion_list_after_a_character_is_deleted.IsChecked = false;
            Show_completion_list_after_a_character_is_deleted_Unchecked(sender, e);
        }

        private void Show_completion_list_after_a_character_is_deleted_Checked(object sender, RoutedEventArgs e)
            => this.OptionStore.SetOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, value: true);

        private void Show_completion_list_after_a_character_is_deleted_Unchecked(object sender, RoutedEventArgs e)
            => this.OptionStore.SetOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, value: false);

        private void Show_items_from_unimported_namespaces_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Show_items_from_unimported_namespaces.IsThreeState = false;
            this.OptionStore.SetOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, value: Show_items_from_unimported_namespaces.IsChecked);
        }

        private void Tab_twice_to_insert_arguments_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Tab_twice_to_insert_arguments.IsThreeState = false;
            this.OptionStore.SetOption(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, LanguageNames.CSharp, value: Tab_twice_to_insert_arguments.IsChecked);
        }

        private void Show_new_snippet_experience_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Show_new_snippet_experience.IsThreeState = false;
            this.OptionStore.SetOption(CompletionOptionsStorage.ShowNewSnippetExperienceUserOption, LanguageNames.CSharp, value: Show_new_snippet_experience.IsChecked);
        }
    }
}
