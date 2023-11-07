// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EditorConfigSettings.DataProvider.Whitespace
{
    internal sealed class CSharpWhitespaceSettingsProvider : SettingsProviderBase<Setting, OptionUpdater, IOption2, object>
    {
        private static readonly Conversions<SpacePlacementWithinParentheses, int> s_spaceBetweenParenthesesConversions = new(v => (int)v, v => (SpacePlacementWithinParentheses)v);
        private static readonly Conversions<NewLineBeforeOpenBracePlacement, int> s_newLinesForBracesConversions = new(v => (int)v, v => (NewLineBeforeOpenBracePlacement)v);

        public CSharpWhitespaceSettingsProvider(string filePath, OptionUpdater updaterService, Workspace workspace, IGlobalOptionService globalOptions)
            : base(filePath, updaterService, workspace, globalOptions)
        {
            Update();
        }

        protected override void UpdateOptions(TieredAnalyzerConfigOptions options, ImmutableArray<Project> projectsInScope)
        {
            var spacingOptions = GetSpacingOptions(options, SettingsUpdater);
            AddRange(spacingOptions.ToImmutableArray());
            var newLineOptions = GetNewLineOptions(options, SettingsUpdater);
            AddRange(newLineOptions.ToImmutableArray());
            var indentationOptions = GetIndentationOptions(options, SettingsUpdater);
            AddRange(indentationOptions.ToImmutableArray());
            var wrappingOptions = GetWrappingOptions(options, SettingsUpdater);
            AddRange(wrappingOptions.ToImmutableArray());
        }

        private static IEnumerable<Setting> GetSpacingOptions(TieredAnalyzerConfigOptions options, OptionUpdater updaterService)
        {
            yield return Setting.Create(CSharpFormattingOptions2.SpacingAfterMethodDeclarationName, CSharpVSResources.Insert_space_between_method_name_and_its_opening_parenthesis, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis, CSharpVSResources.Insert_space_within_parameter_list_parentheses, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses, CSharpVSResources.Insert_space_within_empty_parameter_list_parentheses, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.SpaceAfterMethodCallName, CSharpVSResources.Insert_space_between_called_method_name_and_its_opening_parenthesis, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.SpaceWithinMethodCallParentheses, CSharpVSResources.Insert_space_within_argument_list_parentheses, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses, CSharpVSResources.Insert_space_within_empty_argument_list_parentheses, options, updaterService);

            yield return Setting.Create(CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword, CSharpVSResources.Insert_space_after_keywords_in_control_flow_statements, options, updaterService);

            var spaceBetweenParenthesesValue = new StrongBox<SpacePlacementWithinParentheses>();
            yield return Setting.CreateEnumFlags(CSharpFormattingOptions2.SpaceBetweenParentheses, (int)SpacePlacementWithinParentheses.Expressions, CSharpVSResources.Insert_space_within_parentheses_of_expressions, spaceBetweenParenthesesValue, s_spaceBetweenParenthesesConversions, options, updaterService);
            yield return Setting.CreateEnumFlags(CSharpFormattingOptions2.SpaceBetweenParentheses, (int)SpacePlacementWithinParentheses.TypeCasts, CSharpVSResources.Insert_space_within_parentheses_of_type_casts, spaceBetweenParenthesesValue, s_spaceBetweenParenthesesConversions, options, updaterService);
            yield return Setting.CreateEnumFlags(CSharpFormattingOptions2.SpaceBetweenParentheses, (int)SpacePlacementWithinParentheses.ControlFlowStatements, CSharpVSResources.Insert_spaces_within_parentheses_of_control_flow_statements, spaceBetweenParenthesesValue, s_spaceBetweenParenthesesConversions, options, updaterService);

            yield return Setting.Create(CSharpFormattingOptions2.SpaceAfterCast, CSharpVSResources.Insert_space_after_cast, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration, CSharpVSResources.Ignore_spaces_in_declaration_statements, options, updaterService);

            yield return Setting.Create(CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket, CSharpVSResources.Insert_space_before_open_square_bracket, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets, CSharpVSResources.Insert_space_within_empty_square_brackets, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.SpaceWithinSquareBrackets, CSharpVSResources.Insert_spaces_within_square_brackets, options, updaterService);

            yield return Setting.Create(CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration, CSharpVSResources.Insert_space_after_colon_for_base_or_interface_in_type_declaration, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.SpaceAfterComma, CSharpVSResources.Insert_space_after_comma, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.SpaceAfterDot, CSharpVSResources.Insert_space_after_dot, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement, CSharpVSResources.Insert_space_after_semicolon_in_for_statement, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration, CSharpVSResources.Insert_space_before_colon_for_base_or_interface_in_type_declaration, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.SpaceBeforeComma, CSharpVSResources.Insert_space_before_comma, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.SpaceBeforeDot, CSharpVSResources.Insert_space_before_dot, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement, CSharpVSResources.Insert_space_before_semicolon_in_for_statement, options, updaterService);

            yield return Setting.Create(CSharpFormattingOptions2.SpacingAroundBinaryOperator, CSharpVSResources.Set_spacing_for_operators, options, updaterService);
        }

        private static IEnumerable<Setting> GetNewLineOptions(TieredAnalyzerConfigOptions options, OptionUpdater updaterService)
        {
            var newLineBeforeOpenBraceValue = new StrongBox<NewLineBeforeOpenBracePlacement>();
            yield return Setting.CreateEnumFlags(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.Types, CSharpVSResources.Place_open_brace_on_new_line_for_types, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions, options, updaterService);
            yield return Setting.CreateEnumFlags(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.Methods, CSharpVSResources.Place_open_brace_on_new_line_for_methods_local_functions, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions, options, updaterService);
            yield return Setting.CreateEnumFlags(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.Properties, CSharpVSResources.Place_open_brace_on_new_line_for_properties_indexers_and_events, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions, options, updaterService);
            yield return Setting.CreateEnumFlags(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.Accessors, CSharpVSResources.Place_open_brace_on_new_line_for_property_indexer_and_event_accessors, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions, options, updaterService);
            yield return Setting.CreateEnumFlags(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.AnonymousMethods, CSharpVSResources.Place_open_brace_on_new_line_for_anonymous_methods, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions, options, updaterService);
            yield return Setting.CreateEnumFlags(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.ControlBlocks, CSharpVSResources.Place_open_brace_on_new_line_for_control_blocks, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions, options, updaterService);
            yield return Setting.CreateEnumFlags(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.AnonymousTypes, CSharpVSResources.Place_open_brace_on_new_line_for_anonymous_types, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions, options, updaterService);
            yield return Setting.CreateEnumFlags(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, CSharpVSResources.Place_open_brace_on_new_line_for_object_collection_array_and_with_initializers, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions, options, updaterService);
            yield return Setting.CreateEnumFlags(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.LambdaExpressionBody, CSharpVSResources.Place_open_brace_on_new_line_for_lambda_expression, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions, options, updaterService);

            yield return Setting.Create(CSharpFormattingOptions2.NewLineForElse, CSharpVSResources.Place_else_on_new_line, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.NewLineForCatch, CSharpVSResources.Place_catch_on_new_line, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.NewLineForFinally, CSharpVSResources.Place_finally_on_new_line, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.NewLineForMembersInObjectInit, CSharpVSResources.Place_members_in_object_initializers_on_new_line, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes, CSharpVSResources.Place_members_in_anonymous_types_on_new_line, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.NewLineForClausesInQuery, CSharpVSResources.Place_query_expression_clauses_on_new_line, options, updaterService);
        }

        private static IEnumerable<Setting> GetIndentationOptions(TieredAnalyzerConfigOptions options, OptionUpdater updaterService)
        {
            yield return Setting.Create(CSharpFormattingOptions2.IndentBlock, CSharpVSResources.Indent_block_contents, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.IndentBraces, CSharpVSResources.Indent_open_and_close_braces, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.IndentSwitchCaseSection, CSharpVSResources.Indent_case_contents, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock, CSharpVSResources.Indent_case_contents_when_block, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.IndentSwitchSection, CSharpVSResources.Indent_case_labels, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.LabelPositioning, CSharpVSResources.Label_Indentation, options, updaterService);
        }

        private static IEnumerable<Setting> GetWrappingOptions(TieredAnalyzerConfigOptions options, OptionUpdater updaterService)
        {
            yield return Setting.Create(CSharpFormattingOptions2.WrappingPreserveSingleLine, CSharpVSResources.Leave_block_on_single_line, options, updaterService);
            yield return Setting.Create(CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine, CSharpVSResources.Leave_statements_and_member_declarations_on_the_same_line, options, updaterService);
        }
    }
}
