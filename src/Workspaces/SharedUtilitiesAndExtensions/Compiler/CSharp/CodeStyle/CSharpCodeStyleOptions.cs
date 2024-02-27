// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle;

internal static partial class CSharpCodeStyleOptions
{
    private static readonly ImmutableArray<IOption2>.Builder s_allOptionsBuilder = ImmutableArray.CreateBuilder<IOption2>();

    private static Option2<CodeStyleOption2<T>> CreateOption<T>(
        OptionGroup group,
        string name,
        CodeStyleOption2<T> defaultValue,
        Func<CodeStyleOption2<T>, EditorConfigValueSerializer<CodeStyleOption2<T>>>? serializerFactory = null)
        => s_allOptionsBuilder.CreateEditorConfigOption(name, defaultValue, group, LanguageNames.CSharp, serializerFactory);

    public static readonly Option2<CodeStyleOption2<bool>> VarForBuiltInTypes = CreateOption(
        CSharpCodeStyleOptionGroups.VarPreferences, "csharp_style_var_for_built_in_types",
        CSharpSimplifierOptions.Default.VarForBuiltInTypes);

    public static readonly Option2<CodeStyleOption2<bool>> VarWhenTypeIsApparent = CreateOption(
        CSharpCodeStyleOptionGroups.VarPreferences, "csharp_style_var_when_type_is_apparent",
        CSharpSimplifierOptions.Default.VarWhenTypeIsApparent);

    public static readonly Option2<CodeStyleOption2<bool>> VarElsewhere = CreateOption(
        CSharpCodeStyleOptionGroups.VarPreferences, "csharp_style_var_elsewhere",
        CSharpSimplifierOptions.Default.VarElsewhere);

    public static readonly Option2<CodeStyleOption2<bool>> PreferConditionalDelegateCall = CreateOption(
        CSharpCodeStyleOptionGroups.NullCheckingPreferences, "csharp_style_conditional_delegate_call",
        CSharpIdeCodeStyleOptions.Default.PreferConditionalDelegateCall);

    public static readonly Option2<CodeStyleOption2<bool>> PreferSwitchExpression = CreateOption(
        CSharpCodeStyleOptionGroups.PatternMatching, "csharp_style_prefer_switch_expression",
        CSharpIdeCodeStyleOptions.Default.PreferSwitchExpression);

    public static readonly Option2<CodeStyleOption2<bool>> PreferPatternMatching = CreateOption(
        CSharpCodeStyleOptionGroups.PatternMatching, "csharp_style_prefer_pattern_matching",
        CSharpIdeCodeStyleOptions.Default.PreferPatternMatching);

    public static readonly Option2<CodeStyleOption2<bool>> PreferPatternMatchingOverAsWithNullCheck = CreateOption(
        CSharpCodeStyleOptionGroups.PatternMatching, "csharp_style_pattern_matching_over_as_with_null_check",
        CSharpIdeCodeStyleOptions.Default.PreferPatternMatchingOverAsWithNullCheck);

    public static readonly Option2<CodeStyleOption2<bool>> PreferPatternMatchingOverIsWithCastCheck = CreateOption(
        CSharpCodeStyleOptionGroups.PatternMatching, "csharp_style_pattern_matching_over_is_with_cast_check",
        CSharpIdeCodeStyleOptions.Default.PreferPatternMatchingOverIsWithCastCheck);

    public static readonly Option2<CodeStyleOption2<bool>> PreferNotPattern = CreateOption(
        CSharpCodeStyleOptionGroups.PatternMatching, "csharp_style_prefer_not_pattern",
        CSharpIdeCodeStyleOptions.Default.PreferNotPattern);

    public static readonly Option2<CodeStyleOption2<bool>> PreferExtendedPropertyPattern = CreateOption(
        CSharpCodeStyleOptionGroups.PatternMatching, "csharp_style_prefer_extended_property_pattern",
        CSharpIdeCodeStyleOptions.Default.PreferExtendedPropertyPattern);

    public static readonly Option2<CodeStyleOption2<bool>> PreferThrowExpression = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences, "csharp_style_throw_expression",
        CSharpSimplifierOptions.Default.PreferThrowExpression);

    public static readonly Option2<CodeStyleOption2<bool>> PreferInlinedVariableDeclaration = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences, "csharp_style_inlined_variable_declaration",
        CSharpIdeCodeStyleOptions.Default.PreferInlinedVariableDeclaration);

    public static readonly Option2<CodeStyleOption2<bool>> PreferDeconstructedVariableDeclaration = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences, "csharp_style_deconstructed_variable_declaration",
        CSharpIdeCodeStyleOptions.Default.PreferDeconstructedVariableDeclaration);

    public static readonly Option2<CodeStyleOption2<bool>> PreferIndexOperator = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences, "csharp_style_prefer_index_operator",
        CSharpIdeCodeStyleOptions.Default.PreferIndexOperator);

    public static readonly Option2<CodeStyleOption2<bool>> PreferRangeOperator = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences, "csharp_style_prefer_range_operator",
        CSharpIdeCodeStyleOptions.Default.PreferRangeOperator);

    public static readonly Option2<CodeStyleOption2<bool>> PreferUtf8StringLiterals = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences, "csharp_style_prefer_utf8_string_literals",
        CSharpIdeCodeStyleOptions.Default.PreferUtf8StringLiterals);

    public static readonly CodeStyleOption2<ExpressionBodyPreference> NeverWithSilentEnforcement =
        new(ExpressionBodyPreference.Never, NotificationOption2.Silent);

    public static readonly CodeStyleOption2<ExpressionBodyPreference> NeverWithSuggestionEnforcement =
        new(ExpressionBodyPreference.Never, NotificationOption2.Suggestion);

    public static readonly CodeStyleOption2<ExpressionBodyPreference> WhenPossibleWithSilentEnforcement =
        new(ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent);

    public static readonly CodeStyleOption2<ExpressionBodyPreference> WhenPossibleWithSuggestionEnforcement =
        new(ExpressionBodyPreference.WhenPossible, NotificationOption2.Suggestion);

    public static readonly CodeStyleOption2<ExpressionBodyPreference> WhenOnSingleLineWithSilentEnforcement =
        new(ExpressionBodyPreference.WhenOnSingleLine, NotificationOption2.Silent);

    private static Option2<CodeStyleOption2<ExpressionBodyPreference>> CreatePreferExpressionBodyOption(CodeStyleOption2<ExpressionBodyPreference> defaultValue, string name)
        => CreateOption(CSharpCodeStyleOptionGroups.ExpressionBodiedMembers, name, defaultValue, defaultValue =>
            new(parseValue: str => ParseExpressionBodyPreference(str, defaultValue),
                serializeValue: value => GetExpressionBodyPreferenceEditorConfigString(value, defaultValue)));

    public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedConstructors = CreatePreferExpressionBodyOption(
        defaultValue: NeverWithSilentEnforcement, name: "csharp_style_expression_bodied_constructors");

    public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedMethods = CreatePreferExpressionBodyOption(
        defaultValue: NeverWithSilentEnforcement, name: "csharp_style_expression_bodied_methods");

    public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedOperators = CreatePreferExpressionBodyOption(
        defaultValue: NeverWithSilentEnforcement, name: "csharp_style_expression_bodied_operators");

    public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedProperties = CreatePreferExpressionBodyOption(
        defaultValue: WhenPossibleWithSilentEnforcement, name: "csharp_style_expression_bodied_properties");

    public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedIndexers = CreatePreferExpressionBodyOption(
        defaultValue: WhenPossibleWithSilentEnforcement, name: "csharp_style_expression_bodied_indexers");

    public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedAccessors = CreatePreferExpressionBodyOption(
        defaultValue: WhenPossibleWithSilentEnforcement, name: "csharp_style_expression_bodied_accessors");

    public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedLambdas = CreatePreferExpressionBodyOption(
        defaultValue: WhenPossibleWithSilentEnforcement, name: "csharp_style_expression_bodied_lambdas");

    public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedLocalFunctions = CreatePreferExpressionBodyOption(
        defaultValue: NeverWithSilentEnforcement, name: "csharp_style_expression_bodied_local_functions");

    public static readonly Option2<CodeStyleOption2<PreferBracesPreference>> PreferBraces = CreateOption(
        CSharpCodeStyleOptionGroups.CodeBlockPreferences,
        "csharp_prefer_braces",
        CSharpSimplifierOptions.Default.PreferBraces,
        defaultValue => new(
            parseValue: str => ParsePreferBracesPreference(str, defaultValue),
            serializeValue: value => GetPreferBracesPreferenceEditorConfigString(value, defaultValue)));

    public static readonly Option2<CodeStyleOption2<bool>> PreferSimpleDefaultExpression = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "csharp_prefer_simple_default_expression",
        CSharpSimplifierOptions.Default.PreferSimpleDefaultExpression);

    public static readonly Option2<CodeStyleOption2<string>> PreferredModifierOrder = CreateOption(
        CodeStyleOptionGroups.Modifier,
        "csharp_preferred_modifier_order",
        CSharpIdeCodeStyleOptions.Default.PreferredModifierOrder);

    public static readonly Option2<CodeStyleOption2<bool>> PreferStaticLocalFunction = CreateOption(
        CodeStyleOptionGroups.Modifier,
        "csharp_prefer_static_local_function",
        CSharpIdeCodeStyleOptions.Default.PreferStaticLocalFunction);

    public static readonly Option2<CodeStyleOption2<bool>> PreferReadOnlyStruct = CreateOption(
        CodeStyleOptionGroups.Modifier,
        "csharp_style_prefer_readonly_struct",
        CSharpIdeCodeStyleOptions.Default.PreferReadOnlyStruct);

    public static readonly Option2<CodeStyleOption2<bool>> PreferReadOnlyStructMember = CreateOption(
        CodeStyleOptionGroups.Modifier,
        "csharp_style_prefer_readonly_struct_member",
        CSharpIdeCodeStyleOptions.Default.PreferReadOnlyStructMember);

    public static readonly Option2<CodeStyleOption2<bool>> PreferSimpleUsingStatement = CreateOption(
        CSharpCodeStyleOptionGroups.CodeBlockPreferences,
        "csharp_prefer_simple_using_statement",
        CSharpIdeCodeStyleOptions.Default.PreferSimpleUsingStatement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferLocalOverAnonymousFunction = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "csharp_style_prefer_local_over_anonymous_function",
        CSharpIdeCodeStyleOptions.Default.PreferLocalOverAnonymousFunction);

    public static readonly Option2<CodeStyleOption2<bool>> PreferTupleSwap = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences, "csharp_style_prefer_tuple_swap",
        CSharpIdeCodeStyleOptions.Default.PreferTupleSwap);

    public static readonly Option2<CodeStyleOption2<AddImportPlacement>> PreferredUsingDirectivePlacement = CreateOption(
        CSharpCodeStyleOptionGroups.UsingDirectivePreferences,
        "csharp_using_directive_placement",
        AddImportPlacementOptions.Default.UsingDirectivePlacement,
        defaultValue => new(
            parseValue: str => ParseUsingDirectivesPlacement(str, defaultValue),
            serializeValue: value => GetUsingDirectivesPlacementEditorConfigString(value, defaultValue)));

    internal static readonly Option2<CodeStyleOption2<UnusedValuePreference>> UnusedValueExpressionStatement = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "csharp_style_unused_value_expression_statement_preference",
        CSharpIdeCodeStyleOptions.Default.UnusedValueExpressionStatement,
        serializerFactory: CodeStyleHelpers.GetUnusedValuePreferenceSerializer);

    internal static readonly Option2<CodeStyleOption2<UnusedValuePreference>> UnusedValueAssignment = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "csharp_style_unused_value_assignment_preference",
        CSharpIdeCodeStyleOptions.Default.UnusedValueAssignment,
        serializerFactory: CodeStyleHelpers.GetUnusedValuePreferenceSerializer);

    public static readonly Option2<CodeStyleOption2<bool>> ImplicitObjectCreationWhenTypeIsApparent = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "csharp_style_implicit_object_creation_when_type_is_apparent",
        CSharpIdeCodeStyleOptions.Default.ImplicitObjectCreationWhenTypeIsApparent);

    internal static readonly Option2<CodeStyleOption2<bool>> PreferNullCheckOverTypeCheck = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "csharp_style_prefer_null_check_over_type_check",
        CSharpIdeCodeStyleOptions.Default.PreferNullCheckOverTypeCheck);

    public static Option2<CodeStyleOption2<bool>> AllowEmbeddedStatementsOnSameLine { get; } = CreateOption(
        CodeStyleOptionGroups.NewLinePreferences,
        "csharp_style_allow_embedded_statements_on_same_line_experimental",
         CSharpSimplifierOptions.Default.AllowEmbeddedStatementsOnSameLine);

    public static Option2<CodeStyleOption2<bool>> AllowBlankLinesBetweenConsecutiveBraces { get; } = CreateOption(
        CodeStyleOptionGroups.NewLinePreferences,
        "csharp_style_allow_blank_lines_between_consecutive_braces_experimental",
        CSharpIdeCodeStyleOptions.Default.AllowBlankLinesBetweenConsecutiveBraces);

    public static Option2<CodeStyleOption2<bool>> AllowBlankLineAfterColonInConstructorInitializer { get; } = CreateOption(
        CodeStyleOptionGroups.NewLinePreferences,
        "csharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental",
        CSharpIdeCodeStyleOptions.Default.AllowBlankLineAfterColonInConstructorInitializer);

    public static Option2<CodeStyleOption2<bool>> AllowBlankLineAfterTokenInConditionalExpression { get; } = CreateOption(
        CodeStyleOptionGroups.NewLinePreferences,
        "csharp_style_allow_blank_line_after_token_in_conditional_expression_experimental",
        CSharpIdeCodeStyleOptions.Default.AllowBlankLineAfterTokenInConditionalExpression);

    public static Option2<CodeStyleOption2<bool>> AllowBlankLineAfterTokenInArrowExpressionClause { get; } = CreateOption(
        CodeStyleOptionGroups.NewLinePreferences,
        "csharp_style_allow_blank_line_after_token_in_arrow_expression_clause_experimental",
        CSharpIdeCodeStyleOptions.Default.AllowBlankLineAfterTokenInArrowExpressionClause);

    public static Option2<CodeStyleOption2<NamespaceDeclarationPreference>> NamespaceDeclarations { get; } = CreateOption(
        CSharpCodeStyleOptionGroups.CodeBlockPreferences,
        "csharp_style_namespace_declarations",
        CSharpSyntaxFormattingOptions.Default.NamespaceDeclarations,
        defaultValue => new(
            parseValue: str => ParseNamespaceDeclaration(str, defaultValue),
            serializeValue: value => GetNamespaceDeclarationEditorConfigString(value, defaultValue)));

    public static readonly Option2<CodeStyleOption2<bool>> PreferMethodGroupConversion = CreateOption(
        CSharpCodeStyleOptionGroups.CodeBlockPreferences,
        "csharp_style_prefer_method_group_conversion",
        CSharpIdeCodeStyleOptions.Default.PreferMethodGroupConversion);

    public static readonly Option2<CodeStyleOption2<bool>> PreferTopLevelStatements = CreateOption(
        CSharpCodeStyleOptionGroups.CodeBlockPreferences,
        "csharp_style_prefer_top_level_statements",
        CSharpSyntaxFormattingOptions.Default.PreferTopLevelStatements);

    public static readonly Option2<CodeStyleOption2<bool>> PreferPrimaryConstructors = CreateOption(
        CSharpCodeStyleOptionGroups.CodeBlockPreferences,
        "csharp_style_prefer_primary_constructors",
        CSharpIdeCodeStyleOptions.Default.PreferPrimaryConstructors);

    internal static readonly ImmutableArray<IOption2> AllOptions = s_allOptionsBuilder.ToImmutable();
}

internal static class CSharpCodeStyleOptionGroups
{
    public static readonly OptionGroup VarPreferences = new("csharp_var_keyword_usage", CSharpCompilerExtensionsResources.var_preferences, priority: 1, parent: CodeStyleOptionGroups.CodeStyle);
    public static readonly OptionGroup ExpressionBodiedMembers = new("csharp_expression_bodied_members", CSharpCompilerExtensionsResources.Expression_bodied_members, priority: 2, parent: CodeStyleOptionGroups.CodeStyle);
    public static readonly OptionGroup PatternMatching = new("csharp_pattern_matching", CSharpCompilerExtensionsResources.Pattern_matching_preferences, priority: 3, parent: CodeStyleOptionGroups.CodeStyle);
    public static readonly OptionGroup NullCheckingPreferences = new("csharp_null_checks", CSharpCompilerExtensionsResources.Null_checking_preferences, priority: 4, parent: CodeStyleOptionGroups.CodeStyle);
    public static readonly OptionGroup CodeBlockPreferences = new("csharp_code_blocks", CSharpCompilerExtensionsResources.Code_block_preferences, priority: 6, parent: CodeStyleOptionGroups.CodeStyle);
    public static readonly OptionGroup UsingDirectivePreferences = new("csharp_using_directives", CSharpCompilerExtensionsResources.using_directive_preferences, priority: 8, parent: CodeStyleOptionGroups.CodeStyle);
}
