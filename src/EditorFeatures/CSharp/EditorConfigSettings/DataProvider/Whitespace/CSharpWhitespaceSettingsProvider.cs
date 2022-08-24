// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.EditorConfigSettings;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EditorConfigSettings.DataProvider.Whitespace
{
    internal class CSharpWhitespaceSettingsProvider : SettingsProviderBase<WhitespaceSetting, OptionUpdater, IOption2, object>
    {
        public CSharpWhitespaceSettingsProvider(string filePath, OptionUpdater updaterService, Workspace workspace)
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

        private IEnumerable<WhitespaceSetting> GetSpacingOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpacingAfterMethodDeclarationName, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpacingAfterMethodDeclarationName);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceWithinMethodDeclarationParenthesis);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceBetweenEmptyMethodDeclarationParentheses);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceAfterMethodCallName, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceAfterMethodCallName);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceWithinMethodCallParentheses, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceWithinMethodCallParentheses);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceBetweenEmptyMethodCallParentheses);

            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceAfterControlFlowStatementKeyword);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceWithinExpressionParentheses, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceBetweenParentheses, description: CSharpEditorResources.Insert_space_within_parentheses_of_expressions);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceWithinCastParentheses, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceBetweenParentheses, description: CSharpEditorResources.Insert_space_within_parentheses_of_type_casts);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceWithinOtherParentheses, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceBetweenParentheses, description: CSharpEditorResources.Insert_spaces_within_parentheses_of_control_flow_statements);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceAfterCast, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceAfterCast);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpacesIgnoreAroundVariableDeclaration);

            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceBeforeOpenSquareBracket);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceBetweenEmptySquareBrackets);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceWithinSquareBrackets, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceWithinSquareBrackets);

            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceAfterColonInBaseTypeDeclaration);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceAfterComma, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceAfterComma);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceAfterDot, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceAfterDot);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceAfterSemicolonsInForStatement);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceBeforeColonInBaseTypeDeclaration);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceBeforeComma, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceBeforeComma);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceBeforeDot, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceBeforeDot);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpaceBeforeSemicolonsInForStatement);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.SpacingAroundBinaryOperator, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.SpacingAroundBinaryOperator);
        }

        private IEnumerable<WhitespaceSetting> GetNewLineOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInTypes, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.NewLineBeforeOpenBrace, description: CSharpEditorResources.Place_open_brace_on_new_line_for_types);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInMethods, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.NewLineBeforeOpenBrace, description: CSharpEditorResources.Place_open_brace_on_new_line_for_methods_local_functions);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInProperties, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.NewLineBeforeOpenBrace, description: CSharpEditorResources.Place_open_brace_on_new_line_for_properties_indexers_and_events);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInAccessors, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.NewLineBeforeOpenBrace, description: CSharpEditorResources.Place_open_brace_on_new_line_for_property_indexer_and_event_accessors);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInAnonymousMethods, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.NewLineBeforeOpenBrace, description: CSharpEditorResources.Place_open_brace_on_new_line_for_anonymous_methods);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInControlBlocks, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.NewLineBeforeOpenBrace, description: CSharpEditorResources.Place_open_brace_on_new_line_for_control_blocks);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInAnonymousTypes, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.NewLineBeforeOpenBrace, description: CSharpEditorResources.Place_open_brace_on_new_line_for_anonymous_types);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.NewLineBeforeOpenBrace, description: CSharpEditorResources.Place_open_brace_on_new_line_for_object_collection_array_and_with_initializers);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.NewLinesForBracesInLambdaExpressionBody, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.NewLineBeforeOpenBrace, description: CSharpEditorResources.Place_open_brace_on_new_line_for_lambda_expression);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.NewLineForElse, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.NewLineForElse);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.NewLineForCatch, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.NewLineForCatch);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.NewLineForFinally, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.NewLineForFinally);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.NewLineForMembersInObjectInit, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.NewLineForMembersInObjectInit);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.NewLineForMembersInAnonymousTypes);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.NewLineForClausesInQuery, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.NewLineForClausesInQuery);
        }

        private IEnumerable<WhitespaceSetting> GetIndentationOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.IndentBlock, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.IndentBlock);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.IndentBraces, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.IndentBraces);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.IndentSwitchCaseSection, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.IndentSwitchCaseSection);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.IndentSwitchCaseSectionWhenBlock);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.IndentSwitchSection, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.IndentSwitchSection);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.LabelPositioning, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.LabelPositioning);
        }

        private IEnumerable<WhitespaceSetting> GetWrappingOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.WrappingPreserveSingleLine, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.WrappingPreserveSingleLine);
            yield return WhitespaceSetting.Create(CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine, editorConfigOptions, visualStudioOptions, updaterService, FileName, CSharpEditorConfigSettingsData.WrappingKeepStatementsOnSingleLine);
        }
    }
}
