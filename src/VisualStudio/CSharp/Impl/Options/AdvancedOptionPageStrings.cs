// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    internal static class AdvancedOptionPageStrings
    {
        public static string Option_AllowMovingDeclaration
        {
            get { return CSharpVSResources.Move_local_declaration_to_the_extracted_method_if_it_is_not_used_elsewhere; }
        }

        public static string Option_ClosedFileDiagnostics
        {
            get { return CSharpVSResources.Enable_full_solution_analysis; }
        }

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
        {
            get { return CSharpVSResources.Extract_Method; }
        }

        public static string Option_GenerateXmlDocCommentsForTripleSlash
        {
            get { return CSharpVSResources.Generate_XML_documentation_comments_for; }
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
        {
            get { return CSharpVSResources.Outlining; }
        }

        public static string Option_Performance
        {
            get { return CSharpVSResources.Performance; }
        }

        public static string Option_PlaceSystemNamespaceFirst
        {
            get { return CSharpVSResources.Place_System_directives_first_when_sorting_usings; }
        }

        public static string Option_Using_Directives =>
            CSharpVSResources.Using_Directives;

        public static string Option_Suggest_usings_for_types_in_reference_assemblies =>
            CSharpVSResources.Suggest_usings_for_types_in_reference_assemblies;

        public static string Option_Suggest_usings_for_types_in_NuGet_packages =>
            CSharpVSResources.Suggest_usings_for_types_in_NuGet_packages;
    }
}
