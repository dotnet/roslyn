// Licensed to the .NET Foundation under one or more agreements.
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
#if !CODE_STYLE
        private const string FeatureName = "CSharpFormattingOptions";
#endif
        private static readonly ImmutableArray<IOption2>.Builder s_allOptionsBuilder = ImmutableArray.CreateBuilder<IOption2>();

        // Maps to store mapping between special option kinds and the corresponding editor config string representations.
        #region Editor Config maps
        private static readonly BidirectionalMap<string, SpacePlacementWithinParentheses> s_spacingWithinParenthesisOptionsEditorConfigMap =
            new(new[]
            {
                KeyValuePairUtil.Create("expressions", SpacePlacementWithinParentheses.Expressions),
                KeyValuePairUtil.Create("type_casts", SpacePlacementWithinParentheses.TypeCasts),
                KeyValuePairUtil.Create("control_flow_statements", SpacePlacementWithinParentheses.ControlFlowStatements),
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
        private static readonly BidirectionalMap<string, NewLineBeforeOpenBracePlacement> s_legacyNewLineOptionsEditorConfigMap =
            new(new[]
            {
                KeyValuePairUtil.Create("object_collection_array_initalizers", NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers),
            });
        private static readonly BidirectionalMap<string, NewLineBeforeOpenBracePlacement> s_newLineOptionsEditorConfigMap =
            new(new[]
            {
                KeyValuePairUtil.Create("all", NewLineBeforeOpenBracePlacement.All),
                KeyValuePairUtil.Create("accessors", NewLineBeforeOpenBracePlacement.Accessors),
                KeyValuePairUtil.Create("types", NewLineBeforeOpenBracePlacement.Types),
                KeyValuePairUtil.Create("methods", NewLineBeforeOpenBracePlacement.Methods),
                KeyValuePairUtil.Create("properties", NewLineBeforeOpenBracePlacement.Properties),
                KeyValuePairUtil.Create("anonymous_methods", NewLineBeforeOpenBracePlacement.AnonymousMethods),
                KeyValuePairUtil.Create("control_blocks", NewLineBeforeOpenBracePlacement.ControlBlocks),
                KeyValuePairUtil.Create("anonymous_types", NewLineBeforeOpenBracePlacement.AnonymousTypes),
                KeyValuePairUtil.Create("object_collection_array_initializers", NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers),
                KeyValuePairUtil.Create("lambdas", NewLineBeforeOpenBracePlacement.LambdaExpressionBody),
            });
        #endregion

        internal static ImmutableArray<IOption2> AllOptions { get; }

        private static Option2<T> CreateOption<T>(OptionGroup group, string name, T defaultValue, EditorConfigStorageLocation<T> storageLocation)
        {
            var option = new Option2<T>(group, name, defaultValue, storageLocation, LanguageNames.CSharp);
            s_allOptionsBuilder.Add(option);
            return option;
        }

        private static Option2<bool> CreateNewLineForBracesLegacyOption(string name, bool defaultValue)
            => new(CSharpFormattingOptionGroups.NewLine, name, defaultValue);

        public static Option2<bool> SpacingAfterMethodDeclarationName { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_between_method_declaration_name_and_open_parenthesis",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterMethodDeclarationName),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpaceWithinMethodDeclarationParenthesis { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_between_method_declaration_parameter_list_parentheses",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinMethodDeclarationParenthesis),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpaceBetweenEmptyMethodDeclarationParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_between_method_declaration_empty_parameter_list_parentheses",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BetweenEmptyMethodDeclarationParentheses),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpaceAfterMethodCallName { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_between_method_call_name_and_opening_parenthesis",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterMethodCallName),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpaceWithinMethodCallParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_between_method_call_parameter_list_parentheses",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinMethodCallParentheses),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpaceBetweenEmptyMethodCallParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_between_method_call_empty_parameter_list_parentheses",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BetweenEmptyMethodCallParentheses),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpaceAfterControlFlowStatementKeyword { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_after_keywords_in_control_flow_statements",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterControlFlowStatementKeyword),
            EditorConfigStorageLocation.ForBoolOption());

        // Legacy options, only to be used in OptionSets and global options.

        public static Option2<bool> SpaceWithinExpressionParentheses { get; } = new Option2<bool>(
            CSharpFormattingOptionGroups.Spacing,
            name: "CSharpFormattingOptions_SpaceWithinExpressionParentheses",
            defaultValue: CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinExpressionParentheses));

        public static Option2<bool> SpaceWithinCastParentheses { get; } = new Option2<bool>(
            CSharpFormattingOptionGroups.Spacing,
            name: "CSharpFormattingOptions_SpaceWithinCastParentheses",
            defaultValue: CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinCastParentheses));

        public static Option2<bool> SpaceWithinOtherParentheses { get; } = new Option2<bool>(
            CSharpFormattingOptionGroups.Spacing,
            name: "CSharpFormattingOptions_SpaceWithinOtherParentheses",
            defaultValue: CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinOtherParentheses));

        // editor config option:
        public static Option2<SpacePlacementWithinParentheses> SpaceBetweenParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing,
            name: "csharp_space_between_parentheses",
            CSharpSyntaxFormattingOptions.SpacingDefault.ToSpacingWithinParentheses(),
            new EditorConfigStorageLocation<SpacePlacementWithinParentheses>(
                parseValue: list => ParseSpacingWithinParenthesesList(list),
#if !CODE_STYLE
#pragma warning disable RS0030 // Do not used banned APIs
                getValueFromOptionSet: set =>
                    (set.GetOption(CSharpFormattingOptions.SpaceWithinExpressionParentheses) ? SpacePlacementWithinParentheses.Expressions : 0) |
                    (set.GetOption(CSharpFormattingOptions.SpaceWithinCastParentheses) ? SpacePlacementWithinParentheses.TypeCasts : 0) |
                    (set.GetOption(CSharpFormattingOptions.SpaceWithinOtherParentheses) ? SpacePlacementWithinParentheses.ControlFlowStatements : 0),
#pragma warning restore
#endif
                serializeValue: ToEditorConfigValue));

        public static Option2<bool> SpaceAfterCast { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_after_cast",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterCast),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpacesIgnoreAroundVariableDeclaration { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_around_declaration_statements",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.IgnoreAroundVariableDeclaration),
            new EditorConfigStorageLocation<bool>(
                s => DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet(s),
                v => v ? "ignore" : "false"));

        public static Option2<bool> SpaceBeforeOpenSquareBracket { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_before_open_square_brackets",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeOpenSquareBracket),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpaceBetweenEmptySquareBrackets { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_between_empty_square_brackets",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BetweenEmptySquareBrackets),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpaceWithinSquareBrackets { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_between_square_brackets",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinSquareBrackets),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpaceAfterColonInBaseTypeDeclaration { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_after_colon_in_inheritance_clause",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterColonInBaseTypeDeclaration),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpaceAfterComma { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_after_comma",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterComma),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpaceAfterDot { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_after_dot",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterDot),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpaceAfterSemicolonsInForStatement { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_after_semicolon_in_for_statement",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterSemicolonsInForStatement),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpaceBeforeColonInBaseTypeDeclaration { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_before_colon_in_inheritance_clause",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeColonInBaseTypeDeclaration),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpaceBeforeComma { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_before_comma",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeComma),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpaceBeforeDot { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_before_dot",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeDot),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> SpaceBeforeSemicolonsInForStatement { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_before_semicolon_in_for_statement",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeSemicolonsInForStatement),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_around_binary_operators",
            CSharpSyntaxFormattingOptions.Default.SpacingAroundBinaryOperator,
            new EditorConfigStorageLocation<BinaryOperatorSpacingOptions>(
                s => ParseEditorConfigSpacingAroundBinaryOperator(s),
                GetSpacingAroundBinaryOperatorEditorConfigString));

        public static Option2<bool> IndentBraces { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, "csharp_indent_braces",
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.Braces),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> IndentBlock { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, "csharp_indent_block_contents",
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.BlockContents),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> IndentSwitchSection { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, "csharp_indent_switch_labels",
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.SwitchSection),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> IndentSwitchCaseSection { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, "csharp_indent_case_contents",
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.SwitchSection),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> IndentSwitchCaseSectionWhenBlock { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, "csharp_indent_case_contents_when_block",
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.SwitchCaseContentsWhenBlock),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<LabelPositionOptions> LabelPositioning { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, "csharp_indent_labels",
            CSharpSyntaxFormattingOptions.Default.LabelPositioning,
            new EditorConfigStorageLocation<LabelPositionOptions>(
                s => ParseEditorConfigLabelPositioning(s),
                GetLabelPositionOptionEditorConfigString));

        public static Option2<bool> WrappingPreserveSingleLine { get; } = CreateOption(
            CSharpFormattingOptionGroups.Wrapping, "csharp_preserve_single_line_blocks",
            CSharpSyntaxFormattingOptions.Default.WrappingPreserveSingleLine,
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> WrappingKeepStatementsOnSingleLine { get; } = CreateOption(
            CSharpFormattingOptionGroups.Wrapping, "csharp_preserve_single_line_statements",
            CSharpSyntaxFormattingOptions.Default.WrappingKeepStatementsOnSingleLine,
            EditorConfigStorageLocation.ForBoolOption());

        // Legacy options, only to be used in OptionSets and global options.

        public static Option2<bool> NewLinesForBracesInTypes { get; } = CreateNewLineForBracesLegacyOption(
            "CSharpFormattingOptions_NewLinesForBracesInTypes",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInTypes));

        public static Option2<bool> NewLinesForBracesInMethods { get; } = CreateNewLineForBracesLegacyOption(
            "CSharpFormattingOptions_NewLinesForBracesInMethods",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInMethods));

        public static Option2<bool> NewLinesForBracesInProperties { get; } = CreateNewLineForBracesLegacyOption(
            "CSharpFormattingOptions_NewLinesForBracesInProperties",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInProperties));

        public static Option2<bool> NewLinesForBracesInAccessors { get; } = CreateNewLineForBracesLegacyOption(
            "CSharpFormattingOptions_NewLinesForBracesInAccessors",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInAccessors));

        public static Option2<bool> NewLinesForBracesInAnonymousMethods { get; } = CreateNewLineForBracesLegacyOption(
            "CSharpFormattingOptions_NewLinesForBracesInAnonymousMethods",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousMethods));

        public static Option2<bool> NewLinesForBracesInControlBlocks { get; } = CreateNewLineForBracesLegacyOption(
            "CSharpFormattingOptions_NewLinesForBracesInControlBlocks",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInControlBlocks));

        public static Option2<bool> NewLinesForBracesInAnonymousTypes { get; } = CreateNewLineForBracesLegacyOption(
            "CSharpFormattingOptions_NewLinesForBracesInAnonymousTypes",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousTypes));

        public static Option2<bool> NewLinesForBracesInObjectCollectionArrayInitializers { get; } = CreateNewLineForBracesLegacyOption(
            "CSharpFormattingOptions_NewLinesForBracesInObjectCollectionArrayInitializers",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers));

        public static Option2<bool> NewLinesForBracesInLambdaExpressionBody { get; } = CreateNewLineForBracesLegacyOption(
            "CSharpFormattingOptions_NewLinesForBracesInLambdaExpressionBody",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody));

        // editor config option:
        public static Option2<NewLineBeforeOpenBracePlacement> NewLineBeforeOpenBrace { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine,
            name: "csharp_new_line_before_open_brace",
            CSharpSyntaxFormattingOptions.NewLinesDefault.ToNewLineBeforeOpenBracePlacement(),
            new EditorConfigStorageLocation<NewLineBeforeOpenBracePlacement>(
                parseValue: list => ParseNewLineBeforeOpenBracePlacementList(list),
#if !CODE_STYLE
#pragma warning disable RS0030 // Do not used banned APIs
                getValueFromOptionSet: set =>
                    (set.GetOption(CSharpFormattingOptions.NewLinesForBracesInTypes) ? NewLineBeforeOpenBracePlacement.Types : 0) |
                    (set.GetOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes) ? NewLineBeforeOpenBracePlacement.AnonymousTypes : 0) |
                    (set.GetOption(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers) ? NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers : 0) |
                    (set.GetOption(CSharpFormattingOptions.NewLinesForBracesInProperties) ? NewLineBeforeOpenBracePlacement.Properties : 0) |
                    (set.GetOption(CSharpFormattingOptions.NewLinesForBracesInMethods) ? NewLineBeforeOpenBracePlacement.Methods : 0) |
                    (set.GetOption(CSharpFormattingOptions.NewLinesForBracesInAccessors) ? NewLineBeforeOpenBracePlacement.Accessors : 0) |
                    (set.GetOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods) ? NewLineBeforeOpenBracePlacement.AnonymousMethods : 0) |
                    (set.GetOption(CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody) ? NewLineBeforeOpenBracePlacement.LambdaExpressionBody : 0) |
                    (set.GetOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks) ? NewLineBeforeOpenBracePlacement.ControlBlocks : 0),
#pragma warning restore
#endif
                serializeValue: ToEditorConfigValue));

        public static Option2<bool> NewLineForElse { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, "csharp_new_line_before_else",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeElse),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> NewLineForCatch { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, "csharp_new_line_before_catch",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeCatch),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> NewLineForFinally { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, "csharp_new_line_before_finally",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeFinally),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> NewLineForMembersInObjectInit { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, "csharp_new_line_before_members_in_object_initializers",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeMembersInObjectInitializers),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> NewLineForMembersInAnonymousTypes { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, "csharp_new_line_before_members_in_anonymous_types",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeMembersInAnonymousTypes),
            EditorConfigStorageLocation.ForBoolOption());

        public static Option2<bool> NewLineForClausesInQuery { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, "csharp_new_line_between_query_expression_clauses",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BetweenQueryExpressionClauses),
            EditorConfigStorageLocation.ForBoolOption());

        static CSharpFormattingOptions2()
        {
            // Note that the static constructor executes after all the static field initializers for the options have executed,
            // and each field initializer adds the created option to the following builders.

            AllOptions = s_allOptionsBuilder.ToImmutable();
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
