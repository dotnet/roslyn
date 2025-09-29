// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle;

internal static partial class CSharpCodeStyleOptions
{
    private static readonly ImmutableArray<IOption2>.Builder s_editorConfigOptionsBuilder = ImmutableArray.CreateBuilder<IOption2>();

    private static Option2<CodeStyleOption2<T>> CreateOption<T>(
        OptionGroup group,
        string name,
        CodeStyleOption2<T> defaultValue,
        Func<CodeStyleOption2<T>, EditorConfigValueSerializer<CodeStyleOption2<T>>>? serializerFactory = null)
        => s_editorConfigOptionsBuilder.CreateEditorConfigOption(name, defaultValue, group, LanguageNames.CSharp, serializerFactory);

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
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferSwitchExpression = CreateOption(
        CSharpCodeStyleOptionGroups.PatternMatching, "csharp_style_prefer_switch_expression",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferPatternMatching = CreateOption(
        CSharpCodeStyleOptionGroups.PatternMatching, "csharp_style_prefer_pattern_matching",
        defaultValue: CodeStyleOption2.TrueWithSilentEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferPatternMatchingOverAsWithNullCheck = CreateOption(
        CSharpCodeStyleOptionGroups.PatternMatching, "csharp_style_pattern_matching_over_as_with_null_check",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferPatternMatchingOverIsWithCastCheck = CreateOption(
        CSharpCodeStyleOptionGroups.PatternMatching, "csharp_style_pattern_matching_over_is_with_cast_check",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferNotPattern = CreateOption(
        CSharpCodeStyleOptionGroups.PatternMatching, "csharp_style_prefer_not_pattern",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferExtendedPropertyPattern = CreateOption(
        CSharpCodeStyleOptionGroups.PatternMatching, "csharp_style_prefer_extended_property_pattern",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferThrowExpression = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences, "csharp_style_throw_expression",
        CSharpSimplifierOptions.Default.PreferThrowExpression);

    public static readonly Option2<CodeStyleOption2<bool>> PreferInlinedVariableDeclaration = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences, "csharp_style_inlined_variable_declaration",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferDeconstructedVariableDeclaration = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences, "csharp_style_deconstructed_variable_declaration",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferIndexOperator = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences, "csharp_style_prefer_index_operator",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferRangeOperator = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences, "csharp_style_prefer_range_operator",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferUtf8StringLiterals = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences, "csharp_style_prefer_utf8_string_literals",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferUnboundGenericTypeInNameOf = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences, "csharp_style_prefer_unbound_generic_type_in_nameof",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

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

    public static readonly Option2<CodeStyleOption2<bool>> PreferImplicitlyTypedLambdaExpression = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "csharp_style_prefer_implicitly_typed_lambda_expression",
        CSharpSimplifierOptions.Default.PreferImplicitlyTypedLambdaExpression);

    private static readonly ImmutableArray<SyntaxKind> s_preferredModifierOrderDefault =
    [
        SyntaxKind.PublicKeyword,
        SyntaxKind.PrivateKeyword,
        SyntaxKind.ProtectedKeyword,
        SyntaxKind.InternalKeyword,
        SyntaxKind.FileKeyword,
        SyntaxKind.StaticKeyword,
        SyntaxKind.ExternKeyword,
        SyntaxKind.NewKeyword,
        SyntaxKind.VirtualKeyword,
        SyntaxKind.AbstractKeyword,
        SyntaxKind.SealedKeyword,
        SyntaxKind.OverrideKeyword,
        SyntaxKind.ReadOnlyKeyword,
        SyntaxKind.UnsafeKeyword,
        SyntaxKind.RequiredKeyword,
        SyntaxKind.VolatileKeyword,
        SyntaxKind.AsyncKeyword,
    ];

    public static readonly Option2<CodeStyleOption2<string>> PreferredModifierOrder = CreateOption(
        CodeStyleOptionGroups.Modifier,
        "csharp_preferred_modifier_order",
        defaultValue: new CodeStyleOption2<string>(string.Join(",", s_preferredModifierOrderDefault.Select(SyntaxFacts.GetText)), NotificationOption2.Silent));

    public static readonly Option2<CodeStyleOption2<bool>> PreferStaticLocalFunction = CreateOption(
        CodeStyleOptionGroups.Modifier,
        "csharp_prefer_static_local_function",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferStaticAnonymousFunction = CreateOption(
        CodeStyleOptionGroups.Modifier,
        "csharp_prefer_static_anonymous_function",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferReadOnlyStruct = CreateOption(
        CodeStyleOptionGroups.Modifier,
        "csharp_style_prefer_readonly_struct",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferReadOnlyStructMember = CreateOption(
        CodeStyleOptionGroups.Modifier,
        "csharp_style_prefer_readonly_struct_member",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferSimpleUsingStatement = CreateOption(
        CSharpCodeStyleOptionGroups.CodeBlockPreferences,
        "csharp_prefer_simple_using_statement",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferLocalOverAnonymousFunction = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "csharp_style_prefer_local_over_anonymous_function",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferSystemThreadingLock = CreateOption(
        CSharpCodeStyleOptionGroups.CodeBlockPreferences,
        "csharp_prefer_system_threading_lock",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferTupleSwap = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences, "csharp_style_prefer_tuple_swap",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

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
        defaultValue: new CodeStyleOption2<UnusedValuePreference>(UnusedValuePreference.DiscardVariable, NotificationOption2.Silent),
        serializerFactory: CodeStyleHelpers.GetUnusedValuePreferenceSerializer);

    internal static readonly Option2<CodeStyleOption2<UnusedValuePreference>> UnusedValueAssignment = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "csharp_style_unused_value_assignment_preference",
        defaultValue: new CodeStyleOption2<UnusedValuePreference>(UnusedValuePreference.DiscardVariable, NotificationOption2.Suggestion),
        serializerFactory: CodeStyleHelpers.GetUnusedValuePreferenceSerializer);

    public static readonly Option2<CodeStyleOption2<bool>> ImplicitObjectCreationWhenTypeIsApparent = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "csharp_style_implicit_object_creation_when_type_is_apparent",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    internal static readonly Option2<CodeStyleOption2<bool>> PreferNullCheckOverTypeCheck = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "csharp_style_prefer_null_check_over_type_check",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static Option2<CodeStyleOption2<bool>> AllowEmbeddedStatementsOnSameLine { get; } = CreateOption(
        CodeStyleOptionGroups.NewLinePreferences,
        "csharp_style_allow_embedded_statements_on_same_line_experimental",
        CSharpSimplifierOptions.Default.AllowEmbeddedStatementsOnSameLine);

    public static Option2<CodeStyleOption2<bool>> AllowBlankLinesBetweenConsecutiveBraces { get; } = CreateOption(
        CodeStyleOptionGroups.NewLinePreferences,
        "csharp_style_allow_blank_lines_between_consecutive_braces_experimental",
        defaultValue: CodeStyleOption2.TrueWithSilentEnforcement);

    public static Option2<CodeStyleOption2<bool>> AllowBlankLineAfterColonInConstructorInitializer { get; } = CreateOption(
        CodeStyleOptionGroups.NewLinePreferences,
        "csharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental",
        defaultValue: CodeStyleOption2.TrueWithSilentEnforcement);

    public static Option2<CodeStyleOption2<bool>> AllowBlankLineAfterTokenInConditionalExpression { get; } = CreateOption(
        CodeStyleOptionGroups.NewLinePreferences,
        "csharp_style_allow_blank_line_after_token_in_conditional_expression_experimental",
        defaultValue: CodeStyleOption2.TrueWithSilentEnforcement);

    public static Option2<CodeStyleOption2<bool>> AllowBlankLineAfterTokenInArrowExpressionClause { get; } = CreateOption(
        CodeStyleOptionGroups.NewLinePreferences,
        "csharp_style_allow_blank_line_after_token_in_arrow_expression_clause_experimental",
        defaultValue: CodeStyleOption2.TrueWithSilentEnforcement);

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
        defaultValue: CodeStyleOption2.TrueWithSilentEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferTopLevelStatements = CreateOption(
        CSharpCodeStyleOptionGroups.CodeBlockPreferences,
        "csharp_style_prefer_top_level_statements",
        CSharpSyntaxFormattingOptions.Default.PreferTopLevelStatements);

    public static readonly Option2<CodeStyleOption2<bool>> PreferPrimaryConstructors = CreateOption(
        CSharpCodeStyleOptionGroups.CodeBlockPreferences,
        "csharp_style_prefer_primary_constructors",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    public static readonly Option2<CodeStyleOption2<bool>> PreferSimplePropertyAccessors = CreateOption(
        CSharpCodeStyleOptionGroups.CodeBlockPreferences,
        "csharp_style_prefer_simple_property_accessors",
        defaultValue: CodeStyleOption2.TrueWithSuggestionEnforcement);

    /// <summary>
    /// Options that we expect the user to set in editorconfig.
    /// </summary>
    internal static readonly ImmutableArray<IOption2> EditorConfigOptions = s_editorConfigOptionsBuilder.ToImmutable();
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
