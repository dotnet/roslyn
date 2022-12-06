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

        private static Option2<T> CreateOption<T>(
            OptionGroup group, string name, T defaultValue,
            OptionStorageLocation2 storageLocation1)
        {
            var option = new Option2<T>(
                "CSharpFormattingOptions",
                group, name, defaultValue,
                ImmutableArray.Create(storageLocation1), LanguageNames.CSharp);

            s_allOptionsBuilder.Add(option);
            return option;
        }

        private static Option2<T> CreateOption<T>(
            OptionGroup group, string name, T defaultValue,
            OptionStorageLocation2 storageLocation1,
            OptionStorageLocation2 storageLocation2)
        {
            var option = new Option2<T>(
                "CSharpFormattingOptions",
                group, name, defaultValue,
                ImmutableArray.Create(storageLocation1, storageLocation2), LanguageNames.CSharp);

            s_allOptionsBuilder.Add(option);
            return option;
        }

        private static Option2<bool> CreateNewLineForBracesLegacyOption(string name, bool defaultValue)
            => new(
                feature: "CSharpFormattingOptions",
                CSharpFormattingOptionGroups.NewLine,
                name,
                defaultValue,
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{name}"));

        public static Option2<bool> SpacingAfterMethodDeclarationName { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpacingAfterMethodDeclarationName),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterMethodDeclarationName),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_declaration_name_and_open_parenthesis"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpacingAfterMethodDeclarationName"));

        public static Option2<bool> SpaceWithinMethodDeclarationParenthesis { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceWithinMethodDeclarationParenthesis),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinMethodDeclarationParenthesis),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_declaration_parameter_list_parentheses"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinMethodDeclarationParenthesis"));

        public static Option2<bool> SpaceBetweenEmptyMethodDeclarationParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBetweenEmptyMethodDeclarationParentheses),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BetweenEmptyMethodDeclarationParentheses),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_declaration_empty_parameter_list_parentheses"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBetweenEmptyMethodDeclarationParentheses"));

        public static Option2<bool> SpaceAfterMethodCallName { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterMethodCallName),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterMethodCallName),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_call_name_and_opening_parenthesis"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterMethodCallName"));

        public static Option2<bool> SpaceWithinMethodCallParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceWithinMethodCallParentheses),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinMethodCallParentheses),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_call_parameter_list_parentheses"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinMethodCallParentheses"));

        public static Option2<bool> SpaceBetweenEmptyMethodCallParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBetweenEmptyMethodCallParentheses),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BetweenEmptyMethodCallParentheses),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_between_method_call_empty_parameter_list_parentheses"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBetweenEmptyMethodCallParentheses"));

        public static Option2<bool> SpaceAfterControlFlowStatementKeyword { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterControlFlowStatementKeyword),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterControlFlowStatementKeyword),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_after_keywords_in_control_flow_statements"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterControlFlowStatementKeyword"));

        // legacy OptionSet options:

        public static Option2<bool> SpaceWithinExpressionParentheses { get; } = new Option2<bool>(
            feature: "CSharpFormattingOptions",
            CSharpFormattingOptionGroups.Spacing,
            name: "SpaceWithinExpressionParentheses",
            defaultValue: CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinExpressionParentheses),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinExpressionParentheses"));

        public static Option2<bool> SpaceWithinCastParentheses { get; } = new Option2<bool>(
            feature: "CSharpFormattingOptions",
            CSharpFormattingOptionGroups.Spacing,
            name: "SpaceWithinCastParentheses",
            defaultValue: CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinCastParentheses),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinCastParentheses"));

        public static Option2<bool> SpaceWithinOtherParentheses { get; } = new Option2<bool>(
            feature: "CSharpFormattingOptions",
            CSharpFormattingOptionGroups.Spacing,
            name: "SpaceWithinOtherParentheses",
            defaultValue: CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinOtherParentheses),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinOtherParentheses"));

        // editor config option:
        public static Option2<SpacePlacementWithinParentheses> SpaceBetweenParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing,
            name: "csharp_space_between_parentheses",
            CSharpSyntaxFormattingOptions.SpacingDefault.ToSpacingWithinParentheses(),
            new EditorConfigStorageLocation<SpacePlacementWithinParentheses>(
                "csharp_space_between_parentheses",
                parseValue: list => ParseSpacingWithinParenthesesList(list),
#if !CODE_STYLE
                serializeValueFromOptionSet: set => ToEditorConfigValue(
                    (set.GetOption(SpaceWithinExpressionParentheses) ? SpacePlacementWithinParentheses.Expressions : 0) |
                    (set.GetOption(SpaceWithinCastParentheses) ? SpacePlacementWithinParentheses.TypeCasts : 0) |
                    (set.GetOption(SpaceWithinOtherParentheses) ? SpacePlacementWithinParentheses.ControlFlowStatements : 0)),
#endif
                serializeValue: ToEditorConfigValue));

        public static Option2<bool> SpaceAfterCast { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterCast),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterCast),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_after_cast"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterCast"));

        public static Option2<bool> SpacesIgnoreAroundVariableDeclaration { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpacesIgnoreAroundVariableDeclaration),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.IgnoreAroundVariableDeclaration),
            new EditorConfigStorageLocation<bool>(
                "csharp_space_around_declaration_statements",
                s => DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet(s),
                v => v ? "ignore" : "false"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpacesIgnoreAroundVariableDeclaration"));

        public static Option2<bool> SpaceBeforeOpenSquareBracket { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBeforeOpenSquareBracket),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeOpenSquareBracket),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_before_open_square_brackets"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeOpenSquareBracket"));

        public static Option2<bool> SpaceBetweenEmptySquareBrackets { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBetweenEmptySquareBrackets),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BetweenEmptySquareBrackets),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_between_empty_square_brackets"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBetweenEmptySquareBrackets"));

        public static Option2<bool> SpaceWithinSquareBrackets { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceWithinSquareBrackets),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinSquareBrackets),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_between_square_brackets"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinSquareBrackets"));

        public static Option2<bool> SpaceAfterColonInBaseTypeDeclaration { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterColonInBaseTypeDeclaration),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterColonInBaseTypeDeclaration),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_after_colon_in_inheritance_clause"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterColonInBaseTypeDeclaration"));

        public static Option2<bool> SpaceAfterComma { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterComma),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterComma),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_after_comma"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterComma"));

        public static Option2<bool> SpaceAfterDot { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterDot),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterDot),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_after_dot"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterDot"));

        public static Option2<bool> SpaceAfterSemicolonsInForStatement { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterSemicolonsInForStatement),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterSemicolonsInForStatement),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_after_semicolon_in_for_statement"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterSemicolonsInForStatement"));

        public static Option2<bool> SpaceBeforeColonInBaseTypeDeclaration { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBeforeColonInBaseTypeDeclaration),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeColonInBaseTypeDeclaration),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_before_colon_in_inheritance_clause"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeColonInBaseTypeDeclaration"));

        public static Option2<bool> SpaceBeforeComma { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBeforeComma),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeComma),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_before_comma"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeComma"));

        public static Option2<bool> SpaceBeforeDot { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBeforeDot),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeDot),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_before_dot"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeDot"));

        public static Option2<bool> SpaceBeforeSemicolonsInForStatement { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBeforeSemicolonsInForStatement),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeSemicolonsInForStatement),
            EditorConfigStorageLocation.ForBoolOption("csharp_space_before_semicolon_in_for_statement"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeSemicolonsInForStatement"));

        public static Option2<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpacingAroundBinaryOperator),
            CSharpSyntaxFormattingOptions.Default.SpacingAroundBinaryOperator,
            new EditorConfigStorageLocation<BinaryOperatorSpacingOptions>(
                "csharp_space_around_binary_operators",
                s => ParseEditorConfigSpacingAroundBinaryOperator(s),
                GetSpacingAroundBinaryOperatorEditorConfigString),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpacingAroundBinaryOperator"));

        public static Option2<bool> IndentBraces { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(IndentBraces),
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.Braces),
            EditorConfigStorageLocation.ForBoolOption("csharp_indent_braces"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.OpenCloseBracesIndent"));

        public static Option2<bool> IndentBlock { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(IndentBlock),
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.BlockContents),
            EditorConfigStorageLocation.ForBoolOption("csharp_indent_block_contents"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentBlock"));

        public static Option2<bool> IndentSwitchSection { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(IndentSwitchSection),
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.SwitchSection),
            EditorConfigStorageLocation.ForBoolOption("csharp_indent_switch_labels"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentSwitchSection"));

        public static Option2<bool> IndentSwitchCaseSection { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(IndentSwitchCaseSection),
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.SwitchSection),
            EditorConfigStorageLocation.ForBoolOption("csharp_indent_case_contents"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentSwitchCaseSection"));

        public static Option2<bool> IndentSwitchCaseSectionWhenBlock { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(IndentSwitchCaseSectionWhenBlock),
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.SwitchCaseContentsWhenBlock),
            EditorConfigStorageLocation.ForBoolOption("csharp_indent_case_contents_when_block"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentSwitchCaseSectionWhenBlock"));

        public static Option2<LabelPositionOptions> LabelPositioning { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(LabelPositioning),
            CSharpSyntaxFormattingOptions.Default.LabelPositioning,
            new EditorConfigStorageLocation<LabelPositionOptions>(
                "csharp_indent_labels",
                s => ParseEditorConfigLabelPositioning(s),
                GetLabelPositionOptionEditorConfigString),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.LabelPositioning"));

        public static Option2<bool> WrappingPreserveSingleLine { get; } = CreateOption(
            CSharpFormattingOptionGroups.Wrapping, nameof(WrappingPreserveSingleLine),
            CSharpSyntaxFormattingOptions.Default.WrappingPreserveSingleLine,
            EditorConfigStorageLocation.ForBoolOption("csharp_preserve_single_line_blocks"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.WrappingPreserveSingleLine"));

        public static Option2<bool> WrappingKeepStatementsOnSingleLine { get; } = CreateOption(
            CSharpFormattingOptionGroups.Wrapping, nameof(WrappingKeepStatementsOnSingleLine),
            CSharpSyntaxFormattingOptions.Default.WrappingKeepStatementsOnSingleLine,
            EditorConfigStorageLocation.ForBoolOption("csharp_preserve_single_line_statements"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.WrappingKeepStatementsOnSingleLine"));

        // legacy OptionSet options:

        public static Option2<bool> NewLinesForBracesInTypes { get; } = CreateNewLineForBracesLegacyOption(
            "NewLinesForBracesInTypes",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInTypes));

        public static Option2<bool> NewLinesForBracesInMethods { get; } = CreateNewLineForBracesLegacyOption(
            "NewLinesForBracesInMethods",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInMethods));

        public static Option2<bool> NewLinesForBracesInProperties { get; } = CreateNewLineForBracesLegacyOption(
            "NewLinesForBracesInProperties",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInProperties));

        public static Option2<bool> NewLinesForBracesInAccessors { get; } = CreateNewLineForBracesLegacyOption(
            "NewLinesForBracesInAccessors",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInAccessors));

        public static Option2<bool> NewLinesForBracesInAnonymousMethods { get; } = CreateNewLineForBracesLegacyOption(
            "NewLinesForBracesInAnonymousMethods",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousMethods));

        public static Option2<bool> NewLinesForBracesInControlBlocks { get; } = CreateNewLineForBracesLegacyOption(
            "NewLinesForBracesInControlBlocks",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInControlBlocks));

        public static Option2<bool> NewLinesForBracesInAnonymousTypes { get; } = CreateNewLineForBracesLegacyOption(
            "NewLinesForBracesInAnonymousTypes",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousTypes));

        public static Option2<bool> NewLinesForBracesInObjectCollectionArrayInitializers { get; } = CreateNewLineForBracesLegacyOption(
            "NewLinesForBracesInObjectCollectionArrayInitializers",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers));

        public static Option2<bool> NewLinesForBracesInLambdaExpressionBody { get; } = CreateNewLineForBracesLegacyOption(
            "NewLinesForBracesInLambdaExpressionBody",
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody));

        // editor config option:
        public static Option2<NewLineBeforeOpenBracePlacement> NewLineBeforeOpenBrace { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine,
            name: "csharp_new_line_before_open_brace",
            CSharpSyntaxFormattingOptions.NewLinesDefault.ToNewLineBeforeOpenBracePlacement(),
            new EditorConfigStorageLocation<NewLineBeforeOpenBracePlacement>(
                 "csharp_new_line_before_open_brace",
                parseValue: list => ParseNewLineBeforeOpenBracePlacementList(list),
#if !CODE_STYLE
                serializeValueFromOptionSet: set => ToEditorConfigValue(
                    (set.GetOption(NewLinesForBracesInTypes) ? NewLineBeforeOpenBracePlacement.Types : 0) |
                    (set.GetOption(NewLinesForBracesInAnonymousTypes) ? NewLineBeforeOpenBracePlacement.AnonymousTypes : 0) |
                    (set.GetOption(NewLinesForBracesInObjectCollectionArrayInitializers) ? NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers : 0) |
                    (set.GetOption(NewLinesForBracesInProperties) ? NewLineBeforeOpenBracePlacement.Properties : 0) |
                    (set.GetOption(NewLinesForBracesInMethods) ? NewLineBeforeOpenBracePlacement.Methods : 0) |
                    (set.GetOption(NewLinesForBracesInAccessors) ? NewLineBeforeOpenBracePlacement.Accessors : 0) |
                    (set.GetOption(NewLinesForBracesInAnonymousMethods) ? NewLineBeforeOpenBracePlacement.AnonymousMethods : 0) |
                    (set.GetOption(NewLinesForBracesInLambdaExpressionBody) ? NewLineBeforeOpenBracePlacement.LambdaExpressionBody : 0) |
                    (set.GetOption(NewLinesForBracesInControlBlocks) ? NewLineBeforeOpenBracePlacement.ControlBlocks : 0)),
#endif
                serializeValue: ToEditorConfigValue));

        public static Option2<bool> NewLineForElse { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForElse),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeElse),
            EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_else"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForElse"));

        public static Option2<bool> NewLineForCatch { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForCatch),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeCatch),
            EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_catch"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForCatch"));

        public static Option2<bool> NewLineForFinally { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForFinally),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeFinally),
            EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_finally"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForFinally"));

        public static Option2<bool> NewLineForMembersInObjectInit { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForMembersInObjectInit),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeMembersInObjectInitializers),
            EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_members_in_object_initializers"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForMembersInObjectInit"));

        public static Option2<bool> NewLineForMembersInAnonymousTypes { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForMembersInAnonymousTypes),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeMembersInAnonymousTypes),
            EditorConfigStorageLocation.ForBoolOption("csharp_new_line_before_members_in_anonymous_types"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForMembersInAnonymousTypes"));

        public static Option2<bool> NewLineForClausesInQuery { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForClausesInQuery),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BetweenQueryExpressionClauses),
            EditorConfigStorageLocation.ForBoolOption("csharp_new_line_between_query_expression_clauses"),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForClausesInQuery"));

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
