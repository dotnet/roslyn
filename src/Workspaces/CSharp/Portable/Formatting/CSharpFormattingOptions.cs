// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    public static partial class CSharpFormattingOptions
    {
        public static Option<bool> SpacingAfterMethodDeclarationName { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpacingAfterMethodDeclarationName), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_declaration_name_and_open_parenthesis"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpacingAfterMethodDeclarationName")});

        public static Option<bool> SpaceWithinMethodDeclarationParenthesis { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceWithinMethodDeclarationParenthesis), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_declaration_parameter_list_parentheses"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinMethodDeclarationParenthesis")});

        public static Option<bool> SpaceBetweenEmptyMethodDeclarationParentheses { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceBetweenEmptyMethodDeclarationParentheses), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_declaration_empty_parameter_list_parentheses"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBetweenEmptyMethodDeclarationParentheses")});

        public static Option<bool> SpaceAfterMethodCallName { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceAfterMethodCallName), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_call_name_and_opening_parenthesis"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterMethodCallName")});

        public static Option<bool> SpaceWithinMethodCallParentheses { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceWithinMethodCallParentheses), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_call_parameter_list_parentheses"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinMethodCallParentheses")});

        public static Option<bool> SpaceBetweenEmptyMethodCallParentheses { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceBetweenEmptyMethodCallParentheses), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_call_empty_parameter_list_parentheses"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBetweenEmptyMethodCallParentheses")});

        public static Option<bool> SpaceAfterControlFlowStatementKeyword { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceAfterControlFlowStatementKeyword), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_keywords_in_control_flow_statements"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterControlFlowStatementKeyword")});

        public static Option<bool> SpaceWithinExpressionParentheses { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceWithinExpressionParentheses), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<bool>("csharp_space_between_parentheses", s => DetermineIfSpaceOptionIsSet(s, SpacingWithinParenthesesOption.Expressions)),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinExpressionParentheses")});

        public static Option<bool> SpaceWithinCastParentheses { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceWithinCastParentheses), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<bool>("csharp_space_between_parentheses", s => DetermineIfSpaceOptionIsSet(s, SpacingWithinParenthesesOption.TypeCasts)),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinCastParentheses")});

        public static Option<bool> SpaceWithinOtherParentheses { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceWithinOtherParentheses), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<bool>("csharp_space_between_parentheses", s => DetermineIfSpaceOptionIsSet(s, SpacingWithinParenthesesOption.ControlFlowStatements)),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinOtherParentheses")});

        public static Option<bool> SpaceAfterCast { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceAfterCast), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_cast"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterCast")});

        public static Option<bool> SpacesIgnoreAroundVariableDeclaration { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpacesIgnoreAroundVariableDeclaration), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<bool>("csharp_space_around_declaration_statements", s => DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet(s)),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpacesIgnoreAroundVariableDeclaration")});

        public static Option<bool> SpaceBeforeOpenSquareBracket { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceBeforeOpenSquareBracket), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_before_open_square_brackets"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeOpenSquareBracket")});

        public static Option<bool> SpaceBetweenEmptySquareBrackets { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceBetweenEmptySquareBrackets), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_empty_square_brackets"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBetweenEmptySquareBrackets")});

        public static Option<bool> SpaceWithinSquareBrackets { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceWithinSquareBrackets), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_square_brackets"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinSquareBrackets")});

        public static Option<bool> SpaceAfterColonInBaseTypeDeclaration { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceAfterColonInBaseTypeDeclaration), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_colon_in_inheritance_clause"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterColonInBaseTypeDeclaration")});

        public static Option<bool> SpaceAfterComma { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceAfterComma), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_comma"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterComma")});

        public static Option<bool> SpaceAfterDot { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceAfterDot), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_dot"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterDot")});

        public static Option<bool> SpaceAfterSemicolonsInForStatement { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceAfterSemicolonsInForStatement), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_semicolon_in_for_statement"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterSemicolonsInForStatement")});

        public static Option<bool> SpaceBeforeColonInBaseTypeDeclaration { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceBeforeColonInBaseTypeDeclaration), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_before_colon_in_inheritance_clause"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeColonInBaseTypeDeclaration")});

        public static Option<bool> SpaceBeforeComma { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceBeforeComma), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_before_comma"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeComma")});

        public static Option<bool> SpaceBeforeDot { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceBeforeDot), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_before_dot"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeDot")});

        public static Option<bool> SpaceBeforeSemicolonsInForStatement { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(SpaceBeforeSemicolonsInForStatement), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_before_semicolon_in_for_statement"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeSemicolonsInForStatement")});

        public static Option<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator { get; } = new Option<BinaryOperatorSpacingOptions>(nameof(CSharpFormattingOptions), nameof(SpacingAroundBinaryOperator), defaultValue: BinaryOperatorSpacingOptions.Single,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<BinaryOperatorSpacingOptions>("csharp_space_around_binary_operators", s => ParseEditorConfigSpacingAroundBinaryOperator(s)),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpacingAroundBinaryOperator")});

        public static Option<bool> IndentBraces { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(IndentBraces), defaultValue: false,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_indent_braces"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.OpenCloseBracesIndent")});

        public static Option<bool> IndentBlock { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(IndentBlock), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_indent_block_contents"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentBlock")});

        public static Option<bool> IndentSwitchSection { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(IndentSwitchSection), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_indent_switch_labels"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentSwitchSection")});

        public static Option<bool> IndentSwitchCaseSection { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(IndentSwitchCaseSection), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_indent_case_contents"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentSwitchCaseSection")});

        public static Option<bool> IndentSwitchCaseSectionWhenBlock { get; } = new Option<bool>(
            nameof(CSharpFormattingOptions),
            nameof(IndentSwitchCaseSectionWhenBlock), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_indent_case_contents_when_block"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentSwitchCaseSectionWhenBlock")});

        public static Option<LabelPositionOptions> LabelPositioning { get; } = new Option<LabelPositionOptions>(nameof(CSharpFormattingOptions), nameof(LabelPositioning), defaultValue: LabelPositionOptions.OneLess,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<LabelPositionOptions>("csharp_indent_labels", s => ParseEditorConfigLablePositioning(s)),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.LabelPositioning")});

        public static Option<bool> WrappingPreserveSingleLine { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(WrappingPreserveSingleLine), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_preserve_single_line_blocks"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.WrappingPreserveSingleLine")});

        public static Option<bool> WrappingKeepStatementsOnSingleLine { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(WrappingKeepStatementsOnSingleLine), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_preserve_single_line_statements"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.WrappingKeepStatementsOnSingleLine")});

        public static Option<bool> NewLinesForBracesInTypes { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(NewLinesForBracesInTypes), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<bool>("csharp_new_line_before_open_brace", value => DetermineIfNewLineOptionIsSet(value, NewLineOption.Types)),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLinesBracesType")});

        public static Option<bool> NewLinesForBracesInMethods { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(NewLinesForBracesInMethods), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<bool>("csharp_new_line_before_open_brace", value => DetermineIfNewLineOptionIsSet(value, NewLineOption.Methods)),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLinesForBracesInMethods")});

        public static Option<bool> NewLinesForBracesInProperties { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(NewLinesForBracesInProperties), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<bool>("csharp_new_line_before_open_brace", value => DetermineIfNewLineOptionIsSet(value, NewLineOption.Properties)),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLinesForBracesInProperties")});

        public static Option<bool> NewLinesForBracesInAccessors { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(NewLinesForBracesInAccessors), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<bool>("csharp_new_line_before_open_brace", value => DetermineIfNewLineOptionIsSet(value, NewLineOption.Accessors)),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLinesForBracesInAccessors")});

        public static Option<bool> NewLinesForBracesInAnonymousMethods { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(NewLinesForBracesInAnonymousMethods), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<bool>("csharp_new_line_before_open_brace", value => DetermineIfNewLineOptionIsSet(value, NewLineOption.AnonymousMethods)),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLinesForBracesInAnonymousMethods")});

        public static Option<bool> NewLinesForBracesInControlBlocks { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(NewLinesForBracesInControlBlocks), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<bool>("csharp_new_line_before_open_brace", value => DetermineIfNewLineOptionIsSet(value, NewLineOption.ControlBlocks)),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLinesForBracesInControlBlocks")});

        public static Option<bool> NewLinesForBracesInAnonymousTypes { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(NewLinesForBracesInAnonymousTypes), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<bool>("csharp_new_line_before_open_brace", value => DetermineIfNewLineOptionIsSet(value, NewLineOption.AnonymousTypes)),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLinesForBracesInAnonymousTypes")});

        public static Option<bool> NewLinesForBracesInObjectCollectionArrayInitializers { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(NewLinesForBracesInObjectCollectionArrayInitializers), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<bool>("csharp_new_line_before_open_brace", value => DetermineIfNewLineOptionIsSet(value, NewLineOption.ObjectCollectionsArrayInitializers)),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLinesForBracesInObjectCollectionArrayInitializers")});

        public static Option<bool> NewLinesForBracesInLambdaExpressionBody { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(NewLinesForBracesInLambdaExpressionBody), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<bool>("csharp_new_line_before_open_brace", value => DetermineIfNewLineOptionIsSet(value, NewLineOption.Lambdas)),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLinesForBracesInLambdaExpressionBody")});

        public static Option<bool> NewLineForElse { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(NewLineForElse), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_else"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForElse")});

        public static Option<bool> NewLineForCatch { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(NewLineForCatch), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_catch"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForCatch")});

        public static Option<bool> NewLineForFinally { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(NewLineForFinally), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_finally"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForFinally")});

        public static Option<bool> NewLineForMembersInObjectInit { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(NewLineForMembersInObjectInit), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_members_in_object_initializers"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForMembersInObjectInit")});

        public static Option<bool> NewLineForMembersInAnonymousTypes { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(NewLineForMembersInAnonymousTypes), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_members_in_anonymous_types"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForMembersInAnonymousTypes")});

        public static Option<bool> NewLineForClausesInQuery { get; } = new Option<bool>(nameof(CSharpFormattingOptions), nameof(NewLineForClausesInQuery), defaultValue: true,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_between_query_expression_clauses"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForClausesInQuery")});
    }

    public enum LabelPositionOptions
    {
        /// Placed in the Zeroth column of the text editor
        LeftMost = 0,

        /// Placed at one less indent to the current context
        OneLess = 1,

        /// Placed at the same indent as the current context
        NoIndent = 2
    }

    public enum BinaryOperatorSpacingOptions
    {
        /// Single Spacing
        Single = 0,

        /// Ignore Formatting
        Ignore = 1,

        /// Remove Spacing
        Remove = 2
    }
}
