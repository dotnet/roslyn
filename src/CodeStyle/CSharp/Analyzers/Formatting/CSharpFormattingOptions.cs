// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal static partial class CSharpFormattingOptions
    {
        // Maps to store mapping between special option kinds and the corresponding editor config string representations.
        #region Editor Config maps
        private static readonly BidirectionalMap<string, SpacingWithinParenthesesOption> s_spacingWithinParenthesisOptionsEditorConfigMap =
            new BidirectionalMap<string, SpacingWithinParenthesesOption>(new[]
            {
                KeyValuePairUtil.Create("expressions", SpacingWithinParenthesesOption.Expressions),
                KeyValuePairUtil.Create("type_casts", SpacingWithinParenthesesOption.TypeCasts),
                KeyValuePairUtil.Create("control_flow_statements", SpacingWithinParenthesesOption.ControlFlowStatements),
            });
        private static readonly BidirectionalMap<string, BinaryOperatorSpacingOptions> s_binaryOperatorSpacingOptionsEditorConfigMap =
            new BidirectionalMap<string, BinaryOperatorSpacingOptions>(new[]
            {
                KeyValuePairUtil.Create("ignore", BinaryOperatorSpacingOptions.Ignore),
                KeyValuePairUtil.Create("none", BinaryOperatorSpacingOptions.Remove),
                KeyValuePairUtil.Create("before_and_after", BinaryOperatorSpacingOptions.Single),
            });
        private static readonly BidirectionalMap<string, LabelPositionOptions> s_labelPositionOptionsEditorConfigMap =
            new BidirectionalMap<string, LabelPositionOptions>(new[]
            {
                KeyValuePairUtil.Create("flush_left", LabelPositionOptions.LeftMost),
                KeyValuePairUtil.Create("no_change", LabelPositionOptions.NoIndent),
                KeyValuePairUtil.Create("one_less_than_current", LabelPositionOptions.OneLess),
            });
        private static readonly BidirectionalMap<string, NewLineOption> s_legacyNewLineOptionsEditorConfigMap =
            new BidirectionalMap<string, NewLineOption>(new[]
            {
                KeyValuePairUtil.Create("object_collection_array_initalizers", NewLineOption.ObjectCollectionsArrayInitializers),
            });
        private static readonly BidirectionalMap<string, NewLineOption> s_newLineOptionsEditorConfigMap =
            new BidirectionalMap<string, NewLineOption>(new[]
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
        #endregion

        private static Option<T> CreateOption<T>(string name, T defaultValue, params OptionStorageLocation[] storageLocations)
        {
            return new Option<T>(nameof(CSharpFormattingOptions), name, defaultValue, storageLocations);
        }

        private static Option<bool> CreateSpaceWithinParenthesesOption(SpacingWithinParenthesesOption parenthesesOption, string name)
        {
            return CreateOption(
                name,
                defaultValue: false,
                storageLocations: new OptionStorageLocation[] {
                    new EditorConfigStorageLocation<bool>(
                        "csharp_space_between_parentheses",
                        s => DetermineIfSpaceOptionIsSet(s, parenthesesOption),
                        GetSpacingWithParenthesesEditorConfigString),
                    new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{name}")});
        }

        private static Option<bool> CreateNewLineForBracesOption(NewLineOption newLineOption, string name)
        {
            return CreateOption(
                name,
                defaultValue: true,
                storageLocations: new OptionStorageLocation[] {
                    new EditorConfigStorageLocation<bool>(
                        "csharp_new_line_before_open_brace",
                        value => DetermineIfNewLineOptionIsSet(value, newLineOption),
                        GetNewLineOptionEditorConfigString),
                    new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{name}")});
        }

        public static Option<bool> SpacingAfterMethodDeclarationName { get; } = CreateOption(
            nameof(SpacingAfterMethodDeclarationName),
            defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_declaration_name_and_open_parenthesis"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpacingAfterMethodDeclarationName")});

        public static Option<bool> SpaceWithinMethodDeclarationParenthesis { get; } = CreateOption(
            nameof(SpaceWithinMethodDeclarationParenthesis),
            defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_declaration_parameter_list_parentheses"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinMethodDeclarationParenthesis")});

        public static Option<bool> SpaceBetweenEmptyMethodDeclarationParentheses { get; } = CreateOption(
            nameof(SpaceBetweenEmptyMethodDeclarationParentheses),
            defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_declaration_empty_parameter_list_parentheses"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBetweenEmptyMethodDeclarationParentheses")});

        public static Option<bool> SpaceAfterMethodCallName { get; } = CreateOption(
            nameof(SpaceAfterMethodCallName),
            defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_call_name_and_opening_parenthesis"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterMethodCallName")});

        public static Option<bool> SpaceWithinMethodCallParentheses { get; } = CreateOption(
            nameof(SpaceWithinMethodCallParentheses),
            defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_call_parameter_list_parentheses"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinMethodCallParentheses")});

        public static Option<bool> SpaceBetweenEmptyMethodCallParentheses { get; } = CreateOption(
            nameof(SpaceBetweenEmptyMethodCallParentheses),
            defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_call_empty_parameter_list_parentheses"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBetweenEmptyMethodCallParentheses")});

        public static Option<bool> SpaceAfterControlFlowStatementKeyword { get; } = CreateOption(
            nameof(SpaceAfterControlFlowStatementKeyword),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_keywords_in_control_flow_statements"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterControlFlowStatementKeyword")});

        public static Option<bool> SpaceWithinExpressionParentheses { get; } = CreateSpaceWithinParenthesesOption(
            SpacingWithinParenthesesOption.Expressions, nameof(SpaceWithinExpressionParentheses));

        public static Option<bool> SpaceWithinCastParentheses { get; } = CreateSpaceWithinParenthesesOption(
            SpacingWithinParenthesesOption.TypeCasts, nameof(SpaceWithinCastParentheses));

        public static Option<bool> SpaceWithinOtherParentheses { get; } = CreateSpaceWithinParenthesesOption(
            SpacingWithinParenthesesOption.ControlFlowStatements, nameof(SpaceWithinOtherParentheses));

        public static Option<bool> SpaceAfterCast { get; } = CreateOption(
            nameof(SpaceAfterCast),
            defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_cast"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterCast")});

        public static Option<bool> SpacesIgnoreAroundVariableDeclaration { get; } = CreateOption(
            nameof(SpacesIgnoreAroundVariableDeclaration),
            defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<bool>(
                    "csharp_space_around_declaration_statements",
                    s => DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet(s),
                    v => v ? "ignore" : "false"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpacesIgnoreAroundVariableDeclaration")});

        public static Option<bool> SpaceBeforeOpenSquareBracket { get; } = CreateOption(
            nameof(SpaceBeforeOpenSquareBracket),
            defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_before_open_square_brackets"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeOpenSquareBracket")});

        public static Option<bool> SpaceBetweenEmptySquareBrackets { get; } = CreateOption(
            nameof(SpaceBetweenEmptySquareBrackets),
            defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_empty_square_brackets"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBetweenEmptySquareBrackets")});

        public static Option<bool> SpaceWithinSquareBrackets { get; } = CreateOption(
            nameof(SpaceWithinSquareBrackets),
            defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_square_brackets"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinSquareBrackets")});

        public static Option<bool> SpaceAfterColonInBaseTypeDeclaration { get; } = CreateOption(
            nameof(SpaceAfterColonInBaseTypeDeclaration),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_colon_in_inheritance_clause"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterColonInBaseTypeDeclaration")});

        public static Option<bool> SpaceAfterComma { get; } = CreateOption(
            nameof(SpaceAfterComma),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_comma"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterComma")});

        public static Option<bool> SpaceAfterDot { get; } = CreateOption(
            nameof(SpaceAfterDot),
            defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_dot"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterDot")});

        public static Option<bool> SpaceAfterSemicolonsInForStatement { get; } = CreateOption(
            nameof(SpaceAfterSemicolonsInForStatement),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_semicolon_in_for_statement"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterSemicolonsInForStatement")});

        public static Option<bool> SpaceBeforeColonInBaseTypeDeclaration { get; } = CreateOption(
            nameof(SpaceBeforeColonInBaseTypeDeclaration),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_before_colon_in_inheritance_clause"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeColonInBaseTypeDeclaration")});

        public static Option<bool> SpaceBeforeComma { get; } = CreateOption(
            nameof(SpaceBeforeComma),
            defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_before_comma"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeComma")});

        public static Option<bool> SpaceBeforeDot { get; } = CreateOption(
            nameof(SpaceBeforeDot),
            defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_before_dot"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeDot")});

        public static Option<bool> SpaceBeforeSemicolonsInForStatement { get; } = CreateOption(
            nameof(SpaceBeforeSemicolonsInForStatement),
            defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_before_semicolon_in_for_statement"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeSemicolonsInForStatement")});

        public static Option<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator { get; } = CreateOption(
            nameof(SpacingAroundBinaryOperator),
            defaultValue: BinaryOperatorSpacingOptions.Single,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<BinaryOperatorSpacingOptions>(
                    "csharp_space_around_binary_operators",
                    s => ParseEditorConfigSpacingAroundBinaryOperator(s),
                    GetSpacingAroundBinaryOperatorEditorConfigString),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpacingAroundBinaryOperator")});

        public static Option<bool> IndentBraces { get; } = CreateOption(
            nameof(IndentBraces),
            defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_indent_braces"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.OpenCloseBracesIndent")});

        public static Option<bool> IndentBlock { get; } = CreateOption(
            nameof(IndentBlock),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_indent_block_contents"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentBlock")});

        public static Option<bool> IndentSwitchSection { get; } = CreateOption(
            nameof(IndentSwitchSection),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_indent_switch_labels"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentSwitchSection")});

        public static Option<bool> IndentSwitchCaseSection { get; } = CreateOption(
            nameof(IndentSwitchCaseSection),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_indent_case_contents"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentSwitchCaseSection")});

        public static Option<bool> IndentSwitchCaseSectionWhenBlock { get; } = CreateOption(
            nameof(IndentSwitchCaseSectionWhenBlock),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_indent_case_contents_when_block"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentSwitchCaseSectionWhenBlock")});

        public static Option<LabelPositionOptions> LabelPositioning { get; } = CreateOption(
            nameof(LabelPositioning),
            defaultValue: LabelPositionOptions.OneLess,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<LabelPositionOptions>(
                    "csharp_indent_labels",
                    s => ParseEditorConfigLabelPositioning(s),
                    GetLabelPositionOptionEditorConfigString),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.LabelPositioning")});

        public static Option<bool> WrappingPreserveSingleLine { get; } = CreateOption(
            nameof(WrappingPreserveSingleLine),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_preserve_single_line_blocks"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.WrappingPreserveSingleLine")});

        public static Option<bool> WrappingKeepStatementsOnSingleLine { get; } = CreateOption(
            nameof(WrappingKeepStatementsOnSingleLine),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_preserve_single_line_statements"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.WrappingKeepStatementsOnSingleLine")});

        public static Option<bool> NewLinesForBracesInTypes { get; } = CreateNewLineForBracesOption(
            NewLineOption.Types, nameof(NewLinesForBracesInTypes));

        public static Option<bool> NewLinesForBracesInMethods { get; } = CreateNewLineForBracesOption(
            NewLineOption.Methods, nameof(NewLinesForBracesInMethods));

        public static Option<bool> NewLinesForBracesInProperties { get; } = CreateNewLineForBracesOption(
            NewLineOption.Properties, nameof(NewLinesForBracesInProperties));

        public static Option<bool> NewLinesForBracesInAccessors { get; } = CreateNewLineForBracesOption(
            NewLineOption.Accessors, nameof(NewLinesForBracesInAccessors));

        public static Option<bool> NewLinesForBracesInAnonymousMethods { get; } = CreateNewLineForBracesOption(
            NewLineOption.AnonymousMethods, nameof(NewLinesForBracesInAnonymousMethods));

        public static Option<bool> NewLinesForBracesInControlBlocks { get; } = CreateNewLineForBracesOption(
            NewLineOption.ControlBlocks, nameof(NewLinesForBracesInControlBlocks));

        public static Option<bool> NewLinesForBracesInAnonymousTypes { get; } = CreateNewLineForBracesOption(
            NewLineOption.AnonymousTypes, nameof(NewLinesForBracesInAnonymousTypes));

        public static Option<bool> NewLinesForBracesInObjectCollectionArrayInitializers { get; } = CreateNewLineForBracesOption(
            NewLineOption.ObjectCollectionsArrayInitializers, nameof(NewLinesForBracesInObjectCollectionArrayInitializers));

        public static Option<bool> NewLinesForBracesInLambdaExpressionBody { get; } = CreateNewLineForBracesOption(
            NewLineOption.Lambdas, nameof(NewLinesForBracesInLambdaExpressionBody));

        public static Option<bool> NewLineForElse { get; } = CreateOption(
            nameof(NewLineForElse),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_else"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForElse")});

        public static Option<bool> NewLineForCatch { get; } = CreateOption(
            nameof(NewLineForCatch),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_catch"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForCatch")});

        public static Option<bool> NewLineForFinally { get; } = CreateOption(
            nameof(NewLineForFinally),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_finally"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForFinally")});

        public static Option<bool> NewLineForMembersInObjectInit { get; } = CreateOption(
            nameof(NewLineForMembersInObjectInit),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_members_in_object_initializers"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForMembersInObjectInit")});

        public static Option<bool> NewLineForMembersInAnonymousTypes { get; } = CreateOption(
            nameof(NewLineForMembersInAnonymousTypes),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_members_in_anonymous_types"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForMembersInAnonymousTypes")});

        public static Option<bool> NewLineForClausesInQuery { get; } = CreateOption(
            nameof(NewLineForClausesInQuery),
            defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_between_query_expression_clauses"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForClausesInQuery")});
    }

    internal enum LabelPositionOptions
    {
        /// Placed in the Zeroth column of the text editor
        LeftMost = 0,

        /// Placed at one less indent to the current context
        OneLess = 1,

        /// Placed at the same indent as the current context
        NoIndent = 2
    }

    internal enum BinaryOperatorSpacingOptions
    {
        /// Single Spacing
        Single = 0,

        /// Ignore Formatting
        Ignore = 1,

        /// Remove Spacing
        Remove = 2
    }
}
