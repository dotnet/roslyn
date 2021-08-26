// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;

#if CODE_STYLE
using CSharpWorkspaceResources = Microsoft.CodeAnalysis.CSharp.CSharpCodeStyleResources;
using WorkspacesResources = Microsoft.CodeAnalysis.CodeStyleResources;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal static partial class CSharpFormattingOptions2
    {
        internal static EditorConfigHelpers EditorConfig { get; }

        internal static ImmutableArray<IOption2> AllOptions { get; }
        private static ImmutableDictionary<Option2<bool>, SpacingWithinParenthesesOption> SpacingWithinParenthesisOptionsMap { get; }
        private static ImmutableDictionary<Option2<bool>, NewLineOption> NewLineOptionsMap { get; }

        public static Option2<bool> SpacingAfterMethodDeclarationName { get; }
        public static Option2<bool> SpaceWithinMethodDeclarationParenthesis { get; }
        public static Option2<bool> SpaceBetweenEmptyMethodDeclarationParentheses { get; }
        public static Option2<bool> SpaceAfterMethodCallName { get; }
        public static Option2<bool> SpaceWithinMethodCallParentheses { get; }
        public static Option2<bool> SpaceBetweenEmptyMethodCallParentheses { get; }
        public static Option2<bool> SpaceAfterControlFlowStatementKeyword { get; }
        public static Option2<bool> SpaceWithinExpressionParentheses { get; }
        public static Option2<bool> SpaceWithinCastParentheses { get; }
        public static Option2<bool> SpaceWithinOtherParentheses { get; }
        public static Option2<bool> SpaceAfterCast { get; }
        public static Option2<bool> SpacesIgnoreAroundVariableDeclaration { get; }
        public static Option2<bool> SpaceBeforeOpenSquareBracket { get; }
        public static Option2<bool> SpaceBetweenEmptySquareBrackets { get; }
        public static Option2<bool> SpaceWithinSquareBrackets { get; }
        public static Option2<bool> SpaceAfterColonInBaseTypeDeclaration { get; }
        public static Option2<bool> SpaceAfterComma { get; }
        public static Option2<bool> SpaceAfterDot { get; }
        public static Option2<bool> SpaceAfterSemicolonsInForStatement { get; }
        public static Option2<bool> SpaceBeforeColonInBaseTypeDeclaration { get; }
        public static Option2<bool> SpaceBeforeComma { get; }
        public static Option2<bool> SpaceBeforeDot { get; }
        public static Option2<bool> SpaceBeforeSemicolonsInForStatement { get; }
        public static Option2<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator { get; }
        public static Option2<bool> IndentBraces { get; }
        public static Option2<bool> IndentBlock { get; }
        public static Option2<bool> IndentSwitchSection { get; }
        public static Option2<bool> IndentSwitchCaseSection { get; }
        public static Option2<bool> IndentSwitchCaseSectionWhenBlock { get; }
        public static Option2<LabelPositionOptions> LabelPositioning { get; }
        public static Option2<bool> WrappingPreserveSingleLine { get; }
        public static Option2<bool> WrappingKeepStatementsOnSingleLine { get; }
        public static Option2<bool> NewLinesForBracesInTypes { get; }
        public static Option2<bool> NewLinesForBracesInMethods { get; }
        public static Option2<bool> NewLinesForBracesInProperties { get; }
        public static Option2<bool> NewLinesForBracesInAccessors { get; }
        public static Option2<bool> NewLinesForBracesInAnonymousMethods { get; }
        public static Option2<bool> NewLinesForBracesInControlBlocks { get; }
        public static Option2<bool> NewLinesForBracesInAnonymousTypes { get; }
        public static Option2<bool> NewLinesForBracesInObjectCollectionArrayInitializers { get; }
        public static Option2<bool> NewLinesForBracesInLambdaExpressionBody { get; }
        public static Option2<bool> NewLineForElse { get; }
        public static Option2<bool> NewLineForCatch { get; }
        public static Option2<bool> NewLineForFinally { get; }
        public static Option2<bool> NewLineForMembersInObjectInit { get; }
        public static Option2<bool> NewLineForMembersInAnonymousTypes { get; }
        public static Option2<bool> NewLineForClausesInQuery { get; }

        static CSharpFormattingOptions2()
        {
            var editorConfig = new EditorConfigHelpers();
            var builder = new Builder(editorConfig);

            SpacingAfterMethodDeclarationName = builder.CreateSpacingOption(
                nameof(SpacingAfterMethodDeclarationName),
                defaultValue: false,
                editorConfigKeyName: "csharp_space_between_method_declaration_name_and_open_parenthesis",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpacingAfterMethodDeclarationName");

            SpaceWithinMethodDeclarationParenthesis = builder.CreateSpacingOption(
                nameof(SpaceWithinMethodDeclarationParenthesis),
                defaultValue: false,
                editorConfigKeyName: "csharp_space_between_method_declaration_parameter_list_parentheses",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceWithinMethodDeclarationParenthesis");

            SpaceBetweenEmptyMethodDeclarationParentheses = builder.CreateSpacingOption(
                nameof(SpaceBetweenEmptyMethodDeclarationParentheses),
                defaultValue: false,
                editorConfigKeyName: "csharp_space_between_method_declaration_empty_parameter_list_parentheses",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceBetweenEmptyMethodDeclarationParentheses");

            SpaceAfterMethodCallName = builder.CreateSpacingOption(
                nameof(SpaceAfterMethodCallName),
                defaultValue: false,
                editorConfigKeyName: "csharp_space_between_method_call_name_and_opening_parenthesis",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceAfterMethodCallName");

            SpaceWithinMethodCallParentheses = builder.CreateSpacingOption(
                nameof(SpaceWithinMethodCallParentheses),
                defaultValue: false,
                editorConfigKeyName: "csharp_space_between_method_call_parameter_list_parentheses",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceWithinMethodCallParentheses");

            SpaceBetweenEmptyMethodCallParentheses = builder.CreateSpacingOption(
                nameof(SpaceBetweenEmptyMethodCallParentheses),
                defaultValue: false,
                editorConfigKeyName: "csharp_space_between_method_call_empty_parameter_list_parentheses",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceBetweenEmptyMethodCallParentheses");

            SpaceAfterControlFlowStatementKeyword = builder.CreateSpacingOption(
                nameof(SpaceAfterControlFlowStatementKeyword),
                defaultValue: true,
                editorConfigKeyName: "csharp_space_after_keywords_in_control_flow_statements",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceAfterControlFlowStatementKeyword");

            SpaceWithinExpressionParentheses = builder.CreateSpaceWithinParenthesesOption(
                SpacingWithinParenthesesOption.Expressions, nameof(SpaceWithinExpressionParentheses));

            SpaceWithinCastParentheses = builder.CreateSpaceWithinParenthesesOption(
                SpacingWithinParenthesesOption.TypeCasts, nameof(SpaceWithinCastParentheses));

            SpaceWithinOtherParentheses = builder.CreateSpaceWithinParenthesesOption(
                SpacingWithinParenthesesOption.ControlFlowStatements, nameof(SpaceWithinOtherParentheses));

            SpaceAfterCast = builder.CreateSpacingOption(
                nameof(SpaceAfterCast),
                defaultValue: false,
                editorConfigKeyName: "csharp_space_after_cast",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceAfterCast");

            SpacesIgnoreAroundVariableDeclaration = builder.CreateSpacingOption(
                nameof(SpacesIgnoreAroundVariableDeclaration),
                defaultValue: false,
                new EditorConfigStorageLocation<bool>(
                    "csharp_space_around_declaration_statements",
                    editorConfig.DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet,
                    editorConfig.GetIgnoreSpacesAroundVariableDeclarationString),
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpacesIgnoreAroundVariableDeclaration");

            SpaceBeforeOpenSquareBracket = builder.CreateSpacingOption(
                nameof(SpaceBeforeOpenSquareBracket),
                defaultValue: false,
                editorConfigKeyName: "csharp_space_before_open_square_brackets",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceBeforeOpenSquareBracket");

            SpaceBetweenEmptySquareBrackets = builder.CreateSpacingOption(
                nameof(SpaceBetweenEmptySquareBrackets),
                defaultValue: false,
                editorConfigKeyName: "csharp_space_between_empty_square_brackets",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceBetweenEmptySquareBrackets");

            SpaceWithinSquareBrackets = builder.CreateSpacingOption(
                nameof(SpaceWithinSquareBrackets),
                defaultValue: false,
                editorConfigKeyName: "csharp_space_between_square_brackets",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceWithinSquareBrackets");

            SpaceAfterColonInBaseTypeDeclaration = builder.CreateSpacingOption(
                nameof(SpaceAfterColonInBaseTypeDeclaration),
                defaultValue: true,
                editorConfigKeyName: "csharp_space_after_colon_in_inheritance_clause",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceAfterColonInBaseTypeDeclaration");

            SpaceAfterComma = builder.CreateSpacingOption(
                nameof(SpaceAfterComma),
                defaultValue: true,
                editorConfigKeyName: "csharp_space_after_comma",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceAfterComma");

            SpaceAfterDot = builder.CreateSpacingOption(
                nameof(SpaceAfterDot),
                defaultValue: false,
                editorConfigKeyName: "csharp_space_after_dot",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceAfterDot");

            SpaceAfterSemicolonsInForStatement = builder.CreateSpacingOption(
                nameof(SpaceAfterSemicolonsInForStatement),
                defaultValue: true,
                editorConfigKeyName: "csharp_space_after_semicolon_in_for_statement",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceAfterSemicolonsInForStatement");

            SpaceBeforeColonInBaseTypeDeclaration = builder.CreateSpacingOption(
                nameof(SpaceBeforeColonInBaseTypeDeclaration),
                defaultValue: true,
                editorConfigKeyName: "csharp_space_before_colon_in_inheritance_clause",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceBeforeColonInBaseTypeDeclaration");

            SpaceBeforeComma = builder.CreateSpacingOption(
                nameof(SpaceBeforeComma),
                defaultValue: false,
                editorConfigKeyName: "csharp_space_before_comma",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceBeforeComma");

            SpaceBeforeDot = builder.CreateSpacingOption(
                nameof(SpaceBeforeDot),
                defaultValue: false,
                editorConfigKeyName: "csharp_space_before_dot",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceBeforeDot");

            SpaceBeforeSemicolonsInForStatement = builder.CreateSpacingOption(
                nameof(SpaceBeforeSemicolonsInForStatement),
                defaultValue: false,
                editorConfigKeyName: "csharp_space_before_semicolon_in_for_statement",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpaceBeforeSemicolonsInForStatement");

            SpacingAroundBinaryOperator = builder.CreateSpacingOption(
                nameof(SpacingAroundBinaryOperator),
                defaultValue: BinaryOperatorSpacingOptions.Single,
                new EditorConfigStorageLocation<BinaryOperatorSpacingOptions>(
                    "csharp_space_around_binary_operators",
                    editorConfig.ParseSpacingAroundBinaryOperator,
                    editorConfig.GetSpacingAroundBinaryOperatorString),
                roamingProfileKeyName: "TextEditor.CSharp.Specific.SpacingAroundBinaryOperator");

            IndentBraces = builder.CreateIndentationOption(
                nameof(IndentBraces),
                defaultValue: false,
                editorConfigKeyName: "csharp_indent_braces",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.OpenCloseBracesIndent");

            IndentBlock = builder.CreateIndentationOption(
                nameof(IndentBlock),
                defaultValue: true,
                editorConfigKeyName: "csharp_indent_block_contents",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.IndentBlock");

            IndentSwitchSection = builder.CreateIndentationOption(
                nameof(IndentSwitchSection),
                defaultValue: true,
                editorConfigKeyName: "csharp_indent_switch_labels",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.IndentSwitchSection");

            IndentSwitchCaseSection = builder.CreateIndentationOption(
                nameof(IndentSwitchCaseSection),
                defaultValue: true,
                editorConfigKeyName: "csharp_indent_case_contents",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.IndentSwitchCaseSection");

            IndentSwitchCaseSectionWhenBlock = builder.CreateIndentationOption(
                nameof(IndentSwitchCaseSectionWhenBlock),
                defaultValue: true,
                editorConfigKeyName: "csharp_indent_case_contents_when_block",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.IndentSwitchCaseSectionWhenBlock");

            LabelPositioning = builder.CreateIndentationOption(
                nameof(LabelPositioning),
                defaultValue: LabelPositionOptions.OneLess,
                new EditorConfigStorageLocation<LabelPositionOptions>(
                    "csharp_indent_labels",
                    editorConfig.ParseLabelPositioning,
                    editorConfig.GetLabelPositionOptionString),
                roamingProfileKeyName: "TextEditor.CSharp.Specific.LabelPositioning");

            WrappingPreserveSingleLine = builder.CreateWrappingOption(
                nameof(WrappingPreserveSingleLine),
                defaultValue: true,
                editorConfigKeyName: "csharp_preserve_single_line_blocks",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.WrappingPreserveSingleLine");

            WrappingKeepStatementsOnSingleLine = builder.CreateWrappingOption(
                nameof(WrappingKeepStatementsOnSingleLine),
                defaultValue: true,
                editorConfigKeyName: "csharp_preserve_single_line_statements",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.WrappingKeepStatementsOnSingleLine");

            NewLinesForBracesInTypes = builder.CreateNewLineForBracesOption(
                NewLineOption.Types, nameof(NewLinesForBracesInTypes));

            NewLinesForBracesInMethods = builder.CreateNewLineForBracesOption(
                NewLineOption.Methods, nameof(NewLinesForBracesInMethods));

            NewLinesForBracesInProperties = builder.CreateNewLineForBracesOption(
                NewLineOption.Properties, nameof(NewLinesForBracesInProperties));

            NewLinesForBracesInAccessors = builder.CreateNewLineForBracesOption(
                NewLineOption.Accessors, nameof(NewLinesForBracesInAccessors));

            NewLinesForBracesInAnonymousMethods = builder.CreateNewLineForBracesOption(
                NewLineOption.AnonymousMethods, nameof(NewLinesForBracesInAnonymousMethods));

            NewLinesForBracesInControlBlocks = builder.CreateNewLineForBracesOption(
                NewLineOption.ControlBlocks, nameof(NewLinesForBracesInControlBlocks));

            NewLinesForBracesInAnonymousTypes = builder.CreateNewLineForBracesOption(
                NewLineOption.AnonymousTypes, nameof(NewLinesForBracesInAnonymousTypes));

            NewLinesForBracesInObjectCollectionArrayInitializers = builder.CreateNewLineForBracesOption(
                NewLineOption.ObjectCollectionsArrayInitializers, nameof(NewLinesForBracesInObjectCollectionArrayInitializers));

            NewLinesForBracesInLambdaExpressionBody = builder.CreateNewLineForBracesOption(
                NewLineOption.Lambdas, nameof(NewLinesForBracesInLambdaExpressionBody));

            NewLineForElse = builder.CreateNewLineOption(
                nameof(NewLineForElse),
                defaultValue: true,
                editorConfigKeyName: "csharp_new_line_before_else",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.NewLineForElse");

            NewLineForCatch = builder.CreateNewLineOption(
                nameof(NewLineForCatch),
                defaultValue: true,
                editorConfigKeyName: "csharp_new_line_before_catch",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.NewLineForCatch");

            NewLineForFinally = builder.CreateNewLineOption(
                nameof(NewLineForFinally),
                defaultValue: true,
                editorConfigKeyName: "csharp_new_line_before_finally",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.NewLineForFinally");

            NewLineForMembersInObjectInit = builder.CreateNewLineOption(
                nameof(NewLineForMembersInObjectInit),
                defaultValue: true,
                editorConfigKeyName: "csharp_new_line_before_members_in_object_initializers",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.NewLineForMembersInObjectInit");

            NewLineForMembersInAnonymousTypes = builder.CreateNewLineOption(
                nameof(NewLineForMembersInAnonymousTypes),
                defaultValue: true,
                editorConfigKeyName: "csharp_new_line_before_members_in_anonymous_types",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.NewLineForMembersInAnonymousTypes");

            NewLineForClausesInQuery = builder.CreateNewLineOption(
                nameof(NewLineForClausesInQuery),
                defaultValue: true,
                editorConfigKeyName: "csharp_new_line_between_query_expression_clauses",
                roamingProfileKeyName: "TextEditor.CSharp.Specific.NewLineForClausesInQuery");

            EditorConfig = editorConfig;
            AllOptions = builder.AllOptionsBuilder.ToImmutable();
            SpacingWithinParenthesisOptionsMap = builder.SpacingWithinParenthesisOptionsMapBuilder.ToImmutable();
            NewLineOptionsMap = builder.NewLineOptionsMapBuilder.ToImmutable();
        }
    }

#if CODE_STYLE
    internal enum LabelPositionOptions
#else
    public enum LabelPositionOptions
#endif
    {
        /// Placed in the Zeroth column of the text editor
        LeftMost = 0,

        /// Placed at one less indent to the current context
        OneLess = 1,

        /// Placed at the same indent as the current context
        NoIndent = 2
    }

#if CODE_STYLE
    internal enum BinaryOperatorSpacingOptions
#else
    public enum BinaryOperatorSpacingOptions
#endif
    {
        /// Single Spacing
        Single = 0,

        /// Ignore Formatting
        Ignore = 1,

        /// Remove Spacing
        Remove = 2
    }

    internal static class CSharpFormattingOptionGroups
    {
        public static readonly OptionGroup NewLine = new(WorkspacesResources.New_line_preferences, priority: 1);
        public static readonly OptionGroup Indentation = new(CSharpWorkspaceResources.Indentation_preferences, priority: 2);
        public static readonly OptionGroup Spacing = new(CSharpWorkspaceResources.Space_preferences, priority: 3);
        public static readonly OptionGroup Wrapping = new(CSharpWorkspaceResources.Wrapping_preferences, priority: 4);
    }
}
