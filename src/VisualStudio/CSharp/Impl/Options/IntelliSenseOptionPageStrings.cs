// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options;

internal static class IntelliSenseOptionPageStrings
{
    public static string Option_Show_completion_list_after_a_character_is_typed
        => ServicesVSResources._Show_completion_list_after_a_character_is_typed;

    public static string Option_Show_completion_list_after_a_character_is_deleted
        => ServicesVSResources.Show_completion_list_after_a_character_is__deleted;

    public static string Option_Completion
    {
        get { return CSharpVSResources.Completion; }
    }

    public static string Option_SelectionInCompletionList
    {
        get { return CSharpVSResources.Selection_In_Completion_List; }
    }

    public static string Option_ShowKeywords
    {
        get { return CSharpVSResources.Place_keywords_in_completion_lists; }
    }

    public static string Option_ShowSnippets
    {
        get { return CSharpVSResources.Place_code_snippets_in_completion_lists; }
    }

    public static string Option_Highlight_matching_portions_of_completion_list_items
        => ServicesVSResources._Highlight_matching_portions_of_completion_list_items;

    public static string Option_Show_completion_item_filters
        => ServicesVSResources.Show_completion_item__filters;

    public static string Option_Automatically_complete_statement_on_semicolon => CSharpVSResources.Automatically_complete_statement_on_semicolon;

    public static string Enter_key_behavior_Title
        => ServicesVSResources.Enter_key_behavior_colon;

    public static string Option_Never_add_new_line_on_enter
        => ServicesVSResources.Never_add_new_line_on_enter;

    public static string Option_Only_add_new_line_on_enter_with_whole_word
        => ServicesVSResources._Only_add_new_line_on_enter_after_end_of_fully_typed_word;

    public static string Option_Always_add_new_line_on_enter
        => ServicesVSResources.Always_add_new_line_on_enter;

    public static string Snippets_behavior
        => ServicesVSResources.Snippets_behavior;

    public static string Option_Never_include_snippets
        => ServicesVSResources.Never_include_snippets;

    public static string Option_Always_include_snippets
        => CSharpVSResources.Always_include_snippets;

    public static string Option_Include_snippets_when_question_Tab_is_typed_after_an_identifier
        => CSharpVSResources.Include_snippets_when_Tab_is_typed_after_an_identifier;

    public static string Option_Show_name_s_uggestions
        => CSharpVSResources.Show_name_s_uggestions;

    public static string Option_Show_items_from_unimported_namespaces
        => ServicesVSResources.Show_items_from_unimported_namespaces;

    public static string Option_Tab_twice_to_insert_arguments_experimental
        => ServicesVSResources.Tab_twice_to_insert_arguments_experimental;

    public static string Automatically_show_completion_list_in_argument_lists
        => CSharpVSResources.Automatically_show_completion_list_in_argument_lists;

    public static string Option_Show_new_snippet_experience_experimental
        => CSharpVSResources.Show_new_snippet_experience_experimental;
}
