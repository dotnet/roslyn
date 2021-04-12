﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Options;

#if CODE_STYLE
using CSharpWorkspaceResources = Microsoft.CodeAnalysis.CSharp.CSharpCodeStyleResources;
using WorkspacesResources = Microsoft.CodeAnalysis.CodeStyleResources;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal static partial class CSharpFormattingOptions2
    {
        private static readonly ImmutableArray<IOption2>.Builder s_allOptionsBuilder = ImmutableArray.CreateBuilder<IOption2>();

        // Maps to store mapping between special option kinds and the corresponding options.
        private static readonly ImmutableDictionary<Option2<bool>, SpacingWithinParenthesesOption>.Builder s_spacingWithinParenthesisOptionsMapBuilder
            = ImmutableDictionary.CreateBuilder<Option2<bool>, SpacingWithinParenthesesOption>();
        private static readonly ImmutableDictionary<Option2<bool>, NewLineOption>.Builder s_newLineOptionsMapBuilder =
           ImmutableDictionary.CreateBuilder<Option2<bool>, NewLineOption>();

        // Maps to store mapping between special option kinds and the corresponding editor config string representations.
        #region Editor Config maps
        private static readonly BidirectionalMap<string, SpacingWithinParenthesesOption> s_spacingWithinParenthesisOptionsEditorConfigMap =
            new(new[]
            {
                KeyValuePairUtil.Create("expressions", SpacingWithinParenthesesOption.Expressions),
                KeyValuePairUtil.Create("type_casts", SpacingWithinParenthesesOption.TypeCasts),
                KeyValuePairUtil.Create("control_flow_statements", SpacingWithinParenthesesOption.ControlFlowStatements),
            });
        private static readonly BidirectionalMap<string, BinaryOperatorSpacingOptions> s_binaryOperatorSpacingOptionsEditorConfigMap =
            new(new[]
            {
                KeyValuePairUtil.Create("ignore", BinaryOperatorSpacingOptions.Ignore),
                KeyValuePairUtil.Create("none", BinaryOperatorSpacingOptions.Remove),
                KeyValuePairUtil.Create("before_and_after", BinaryOperatorSpacingOptions.Single),
            });
        private static readonly BidirectionalMap<string, LabelPositionOptions> s_labelPositionOptionsEditorConfigMap =
            new(new[]
            {
                KeyValuePairUtil.Create("flush_left", LabelPositionOptions.LeftMost),
                KeyValuePairUtil.Create("no_change", LabelPositionOptions.NoIndent),
                KeyValuePairUtil.Create("one_less_than_current", LabelPositionOptions.OneLess),
            });
        private static readonly BidirectionalMap<string, NewLineOption> s_legacyNewLineOptionsEditorConfigMap =
            new(new[]
            {
                KeyValuePairUtil.Create("object_collection_array_initalizers", NewLineOption.ObjectCollectionsArrayInitializers),
            });
        private static readonly BidirectionalMap<string, NewLineOption> s_newLineOptionsEditorConfigMap =
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
        #endregion

        internal static ImmutableArray<IOption2> AllOptions { get; }
        private static ImmutableDictionary<Option2<bool>, SpacingWithinParenthesesOption> SpacingWithinParenthesisOptionsMap { get; }
        private static ImmutableDictionary<Option2<bool>, NewLineOption> NewLineOptionsMap { get; }

        private static Option2<T> CreateOption<T>(OptionGroup group, string name, T defaultValue, params OptionStorageLocation2[] storageLocations)
        {
            var option = new Option2<T>("CSharpFormattingOptions", group, name, defaultValue, storageLocations);
            s_allOptionsBuilder.Add(option);
            return option;
        }

        private static Option2<bool> CreateSpaceWithinParenthesesOption(SpacingWithinParenthesesOption parenthesesOption, string name)
        {
            var option = CreateOption(
                CSharpFormattingOptionGroups.Spacing, name,
                defaultValue: false,
                storageLocations: new OptionStorageLocation2[] {
                    new EditorConfigStorageLocation<bool>(
                        "csharp_space_between_parentheses",
                        s => DetermineIfSpaceOptionIsSet(s, parenthesesOption),
                        GetSpacingWithParenthesesEditorConfigString),
                    new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{name}")});

            Debug.Assert(s_spacingWithinParenthesisOptionsEditorConfigMap.ContainsValue(parenthesesOption));
            s_spacingWithinParenthesisOptionsMapBuilder.Add(option, parenthesesOption);

            return option;
        }

        private static Option2<bool> CreateNewLineForBracesOption(NewLineOption newLineOption, string name)
        {
            var option = CreateOption(
                CSharpFormattingOptionGroups.NewLine, name,
                defaultValue: true,
                storageLocations: new OptionStorageLocation2[] {
                    new EditorConfigStorageLocation<bool>(
                        "csharp_new_line_before_open_brace",
                        value => DetermineIfNewLineOptionIsSet(value, newLineOption),
                        GetNewLineOptionEditorConfigString),
                    new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{name}")});

            Debug.Assert(s_newLineOptionsEditorConfigMap.ContainsValue(newLineOption));
            s_newLineOptionsMapBuilder.Add(option, newLineOption);

            return option;
        }

        public static Option2<bool> SpacingAfterMethodDeclarationName { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpacingAfterMethodDeclarationName),
            defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_declaration_name_and_open_parenthesis"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpacingAfterMethodDeclarationName")});

        public static Option2<bool> SpaceWithinMethodDeclarationParenthesis { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceWithinMethodDeclarationParenthesis),
            defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_declaration_parameter_list_parentheses"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinMethodDeclarationParenthesis")});

        public static Option2<bool> SpaceBetweenEmptyMethodDeclarationParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBetweenEmptyMethodDeclarationParentheses),
            defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_declaration_empty_parameter_list_parentheses"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBetweenEmptyMethodDeclarationParentheses")});

        public static Option2<bool> SpaceAfterMethodCallName { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterMethodCallName),
            defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_call_name_and_opening_parenthesis"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterMethodCallName")});

        public static Option2<bool> SpaceWithinMethodCallParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceWithinMethodCallParentheses),
            defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_call_parameter_list_parentheses"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinMethodCallParentheses")});

        public static Option2<bool> SpaceBetweenEmptyMethodCallParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBetweenEmptyMethodCallParentheses),
            defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_call_empty_parameter_list_parentheses"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBetweenEmptyMethodCallParentheses")});

        public static Option2<bool> SpaceAfterControlFlowStatementKeyword { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterControlFlowStatementKeyword),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_keywords_in_control_flow_statements"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterControlFlowStatementKeyword")});

        public static Option2<bool> SpaceWithinExpressionParentheses { get; } = CreateSpaceWithinParenthesesOption(
            SpacingWithinParenthesesOption.Expressions, nameof(SpaceWithinExpressionParentheses));

        public static Option2<bool> SpaceWithinCastParentheses { get; } = CreateSpaceWithinParenthesesOption(
            SpacingWithinParenthesesOption.TypeCasts, nameof(SpaceWithinCastParentheses));

        public static Option2<bool> SpaceWithinOtherParentheses { get; } = CreateSpaceWithinParenthesesOption(
            SpacingWithinParenthesesOption.ControlFlowStatements, nameof(SpaceWithinOtherParentheses));

        public static Option2<bool> SpaceAfterCast { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterCast),
            defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_cast"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterCast")});

        public static Option2<bool> SpacesIgnoreAroundVariableDeclaration { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpacesIgnoreAroundVariableDeclaration),
            defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                new EditorConfigStorageLocation<bool>(
                    "csharp_space_around_declaration_statements",
                    s => DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet(s),
                    v => v ? "ignore" : "false"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpacesIgnoreAroundVariableDeclaration")});

        public static Option2<bool> SpaceBeforeOpenSquareBracket { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBeforeOpenSquareBracket),
            defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_before_open_square_brackets"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeOpenSquareBracket")});

        public static Option2<bool> SpaceBetweenEmptySquareBrackets { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBetweenEmptySquareBrackets),
            defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_empty_square_brackets"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBetweenEmptySquareBrackets")});

        public static Option2<bool> SpaceWithinSquareBrackets { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceWithinSquareBrackets),
            defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_between_square_brackets"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinSquareBrackets")});

        public static Option2<bool> SpaceAfterColonInBaseTypeDeclaration { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterColonInBaseTypeDeclaration),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_colon_in_inheritance_clause"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterColonInBaseTypeDeclaration")});

        public static Option2<bool> SpaceAfterComma { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterComma),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_comma"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterComma")});

        public static Option2<bool> SpaceAfterDot { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterDot),
            defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_dot"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterDot")});

        public static Option2<bool> SpaceAfterSemicolonsInForStatement { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterSemicolonsInForStatement),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_after_semicolon_in_for_statement"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterSemicolonsInForStatement")});

        public static Option2<bool> SpaceBeforeColonInBaseTypeDeclaration { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBeforeColonInBaseTypeDeclaration),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_before_colon_in_inheritance_clause"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeColonInBaseTypeDeclaration")});

        public static Option2<bool> SpaceBeforeComma { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBeforeComma),
            defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_before_comma"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeComma")});

        public static Option2<bool> SpaceBeforeDot { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBeforeDot),
            defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_before_dot"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeDot")});

        public static Option2<bool> SpaceBeforeSemicolonsInForStatement { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBeforeSemicolonsInForStatement),
            defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_space_before_semicolon_in_for_statement"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeSemicolonsInForStatement")});

        public static Option2<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpacingAroundBinaryOperator),
            defaultValue: BinaryOperatorSpacingOptions.Single,
            storageLocations: new OptionStorageLocation2[] {
                new EditorConfigStorageLocation<BinaryOperatorSpacingOptions>(
                    "csharp_space_around_binary_operators",
                    s => ParseEditorConfigSpacingAroundBinaryOperator(s),
                    GetSpacingAroundBinaryOperatorEditorConfigString),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpacingAroundBinaryOperator")});

        public static Option2<bool> IndentBraces { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(IndentBraces),
            defaultValue: false,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_indent_braces"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.OpenCloseBracesIndent")});

        public static Option2<bool> IndentBlock { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(IndentBlock),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_indent_block_contents"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentBlock")});

        public static Option2<bool> IndentSwitchSection { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(IndentSwitchSection),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_indent_switch_labels"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentSwitchSection")});

        public static Option2<bool> IndentSwitchCaseSection { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(IndentSwitchCaseSection),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_indent_case_contents"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentSwitchCaseSection")});

        public static Option2<bool> IndentSwitchCaseSectionWhenBlock { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(IndentSwitchCaseSectionWhenBlock),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_indent_case_contents_when_block"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentSwitchCaseSectionWhenBlock")});

        public static Option2<LabelPositionOptions> LabelPositioning { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(LabelPositioning),
            defaultValue: LabelPositionOptions.OneLess,
            storageLocations: new OptionStorageLocation2[] {
                new EditorConfigStorageLocation<LabelPositionOptions>(
                    "csharp_indent_labels",
                    s => ParseEditorConfigLabelPositioning(s),
                    GetLabelPositionOptionEditorConfigString),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.LabelPositioning")});

        public static Option2<bool> WrappingPreserveSingleLine { get; } = CreateOption(
            CSharpFormattingOptionGroups.Wrapping, nameof(WrappingPreserveSingleLine),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_preserve_single_line_blocks"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.WrappingPreserveSingleLine")});

        public static Option2<bool> WrappingKeepStatementsOnSingleLine { get; } = CreateOption(
            CSharpFormattingOptionGroups.Wrapping, nameof(WrappingKeepStatementsOnSingleLine),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_preserve_single_line_statements"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.WrappingKeepStatementsOnSingleLine")});

        public static Option2<bool> NewLinesForBracesInTypes { get; } = CreateNewLineForBracesOption(
            NewLineOption.Types, nameof(NewLinesForBracesInTypes));

        public static Option2<bool> NewLinesForBracesInMethods { get; } = CreateNewLineForBracesOption(
            NewLineOption.Methods, nameof(NewLinesForBracesInMethods));

        public static Option2<bool> NewLinesForBracesInProperties { get; } = CreateNewLineForBracesOption(
            NewLineOption.Properties, nameof(NewLinesForBracesInProperties));

        public static Option2<bool> NewLinesForBracesInAccessors { get; } = CreateNewLineForBracesOption(
            NewLineOption.Accessors, nameof(NewLinesForBracesInAccessors));

        public static Option2<bool> NewLinesForBracesInAnonymousMethods { get; } = CreateNewLineForBracesOption(
            NewLineOption.AnonymousMethods, nameof(NewLinesForBracesInAnonymousMethods));

        public static Option2<bool> NewLinesForBracesInControlBlocks { get; } = CreateNewLineForBracesOption(
            NewLineOption.ControlBlocks, nameof(NewLinesForBracesInControlBlocks));

        public static Option2<bool> NewLinesForBracesInAnonymousTypes { get; } = CreateNewLineForBracesOption(
            NewLineOption.AnonymousTypes, nameof(NewLinesForBracesInAnonymousTypes));

        public static Option2<bool> NewLinesForBracesInObjectCollectionArrayInitializers { get; } = CreateNewLineForBracesOption(
            NewLineOption.ObjectCollectionsArrayInitializers, nameof(NewLinesForBracesInObjectCollectionArrayInitializers));

        public static Option2<bool> NewLinesForBracesInLambdaExpressionBody { get; } = CreateNewLineForBracesOption(
            NewLineOption.Lambdas, nameof(NewLinesForBracesInLambdaExpressionBody));

        public static Option2<bool> NewLineForElse { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForElse),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_else"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForElse")});

        public static Option2<bool> NewLineForCatch { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForCatch),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_catch"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForCatch")});

        public static Option2<bool> NewLineForFinally { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForFinally),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_finally"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForFinally")});

        public static Option2<bool> NewLineForMembersInObjectInit { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForMembersInObjectInit),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_members_in_object_initializers"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForMembersInObjectInit")});

        public static Option2<bool> NewLineForMembersInAnonymousTypes { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForMembersInAnonymousTypes),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_members_in_anonymous_types"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForMembersInAnonymousTypes")});

        public static Option2<bool> NewLineForClausesInQuery { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForClausesInQuery),
            defaultValue: true,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolOption("csharp_new_line_between_query_expression_clauses"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForClausesInQuery")});

        static CSharpFormattingOptions2()
        {
            // Note that the static constructor executes after all the static field initializers for the options have executed,
            // and each field initializer adds the created option to the following builders.
            AllOptions = s_allOptionsBuilder.ToImmutable();
            SpacingWithinParenthesisOptionsMap = s_spacingWithinParenthesisOptionsMapBuilder.ToImmutable();
            NewLineOptionsMap = s_newLineOptionsMapBuilder.ToImmutable();
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
