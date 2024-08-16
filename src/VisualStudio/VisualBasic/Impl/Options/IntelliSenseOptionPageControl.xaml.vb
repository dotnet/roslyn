' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
            BindToOption(Show_completion_list_after_a_character_is_deleted, CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.VisualBasic, onNullValue:=function() True)

            BindToOption(Highlight_matching_portions_of_completion_list_items, CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems, LanguageNames.VisualBasic)
            BindToOption(Show_completion_item_filters, CompletionViewOptionsStorage.ShowCompletionItemFilters, LanguageNames.VisualBasic)

            BindToOption(Never_include_snippets, CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.NeverInclude, LanguageNames.VisualBasic)
            BindToOption(Always_include_snippets, CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.AlwaysInclude, LanguageNames.VisualBasic)
            BindToOption(Include_snippets_when_question_Tab_is_typed_after_an_identifier, CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.IncludeAfterTypingIdentifierQuestionTab, LanguageNames.VisualBasic)
            SetSnippetDefaultBehavior()

            BindToOption(Never_add_new_line_on_enter, CompletionOptionsStorage.EnterKeyBehavior, EnterKeyRule.Never, LanguageNames.VisualBasic)
            BindToOption(Only_add_new_line_on_enter_with_whole_word, CompletionOptionsStorage.EnterKeyBehavior, EnterKeyRule.AfterFullyTypedWord, LanguageNames.VisualBasic)
            BindToOption(Always_add_new_line_on_enter, CompletionOptionsStorage.EnterKeyBehavior, EnterKeyRule.Always, LanguageNames.VisualBasic)
            SetEnterKeyDefaultBehavior()

            BindToOption(Show_items_from_unimported_namespaces, CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.VisualBasic, onNullValue:=function() True)
            BindToOption(Tab_twice_to_insert_arguments, CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, LanguageNames.VisualBasic, onNullValue:=function() False)
        End Sub

        Private Sub SetSnippetDefaultBehavior()
            Dim snippetValue = Me.OptionStore.GetOption(CompletionOptionsStorage.SnippetsBehavior, LanguageNames.VisualBasic)
            If snippetValue = SnippetsRule.Default Then
                Include_snippets_when_question_Tab_is_typed_after_an_identifier.IsChecked = True
            End If
        End Sub

        Private Sub SetEnterKeyDefaultBehavior()
            Dim enterKeyRule = Me.OptionStore.GetOption(CompletionOptionsStorage.EnterKeyBehavior, LanguageNames.VisualBasic)
            If enterKeyRule = EnterKeyRule.Default Then
                Always_add_new_line_on_enter.IsChecked = True
            End If
        End Sub
    End Class
End Namespace
