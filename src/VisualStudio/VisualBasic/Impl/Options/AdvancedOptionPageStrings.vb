' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Friend Module AdvancedOptionPageStrings

        Public ReadOnly Property Option_AllowMovingDeclaration As String
            Get
                Return BasicVSResources.Move_local_declaration_to_the_extracted_method_if_it_is_not_used_elsewhere
            End Get
        End Property

        Public ReadOnly Property Option_AutomaticInsertionOfInterfaceAndMustOverrideMembers As String
            Get
                Return BasicVSResources.Automatic_insertion_of_Interface_and_MustOverride_members
            End Get
        End Property

        Public ReadOnly Property Option_Analysis As String =
            ServicesVSResources.Analysis

        Public ReadOnly Property Option_Enable_full_solution_analysis As String =
            ServicesVSResources.Enable_full_solution_analysis

        Public ReadOnly Property Option_use_editorconfig_compatibility_mode As String = ServicesVSResources.Use_editorconfig_compatibility_mode

        Public ReadOnly Property Option_DisplayLineSeparators As String
            Get
                Return BasicVSResources.Show_procedure_line_separators
            End Get
        End Property

        Public ReadOnly Property Option_DontPutOutOrRefOnStruct As String
            Get
                Return BasicVSResources.Don_t_put_ByRef_on_custom_structure
            End Get
        End Property

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

        Public ReadOnly Property Option_GenerateXmlDocCommentsForTripleApostrophes As String
            Get
                Return BasicVSResources.Generate_XML_documentation_comments_for
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

        Public ReadOnly Property Option_Classifications As String =
            ServicesVSResources.Classifications

        Public ReadOnly Property Option_Use_enhanced_colors_for_C_and_Basic As String =
            ServicesVSResources.Use_enhanced_colors_for_C_and_Basic
    End Module
End Namespace
