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

        Public ReadOnly Property Option_ClosedFileDiagnostics As String
            Get
                Return BasicVSResources.Enable_full_solution_analysis
            End Get
        End Property

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

        Public ReadOnly Property Option_Outlining As String
            Get
                Return BasicVSResources.Outlining
            End Get
        End Property

        Public ReadOnly Property Option_Performance As String
            Get
                Return BasicVSResources.Performance
            End Get
        End Property

        Public ReadOnly Property Option_RenameTrackingPreview As String
            Get
                Return BasicVSResources.Show_preview_for_rename_tracking
            End Get
        End Property

        Public ReadOnly Property Option_Import_Directives As String
            Get
                Return BasicVSResources.Import_Directives
            End Get
        End Property

        Public ReadOnly Property Option_PlaceSystemNamespaceFirst As String
            Get
                Return BasicVSResources.Place_System_directives_first_when_sorting_imports
            End Get
        End Property

        Public ReadOnly Property Option_Suggest_imports_for_types_in_reference_assemblies As String
            Get
                Return BasicVSResources.Suggest_imports_for_types_in_reference_assemblies
            End Get
        End Property

        Public ReadOnly Property Option_Suggest_imports_for_types_in_NuGet_packages As String
            Get
                Return BasicVSResources.Suggest_imports_for_types_in_NuGet_packages
            End Get
        End Property
    End Module
End Namespace