// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    internal static class IntelliSenseOptionPageStrings
    {
        public static string Option_Show_completion_list_after_a_character_is_typed
            => CSharpVSResources.Show_completion_list_after_a_character_is_typed;

        public static string Option_Show_completion_list_after_a_character_is_deleted
            => CSharpVSResources.Show_completion_list_after_a_character_is_deleted;

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
            => CSharpVSResources.Highlight_matching_portions_of_completion_list_items;

        public static string Option_Show_completion_item_filters
            => CSharpVSResources.Show_completion_item_filters;

        public static string Option_Automatically_complete_statement_on_semicolon => CSharpVSResources.Automatically_complete_statement_on_semicolon;

        public static string Enter_key_behavior_Title
            => CSharpVSResources.Enter_key_behavior_colon;

        public static string Option_Never_add_new_line_on_enter
            => CSharpVSResources.Never_add_new_line_on_enter;

        public static string Option_Only_add_new_line_on_enter_with_whole_word
            => CSharpVSResources.Only_add_new_line_on_enter_after_end_of_fully_typed_word;

        public static string Option_Always_add_new_line_on_enter
            => CSharpVSResources.Always_add_new_line_on_enter;

        public static string Snippets_behavior
            => CSharpVSResources.Snippets_behavior;

        public static string Option_Never_include_snippets
            => CSharpVSResources.Never_include_snippets;

        public static string Option_Always_include_snippets
            => CSharpVSResources.Always_include_snippets;

        public static string Option_Include_snippets_when_question_Tab_is_typed_after_an_identifier
            => CSharpVSResources.Include_snippets_when_Tab_is_typed_after_an_identifier;

        public static string Option_Show_name_suggestions
            => CSharpVSResources.Show_name_suggestions;

        public static string Option_Show_items_from_unimported_namespaces
            => CSharpVSResources.Show_items_from_unimported_namespaces;

        public static string Option_Tab_twice_to_insert_arguments
            => ServicesVSResources.Tab_twice_to_insert_arguments;

        public static string Automatically_show_completion_list_in_argument_lists
            => CSharpVSResources.Automatically_show_completion_list_in_argument_lists;

        public static string Option_Show_new_snippet_experience_experimental
            => CSharpVSResources.Show_new_snippet_experience_experimental;
    }
}
