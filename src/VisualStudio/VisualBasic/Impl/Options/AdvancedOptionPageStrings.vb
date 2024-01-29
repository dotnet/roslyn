' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.ColorSchemes
Imports Microsoft.CodeAnalysis.SolutionCrawler

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Friend Module AdvancedOptionPageStrings
        Public ReadOnly Property Option_AutomaticInsertionOfInterfaceAndMustOverrideMembers As String
            Get
                Return BasicVSResources.Automatic_insertion_of_Interface_and_MustOverride_members
            End Get
        End Property

        Public ReadOnly Property Option_Analysis As String =
            ServicesVSResources.Analysis

        Public ReadOnly Property Option_Run_background_code_analysis_for As String =
            ServicesVSResources.Run_background_code_analysis_for_colon

        Public ReadOnly Property Option_analyze_source_generated_files As String =
             ServicesVSResources.Analyze_source_generated_files

        Public ReadOnly Property Option_Background_Analysis_Scope_None As String =
            ServicesVSResources.None

        Public ReadOnly Property Option_Background_Analysis_Scope_Active_File As String =
            ServicesVSResources.Current_document

        Public ReadOnly Property Option_Background_Analysis_Scope_Open_Files As String =
            ServicesVSResources.Open_documents

        Public ReadOnly Property Option_Background_Analysis_Scope_Full_Solution As String =
            ServicesVSResources.Entire_solution

        Public ReadOnly Property Option_Background_Analysis_Scope_None_Tag As BackgroundAnalysisScope =
            BackgroundAnalysisScope.None

        Public ReadOnly Property Option_Background_Analysis_Scope_Active_File_Tag As BackgroundAnalysisScope =
            BackgroundAnalysisScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics

        Public ReadOnly Property Option_Background_Analysis_Scope_Open_Files_Tag As BackgroundAnalysisScope =
            BackgroundAnalysisScope.OpenFiles

        Public ReadOnly Property Option_Background_Analysis_Scope_Full_Solution_Tag As BackgroundAnalysisScope =
            BackgroundAnalysisScope.FullSolution

        Public ReadOnly Property Option_Show_compiler_errors_and_warnings_for As String =
            ServicesVSResources.Show_compiler_errors_and_warnings_for_colon

        Public ReadOnly Property Option_Compiler_Diagnostics_Scope_None As String =
            ServicesVSResources.None

        Public ReadOnly Property Option_Compiler_Diagnostics_Scope_Visible_Files As String =
            ServicesVSResources.Current_document ' We show "Current document" to users for consistency with term used elsewhere.

        Public ReadOnly Property Option_Compiler_Diagnostics_Scope_Open_Files As String =
            ServicesVSResources.Open_documents

        Public ReadOnly Property Option_Compiler_Diagnostics_Scope_Full_Solution As String =
            ServicesVSResources.Entire_solution

        Public ReadOnly Property Option_Compiler_Diagnostics_Scope_None_Tag As CompilerDiagnosticsScope =
            CompilerDiagnosticsScope.None

        Public ReadOnly Property Option_Compiler_Diagnostics_Scope_Visible_Files_Tag As CompilerDiagnosticsScope =
            CompilerDiagnosticsScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics

        Public ReadOnly Property Option_Compiler_Diagnostics_Scope_Open_Files_Tag As CompilerDiagnosticsScope =
            CompilerDiagnosticsScope.OpenFiles

        Public ReadOnly Property Option_Compiler_Diagnostics_Scope_Full_Solution_Tag As CompilerDiagnosticsScope =
            CompilerDiagnosticsScope.FullSolution

        Public ReadOnly Property Option_run_code_analysis_in_separate_process As String =
            ServicesVSResources.Run_code_analysis_in_separate_process_requires_restart

        Public ReadOnly Property Option_DisplayLineSeparators As String =
            BasicVSResources.Show_procedure_line_separators

        Public ReadOnly Property Option_Underline_reassigned_variables As String =
            ServicesVSResources.Underline_reassigned_variables

        Public ReadOnly Property Option_Strike_out_obsolete_symbols As String =
            ServicesVSResources.Strike_out_obsolete_symbols

        Public ReadOnly Property Option_Display_all_hints_while_pressing_Alt_F1 As String =
            ServicesVSResources.Display_all_hints_while_pressing_Alt_F1

        Public ReadOnly Property Option_Color_hints As String =
            ServicesVSResources.Color_hints

        Public ReadOnly Property Option_Inline_Hints As String =
            ServicesVSResources.Inlay_Hints

        Public ReadOnly Property Option_Display_inline_parameter_name_hints As String =
            ServicesVSResources.Display_inlay_parameter_name_hints

        Public ReadOnly Property Option_Show_hints_for_literals As String =
            ServicesVSResources.Show_hints_for_literals

        Public ReadOnly Property Option_Show_hints_for_New_expressions As String =
            BasicVSResources.Show_hints_for_New_expressions

        Public ReadOnly Property Option_Show_hints_for_everything_else As String =
            ServicesVSResources.Show_hints_for_everything_else

        Public ReadOnly Property Option_Show_hints_for_indexers As String =
            ServicesVSResources.Show_hints_for_indexers

        Public ReadOnly Property Option_Suppress_hints_when_parameter_name_matches_the_method_s_intent As String =
            ServicesVSResources.Suppress_hints_when_parameter_name_matches_the_method_s_intent

        Public ReadOnly Property Option_Suppress_hints_when_parameter_names_differ_only_by_suffix As String =
            ServicesVSResources.Suppress_hints_when_parameter_names_differ_only_by_suffix

        Public ReadOnly Property Option_Suppress_hints_when_argument_matches_parameter_name As String =
            ServicesVSResources.Suppress_hints_when_argument_matches_parameter_name

        Public ReadOnly Property Option_Display_diagnostics_inline_experimental As String =
            ServicesVSResources.Display_diagnostics_inline_experimental

        Public ReadOnly Property Option_at_the_end_of_the_line_of_code As String =
            ServicesVSResources.at_the_end_of_the_line_of_code

        Public ReadOnly Property Option_on_the_right_edge_of_the_editor_window As String =
            ServicesVSResources.on_the_right_edge_of_the_editor_window

        Public ReadOnly Property Option_DontPutOutOrRefOnStruct As String =
            BasicVSResources.Don_t_put_ByRef_on_custom_structure

        Public ReadOnly Property Option_EditorHelp As String =
            BasicVSResources.Editor_Help

        Public ReadOnly Property Option_EnableEndConstruct As String =
            BasicVSResources.A_utomatic_insertion_of_end_constructs

        Public ReadOnly Property Option_EnableHighlightKeywords As String =
            BasicVSResources.Highlight_related_keywords_under_cursor

        Public ReadOnly Property Option_EnableHighlightReferences As String =
            BasicVSResources.Highlight_references_to_symbol_under_cursor

        Public ReadOnly Property Option_EnableLineCommit As String =
            BasicVSResources.Pretty_listing_reformatting_of_code

        Public ReadOnly Property Option_Quick_Actions As String =
            ServicesVSResources.Quick_Actions

        Public ReadOnly Property Option_EnableOutlining As String =
            BasicVSResources.Enter_outlining_mode_when_files_open

        Public ReadOnly Property Option_Collapse_regions_on_file_open As String =
            ServicesVSResources.Collapse_regions_on_file_open

        Public ReadOnly Property Option_Collapse_imports_on_file_open As String =
            BasicVSResources.Collapse_imports_on_file_open

        Public ReadOnly Property Option_Collapse_sourcelink_embedded_decompiled_files_on_open As String =
            ServicesVSResources.Collapse_sourcelink_embedded_decompiled_files_on_open

        Public ReadOnly Property Option_Collapse_metadata_signature_files_on_open As String =
            ServicesVSResources.Collapse_metadata_signature_files_on_open

        Public ReadOnly Property Option_ExtractMethod As String =
            BasicVSResources.Extract_Method

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

        Public ReadOnly Property Option_OptimizeForSolutionSize_Small As String =
            BasicVSResources.Small

        Public ReadOnly Property Option_OptimizeForSolutionSize_Regular As String =
            BasicVSResources.Regular

        Public ReadOnly Property Option_OptimizeForSolutionSize_Large As String =
            BasicVSResources.Large

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

        Public ReadOnly Property Option_Show_guides_for_comments_and_preprocessor_regions As String =
            ServicesVSResources.Show_guides_for_comments_and_preprocessor_regions

        Public ReadOnly Property Option_Fading As String =
            ServicesVSResources.Fading

        Public ReadOnly Property Option_Fade_out_unused_imports As String =
            BasicVSResources.Fade_out_unused_imports

        Public ReadOnly Property Option_Performance As String =
            BasicVSResources.Performance

        Public ReadOnly Property Option_Report_invalid_placeholders_in_string_dot_format_calls As String =
            BasicVSResources.Report_invalid_placeholders_in_string_dot_format_calls

        Public ReadOnly Property Option_RenameTrackingPreview As String =
            BasicVSResources.Show_preview_for_rename_tracking

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

        Public ReadOnly Property Option_JSON_strings As String =
            ServicesVSResources.JSON_strings

        Public ReadOnly Property Option_Colorize_JSON_strings As String =
            ServicesVSResources.Colorize_JSON_strings

        Public ReadOnly Property Option_Report_invalid_JSON_strings As String =
            ServicesVSResources.Report_invalid_JSON_strings

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

        Public ReadOnly Property Color_Scheme_VisualStudio2019_Tag As ColorSchemeName =
            ColorSchemeName.VisualStudio2019

        Public ReadOnly Property Color_Scheme_VisualStudio2017_Tag As ColorSchemeName =
            ColorSchemeName.VisualStudio2017

        Public ReadOnly Property Option_Show_Remove_Unused_References_command_in_Solution_Explorer_experimental As String =
            ServicesVSResources.Show_Remove_Unused_References_command_in_Solution_Explorer_experimental

        Public ReadOnly Property Option_Enable_file_logging_for_diagnostics As String =
            ServicesVSResources.Enable_file_logging_for_diagnostics

        Public ReadOnly Property Option_Skip_analyzers_for_implicitly_triggered_builds As String =
            ServicesVSResources.Skip_analyzers_for_implicitly_triggered_builds

        Public ReadOnly Property Show_inheritance_margin As String =
            ServicesVSResources.Show_inheritance_margin

        Public ReadOnly Property Combine_inheritance_margin_with_indicator_margin As String =
            ServicesVSResources.Combine_inheritance_margin_with_indicator_margin

        Public ReadOnly Property Include_global_imports As String =
            ServicesVSResources.Include_global_imports

        Public ReadOnly Property Inheritance_Margin As String =
            ServicesVSResources.Inheritance_Margin

        Public ReadOnly Property Option_Go_To_Definition As String =
            ServicesVSResources.Go_To_Definition

        Public ReadOnly Property Option_Navigate_asynchronously_exerimental As String =
            ServicesVSResources.Navigate_asynchronously_exerimental

        Public ReadOnly Property Option_Rename As String =
            ServicesVSResources.Rename

        Public ReadOnly Property Option_Rename_asynchronously_experimental As String =
            ServicesVSResources.Rename_asynchronously_experimental

        Public ReadOnly Property Where_should_the_rename_UI_be_shown As String =
            ServicesVSResources.Where_should_the_rename_UI_be_shown

        Public ReadOnly Property Option_Show_UI_inline As String =
            ServicesVSResources.Show_UI_inline

        Public ReadOnly Property Option_Show_UI_as_dashboard_in_top_right As String =
            ServicesVSResources.Show_UI_as_dashboard_in_top_right

        Public ReadOnly Property Document_Outline As String =
            ServicesVSResources.Document_Outline

        Public ReadOnly Property Option_Enable_document_outline_experimental_requires_restart As String =
            ServicesVSResources.Enable_document_outline_experimental_requires_restart

        Public ReadOnly Property Option_Source_Generators As String =
            ServicesVSResources.Source_Generators

        Public ReadOnly Property Option_Source_generator_execution_requires_restart As String =
            ServicesVSResources.Source_generator_execution_requires_restart

        Public ReadOnly Property Option_Automatic_Run_generators_after_any_change As String =
            ServicesVSResources.Automatic_Run_generators_after_any_change

        Public ReadOnly Property Option_Balanced_Run_generators_after_saving_or_building As String =
            ServicesVSResources.Balanced_Run_generators_after_saving_or_building

    End Module
End Namespace
