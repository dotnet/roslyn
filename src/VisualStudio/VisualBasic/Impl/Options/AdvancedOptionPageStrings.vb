﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.ColorSchemes

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Friend Module AdvancedOptionPageStrings
        Public ReadOnly Property Option_AutomaticInsertionOfInterfaceAndMustOverrideMembers As String
            Get
                Return BasicVSResources.Automatic_insertion_of_Interface_and_MustOverride_members
            End Get
        End Property

        Public ReadOnly Property Option_Analysis As String =
            ServicesVSResources.Analysis

        Public ReadOnly Property Option_Background_analysis_scope As String =
            ServicesVSResources.Background_analysis_scope_colon

        Public ReadOnly Property Option_Background_Analysis_Scope_Active_File As String =
            ServicesVSResources.Current_document

        Public ReadOnly Property Option_Background_Analysis_Scope_Open_Files_And_Projects As String =
            ServicesVSResources.Open_documents

        Public ReadOnly Property Option_Background_Analysis_Scope_Full_Solution As String =
            ServicesVSResources.Entire_solution

        Public ReadOnly Property Option_use_64bit_analysis_process As String =
            ServicesVSResources.Use_64_bit_process_for_code_analysis_requires_restart

        Public ReadOnly Property Option_DisplayLineSeparators As String =
            BasicVSResources.Show_procedure_line_separators

        Public ReadOnly Property Option_Display_all_hints_while_pressing_Alt_F1 As String =
            ServicesVSResources.Display_all_hints_while_pressing_Alt_F1

        Public ReadOnly Property Option_Color_hints As String =
            ServicesVSResources.Color_hints

        Public ReadOnly Property Option_Inline_Hints_experimental As String =
            ServicesVSResources.Inline_Hints_experimental

        Public ReadOnly Property Option_Display_inline_parameter_name_hints As String =
            ServicesVSResources.Display_inline_parameter_name_hints

        Public ReadOnly Property Option_Show_hints_for_literals As String =
            ServicesVSResources.Show_hints_for_literals

        Public ReadOnly Property Option_Show_hints_for_New_expressions As String =
            BasicVSResources.Show_hints_for_New_expressions

        Public ReadOnly Property Option_Show_hints_for_everything_else As String =
            ServicesVSResources.Show_hints_for_everything_else

        Public ReadOnly Property Option_Suppress_hints_when_parameter_name_matches_the_method_s_intent As String =
            ServicesVSResources.Suppress_hints_when_parameter_name_matches_the_method_s_intent

        Public ReadOnly Property Option_Suppress_hints_when_parameter_names_differ_only_by_suffix As String =
            ServicesVSResources.Suppress_hints_when_parameter_names_differ_only_by_suffix

        Public ReadOnly Property Option_DontPutOutOrRefOnStruct As String =
            BasicVSResources.Don_t_put_ByRef_on_custom_structure

        Public ReadOnly Property Option_EditorHelp As String
            Get
                Return BasicVSResources.Editor_Help
            End Get
        End Property

        Public ReadOnly Property Option_EnableEndConstruct As String
            Get
                Return BasicVSResources.A_utomatic_insertion_of_end_constructs
            End Get
        End Property

        Public ReadOnly Property Option_EnableHighlightKeywords As String
            Get
                Return BasicVSResources.Highlight_related_keywords_under_cursor
            End Get
        End Property

        Public ReadOnly Property Option_EnableHighlightReferences As String
            Get
                Return BasicVSResources.Highlight_references_to_symbol_under_cursor
            End Get
        End Property

        Public ReadOnly Property Option_EnableLineCommit As String
            Get
                Return BasicVSResources.Pretty_listing_reformatting_of_code
            End Get
        End Property

        Public ReadOnly Property Option_EnableOutlining As String
            Get
                Return BasicVSResources.Enter_outlining_mode_when_files_open
            End Get
        End Property

        Public ReadOnly Property Option_ExtractMethod As String
            Get
                Return BasicVSResources.Extract_Method
            End Get
        End Property

        Public ReadOnly Property Option_Implement_Interface_or_Abstract_Class As String =
            ServicesVSResources.Implement_Interface_or_Abstract_Class

        Public ReadOnly Property Option_When_inserting_properties_events_and_methods_place_them As String =
            ServicesVSResources.When_inserting_properties_events_and_methods_place_them

        Public ReadOnly Property Option_with_other_members_of_the_same_kind As String =
            ServicesVSResources.with_other_members_of_the_same_kind

        Public ReadOnly Property Option_When_generating_properties As String =
            ServicesVSResources.When_generating_properties

        Public ReadOnly Property Option_prefer_auto_properties As String =
            ServicesVSResources.codegen_prefer_auto_properties

        Public ReadOnly Property Option_prefer_throwing_properties As String =
            ServicesVSResources.prefer_throwing_properties

        Public ReadOnly Property Option_at_the_end As String =
            ServicesVSResources.at_the_end

        Public ReadOnly Property Option_GenerateXmlDocCommentsForTripleApostrophes As String =
            BasicVSResources.Generate_XML_documentation_comments_for

        Public ReadOnly Property Option_InsertApostropheAtTheStartOfNewLinesWhenWritingApostropheComments As String =
            BasicVSResources.Insert_apostrophe_at_the_start_of_new_lines_when_writing_apostrophe_comments

        Public ReadOnly Property Option_ShowRemarksInQuickInfo As String
            Get
                Return BasicVSResources.Show_remarks_in_Quick_Info
            End Get
        End Property

        Public ReadOnly Property Option_GoToDefinition As String
            Get
                Return BasicVSResources.Go_to_Definition
            End Get
        End Property

        Public ReadOnly Property Option_Highlighting As String
            Get
                Return BasicVSResources.Highlighting
            End Get
        End Property

        Public ReadOnly Property Option_NavigateToObjectBrowser As String
            Get
                Return BasicVSResources.Navigate_to_Object_Browser_for_symbols_defined_in_metadata
            End Get
        End Property

        Public ReadOnly Property Option_OptimizeForSolutionSize As String
            Get
                Return BasicVSResources.Optimize_for_solution_size
            End Get
        End Property

        Public ReadOnly Property Option_OptimizeForSolutionSize_Small As String
            Get
                Return BasicVSResources.Small
            End Get
        End Property

        Public ReadOnly Property Option_OptimizeForSolutionSize_Regular As String
            Get
                Return BasicVSResources.Regular
            End Get
        End Property

        Public ReadOnly Property Option_OptimizeForSolutionSize_Large As String
            Get
                Return BasicVSResources.Large
            End Get
        End Property

        Public ReadOnly Property Option_Outlining As String = ServicesVSResources.Outlining

        Public ReadOnly Property Option_Show_outlining_for_declaration_level_constructs As String =
            ServicesVSResources.Show_outlining_for_declaration_level_constructs

        Public ReadOnly Property Option_Show_outlining_for_code_level_constructs As String =
            ServicesVSResources.Show_outlining_for_code_level_constructs

        Public ReadOnly Property Option_Show_outlining_for_comments_and_preprocessor_regions As String =
            ServicesVSResources.Show_outlining_for_comments_and_preprocessor_regions

        Public ReadOnly Property Option_Collapse_regions_when_collapsing_to_definitions As String =
            ServicesVSResources.Collapse_regions_when_collapsing_to_definitions

        Public ReadOnly Property Option_Block_Structure_Guides As String =
            ServicesVSResources.Block_Structure_Guides

        Public ReadOnly Property Option_Comments As String =
            ServicesVSResources.Comments

        Public ReadOnly Property Option_Show_guides_for_declaration_level_constructs As String =
            ServicesVSResources.Show_guides_for_declaration_level_constructs

        Public ReadOnly Property Option_Show_guides_for_code_level_constructs As String =
            ServicesVSResources.Show_guides_for_code_level_constructs

        Public ReadOnly Property Option_Fading As String = ServicesVSResources.Fading
        Public ReadOnly Property Option_Fade_out_unused_imports As String = BasicVSResources.Fade_out_unused_imports

        Public ReadOnly Property Option_Performance As String
            Get
                Return BasicVSResources.Performance
            End Get
        End Property

        Public ReadOnly Property Option_Report_invalid_placeholders_in_string_dot_format_calls As String
            Get
                Return BasicVSResources.Report_invalid_placeholders_in_string_dot_format_calls
            End Get
        End Property

        Public ReadOnly Property Option_RenameTrackingPreview As String
            Get
                Return BasicVSResources.Show_preview_for_rename_tracking
            End Get
        End Property

        Public ReadOnly Property Option_Import_Directives As String =
            BasicVSResources.Import_Directives

        Public ReadOnly Property Option_PlaceSystemNamespaceFirst As String =
            BasicVSResources.Place_System_directives_first_when_sorting_imports

        Public ReadOnly Property Option_SeparateImportGroups As String =
            BasicVSResources.Separate_import_directive_groups

        Public ReadOnly Property Option_Suggest_imports_for_types_in_reference_assemblies As String =
            BasicVSResources.Suggest_imports_for_types_in_reference_assemblies

        Public ReadOnly Property Option_Suggest_imports_for_types_in_NuGet_packages As String =
            BasicVSResources.Suggest_imports_for_types_in_NuGet_packages

        Public ReadOnly Property Option_Add_missing_imports_on_paste As String =
            BasicVSResources.Add_missing_imports_on_paste

        Public ReadOnly Property Option_Regular_Expressions As String =
            ServicesVSResources.Regular_Expressions

        Public ReadOnly Property Option_Colorize_regular_expressions As String =
            ServicesVSResources.Colorize_regular_expressions

        Public ReadOnly Property Option_Report_invalid_regular_expressions As String =
            ServicesVSResources.Report_invalid_regular_expressions

        Public ReadOnly Property Option_Highlight_related_components_under_cursor As String =
            ServicesVSResources.Highlight_related_components_under_cursor

        Public ReadOnly Property Option_Show_completion_list As String =
            ServicesVSResources.Show_completion_list

        Public ReadOnly Property Option_Editor_Color_Scheme As String =
            ServicesVSResources.Editor_Color_Scheme

        Public ReadOnly Property Editor_color_scheme_options_are_only_available_when_using_a_color_theme_bundled_with_Visual_Studio_The_color_theme_can_be_configured_from_the_Environment_General_options_page As String =
            ServicesVSResources.Editor_color_scheme_options_are_only_available_when_using_a_color_theme_bundled_with_Visual_Studio_The_color_theme_can_be_configured_from_the_Environment_General_options_page

        Public ReadOnly Property Some_color_scheme_colors_are_being_overridden_by_changes_made_in_the_Environment_Fonts_and_Colors_options_page_Choose_Use_Defaults_in_the_Fonts_and_Colors_page_to_revert_all_customizations As String =
            ServicesVSResources.Some_color_scheme_colors_are_being_overridden_by_changes_made_in_the_Environment_Fonts_and_Colors_options_page_Choose_Use_Defaults_in_the_Fonts_and_Colors_page_to_revert_all_customizations

        Public ReadOnly Property Option_Color_Scheme_VisualStudio2019 As String =
            ServicesVSResources.Visual_Studio_2019

        Public ReadOnly Property Option_Color_Scheme_VisualStudio2017 As String =
            ServicesVSResources.Visual_Studio_2017

        Public ReadOnly Property Color_Scheme_VisualStudio2019_Tag As SchemeName =
            SchemeName.VisualStudio2019

        Public ReadOnly Property Color_Scheme_VisualStudio2017_Tag As SchemeName =
            SchemeName.VisualStudio2017

        Public ReadOnly Property Option_Show_Remove_Unused_References_command_in_Solution_Explorer_experimental As String =
            ServicesVSResources.Show_Remove_Unused_References_command_in_Solution_Explorer_experimental
    End Module
End Namespace
