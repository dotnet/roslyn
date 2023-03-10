// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Formatting;

#if CODE_STYLE
using CSharpWorkspaceResources = Microsoft.CodeAnalysis.CSharp.CSharpCodeStyleResources;
using WorkspacesResources = Microsoft.CodeAnalysis.CodeStyleResources;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal static partial class CSharpFormattingOptions2
    {
        private const string PublicFeatureName = "CSharpFormattingOptions";

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

        private static Option2<T> CreateOption<T>(OptionGroup group, string name, T defaultValue, EditorConfigValueSerializer<T>? serializer = null)
        {
            var option = new Option2<T>(name, defaultValue, group, LanguageNames.CSharp, isEditorConfigOption: true, serializer: serializer);
            s_allOptionsBuilder.Add(option);
            return option;
        }

        public static Option2<bool> SpacingAfterMethodDeclarationName { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_between_method_declaration_name_and_open_parenthesis",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterMethodDeclarationName))
            .WithPublicOption(PublicFeatureName, "SpacingAfterMethodDeclarationName");

        public static Option2<bool> SpaceWithinMethodDeclarationParenthesis { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_between_method_declaration_parameter_list_parentheses",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinMethodDeclarationParenthesis))
            .WithPublicOption(PublicFeatureName, "SpaceWithinMethodDeclarationParenthesis");

        public static Option2<bool> SpaceBetweenEmptyMethodDeclarationParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_between_method_declaration_empty_parameter_list_parentheses",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BetweenEmptyMethodDeclarationParentheses))
            .WithPublicOption(PublicFeatureName, "SpaceBetweenEmptyMethodDeclarationParentheses");

        public static Option2<bool> SpaceAfterMethodCallName { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_between_method_call_name_and_opening_parenthesis",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterMethodCallName))
            .WithPublicOption(PublicFeatureName, "SpaceAfterMethodCallName");

        public static Option2<bool> SpaceWithinMethodCallParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_between_method_call_parameter_list_parentheses",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinMethodCallParentheses))
            .WithPublicOption(PublicFeatureName, "SpaceWithinMethodCallParentheses");

        public static Option2<bool> SpaceBetweenEmptyMethodCallParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_between_method_call_empty_parameter_list_parentheses",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BetweenEmptyMethodCallParentheses))
            .WithPublicOption(PublicFeatureName, "SpaceBetweenEmptyMethodCallParentheses");

        public static Option2<bool> SpaceAfterControlFlowStatementKeyword { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_after_keywords_in_control_flow_statements",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterControlFlowStatementKeyword))
            .WithPublicOption(PublicFeatureName, "SpaceAfterControlFlowStatementKeyword");

        public static Option2<SpacePlacementWithinParentheses> SpaceBetweenParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing,
            name: "csharp_space_between_parentheses",
            CSharpSyntaxFormattingOptions.SpacingDefault.ToSpacingWithinParentheses(),
            new EditorConfigValueSerializer<SpacePlacementWithinParentheses>(
                parseValue: list => ParseSpacingWithinParenthesesList(list),
                serializeValue: ToEditorConfigValue));

        public static Option2<bool> SpaceAfterCast { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_after_cast",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterCast))
            .WithPublicOption(PublicFeatureName, "SpaceAfterCast");

        public static Option2<bool> SpacesIgnoreAroundVariableDeclaration { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_around_declaration_statements",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.IgnoreAroundVariableDeclaration),
            new EditorConfigValueSerializer<bool>(
                s => DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet(s),
                v => v ? "ignore" : "false"))
            .WithPublicOption(PublicFeatureName, "SpacesIgnoreAroundVariableDeclaration");

        public static Option2<bool> SpaceBeforeOpenSquareBracket { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_before_open_square_brackets",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeOpenSquareBracket))
            .WithPublicOption(PublicFeatureName, "SpaceBeforeOpenSquareBracket");

        public static Option2<bool> SpaceBetweenEmptySquareBrackets { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_between_empty_square_brackets",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BetweenEmptySquareBrackets))
            .WithPublicOption(PublicFeatureName, "SpaceBetweenEmptySquareBrackets");

        public static Option2<bool> SpaceWithinSquareBrackets { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_between_square_brackets",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinSquareBrackets))
            .WithPublicOption(PublicFeatureName, "SpaceWithinSquareBrackets");

        public static Option2<bool> SpaceAfterColonInBaseTypeDeclaration { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_after_colon_in_inheritance_clause",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterColonInBaseTypeDeclaration))
            .WithPublicOption(PublicFeatureName, "SpaceAfterColonInBaseTypeDeclaration");

        public static Option2<bool> SpaceAfterComma { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_after_comma",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterComma))
            .WithPublicOption(PublicFeatureName, "SpaceAfterComma");

        public static Option2<bool> SpaceAfterDot { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_after_dot",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterDot))
            .WithPublicOption(PublicFeatureName, "SpaceAfterDot");

        public static Option2<bool> SpaceAfterSemicolonsInForStatement { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_after_semicolon_in_for_statement",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterSemicolonsInForStatement))
            .WithPublicOption(PublicFeatureName, "SpaceAfterSemicolonsInForStatement");

        public static Option2<bool> SpaceBeforeColonInBaseTypeDeclaration { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_before_colon_in_inheritance_clause",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeColonInBaseTypeDeclaration))
            .WithPublicOption(PublicFeatureName, "SpaceBeforeColonInBaseTypeDeclaration");

        public static Option2<bool> SpaceBeforeComma { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_before_comma",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeComma))
            .WithPublicOption(PublicFeatureName, "SpaceBeforeComma");

        public static Option2<bool> SpaceBeforeDot { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_before_dot",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeDot))
            .WithPublicOption(PublicFeatureName, "SpaceBeforeDot");

        public static Option2<bool> SpaceBeforeSemicolonsInForStatement { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_before_semicolon_in_for_statement",
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeSemicolonsInForStatement))
            .WithPublicOption(PublicFeatureName, "SpaceBeforeSemicolonsInForStatement");

        public static Option2<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, "csharp_space_around_binary_operators",
            CSharpSyntaxFormattingOptions.Default.SpacingAroundBinaryOperator,
            new EditorConfigValueSerializer<BinaryOperatorSpacingOptions>(
                s => ParseEditorConfigSpacingAroundBinaryOperator(s),
                GetSpacingAroundBinaryOperatorEditorConfigString))
            .WithPublicOption(PublicFeatureName, "SpacingAroundBinaryOperator");

        public static Option2<bool> IndentBraces { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, "csharp_indent_braces",
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.Braces))
            .WithPublicOption(PublicFeatureName, "IndentBraces");

        public static Option2<bool> IndentBlock { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, "csharp_indent_block_contents",
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.BlockContents))
            .WithPublicOption(PublicFeatureName, "IndentBlock");

        public static Option2<bool> IndentSwitchSection { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, "csharp_indent_switch_labels",
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.SwitchSection))
            .WithPublicOption(PublicFeatureName, "IndentSwitchSection");

        public static Option2<bool> IndentSwitchCaseSection { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, "csharp_indent_case_contents",
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.SwitchSection))
            .WithPublicOption(PublicFeatureName, "IndentSwitchCaseSection");

        public static Option2<bool> IndentSwitchCaseSectionWhenBlock { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, "csharp_indent_case_contents_when_block",
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.SwitchCaseContentsWhenBlock))
            .WithPublicOption(PublicFeatureName, "IndentSwitchCaseSectionWhenBlock");

        public static Option2<LabelPositionOptions> LabelPositioning { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, "csharp_indent_labels",
            CSharpSyntaxFormattingOptions.Default.LabelPositioning,
            new EditorConfigValueSerializer<LabelPositionOptions>(
                s => ParseEditorConfigLabelPositioning(s),
                GetLabelPositionOptionEditorConfigString))
            .WithPublicOption(PublicFeatureName, "LabelPositioning");

        public static Option2<bool> WrappingPreserveSingleLine { get; } = CreateOption(
            CSharpFormattingOptionGroups.Wrapping, "csharp_preserve_single_line_blocks",
            CSharpSyntaxFormattingOptions.Default.WrappingPreserveSingleLine)
            .WithPublicOption(PublicFeatureName, "WrappingPreserveSingleLine");

        public static Option2<bool> WrappingKeepStatementsOnSingleLine { get; } = CreateOption(
            CSharpFormattingOptionGroups.Wrapping, "csharp_preserve_single_line_statements",
            CSharpSyntaxFormattingOptions.Default.WrappingKeepStatementsOnSingleLine)
            .WithPublicOption(PublicFeatureName, "WrappingKeepStatementsOnSingleLine");

        public static Option2<NewLineBeforeOpenBracePlacement> NewLineBeforeOpenBrace { get; } = CreateOption(
            FormattingOptionGroups.NewLine,
            name: "csharp_new_line_before_open_brace",
            CSharpSyntaxFormattingOptions.NewLinesDefault.ToNewLineBeforeOpenBracePlacement(),
            new EditorConfigValueSerializer<NewLineBeforeOpenBracePlacement>(
                parseValue: list => ParseNewLineBeforeOpenBracePlacementList(list),
                serializeValue: ToEditorConfigValue));

        public static Option2<bool> NewLineForElse { get; } = CreateOption(
            FormattingOptionGroups.NewLine, "csharp_new_line_before_else",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeElse))
            .WithPublicOption(PublicFeatureName, "NewLineForElse");

        public static Option2<bool> NewLineForCatch { get; } = CreateOption(
            FormattingOptionGroups.NewLine, "csharp_new_line_before_catch",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeCatch))
            .WithPublicOption(PublicFeatureName, "NewLineForCatch");

        public static Option2<bool> NewLineForFinally { get; } = CreateOption(
            FormattingOptionGroups.NewLine, "csharp_new_line_before_finally",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeFinally))
            .WithPublicOption(PublicFeatureName, "NewLineForFinally");

        public static Option2<bool> NewLineForMembersInObjectInit { get; } = CreateOption(
            FormattingOptionGroups.NewLine, "csharp_new_line_before_members_in_object_initializers",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeMembersInObjectInitializers))
            .WithPublicOption(PublicFeatureName, "NewLineForMembersInObjectInit");

        public static Option2<bool> NewLineForMembersInAnonymousTypes { get; } = CreateOption(
            FormattingOptionGroups.NewLine, "csharp_new_line_before_members_in_anonymous_types",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeMembersInAnonymousTypes))
            .WithPublicOption(PublicFeatureName, "NewLineForMembersInAnonymousTypes");

        public static Option2<bool> NewLineForClausesInQuery { get; } = CreateOption(
            FormattingOptionGroups.NewLine, "csharp_new_line_between_query_expression_clauses",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BetweenQueryExpressionClauses))
            .WithPublicOption(PublicFeatureName, "NewLineForClausesInQuery");

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
        public static readonly OptionGroup Indentation = new("csharp_indentation", CSharpWorkspaceResources.Indentation_preferences, priority: 3, parent: FormattingOptionGroups.FormattingOptionGroup);
        public static readonly OptionGroup Spacing = new("csharp_spacing", CSharpWorkspaceResources.Space_preferences, priority: 4, parent: FormattingOptionGroups.FormattingOptionGroup);
        public static readonly OptionGroup Wrapping = new("csharp_wrapping", CSharpWorkspaceResources.Wrapping_preferences, priority: 5, parent: FormattingOptionGroups.FormattingOptionGroup);
    }
}
