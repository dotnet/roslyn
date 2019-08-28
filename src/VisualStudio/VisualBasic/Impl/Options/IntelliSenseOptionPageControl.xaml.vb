' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Windows
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Friend Class IntelliSenseOptionPageControl
        Inherits AbstractOptionPageControl

        Public Sub New(optionStore As OptionStore)
            MyBase.New(optionStore)
            InitializeComponent()

            BindToOption(Show_completion_list_after_a_character_is_typed, CompletionOptions.TriggerOnTypingLetters, LanguageNames.VisualBasic)
            Show_completion_list_after_a_character_is_deleted.IsChecked = Me.OptionStore.GetOption(
                CompletionOptions.TriggerOnDeletion, LanguageNames.VisualBasic) <> False

            BindToOption(Show_completion_item_filters, CompletionOptions.ShowCompletionItemFilters, LanguageNames.VisualBasic)
            BindToOption(Highlight_matching_portions_of_completion_list_items, CompletionOptions.HighlightMatchingPortionsOfCompletionListItems, LanguageNames.VisualBasic)

            BindToOption(Never_include_snippets, CompletionOptions.SnippetsBehavior, SnippetsRule.NeverInclude, LanguageNames.VisualBasic)
            BindToOption(Always_include_snippets, CompletionOptions.SnippetsBehavior, SnippetsRule.AlwaysInclude, LanguageNames.VisualBasic)
            BindToOption(Include_snippets_when_question_Tab_is_typed_after_an_identifier, CompletionOptions.SnippetsBehavior, SnippetsRule.IncludeAfterTypingIdentifierQuestionTab, LanguageNames.VisualBasic)

            BindToOption(Never_add_new_line_on_enter, CompletionOptions.EnterKeyBehavior, EnterKeyRule.Never, LanguageNames.VisualBasic)
            BindToOption(Only_add_new_line_on_enter_with_whole_word, CompletionOptions.EnterKeyBehavior, EnterKeyRule.AfterFullyTypedWord, LanguageNames.VisualBasic)
            BindToOption(Always_add_new_line_on_enter, CompletionOptions.EnterKeyBehavior, EnterKeyRule.Always, LanguageNames.VisualBasic)

            Show_items_from_unimported_namespaces.IsChecked = Me.OptionStore.GetOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, LanguageNames.VisualBasic)
        End Sub

        Private Sub Show_completion_list_after_a_character_is_deleted_Checked(sender As Object, e As RoutedEventArgs)
            Me.OptionStore.SetOption(CompletionOptions.TriggerOnDeletion, LanguageNames.VisualBasic, value:=True)
        End Sub

        Private Sub Show_completion_list_after_a_character_is_deleted_Unchecked(sender As Object, e As RoutedEventArgs)
            Me.OptionStore.SetOption(CompletionOptions.TriggerOnDeletion, LanguageNames.VisualBasic, value:=False)
        End Sub

        Private Sub Show_items_from_unimported_namespaces_CheckedChanged(sender As Object, e As RoutedEventArgs)
            Show_items_from_unimported_namespaces.IsThreeState = False
            Me.OptionStore.SetOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, LanguageNames.VisualBasic, Show_items_from_unimported_namespaces.IsChecked)
        End Sub
    End Class
End Namespace
