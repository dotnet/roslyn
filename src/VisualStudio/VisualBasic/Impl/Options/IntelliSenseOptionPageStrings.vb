' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Friend Module IntelliSenseOptionPageStrings
        Public ReadOnly Property Option_CompletionLists As String =
            BasicVSResources.Completion_Lists

        Public ReadOnly Property Option_Show_completion_list_after_a_character_is_typed As String =
            ServicesVSResources._Show_completion_list_after_a_character_is_typed

        Public ReadOnly Property Option_Show_completion_list_after_a_character_is_deleted As String =
            ServicesVSResources.Show_completion_list_after_a_character_is__deleted

        Public ReadOnly Property Option_Highlight_matching_portions_of_completion_list_items As String =
            ServicesVSResources._Highlight_matching_portions_of_completion_list_items

        Public ReadOnly Property Option_Show_completion_item_filters As String =
            ServicesVSResources.Show_completion_item__filters

        Public ReadOnly Property Option_Only_add_new_line_on_enter_with_whole_word As String =
            ServicesVSResources.Only_add_new_line_on_enter_after_end_of_fully_typed_word

        Public ReadOnly Property Option_Always_add_new_line_on_enter As String =
            ServicesVSResources.Always_add_new_line_on_enter

        Public ReadOnly Property Option_Never_add_new_line_on_enter As String =
            ServicesVSResources.Never_add_new_line_on_enter

        Public ReadOnly Property Enter_key_behavior_Title As String =
            ServicesVSResources.Enter_key_behavior_colon

        Public ReadOnly Property Snippets_behavior As String =
            ServicesVSResources.Snippets_behavior

        Public ReadOnly Property Option_Never_include_snippets As String =
            VSPackage.Never_include_snippets

        Public ReadOnly Property Option_Always_include_snippets As String =
            VSPackage.Always_include_snippets

        Public ReadOnly Property Option_Include_snippets_when_question_Tab_is_typed_after_an_identifier As String =
            VSPackage.Include_snippets_when_Tab_is_typed_after_an_identifier

        Public ReadOnly Property Option_Show_items_from_unimported_namespaces As String =
            ServicesVSResources.Show_items_from_unimported_namespaces

        Public ReadOnly Property Option_Tab_twice_to_insert_arguments_experimental As String =
            ServicesVSResources.Tab_twice_to_insert_arguments_experimental
    End Module
End Namespace
