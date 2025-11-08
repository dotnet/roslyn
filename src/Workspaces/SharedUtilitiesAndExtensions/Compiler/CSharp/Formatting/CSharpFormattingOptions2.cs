// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal static partial class CSharpFormattingOptions2
{
    private const string PublicFeatureName = "CSharpFormattingOptions";

    private static readonly ImmutableArray<IOption2>.Builder s_editorConfigOptionsBuilder = ImmutableArray.CreateBuilder<IOption2>();

    // Maps to store mapping between special option kinds and the corresponding editor config string representations.
    #region Editor Config maps
    private static readonly BidirectionalMap<string, SpacePlacementWithinParentheses> s_spacingWithinParenthesisOptionsEditorConfigMap =
        new(
        [
            KeyValuePair.Create("expressions", SpacePlacementWithinParentheses.Expressions),
            KeyValuePair.Create("type_casts", SpacePlacementWithinParentheses.TypeCasts),
            KeyValuePair.Create("control_flow_statements", SpacePlacementWithinParentheses.ControlFlowStatements),
        ]);
    private static readonly BidirectionalMap<string, BinaryOperatorSpacingOptionsInternal> s_binaryOperatorSpacingOptionsEditorConfigMap =
        new(
        [
            KeyValuePair.Create("ignore", BinaryOperatorSpacingOptionsInternal.Ignore),
            KeyValuePair.Create("none", BinaryOperatorSpacingOptionsInternal.Remove),
            KeyValuePair.Create("before_and_after", BinaryOperatorSpacingOptionsInternal.Single),
        ]);
    private static readonly BidirectionalMap<string, LabelPositionOptionsInternal> s_labelPositionOptionsEditorConfigMap =
        new(
        [
            KeyValuePair.Create("flush_left", LabelPositionOptionsInternal.LeftMost),
            KeyValuePair.Create("no_change", LabelPositionOptionsInternal.NoIndent),
            KeyValuePair.Create("one_less_than_current", LabelPositionOptionsInternal.OneLess),
        ]);
    private static readonly BidirectionalMap<string, NewLineBeforeOpenBracePlacement> s_legacyNewLineOptionsEditorConfigMap =
        new(
        [
            KeyValuePair.Create("object_collection_array_initalizers", NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers),
        ]);
    private static readonly BidirectionalMap<string, NewLineBeforeOpenBracePlacement> s_newLineOptionsEditorConfigMap =
        new(
        [
            KeyValuePair.Create("all", NewLineBeforeOpenBracePlacement.All),
            KeyValuePair.Create("accessors", NewLineBeforeOpenBracePlacement.Accessors),
            KeyValuePair.Create("types", NewLineBeforeOpenBracePlacement.Types),
            KeyValuePair.Create("methods", NewLineBeforeOpenBracePlacement.Methods),
            KeyValuePair.Create("properties", NewLineBeforeOpenBracePlacement.Properties),
            KeyValuePair.Create("anonymous_methods", NewLineBeforeOpenBracePlacement.AnonymousMethods),
            KeyValuePair.Create("control_blocks", NewLineBeforeOpenBracePlacement.ControlBlocks),
            KeyValuePair.Create("anonymous_types", NewLineBeforeOpenBracePlacement.AnonymousTypes),
            KeyValuePair.Create("object_collection_array_initializers", NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers),
            KeyValuePair.Create("lambdas", NewLineBeforeOpenBracePlacement.LambdaExpressionBody),
        ]);
    #endregion

    private static Option2<T> CreateOption<T>(OptionGroup group, string name, T defaultValue, EditorConfigValueSerializer<T>? serializer = null)
    {
        var option = new Option2<T>(name, defaultValue, group, LanguageNames.CSharp, isEditorConfigOption: true, serializer: serializer);
        s_editorConfigOptionsBuilder.Add(option);
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

    public static Option2<BinaryOperatorSpacingOptionsInternal> SpacingAroundBinaryOperator { get; } = CreateOption(
        CSharpFormattingOptionGroups.Spacing, "csharp_space_around_binary_operators",
        CSharpSyntaxFormattingOptions.Default.SpacingAroundBinaryOperator,
        new EditorConfigValueSerializer<BinaryOperatorSpacingOptionsInternal>(
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

    public static Option2<LabelPositionOptionsInternal> LabelPositioning { get; } = CreateOption(
        CSharpFormattingOptionGroups.Indentation, "csharp_indent_labels",
        CSharpSyntaxFormattingOptions.Default.LabelPositioning,
        new EditorConfigValueSerializer<LabelPositionOptionsInternal>(
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

    public static Option2<bool> WrapCallChains { get; } = CreateOption(
        CSharpFormattingOptionGroups.Wrapping, "csharp_wrap_call_chains",
        defaultValue: false);

    public static Option2<bool> IndentWrappedCallChains { get; } = CreateOption(
        CSharpFormattingOptionGroups.Wrapping, "csharp_indent_wrapped_call_chains",
        defaultValue: false);

    public static Option2<ParameterWrappingOptionsInternal> ParameterWrapping { get; } = CreateOption(
        CSharpFormattingOptionGroups.Wrapping, "csharp_parameter_wrapping",
        defaultValue: ParameterWrappingOptionsInternal.DoNotWrap,
        new EditorConfigValueSerializer<ParameterWrappingOptionsInternal>(
            s => ParseEditorConfigParameterWrapping(s),
            GetParameterWrappingOptionEditorConfigString));

    public static Option2<ParameterFirstPlacementOptionsInternal> ParameterFirstPlacement { get; } = CreateOption(
        CSharpFormattingOptionGroups.Wrapping, "csharp_parameter_first_placement",
        defaultValue: ParameterFirstPlacementOptionsInternal.SameLine,
        new EditorConfigValueSerializer<ParameterFirstPlacementOptionsInternal>(
            s => ParseEditorConfigParameterFirstPlacement(s),
            GetParameterFirstPlacementOptionEditorConfigString));

    public static Option2<ParameterAlignmentOptionsInternal> ParameterAlignment { get; } = CreateOption(
        CSharpFormattingOptionGroups.Wrapping, "csharp_parameter_alignment",
        defaultValue: ParameterAlignmentOptionsInternal.AlignWithFirst,
        new EditorConfigValueSerializer<ParameterAlignmentOptionsInternal>(
            s => ParseEditorConfigParameterAlignment(s),
            GetParameterAlignmentOptionEditorConfigString));

    public static Option2<BinaryExpressionWrappingOptionsInternal> BinaryExpressionWrapping { get; } = CreateOption(
        CSharpFormattingOptionGroups.Wrapping, "csharp_binary_expression_wrapping",
        defaultValue: BinaryExpressionWrappingOptionsInternal.DoNotWrap,
        new EditorConfigValueSerializer<BinaryExpressionWrappingOptionsInternal>(
            s => ParseEditorConfigBinaryExpressionWrapping(s),
            GetBinaryExpressionWrappingOptionEditorConfigString));

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

    /// <summary>
    /// Internal option -- not exposed to editorconfig tooling via <see cref="EditorConfigOptions"/>.
    /// </summary>
    public static readonly Option2<int> CollectionExpressionWrappingLength = new(
        $"csharp_unsupported_collection_expression_wrapping_length",
        defaultValue: CSharpSyntaxFormattingOptions.Default.CollectionExpressionWrappingLength,
        languageName: LanguageNames.CSharp,
        isEditorConfigOption: true);

    /// <summary>
    /// Options that we expect the user to set in editorconfig.
    /// </summary>
    internal static readonly ImmutableArray<IOption2> EditorConfigOptions = s_editorConfigOptionsBuilder.ToImmutable();

    /// <summary>
    /// Options that can be set via editorconfig but we do not provide tooling support.
    /// </summary>
    internal static readonly ImmutableArray<IOption2> UndocumentedOptions = [CollectionExpressionWrappingLength];

    private static ParameterWrappingOptionsInternal ParseEditorConfigParameterWrapping(string str)
        => str switch
        {
            "do_not_wrap" => ParameterWrappingOptionsInternal.DoNotWrap,
            "wrap_long_parameters" => ParameterWrappingOptionsInternal.WrapLongParameters,
            "wrap_every_parameter" => ParameterWrappingOptionsInternal.WrapEveryParameter,
            _ => ParameterWrappingOptionsInternal.DoNotWrap
        };

    private static string GetParameterWrappingOptionEditorConfigString(ParameterWrappingOptionsInternal value)
        => value switch
        {
            ParameterWrappingOptionsInternal.DoNotWrap => "do_not_wrap",
            ParameterWrappingOptionsInternal.WrapLongParameters => "wrap_long_parameters",
            ParameterWrappingOptionsInternal.WrapEveryParameter => "wrap_every_parameter",
            _ => "do_not_wrap"
        };

    private static ParameterFirstPlacementOptionsInternal ParseEditorConfigParameterFirstPlacement(string str)
        => str switch
        {
            "same_line" => ParameterFirstPlacementOptionsInternal.SameLine,
            "new_line" => ParameterFirstPlacementOptionsInternal.NewLine,
            _ => ParameterFirstPlacementOptionsInternal.SameLine
        };

    private static string GetParameterFirstPlacementOptionEditorConfigString(ParameterFirstPlacementOptionsInternal value)
        => value switch
        {
            ParameterFirstPlacementOptionsInternal.SameLine => "same_line",
            ParameterFirstPlacementOptionsInternal.NewLine => "new_line",
            _ => "same_line"
        };

    private static ParameterAlignmentOptionsInternal ParseEditorConfigParameterAlignment(string str)
        => str switch
        {
            "align_with_first" => ParameterAlignmentOptionsInternal.AlignWithFirst,
            "indent" => ParameterAlignmentOptionsInternal.Indent,
            _ => ParameterAlignmentOptionsInternal.AlignWithFirst
        };

    private static string GetParameterAlignmentOptionEditorConfigString(ParameterAlignmentOptionsInternal value)
        => value switch
        {
            ParameterAlignmentOptionsInternal.AlignWithFirst => "align_with_first",
            ParameterAlignmentOptionsInternal.Indent => "indent",
            _ => "align_with_first"
        };

    private static BinaryExpressionWrappingOptionsInternal ParseEditorConfigBinaryExpressionWrapping(string str)
        => str switch
        {
            "do_not_wrap" => BinaryExpressionWrappingOptionsInternal.DoNotWrap,
            "wrap_long_expressions" => BinaryExpressionWrappingOptionsInternal.WrapLongExpressions,
            "wrap_every_operator" => BinaryExpressionWrappingOptionsInternal.WrapEveryOperator,
            _ => BinaryExpressionWrappingOptionsInternal.DoNotWrap
        };

    private static string GetBinaryExpressionWrappingOptionEditorConfigString(BinaryExpressionWrappingOptionsInternal value)
        => value switch
        {
            BinaryExpressionWrappingOptionsInternal.DoNotWrap => "do_not_wrap",
            BinaryExpressionWrappingOptionsInternal.WrapLongExpressions => "wrap_long_expressions",
            BinaryExpressionWrappingOptionsInternal.WrapEveryOperator => "wrap_every_operator",
            _ => "do_not_wrap"
        };
}

internal enum LabelPositionOptionsInternal
{
    /// Placed in the Zeroth column of the text editor
    LeftMost = 0,

    /// Placed at one less indent to the current context
    OneLess = 1,

    /// Placed at the same indent as the current context
    NoIndent = 2
}

internal enum BinaryOperatorSpacingOptionsInternal
{
    /// Single Spacing
    Single = 0,

    /// Ignore Formatting
    Ignore = 1,

    /// Remove Spacing
    Remove = 2
}

internal enum ParameterWrappingOptionsInternal
{
    DoNotWrap,
    WrapLongParameters,
    WrapEveryParameter
}

internal enum ParameterFirstPlacementOptionsInternal
{
    SameLine,
    NewLine
}

internal enum ParameterAlignmentOptionsInternal
{
    AlignWithFirst,
    Indent
}

internal enum BinaryExpressionWrappingOptionsInternal
{
    DoNotWrap,
    WrapLongExpressions,
    WrapEveryOperator
}

internal static class CSharpFormattingOptionGroups
{
    public static readonly OptionGroup Indentation = new("csharp_indentation", CSharpCompilerExtensionsResources.Indentation_preferences, priority: 3, parent: FormattingOptionGroups.FormattingOptionGroup);
    public static readonly OptionGroup Spacing = new("csharp_spacing", CSharpCompilerExtensionsResources.Space_preferences, priority: 4, parent: FormattingOptionGroups.FormattingOptionGroup);
    public static readonly OptionGroup Wrapping = new("csharp_wrapping", CSharpCompilerExtensionsResources.Wrapping_preferences, priority: 5, parent: FormattingOptionGroups.FormattingOptionGroup);
}
