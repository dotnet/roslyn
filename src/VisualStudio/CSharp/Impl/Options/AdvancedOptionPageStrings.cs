// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ColorSchemes;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    internal static class AdvancedOptionPageStrings
    {
        public static string Option_Analysis
            => ServicesVSResources.Analysis;

        public static string Option_Run_background_code_analysis_for
            => ServicesVSResources.Run_background_code_analysis_for_colon;

        public static string Option_Background_Analysis_Scope_None
            => ServicesVSResources.None;

        public static string Option_Background_Analysis_Scope_Active_File
            => ServicesVSResources.Current_document;

        public static string Option_Background_Analysis_Scope_Open_Files
            => ServicesVSResources.Open_documents;

        public static string Option_Background_Analysis_Scope_Full_Solution
            => ServicesVSResources.Entire_solution;

        public static BackgroundAnalysisScope Option_Background_Analysis_Scope_None_Tag
            => BackgroundAnalysisScope.None;

        public static BackgroundAnalysisScope Option_Background_Analysis_Scope_Active_File_Tag
            => BackgroundAnalysisScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics;

        public static BackgroundAnalysisScope Option_Background_Analysis_Scope_Open_Files_Tag
            => BackgroundAnalysisScope.OpenFiles;

        public static BackgroundAnalysisScope Option_Background_Analysis_Scope_Full_Solution_Tag
            => BackgroundAnalysisScope.FullSolution;

        public static string Option_Show_compiler_errors_and_warnings_for
            => ServicesVSResources.Show_compiler_errors_and_warnings_for_colon;

        public static string Option_Compiler_Diagnostics_Scope_None
            => ServicesVSResources.None;

        public static string Option_Compiler_Diagnostics_Scope_Visible_Files
            => ServicesVSResources.Current_document; // We show "Current document" to users for consistency with term used elsewhere.

        public static string Option_Compiler_Diagnostics_Scope_Open_Files
            => ServicesVSResources.Open_documents;

        public static string Option_Compiler_Diagnostics_Scope_Full_Solution
            => ServicesVSResources.Entire_solution;

        public static CompilerDiagnosticsScope Option_Compiler_Diagnostics_Scope_None_Tag
            => CompilerDiagnosticsScope.None;

        public static CompilerDiagnosticsScope Option_Compiler_Diagnostics_Scope_Visible_Files_Tag
            => CompilerDiagnosticsScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics;

        public static CompilerDiagnosticsScope Option_Compiler_Diagnostics_Scope_Open_Files_Tag
            => CompilerDiagnosticsScope.OpenFiles;

        public static CompilerDiagnosticsScope Option_Compiler_Diagnostics_Scope_Full_Solution_Tag
            => CompilerDiagnosticsScope.FullSolution;

        public static string Option_Enable_navigation_to_decompiled_sources
            => ServicesVSResources.Enable_navigation_to_decompiled_sources;

        public static string Option_Enable_navigation_to_sourcelink_and_embedded_sources
            => ServicesVSResources.Enable_navigation_to_sourcelink_and_embedded_sources;

        public static string Option_Always_use_default_symbol_servers_for_navigation
            => ServicesVSResources.Always_use_default_symbol_servers_for_navigation;

        public static string Option_run_code_analysis_in_separate_process
            => ServicesVSResources.Run_code_analysis_in_separate_process_requires_restart;

        public static string Option_analyze_source_generated_files
            => ServicesVSResources.Analyze_source_generated_files;

        public static string Option_Inline_Hints
            => ServicesVSResources.Inline_Hints;

        public static string Option_Display_all_hints_while_pressing_Alt_F1
            => ServicesVSResources.Display_all_hints_while_pressing_Alt_F1;

        public static string Option_Color_hints
            => ServicesVSResources.Color_hints;

        public static string Option_Display_inline_parameter_name_hints
            => ServicesVSResources.Display_inline_parameter_name_hints;

        public static string Option_Show_hints_for_literals
            => ServicesVSResources.Show_hints_for_literals;

        public static string Option_Show_hints_for_new_expressions
            => CSharpVSResources.Show_hints_for_new_expressions;

        public static string Option_Show_hints_for_everything_else
            => ServicesVSResources.Show_hints_for_everything_else;

        public static string Option_Show_hints_for_indexers
            => ServicesVSResources.Show_hints_for_indexers;

        public static string Option_Suppress_hints_when_parameter_name_matches_the_method_s_intent
            => ServicesVSResources.Suppress_hints_when_parameter_name_matches_the_method_s_intent;

        public static string Option_Suppress_hints_when_parameter_names_differ_only_by_suffix
            => ServicesVSResources.Suppress_hints_when_parameter_names_differ_only_by_suffix;

        public static string Option_Suppress_hints_when_argument_matches_parameter_name
            => ServicesVSResources.Suppress_hints_when_argument_matches_parameter_name;

        public static string Option_Display_inline_type_hints
            => ServicesVSResources.Display_inline_type_hints;

        public static string Option_Show_hints_for_variables_with_inferred_types
            => ServicesVSResources.Show_hints_for_variables_with_inferred_types;

        public static string Option_Show_hints_for_lambda_parameter_types
            => ServicesVSResources.Show_hints_for_lambda_parameter_types;

        public static string Option_Show_hints_for_implicit_object_creation
            => ServicesVSResources.Show_hints_for_implicit_object_creation;

        public static string Option_Show_hints_for_collection_expressions
            => ServicesVSResources.Show_hints_for_collection_expressions;

        public static string Option_Display_diagnostics_inline_experimental
            => ServicesVSResources.Display_diagnostics_inline_experimental;

        public static string Option_at_the_end_of_the_line_of_code
            => ServicesVSResources.at_the_end_of_the_line_of_code;

        public static string Option_on_the_right_edge_of_the_editor_window
            => ServicesVSResources.on_the_right_edge_of_the_editor_window;

        public static string Option_RenameTrackingPreview
            => CSharpVSResources.Show_preview_for_rename_tracking;

        public static string Option_Split_string_literals_on_enter
            => CSharpVSResources.Split_string_literals_on_enter;

        public static string Option_DisplayLineSeparators
            => CSharpVSResources.Show_procedure_line_separators;

        public static string Option_Underline_reassigned_variables
            => ServicesVSResources.Underline_reassigned_variables;

        public static string Option_Strike_out_obsolete_symbols
            => ServicesVSResources.Strike_out_obsolete_symbols;

        public static string Option_DontPutOutOrRefOnStruct
            => CSharpVSResources.Don_t_put_ref_or_out_on_custom_struct;

        public static string Option_EditorHelp
            => CSharpVSResources.Editor_Help;

        public static string Option_EnableHighlightKeywords
            => CSharpVSResources.Highlight_related_keywords_under_cursor;

        public static string Option_EnableHighlightReferences
            => CSharpVSResources.Highlight_references_to_symbol_under_cursor;

        public static string Option_EnterOutliningMode
            => CSharpVSResources.Enter_outlining_mode_when_files_open;

        public static string Option_ExtractMethod
            => CSharpVSResources.Extract_Method;

        public static string Option_Implement_Interface_or_Abstract_Class
            => ServicesVSResources.Implement_Interface_or_Abstract_Class;

        public static string Option_When_inserting_properties_events_and_methods_place_them
            => ServicesVSResources.When_inserting_properties_events_and_methods_place_them;

        public static string Option_with_other_members_of_the_same_kind
            => ServicesVSResources.with_other_members_of_the_same_kind;

        public static string Option_at_the_end
            => ServicesVSResources.at_the_end;

        public static string Option_When_generating_properties
            => ServicesVSResources.When_generating_properties;

        public static string Option_prefer_auto_properties
            => ServicesVSResources.codegen_prefer_auto_properties;

        public static string Option_prefer_throwing_properties
            => ServicesVSResources.prefer_throwing_properties;

        public static string Option_Comments
            => ServicesVSResources.Comments;

        public static string Option_GenerateXmlDocCommentsForTripleSlash
            => CSharpVSResources.Generate_XML_documentation_comments_for;

        public static string Option_InsertSlashSlashAtTheStartOfNewLinesWhenWritingSingleLineComments
            => CSharpVSResources.Insert_slash_slash_at_the_start_of_new_lines_when_writing_slash_slash_comments;

        public static string Option_InsertAsteriskAtTheStartOfNewLinesWhenWritingBlockComments
            => CSharpVSResources.Insert_at_the_start_of_new_lines_when_writing_comments;

        public static string Option_ShowRemarksInQuickInfo
            => CSharpVSResources.Show_remarks_in_Quick_Info;

        public static string Option_Highlighting
            => CSharpVSResources.Highlighting;

        public static string Option_OptimizeForSolutionSize
            => CSharpVSResources.Optimize_for_solution_size;

        public static string Option_OptimizeForSolutionSize_Large
            => CSharpVSResources.Large;

        public static string Option_OptimizeForSolutionSize_Regular
            => CSharpVSResources.Regular;

        public static string Option_OptimizeForSolutionSize_Small
            => CSharpVSResources.Small;

        public static string Option_Quick_Actions
            => ServicesVSResources.Quick_Actions;

        public static string Option_Outlining
            => ServicesVSResources.Outlining;

        public static string Option_Collapse_regions_on_file_open
            => ServicesVSResources.Collapse_regions_on_file_open;

        public static string Option_Collapse_usings_on_file_open
            => CSharpVSResources.Collapse_usings_on_file_open;

        public static string Option_Collapse_sourcelink_embedded_decompiled_files_on_open
            => ServicesVSResources.Collapse_sourcelink_embedded_decompiled_files_on_open;

        public static string Option_Collapse_metadata_signature_files_on_open
            => ServicesVSResources.Collapse_metadata_signature_files_on_open;

        public static string Option_Show_outlining_for_declaration_level_constructs
            => ServicesVSResources.Show_outlining_for_declaration_level_constructs;

        public static string Option_Show_outlining_for_code_level_constructs
            => ServicesVSResources.Show_outlining_for_code_level_constructs;

        public static string Option_Show_outlining_for_comments_and_preprocessor_regions
            => ServicesVSResources.Show_outlining_for_comments_and_preprocessor_regions;

        public static string Option_Collapse_regions_when_collapsing_to_definitions
            => ServicesVSResources.Collapse_regions_when_collapsing_to_definitions;

        public static string Option_Collapse_local_functions_when_collapsing_to_definitions
            => ServicesVSResources.Collapse_local_functions_when_collapsing_to_definitions;

        public static string Option_Block_Structure_Guides
            => ServicesVSResources.Block_Structure_Guides;

        public static string Option_Show_guides_for_declaration_level_constructs
            => ServicesVSResources.Show_guides_for_declaration_level_constructs;

        public static string Option_Show_guides_for_code_level_constructs
            => ServicesVSResources.Show_guides_for_code_level_constructs;

        public static string Option_Show_guides_for_comments_and_preprocessor_regions
            => ServicesVSResources.Show_guides_for_comments_and_preprocessor_regions;

        public static string Option_Fading
            => ServicesVSResources.Fading;

        public static string Option_Fade_out_unused_usings
            => CSharpVSResources.Fade_out_unused_usings;

        public static string Option_Fade_out_unreachable_code
            => ServicesVSResources.Fade_out_unreachable_code;

        public static string Option_Performance
            => CSharpVSResources.Performance;

        public static string Option_PlaceSystemNamespaceFirst
            => CSharpVSResources.Place_System_directives_first_when_sorting_usings;

        public static string Option_SeparateImportGroups
            => CSharpVSResources.Separate_using_directive_groups;

        public static string Option_Using_Directives
            => CSharpVSResources.Using_Directives;

        public static string Option_Suggest_usings_for_types_in_reference_assemblies
            => CSharpVSResources.Suggest_usings_for_types_in_dotnet_framework_assemblies;

        public static string Option_Suggest_usings_for_types_in_NuGet_packages
            => CSharpVSResources.Suggest_usings_for_types_in_NuGet_packages;

        public static string Option_Add_missing_using_directives_on_paste
            => CSharpVSResources.Add_missing_using_directives_on_paste;

        public static string Option_Report_invalid_placeholders_in_string_dot_format_calls
            => CSharpVSResources.Report_invalid_placeholders_in_string_dot_format_calls;

        public static string Option_Regular_Expressions
            => ServicesVSResources.Regular_Expressions;

        public static string Option_Colorize_regular_expressions
            => ServicesVSResources.Colorize_regular_expressions;

        public static string Option_Report_invalid_regular_expressions
            => ServicesVSResources.Report_invalid_regular_expressions;

        public static string Option_Highlight_related_components_under_cursor
            => ServicesVSResources.Highlight_related_components_under_cursor;

        public static string Option_Show_completion_list
            => ServicesVSResources.Show_completion_list;

        public static string Option_Editor_Color_Scheme
            => ServicesVSResources.Editor_Color_Scheme;

        public static string Editor_color_scheme_options_are_only_available_when_using_a_color_theme_bundled_with_Visual_Studio_The_color_theme_can_be_configured_from_the_Environment_General_options_page
            => ServicesVSResources.Editor_color_scheme_options_are_only_available_when_using_a_color_theme_bundled_with_Visual_Studio_The_color_theme_can_be_configured_from_the_Environment_General_options_page;

        public static string Some_color_scheme_colors_are_being_overridden_by_changes_made_in_the_Environment_Fonts_and_Colors_options_page_Choose_Use_Defaults_in_the_Fonts_and_Colors_page_to_revert_all_customizations
            => ServicesVSResources.Some_color_scheme_colors_are_being_overridden_by_changes_made_in_the_Environment_Fonts_and_Colors_options_page_Choose_Use_Defaults_in_the_Fonts_and_Colors_page_to_revert_all_customizations;

        public static string Edit_color_scheme
            => ServicesVSResources.Editor_Color_Scheme;

        public static string Option_Color_Scheme_VisualStudio2019
            => ServicesVSResources.Visual_Studio_2019;

        public static string Option_Color_Scheme_VisualStudio2017
            => ServicesVSResources.Visual_Studio_2017;

        public static ColorSchemeName Color_Scheme_VisualStudio2019_Tag
            => ColorSchemeName.VisualStudio2019;

        public static ColorSchemeName Color_Scheme_VisualStudio2017_Tag
            => ColorSchemeName.VisualStudio2017;

        public static string Option_Show_Remove_Unused_References_command_in_Solution_Explorer_experimental
            => ServicesVSResources.Show_Remove_Unused_References_command_in_Solution_Explorer_experimental;

        public static string Option_Enable_file_logging_for_diagnostics
            => ServicesVSResources.Enable_file_logging_for_diagnostics;

        public static string Option_Skip_analyzers_for_implicitly_triggered_builds
            => ServicesVSResources.Skip_analyzers_for_implicitly_triggered_builds;

        public static string Show_inheritance_margin
            => ServicesVSResources.Show_inheritance_margin;

        public static string Combine_inheritance_margin_with_indicator_margin
            => ServicesVSResources.Combine_inheritance_margin_with_indicator_margin;

        public static string Include_global_imports
            => ServicesVSResources.Include_global_imports;

        public static string Option_JSON_strings
            => ServicesVSResources.JSON_strings;

        public static string Option_Colorize_JSON_strings
            => ServicesVSResources.Colorize_JSON_strings;

        public static string Option_Report_invalid_JSON_strings
            => ServicesVSResources.Report_invalid_JSON_strings;

        public static string Inheritance_Margin
            => ServicesVSResources.Inheritance_Margin;

        public static string Stack_Trace_Explorer
            => ServicesVSResources.Stack_Trace_Explorer;

        public static string Option_Automatically_open_stack_trace_explorer_on_focus
            => ServicesVSResources.Automatically_open_stack_trace_explorer_on_focus;

        public static string Option_Fix_text_pasted_into_string_literals_experimental
            => ServicesVSResources.Fix_text_pasted_into_string_literals_experimental;

        public static string Option_Go_To_Definition
            => ServicesVSResources.Go_To_Definition;

        public static string Option_Navigate_asynchronously_exerimental
            => ServicesVSResources.Navigate_asynchronously_exerimental;

        public static string Option_Rename
            => ServicesVSResources.Rename;

        public static string Option_Rename_asynchronously_experimental
            => ServicesVSResources.Rename_asynchronously_experimental;

        public static string Where_should_the_rename_UI_be_shown
            => ServicesVSResources.Where_should_the_rename_UI_be_shown;

        public static string Option_Show_UI_inline
            => ServicesVSResources.Show_UI_inline;

        public static string Option_Show_UI_as_dashboard_in_top_right
            => ServicesVSResources.Show_UI_as_dashboard_in_top_right;

        public static string Document_Outline
            => ServicesVSResources.Document_Outline;

        public static string Option_Enable_document_outline_experimental_requires_restart
            => ServicesVSResources.Enable_document_outline_experimental_requires_restart;

        public static string Option_Source_Generators
            => ServicesVSResources.Source_Generators;

        public static string Option_Source_generator_execution_requires_restart
            => ServicesVSResources.Source_generator_execution_requires_restart;

        public static string Option_Automatic_Run_generators_after_any_change
            => ServicesVSResources.Automatic_Run_generators_after_any_change;

        public static string Option_Balanced_Run_generators_after_saving_or_building
            => ServicesVSResources.Balanced_Run_generators_after_saving_or_building;
    }
}
