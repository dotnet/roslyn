// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.CodeStyle;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal static class CodeStyleOptions2
{
    private const string PublicFeatureName = "CodeStyleOptions";

    private static readonly ImmutableArray<IOption2>.Builder s_editorConfigOptionsBuilder = ImmutableArray.CreateBuilder<IOption2>();

    private static PerLanguageOption2<CodeStyleOption2<T>> CreatePerLanguageOption<T>(
        OptionGroup group, string name, CodeStyleOption2<T> defaultValue, Func<CodeStyleOption2<T>, EditorConfigValueSerializer<CodeStyleOption2<T>>>? serializerFactory = null)
        => s_editorConfigOptionsBuilder.CreatePerLanguageEditorConfigOption(name, defaultValue, group, serializerFactory);

    private static Option2<CodeStyleOption2<T>> CreateOption<T>(
        OptionGroup group, string name, CodeStyleOption2<T> defaultValue, Func<CodeStyleOption2<T>, EditorConfigValueSerializer<CodeStyleOption2<T>>>? serializerFactory = null)
        => s_editorConfigOptionsBuilder.CreateEditorConfigOption(name, defaultValue, group, languageName: null, serializerFactory);

    private static Option2<T> CreateOption<T>(
        OptionGroup group, string name, T defaultValue, EditorConfigValueSerializer<T>? serializer = null)
        => s_editorConfigOptionsBuilder.CreateEditorConfigOption(name, defaultValue, group, serializer);

    private static PerLanguageOption2<CodeStyleOption2<bool>> CreateQualifyAccessOption(string name)
        => CreatePerLanguageOption(CodeStyleOptionGroups.ThisOrMe, name, defaultValue: SimplifierOptions.DefaultQualifyAccess);

    /// <summary>
    /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in field access expressions.
    /// </summary>
    public static readonly PerLanguageOption2<CodeStyleOption2<bool>> QualifyFieldAccess = CreateQualifyAccessOption(
        "dotnet_style_qualification_for_field")
        .WithPublicOption(PublicFeatureName, "QualifyFieldAccess");

    /// <summary>
    /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in property access expressions.
    /// </summary>
    public static readonly PerLanguageOption2<CodeStyleOption2<bool>> QualifyPropertyAccess = CreateQualifyAccessOption(
        "dotnet_style_qualification_for_property")
        .WithPublicOption(PublicFeatureName, "QualifyPropertyAccess");

    /// <summary>
    /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in method access expressions.
    /// </summary>
    public static readonly PerLanguageOption2<CodeStyleOption2<bool>> QualifyMethodAccess = CreateQualifyAccessOption(
        "dotnet_style_qualification_for_method")
        .WithPublicOption(PublicFeatureName, "QualifyMethodAccess");

    /// <summary>
    /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in event access expressions.
    /// </summary>
    public static readonly PerLanguageOption2<CodeStyleOption2<bool>> QualifyEventAccess = CreateQualifyAccessOption(
        "dotnet_style_qualification_for_event")
        .WithPublicOption(PublicFeatureName, "QualifyEventAccess");

    /// <summary>
    /// This option says if we should prefer keyword for Intrinsic Predefined Types in Declarations
    /// </summary>
    public static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferIntrinsicPredefinedTypeKeywordInDeclaration = CreatePerLanguageOption(
        CodeStyleOptionGroups.PredefinedTypeNameUsage,
        defaultValue: SimplifierOptions.CommonDefaults.PreferPredefinedTypeKeywordInDeclaration,
        name: "dotnet_style_predefined_type_for_locals_parameters_members")
        .WithPublicOption(PublicFeatureName, "PreferIntrinsicPredefinedTypeKeywordInDeclaration");

    /// <summary>
    /// This option says if we should prefer keyword for Intrinsic Predefined Types in Member Access Expression
    /// </summary>
    public static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferIntrinsicPredefinedTypeKeywordInMemberAccess = CreatePerLanguageOption(
        CodeStyleOptionGroups.PredefinedTypeNameUsage,
        defaultValue: SimplifierOptions.CommonDefaults.PreferPredefinedTypeKeywordInMemberAccess,
        name: "dotnet_style_predefined_type_for_member_access")
        .WithPublicOption(PublicFeatureName, "PreferIntrinsicPredefinedTypeKeywordInMemberAccess");

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferObjectInitializer = CreatePerLanguageOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_object_initializer",
        IdeCodeStyleOptions.CommonDefaults.PreferObjectInitializer);

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferCollectionInitializer = CreatePerLanguageOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_collection_initializer",
        IdeCodeStyleOptions.CommonDefaults.PreferCollectionInitializer);

    internal static readonly PerLanguageOption2<CodeStyleOption2<CollectionExpressionPreference>> PreferCollectionExpression = CreatePerLanguageOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_prefer_collection_expression",
        IdeCodeStyleOptions.CommonDefaults.PreferCollectionExpression,
        defaultValue => new(
            parseValue: s => CollectionExpressionPreferenceUtilities.Parse(s, defaultValue),
            serializeValue: v => CollectionExpressionPreferenceUtilities.GetEditorConfigString(v, defaultValue)));

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferSimplifiedBooleanExpressions = CreatePerLanguageOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_prefer_simplified_boolean_expressions",
        IdeCodeStyleOptions.CommonDefaults.PreferSimplifiedBooleanExpressions);

    internal static readonly Option2<OperatorPlacementWhenWrappingPreference> OperatorPlacementWhenWrapping = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_operator_placement_when_wrapping",
        IdeCodeStyleOptions.CommonDefaults.OperatorPlacementWhenWrapping,
        serializer: new(
            parseValue: str => OperatorPlacementUtilities.Parse(str, IdeCodeStyleOptions.CommonDefaults.OperatorPlacementWhenWrapping),
            serializeValue: OperatorPlacementUtilities.GetEditorConfigString));

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferCoalesceExpression = CreatePerLanguageOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_coalesce_expression",
        IdeCodeStyleOptions.CommonDefaults.PreferCoalesceExpression);

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferNullPropagation = CreatePerLanguageOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_null_propagation",
        IdeCodeStyleOptions.CommonDefaults.PreferNullPropagation);

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferExplicitTupleNames = CreatePerLanguageOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_explicit_tuple_names",
        IdeCodeStyleOptions.CommonDefaults.PreferExplicitTupleNames);

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferAutoProperties = CreatePerLanguageOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_prefer_auto_properties",
        IdeCodeStyleOptions.CommonDefaults.PreferAutoProperties);

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferInferredTupleNames = CreatePerLanguageOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_prefer_inferred_tuple_names",
        IdeCodeStyleOptions.CommonDefaults.PreferInferredTupleNames);

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferInferredAnonymousTypeMemberNames = CreatePerLanguageOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_prefer_inferred_anonymous_type_member_names",
        IdeCodeStyleOptions.CommonDefaults.PreferInferredAnonymousTypeMemberNames);

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferIsNullCheckOverReferenceEqualityMethod = CreatePerLanguageOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_prefer_is_null_check_over_reference_equality_method",
        IdeCodeStyleOptions.CommonDefaults.PreferIsNullCheckOverReferenceEqualityMethod);

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferConditionalExpressionOverAssignment = CreatePerLanguageOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_prefer_conditional_expression_over_assignment",
        IdeCodeStyleOptions.CommonDefaults.PreferConditionalExpressionOverAssignment);

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferConditionalExpressionOverReturn = CreatePerLanguageOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_prefer_conditional_expression_over_return",
        IdeCodeStyleOptions.CommonDefaults.PreferConditionalExpressionOverReturn);

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferCompoundAssignment = CreatePerLanguageOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_prefer_compound_assignment",
        IdeCodeStyleOptions.CommonDefaults.PreferCompoundAssignment);

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferSimplifiedInterpolation = CreatePerLanguageOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_prefer_simplified_interpolation",
        IdeCodeStyleOptions.CommonDefaults.PreferSimplifiedInterpolation);

    private static readonly BidirectionalMap<string, UnusedParametersPreference> s_unusedParametersPreferenceMap =
        new(
        [
            KeyValuePairUtil.Create("non_public", UnusedParametersPreference.NonPublicMethods),
            KeyValuePairUtil.Create("all", UnusedParametersPreference.AllMethods),
        ]);

    internal static readonly PerLanguageOption2<CodeStyleOption2<UnusedParametersPreference>> UnusedParameters = CreatePerLanguageOption(
        CodeStyleOptionGroups.Parameter,
        "dotnet_code_quality_unused_parameters",
        IdeCodeStyleOptions.CommonDefaults.UnusedParameters,
        defaultValue => new(
            parseValue: str =>
            {
                if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(str, defaultValue.Notification, out var value, out var notification))
                {
                    return new CodeStyleOption2<UnusedParametersPreference>(s_unusedParametersPreferenceMap.GetValueOrDefault(value), notification);
                }

                return defaultValue;
            },
            serializeValue: option =>
            {
                Debug.Assert(s_unusedParametersPreferenceMap.ContainsValue(option.Value));
                var value = s_unusedParametersPreferenceMap.GetKeyOrDefault(option.Value) ?? s_unusedParametersPreferenceMap.GetKeyOrDefault(defaultValue.Value);
                return $"{value}{CodeStyleHelpers.GetEditorConfigStringNotificationPart(option, defaultValue)}";
            }));

    private static readonly BidirectionalMap<string, AccessibilityModifiersRequired> s_accessibilityModifiersRequiredMap =
        new(
        [
            KeyValuePairUtil.Create("never", CodeStyle.AccessibilityModifiersRequired.Never),
            KeyValuePairUtil.Create("always", CodeStyle.AccessibilityModifiersRequired.Always),
            KeyValuePairUtil.Create("for_non_interface_members", CodeStyle.AccessibilityModifiersRequired.ForNonInterfaceMembers),
            KeyValuePairUtil.Create("omit_if_default", CodeStyle.AccessibilityModifiersRequired.OmitIfDefault),
        ]);

    internal static readonly PerLanguageOption2<CodeStyleOption2<AccessibilityModifiersRequired>> AccessibilityModifiersRequired = CreatePerLanguageOption(
        CodeStyleOptionGroups.Modifier, "dotnet_style_require_accessibility_modifiers",
        IdeCodeStyleOptions.CommonDefaults.AccessibilityModifiersRequired,
        defaultValue => new(
            parseValue: str =>
            {
                if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(str, defaultValue.Notification, out var value, out var notificationOpt))
                {
                    Debug.Assert(s_accessibilityModifiersRequiredMap.ContainsKey(value));
                    return new CodeStyleOption2<AccessibilityModifiersRequired>(s_accessibilityModifiersRequiredMap.GetValueOrDefault(value), notificationOpt);
                }

                return defaultValue;
            },
            serializeValue: option =>
            {
                Debug.Assert(s_accessibilityModifiersRequiredMap.ContainsValue(option.Value));
                return $"{s_accessibilityModifiersRequiredMap.GetKeyOrDefault(option.Value)}{CodeStyleHelpers.GetEditorConfigStringNotificationPart(option, defaultValue)}";
            }));

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferReadonly = CreatePerLanguageOption(
        CodeStyleOptionGroups.Field,
        "dotnet_style_readonly_field",
        IdeCodeStyleOptions.CommonDefaults.PreferReadonly);

    internal static readonly Option2<string> FileHeaderTemplate = CreateOption(
        CodeStyleOptionGroups.Usings,
        "file_header_template",
        DocumentFormattingOptions.Default.FileHeaderTemplate,
        EditorConfigValueSerializer.String(emptyStringRepresentation: "unset"));

    internal static readonly Option2<string> RemoveUnnecessarySuppressionExclusions = CreateOption(
        CodeStyleOptionGroups.Suppressions,
        "dotnet_remove_unnecessary_suppression_exclusions",
        IdeCodeStyleOptions.CommonDefaults.RemoveUnnecessarySuppressionExclusions,
        EditorConfigValueSerializer.String(emptyStringRepresentation: "none"));

    private static readonly BidirectionalMap<string, ParenthesesPreference> s_parenthesesPreferenceMap =
        new(
        [
            KeyValuePairUtil.Create("always_for_clarity", ParenthesesPreference.AlwaysForClarity),
            KeyValuePairUtil.Create("never_if_unnecessary", ParenthesesPreference.NeverIfUnnecessary),
        ]);

    private static PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> CreateParenthesesOption(CodeStyleOption2<ParenthesesPreference> defaultValue, string name)
        => CreatePerLanguageOption(
            CodeStyleOptionGroups.Parentheses,
            name,
            defaultValue,
            defaultValue => new(
                parseValue: str =>
                {
                    if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(str, defaultValue.Notification, out var value, out var notification))
                    {
                        Debug.Assert(s_parenthesesPreferenceMap.ContainsKey(value));
                        return new CodeStyleOption2<ParenthesesPreference>(s_parenthesesPreferenceMap.GetValueOrDefault(value), notification);
                    }

                    return defaultValue;
                },
                serializeValue: option =>
                {
                    Debug.Assert(s_parenthesesPreferenceMap.ContainsValue(option.Value));
                    var value = s_parenthesesPreferenceMap.GetKeyOrDefault(option.Value) ?? s_parenthesesPreferenceMap.GetKeyOrDefault(ParenthesesPreference.AlwaysForClarity);
                    return $"{value}{CodeStyleHelpers.GetEditorConfigStringNotificationPart(option, defaultValue)}";
                }));

    internal static readonly PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> ArithmeticBinaryParentheses =
        CreateParenthesesOption(
            IdeCodeStyleOptions.CommonDefaults.ArithmeticBinaryParentheses,
            "dotnet_style_parentheses_in_arithmetic_binary_operators");

    internal static readonly PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> OtherBinaryParentheses =
        CreateParenthesesOption(
            IdeCodeStyleOptions.CommonDefaults.OtherBinaryParentheses,
            "dotnet_style_parentheses_in_other_binary_operators");

    internal static readonly PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> RelationalBinaryParentheses =
        CreateParenthesesOption(
            IdeCodeStyleOptions.CommonDefaults.RelationalBinaryParentheses,
            "dotnet_style_parentheses_in_relational_binary_operators");

    internal static readonly PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> OtherParentheses =
        CreateParenthesesOption(
            IdeCodeStyleOptions.CommonDefaults.OtherParentheses,
            "dotnet_style_parentheses_in_other_operators");

    private static readonly BidirectionalMap<string, ForEachExplicitCastInSourcePreference> s_forEachExplicitCastInSourcePreferencePreferenceMap =
        new(
        [
            KeyValuePairUtil.Create("always", ForEachExplicitCastInSourcePreference.Always),
            KeyValuePairUtil.Create("when_strongly_typed", ForEachExplicitCastInSourcePreference.WhenStronglyTyped),
        ]);

    internal static readonly Option2<CodeStyleOption2<ForEachExplicitCastInSourcePreference>> ForEachExplicitCastInSource = CreateOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_prefer_foreach_explicit_cast_in_source",
        IdeCodeStyleOptions.CommonDefaults.ForEachExplicitCastInSource,
        defaultValue => new(
            parseValue: str =>
            {
                if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(str, defaultValue.Notification, out var value, out var notification))
                {
                    Debug.Assert(s_forEachExplicitCastInSourcePreferencePreferenceMap.ContainsKey(value));
                    return new CodeStyleOption2<ForEachExplicitCastInSourcePreference>(
                        s_forEachExplicitCastInSourcePreferencePreferenceMap.GetValueOrDefault(value), notification);
                }

                return defaultValue;
            },
            serializeValue: option =>
            {
                Debug.Assert(s_forEachExplicitCastInSourcePreferencePreferenceMap.ContainsValue(option.Value));
                var value = s_forEachExplicitCastInSourcePreferencePreferenceMap.GetKeyOrDefault(option.Value) ??
                    s_forEachExplicitCastInSourcePreferencePreferenceMap.GetKeyOrDefault(defaultValue.Value);
                return $"{value}{CodeStyleHelpers.GetEditorConfigStringNotificationPart(option, defaultValue)}";
            }));

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferSystemHashCode = new(
        "dotnet_prefer_system_hash_code",
        IdeAnalyzerOptions.CommonDefault.PreferSystemHashCode,
        group: CodeStyleOptionGroups.ExpressionLevelPreferences);

    public static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferNamespaceAndFolderMatchStructure = CreatePerLanguageOption(
        CodeStyleOptionGroups.ExpressionLevelPreferences,
        "dotnet_style_namespace_match_folder",
        IdeCodeStyleOptions.CommonDefaults.PreferNamespaceAndFolderMatchStructure);

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> AllowMultipleBlankLines = CreatePerLanguageOption(
        CodeStyleOptionGroups.NewLinePreferences,
        "dotnet_style_allow_multiple_blank_lines_experimental",
        IdeCodeStyleOptions.CommonDefaults.AllowMultipleBlankLines);

    internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> AllowStatementImmediatelyAfterBlock = CreatePerLanguageOption(
        CodeStyleOptionGroups.NewLinePreferences,
        "dotnet_style_allow_statement_immediately_after_block_experimental",
        IdeCodeStyleOptions.CommonDefaults.AllowStatementImmediatelyAfterBlock);

    /// <summary>
    /// Options that we expect the user to set in editorconfig.
    /// </summary>
    internal static readonly ImmutableArray<IOption2> EditorConfigOptions = s_editorConfigOptionsBuilder.ToImmutable();
}

internal static class CodeStyleOptionGroups
{
    public static readonly OptionGroup CodeStyle = new(name: "code_style", description: "", priority: 1);

    public static readonly OptionGroup Usings = new("usings", description: CompilerExtensionsResources.Organize_usings, priority: 1, parent: CodeStyle);
    public static readonly OptionGroup ThisOrMe = new("this_or_me", description: CompilerExtensionsResources.this_dot_and_Me_dot_preferences, priority: 2, parent: CodeStyle);
    public static readonly OptionGroup PredefinedTypeNameUsage = new("predefined_type_name_usage", description: CompilerExtensionsResources.Language_keywords_vs_BCL_types_preferences, priority: 3, parent: CodeStyle);
    public static readonly OptionGroup Parentheses = new("parentheses", description: CompilerExtensionsResources.Parentheses_preferences, priority: 4, parent: CodeStyle);
    public static readonly OptionGroup Modifier = new("modifier", description: CompilerExtensionsResources.Modifier_preferences, priority: 5, parent: CodeStyle);
    public static readonly OptionGroup ExpressionLevelPreferences = new("expression_level_preferences", description: CompilerExtensionsResources.Expression_level_preferences, priority: 7, parent: CodeStyle);
    public static readonly OptionGroup Field = new("field", description: CompilerExtensionsResources.Field_preferences, priority: 8, parent: CodeStyle);
    public static readonly OptionGroup Parameter = new("parameter", description: CompilerExtensionsResources.Parameter_preferences, priority: 9, parent: CodeStyle);
    public static readonly OptionGroup Suppressions = new("suppressions", description: CompilerExtensionsResources.Suppression_preferences, priority: 10, parent: CodeStyle);
    public static readonly OptionGroup NewLinePreferences = new("new_line_preferences", description: CompilerExtensionsResources.New_line_preferences, priority: 11, parent: CodeStyle);
}
