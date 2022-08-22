// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.EditorConfigSettings;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions2;

namespace Microsoft.CodeAnalysis.CSharp.EditorConfigSettings

{
    internal partial class CSharpEditorConfigSettingsData
    {
        private static readonly BidirectionalMap<string, bool> SpacesIgnoreAroundVariableDeclarationMap =
           new(new[]
           {
                KeyValuePairUtil.Create("ignore", true),
                KeyValuePairUtil.Create("false", false),
           });

        private static readonly BidirectionalMap<string, SpacingWithinParenthesesOption> SpacingWithinParenthesisOptionsEditorConfigMap =
            new(new[]
            {
                KeyValuePairUtil.Create("expressions", SpacingWithinParenthesesOption.Expressions),
                KeyValuePairUtil.Create("type_casts", SpacingWithinParenthesesOption.TypeCasts),
                KeyValuePairUtil.Create("control_flow_statements", SpacingWithinParenthesesOption.ControlFlowStatements),
            });

        private static readonly BidirectionalMap<string, BinaryOperatorSpacingOptions> BinaryOperatorSpacingOptionsEditorConfigMap =
            new(new[]
            {
                KeyValuePairUtil.Create("ignore", BinaryOperatorSpacingOptions.Ignore),
                KeyValuePairUtil.Create("none", BinaryOperatorSpacingOptions.Remove),
                KeyValuePairUtil.Create("before_and_after", BinaryOperatorSpacingOptions.Single),
            });

        private static readonly BidirectionalMap<string, NewLineOption> NewLineOptionsEditorConfigMap =
            new(new[]
            {
                KeyValuePairUtil.Create("accessors", NewLineOption.Accessors),
                KeyValuePairUtil.Create("types", NewLineOption.Types),
                KeyValuePairUtil.Create("methods", NewLineOption.Methods),
                KeyValuePairUtil.Create("properties", NewLineOption.Properties),
                KeyValuePairUtil.Create("indexers", NewLineOption.Indexers),
                KeyValuePairUtil.Create("events", NewLineOption.Events),
                KeyValuePairUtil.Create("anonymous_methods", NewLineOption.AnonymousMethods),
                KeyValuePairUtil.Create("control_blocks", NewLineOption.ControlBlocks),
                KeyValuePairUtil.Create("anonymous_types", NewLineOption.AnonymousTypes),
                KeyValuePairUtil.Create("object_collection_array_initializers", NewLineOption.ObjectCollectionsArrayInitializers),
                KeyValuePairUtil.Create("lambdas", NewLineOption.Lambdas),
                KeyValuePairUtil.Create("local_functions", NewLineOption.LocalFunction),
            });

        private static readonly BidirectionalMap<string, LabelPositionOptions> LabelPositionOptionsEditorConfigMap =
            new(new[]
            {
                KeyValuePairUtil.Create("flush_left", LabelPositionOptions.LeftMost),
                KeyValuePairUtil.Create("no_change", LabelPositionOptions.NoIndent),
                KeyValuePairUtil.Create("one_less_than_current", LabelPositionOptions.OneLess),
            });

        #region Spacing Options
        public static EditorConfigData<bool> SpacingAfterMethodDeclarationName = new BooleanEditorConfigData("csharp_space_between_method_declaration_name_and_open_parenthesis",
                                                                                                             CSharpCompilerExtensionsResources.Insert_space_between_method_name_and_its_opening_parenthesis2);

        public static EditorConfigData<bool> SpaceWithinMethodDeclarationParenthesis = new BooleanEditorConfigData("csharp_space_between_method_declaration_parameter_list_parentheses",
                                                                                                                   CSharpCompilerExtensionsResources.Insert_space_within_parameter_list_parentheses);

        public static EditorConfigData<bool> SpaceBetweenEmptyMethodDeclarationParentheses = new BooleanEditorConfigData("csharp_space_between_method_declaration_empty_parameter_list_parentheses",
                                                                                                                         CSharpCompilerExtensionsResources.Insert_space_within_empty_parameter_list_parentheses);

        public static EditorConfigData<bool> SpaceAfterMethodCallName = new BooleanEditorConfigData("csharp_space_between_method_call_name_and_opening_parenthesis",
                                                                                                    CSharpCompilerExtensionsResources.Insert_space_between_method_name_and_its_opening_parenthesis1);

        public static EditorConfigData<bool> SpaceWithinMethodCallParentheses = new BooleanEditorConfigData("csharp_space_between_method_call_parameter_list_parentheses",
                                                                                                            CSharpCompilerExtensionsResources.Insert_space_within_argument_list_parentheses);

        public static EditorConfigData<bool> SpaceBetweenEmptyMethodCallParentheses = new BooleanEditorConfigData("csharp_space_between_method_call_empty_parameter_list_parentheses",
                                                                                                                  CSharpCompilerExtensionsResources.Insert_space_within_empty_argument_list_parentheses);

        public static EditorConfigData<bool> SpaceAfterControlFlowStatementKeyword = new BooleanEditorConfigData("csharp_space_after_keywords_in_control_flow_statements",
                                                                                                                 CSharpCompilerExtensionsResources.Insert_space_after_keywords_in_control_flow_statements);

        public static EditorConfigData<SpacingWithinParenthesesOption> SpaceBetweenParentheses = new EnumEditorConfigData<SpacingWithinParenthesesOption>("csharp_space_between_parentheses",
                                                                                                                                                          CSharpCompilerExtensionsResources.Insert_space_within_parentheses,
                                                                                                                                                          SpacingWithinParenthesisOptionsEditorConfigMap,
                                                                                                                                                          allowsMultipleValues: true);

        public static EditorConfigData<bool> SpaceAfterCast = new BooleanEditorConfigData("csharp_space_after_cast",
                                                                                          CSharpCompilerExtensionsResources.Insert_space_after_cast);

        public static EditorConfigData<bool> SpacesIgnoreAroundVariableDeclaration = new BooleanEditorConfigData("csharp_space_around_declaration_statements",
                                                                                                                 CSharpCompilerExtensionsResources.Ignore_spaces_in_declaration_statements,
                                                                                                                 SpacesIgnoreAroundVariableDeclarationMap);

        public static EditorConfigData<bool> SpaceBeforeOpenSquareBracket = new BooleanEditorConfigData("csharp_space_before_open_square_brackets",
                                                                                                        CSharpCompilerExtensionsResources.Insert_space_before_open_square_bracket);

        public static EditorConfigData<bool> SpaceBetweenEmptySquareBrackets = new BooleanEditorConfigData("csharp_space_between_empty_square_brackets",
                                                                                                           CSharpCompilerExtensionsResources.Insert_space_within_empty_square_brackets);

        public static EditorConfigData<bool> SpaceWithinSquareBrackets = new BooleanEditorConfigData("csharp_space_between_square_brackets",
                                                                                                     CSharpCompilerExtensionsResources.Insert_spaces_within_square_brackets);

        public static EditorConfigData<bool> SpaceAfterColonInBaseTypeDeclaration = new BooleanEditorConfigData("csharp_space_after_colon_in_inheritance_clause",
                                                                                                                CSharpCompilerExtensionsResources.Insert_space_after_colon_for_base_or_interface_in_type_declaration);

        public static EditorConfigData<bool> SpaceAfterComma = new BooleanEditorConfigData("csharp_space_after_comma",
                                                                                           CSharpCompilerExtensionsResources.Insert_space_after_comma);

        public static EditorConfigData<bool> SpaceAfterDot = new BooleanEditorConfigData("csharp_space_after_dot",
                                                                                         CSharpCompilerExtensionsResources.Insert_space_after_dot);

        public static EditorConfigData<bool> SpaceAfterSemicolonsInForStatement = new BooleanEditorConfigData("csharp_space_after_semicolon_in_for_statement",
                                                                                                              CSharpCompilerExtensionsResources.Insert_space_after_semicolon_in_for_statement);

        public static EditorConfigData<bool> SpaceBeforeColonInBaseTypeDeclaration = new BooleanEditorConfigData("csharp_space_before_colon_in_inheritance_clause",
                                                                                                                 CSharpCompilerExtensionsResources.Insert_space_before_colon_for_base_or_interface_in_type_declaration);

        public static EditorConfigData<bool> SpaceBeforeComma = new BooleanEditorConfigData("csharp_space_before_comma",
                                                                                            CSharpCompilerExtensionsResources.Insert_space_before_comma);

        public static EditorConfigData<bool> SpaceBeforeDot = new BooleanEditorConfigData("csharp_space_before_dot",
                                                                                          CSharpCompilerExtensionsResources.Insert_space_before_dot);

        public static EditorConfigData<bool> SpaceBeforeSemicolonsInForStatement = new BooleanEditorConfigData("csharp_space_before_semicolon_in_for_statement",
                                                                                                               CSharpCompilerExtensionsResources.Insert_space_before_semicolon_in_for_statement);

        public static EditorConfigData<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator = new EnumEditorConfigData<BinaryOperatorSpacingOptions>("csharp_space_around_binary_operators",
                                                                                                                                                          CSharpCompilerExtensionsResources.Set_spacing_for_operators,
                                                                                                                                                          BinaryOperatorSpacingOptionsEditorConfigMap,
                                                                                                                                                          BinaryOperatorSpacingOptions.Single);
        #endregion

        #region New Line Options
        public static EditorConfigData<NewLineOption> NewLineBeforeOpenBrace = new EnumEditorConfigData<NewLineOption>("csharp_new_line_before_open_brace",
                                                                                                                       CSharpCompilerExtensionsResources.Place_open_brace_on_new_line,
                                                                                                                       NewLineOptionsEditorConfigMap,
                                                                                                                       allowsMultipleValues: true);

        public static EditorConfigData<bool> NewLineForElse = new BooleanEditorConfigData("csharp_new_line_before_else",
                                                                                          CSharpCompilerExtensionsResources.Place_else_on_new_line);

        public static EditorConfigData<bool> NewLineForCatch = new BooleanEditorConfigData("csharp_new_line_before_catch",
                                                                                           CSharpCompilerExtensionsResources.Place_catch_on_new_line);

        public static EditorConfigData<bool> NewLineForFinally = new BooleanEditorConfigData("csharp_new_line_before_finally",
                                                                                             CSharpCompilerExtensionsResources.Place_finally_on_new_line);

        public static EditorConfigData<bool> NewLineForMembersInObjectInit = new BooleanEditorConfigData("csharp_new_line_before_members_in_object_initializers",
                                                                                                         CSharpCompilerExtensionsResources.Place_members_in_object_initializers_on_new_line);

        public static EditorConfigData<bool> NewLineForMembersInAnonymousTypes = new BooleanEditorConfigData("csharp_new_line_before_members_in_anonymous_types",
                                                                                                             CSharpCompilerExtensionsResources.Place_members_in_anonymous_types_on_new_line);

        public static EditorConfigData<bool> NewLineForClausesInQuery = new BooleanEditorConfigData("csharp_new_line_between_query_expression_clauses",
                                                                                                    CSharpCompilerExtensionsResources.Place_query_expression_clauses_on_new_line);
        #endregion

        #region Indentation Options
        public static EditorConfigData<bool> IndentBlock = new BooleanEditorConfigData("csharp_indent_block_contents",
                                                                                       CSharpCompilerExtensionsResources.Indent_block_contents);

        public static EditorConfigData<bool> IndentBraces = new BooleanEditorConfigData("csharp_indent_braces",
                                                                                        CSharpCompilerExtensionsResources.Indent_open_and_close_braces);

        public static EditorConfigData<bool> IndentSwitchCaseSection = new BooleanEditorConfigData("csharp_indent_case_contents",
                                                                                                   CSharpCompilerExtensionsResources.Indent_case_contents);

        public static EditorConfigData<bool> IndentSwitchCaseSectionWhenBlock = new BooleanEditorConfigData("csharp_indent_case_contents_when_block",
                                                                                                            CSharpCompilerExtensionsResources.Indent_case_contents_when_block);

        public static EditorConfigData<bool> IndentSwitchSection = new BooleanEditorConfigData("csharp_indent_switch_labels",
                                                                                               CSharpCompilerExtensionsResources.Indent_case_labels);

        public static EditorConfigData<LabelPositionOptions> LabelPositioning = new EnumEditorConfigData<LabelPositionOptions>("csharp_indent_labels",
                                                                                                                               CSharpCompilerExtensionsResources.Label_Indentation,
                                                                                                                               LabelPositionOptionsEditorConfigMap,
                                                                                                                               LabelPositionOptions.NoIndent);
        #endregion

        #region Wrapping Options
        public static EditorConfigData<bool> WrappingPreserveSingleLine = new BooleanEditorConfigData("csharp_preserve_single_line_blocks",
                                                                                                      CSharpCompilerExtensionsResources.Leave_block_on_single_line);

        public static EditorConfigData<bool> WrappingKeepStatementsOnSingleLine = new BooleanEditorConfigData("csharp_preserve_single_line_statements",
                                                                                                              CSharpCompilerExtensionsResources.Leave_statements_and_member_declarations_on_the_same_line);
        #endregion
    }
}
