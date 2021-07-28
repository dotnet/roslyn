// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EditorConfigSettings.DataProvider.Formatting
{
    internal class CSharpFormattingSettingsProvider : SettingsProviderBase<FormattingSetting, OptionUpdater, IOption2, object>
    {
        public CSharpFormattingSettingsProvider(string filePath, OptionUpdater updaterService, Workspace workspace)
            : base(filePath, updaterService, workspace)
        {
            Update();
        }

        protected override void UpdateOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions)
        {
            var spacingOptions = GetSpacingOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(spacingOptions.ToImmutableArray());
            var newLineOptions = GetNewLineOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(newLineOptions.ToImmutableArray());
            var indentationOptions = GetIndentationOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(indentationOptions.ToImmutableArray());
            var wrappingOptions = GetWrappingOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(wrappingOptions.ToImmutableArray());
        }

        private static IEnumerable<FormattingSetting> GetSpacingOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpacingAfterMethodDeclarationName, CSharpEditorResources.Insert_space_between_method_name_and_its_opening_parenthesis2, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis, CSharpEditorResources.Insert_space_within_parameter_list_parentheses, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses, CSharpEditorResources.Insert_space_within_empty_parameter_list_parentheses, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceAfterMethodCallName, CSharpEditorResources.Insert_space_between_method_name_and_its_opening_parenthesis1, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceWithinMethodCallParentheses, CSharpEditorResources.Insert_space_within_argument_list_parentheses, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses, CSharpEditorResources.Insert_space_within_empty_argument_list_parentheses, editorConfigOptions, visualStudioOptions, updaterService);

            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword, CSharpEditorResources.Insert_space_after_keywords_in_control_flow_statements, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceWithinExpressionParentheses, CSharpEditorResources.Insert_space_within_parentheses_of_expressions, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceWithinCastParentheses, CSharpEditorResources.Insert_space_within_parentheses_of_type_casts, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceWithinOtherParentheses, CSharpEditorResources.Insert_spaces_within_parentheses_of_control_flow_statements, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceAfterCast, CSharpEditorResources.Insert_space_after_cast, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration, CSharpEditorResources.Ignore_spaces_in_declaration_statements, editorConfigOptions, visualStudioOptions, updaterService);

            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket, CSharpEditorResources.Insert_space_before_open_square_bracket, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets, CSharpEditorResources.Insert_space_within_empty_square_brackets, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceWithinSquareBrackets, CSharpEditorResources.Insert_spaces_within_square_brackets, editorConfigOptions, visualStudioOptions, updaterService);

            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration, CSharpEditorResources.Insert_space_after_colon_for_base_or_interface_in_type_declaration, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceAfterComma, CSharpEditorResources.Insert_space_after_comma, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceAfterDot, CSharpEditorResources.Insert_space_after_dot, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement, CSharpEditorResources.Insert_space_after_semicolon_in_for_statement, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration, CSharpEditorResources.Insert_space_before_colon_for_base_or_interface_in_type_declaration, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceBeforeComma, CSharpEditorResources.Insert_space_before_comma, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceBeforeDot, CSharpEditorResources.Insert_space_before_dot, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement, CSharpEditorResources.Insert_space_before_semicolon_in_for_statement, editorConfigOptions, visualStudioOptions, updaterService);

            yield return FormattingSetting.Create(CSharpFormattingOptions2.SpacingAroundBinaryOperator, CSharpEditorResources.Set_spacing_for_operators, editorConfigOptions, visualStudioOptions, updaterService);
        }

        private static IEnumerable<FormattingSetting> GetNewLineOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return FormattingSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInTypes, CSharpEditorResources.Place_open_brace_on_new_line_for_types, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInMethods, CSharpEditorResources.Place_open_brace_on_new_line_for_methods_local_functions, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInProperties, CSharpEditorResources.Place_open_brace_on_new_line_for_properties_indexers_and_events, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInAccessors, CSharpEditorResources.Place_open_brace_on_new_line_for_property_indexer_and_event_accessors, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInAnonymousMethods, CSharpEditorResources.Place_open_brace_on_new_line_for_anonymous_methods, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInControlBlocks, CSharpEditorResources.Place_open_brace_on_new_line_for_control_blocks, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInAnonymousTypes, CSharpEditorResources.Place_open_brace_on_new_line_for_anonymous_types, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers, CSharpEditorResources.Place_open_brace_on_new_line_for_object_collection_array_and_with_initializers, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInLambdaExpressionBody, CSharpEditorResources.Place_open_brace_on_new_line_for_lambda_expression, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.NewLineForElse, CSharpEditorResources.Place_else_on_new_line, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.NewLineForCatch, CSharpEditorResources.Place_catch_on_new_line, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.NewLineForFinally, CSharpEditorResources.Place_finally_on_new_line, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.NewLineForMembersInObjectInit, CSharpEditorResources.Place_members_in_object_initializers_on_new_line, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes, CSharpEditorResources.Place_members_in_anonymous_types_on_new_line, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.NewLineForClausesInQuery, CSharpEditorResources.Place_query_expression_clauses_on_new_line, editorConfigOptions, visualStudioOptions, updaterService);
        }

        private static IEnumerable<FormattingSetting> GetIndentationOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return FormattingSetting.Create(CSharpFormattingOptions2.IndentBlock, CSharpEditorResources.Indent_block_contents, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.IndentBraces, CSharpEditorResources.Indent_open_and_close_braces, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.IndentSwitchCaseSection, CSharpEditorResources.Indent_case_contents, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock, CSharpEditorResources.Indent_case_contents_when_block, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.IndentSwitchSection, CSharpEditorResources.Indent_case_labels, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.LabelPositioning, CSharpEditorResources.Label_Indentation, editorConfigOptions, visualStudioOptions, updaterService);
        }

        private static IEnumerable<FormattingSetting> GetWrappingOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return FormattingSetting.Create(CSharpFormattingOptions2.WrappingPreserveSingleLine, CSharpEditorResources.Leave_block_on_single_line, editorConfigOptions, visualStudioOptions, updaterService);
            yield return FormattingSetting.Create(CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine, CSharpEditorResources.Leave_statements_and_member_declarations_on_the_same_line, editorConfigOptions, visualStudioOptions, updaterService);
        }
    }
}
