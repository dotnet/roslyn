' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

            BindToOption(Show_completion_list_after_a_character_is_typed, CompletionOptionsStorage.TriggerOnTypingLetters, LanguageNames.VisualBasic)
            Show_completion_list_after_a_character_is_deleted.IsChecked = Me.OptionStore.GetOption(
                CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.VisualBasic) <> False

            BindToOption(Show_completion_item_filters, CompletionViewOptionsStorage.ShowCompletionItemFilters, LanguageNames.VisualBasic)
            BindToOption(Highlight_matching_portions_of_completion_list_items, CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems, LanguageNames.VisualBasic)

            BindToOption(Never_include_snippets, CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.NeverInclude, LanguageNames.VisualBasic)
            BindToOption(Always_include_snippets, CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.AlwaysInclude, LanguageNames.VisualBasic)
            BindToOption(Include_snippets_when_question_Tab_is_typed_after_an_identifier, CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.IncludeAfterTypingIdentifierQuestionTab, LanguageNames.VisualBasic)

            BindToOption(Never_add_new_line_on_enter, CompletionOptionsStorage.EnterKeyBehavior, EnterKeyRule.Never, LanguageNames.VisualBasic)
            BindToOption(Only_add_new_line_on_enter_with_whole_word, CompletionOptionsStorage.EnterKeyBehavior, EnterKeyRule.AfterFullyTypedWord, LanguageNames.VisualBasic)
            BindToOption(Always_add_new_line_on_enter, CompletionOptionsStorage.EnterKeyBehavior, EnterKeyRule.Always, LanguageNames.VisualBasic)

            Show_items_from_unimported_namespaces.IsChecked = Me.OptionStore.GetOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.VisualBasic)
            Tab_twice_to_insert_arguments.IsChecked = Me.OptionStore.GetOption(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, LanguageNames.VisualBasic)
        End Sub

        Private Sub Show_completion_list_after_a_character_is_deleted_Checked(sender As Object, e As RoutedEventArgs)
            Me.OptionStore.SetOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.VisualBasic, value:=True)
        End Sub

        Private Sub Show_completion_list_after_a_character_is_deleted_Unchecked(sender As Object, e As RoutedEventArgs)
            Me.OptionStore.SetOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.VisualBasic, value:=False)
        End Sub

        Private Sub Show_items_from_unimported_namespaces_CheckedChanged(sender As Object, e As RoutedEventArgs)
            Show_items_from_unimported_namespaces.IsThreeState = False
            Me.OptionStore.SetOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.VisualBasic, Show_items_from_unimported_namespaces.IsChecked)
        End Sub

        Private Sub Tab_twice_to_insert_arguments_CheckedChanged(sender As Object, e As RoutedEventArgs)
            Tab_twice_to_insert_arguments.IsThreeState = False
            Me.OptionStore.SetOption(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, LanguageNames.VisualBasic, Tab_twice_to_insert_arguments.IsChecked)
        End Sub
    End Class
End Namespace
