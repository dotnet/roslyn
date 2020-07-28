// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.ColorSchemes;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    internal static class AdvancedOptionPageStrings
    {
        public static string Option_Analysis
            => ServicesVSResources.Analysis;

        public static string Option_Background_analysis_scope
            => ServicesVSResources.Background_analysis_scope_colon;

        public static string Option_Background_Analysis_Scope_Active_File
            => ServicesVSResources.Current_document;

        public static string Option_Background_Analysis_Scope_Open_Files_And_Projects
            => ServicesVSResources.Open_documents;

        public static string Option_Background_Analysis_Scope_Full_Solution
            => ServicesVSResources.Entire_solution;

        public static string Option_Enable_navigation_to_decompiled_sources
            => ServicesVSResources.Enable_navigation_to_decompiled_sources;

        public static string Option_use_editorconfig_compatibility_mode
            => ServicesVSResources.Use_editorconfig_compatibility_mode;

        public static string Option_Display_inline_parameter_name_hints
            => ServicesVSResources.Display_inline_parameter_name_hints;

        public static string Option_RenameTrackingPreview => CSharpVSResources.Show_preview_for_rename_tracking;
        public static string Option_Split_string_literals_on_enter => CSharpVSResources.Split_string_literals_on_enter;

        public static string Option_DisplayLineSeparators
        {
            get { return CSharpVSResources.Show_procedure_line_separators; }
        }

        public static string Option_DontPutOutOrRefOnStruct
        {
            get { return CSharpVSResources.Don_t_put_ref_or_out_on_custom_struct; }
        }

        public static string Option_EditorHelp
        {
            get { return CSharpVSResources.Editor_Help; }
        }

        public static string Option_EnableHighlightKeywords
        {
            get { return CSharpVSResources.Highlight_related_keywords_under_cursor; }
        }

        public static string Option_EnableHighlightReferences
        {
            get { return CSharpVSResources.Highlight_references_to_symbol_under_cursor; }
        }

        public static string Option_EnterOutliningMode
        {
            get { return CSharpVSResources.Enter_outlining_mode_when_files_open; }
        }

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

        public static string Option_GenerateXmlDocCommentsForTripleSlash
        {
            get { return CSharpVSResources.Generate_XML_documentation_comments_for; }
        }

        public static string Option_ShowRemarksInQuickInfo
        {
            get { return CSharpVSResources.Show_remarks_in_Quick_Info; }
        }

        public static string Option_InsertAsteriskAtTheStartOfNewLinesWhenWritingBlockComments
        {
            get { return CSharpVSResources.Insert_at_the_start_of_new_lines_when_writing_comments; }
        }

        public static string Option_Highlighting
        {
            get { return CSharpVSResources.Highlighting; }
        }

        public static string Option_OptimizeForSolutionSize
        {
            get { return CSharpVSResources.Optimize_for_solution_size; }
        }

        public static string Option_OptimizeForSolutionSize_Large
        {
            get { return CSharpVSResources.Large; }
        }

        public static string Option_OptimizeForSolutionSize_Regular
        {
            get { return CSharpVSResources.Regular; }
        }

        public static string Option_OptimizeForSolutionSize_Small
        {
            get { return CSharpVSResources.Small; }
        }

        public static string Option_Outlining
            => ServicesVSResources.Outlining;

        public static string Option_Show_outlining_for_declaration_level_constructs
            => ServicesVSResources.Show_outlining_for_declaration_level_constructs;

        public static string Option_Show_outlining_for_code_level_constructs
            => ServicesVSResources.Show_outlining_for_code_level_constructs;

        public static string Option_Show_outlining_for_comments_and_preprocessor_regions
            => ServicesVSResources.Show_outlining_for_comments_and_preprocessor_regions;

        public static string Option_Collapse_regions_when_collapsing_to_definitions
            => ServicesVSResources.Collapse_regions_when_collapsing_to_definitions;

        public static string Option_Block_Structure_Guides
            => ServicesVSResources.Block_Structure_Guides;

        public static string Option_Show_guides_for_declaration_level_constructs
            => ServicesVSResources.Show_guides_for_declaration_level_constructs;

        public static string Option_Show_guides_for_code_level_constructs
            => ServicesVSResources.Show_guides_for_code_level_constructs;

        public static string Option_Fading
            => ServicesVSResources.Fading;

        public static string Option_Fade_out_unused_usings
            => CSharpVSResources.Fade_out_unused_usings;

        public static string Option_Fade_out_unreachable_code
            => ServicesVSResources.Fade_out_unreachable_code;

        public static string Option_Performance
        {
            get { return CSharpVSResources.Performance; }
        }

        public static string Option_PlaceSystemNamespaceFirst
            => CSharpVSResources.Place_System_directives_first_when_sorting_usings;

        public static string Option_SeparateImportGroups
            => CSharpVSResources.Separate_using_directive_groups;

        public static string Option_Using_Directives =>
            CSharpVSResources.Using_Directives;

        public static string Option_Suggest_usings_for_types_in_reference_assemblies =>
            CSharpVSResources.Suggest_usings_for_types_in_dotnet_framework_assemblies;

        public static string Option_Suggest_usings_for_types_in_NuGet_packages =>
            CSharpVSResources.Suggest_usings_for_types_in_NuGet_packages;

        public static string Option_Report_invalid_placeholders_in_string_dot_format_calls =>
            CSharpVSResources.Report_invalid_placeholders_in_string_dot_format_calls;

        public static string Option_Regular_Expressions =>
            ServicesVSResources.Regular_Expressions;

        public static string Option_Colorize_regular_expressions =>
            ServicesVSResources.Colorize_regular_expressions;

        public static string Option_Report_invalid_regular_expressions =>
            ServicesVSResources.Report_invalid_regular_expressions;

        public static string Option_Highlight_related_components_under_cursor =>
            ServicesVSResources.Highlight_related_components_under_cursor;

        public static string Option_Show_completion_list =>
            ServicesVSResources.Show_completion_list;

        public static string Option_Editor_Color_Scheme =>
            ServicesVSResources.Editor_Color_Scheme;

        public static string Editor_color_scheme_options_are_only_available_when_using_a_color_theme_bundled_with_Visual_Studio_The_color_theme_can_be_configured_from_the_Environment_General_options_page
            => ServicesVSResources.Editor_color_scheme_options_are_only_available_when_using_a_color_theme_bundled_with_Visual_Studio_The_color_theme_can_be_configured_from_the_Environment_General_options_page;

        public static string Some_color_scheme_colors_are_being_overridden_by_changes_made_in_the_Environment_Fonts_and_Colors_options_page_Choose_Use_Defaults_in_the_Fonts_and_Colors_page_to_revert_all_customizations
            => ServicesVSResources.Some_color_scheme_colors_are_being_overridden_by_changes_made_in_the_Environment_Fonts_and_Colors_options_page_Choose_Use_Defaults_in_the_Fonts_and_Colors_page_to_revert_all_customizations;

        public static string Option_Color_Scheme_VisualStudio2019 =>
            ServicesVSResources.Visual_Studio_2019;

        public static string Option_Color_Scheme_VisualStudio2017 =>
            ServicesVSResources.Visual_Studio_2017;

        public static SchemeName Color_Scheme_VisualStudio2019_Tag =>
            SchemeName.VisualStudio2019;

        public static SchemeName Color_Scheme_VisualStudio2017_Tag =>
            SchemeName.VisualStudio2017;
    }
}
