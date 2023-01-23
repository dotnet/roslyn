// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal static class CodeStyleOptions2
    {
        private const string PublicFeatureName = "CodeStyleOptions";

        private static readonly ImmutableArray<IOption2>.Builder s_allOptionsBuilder = ImmutableArray.CreateBuilder<IOption2>();

        private static PerLanguageOption2<CodeStyleOption2<T>> CreatePerLanguageOption<T>(
            OptionGroup group, string name, CodeStyleOption2<T> defaultValue, Func<CodeStyleOption2<T>, EditorConfigValueSerializer<CodeStyleOption2<T>>>? serializerFactory = null)
            => s_allOptionsBuilder.CreatePerLanguageEditorConfigOption(name, defaultValue, group, serializerFactory);

        private static Option2<CodeStyleOption2<T>> CreateOption<T>(
            OptionGroup group, string name, CodeStyleOption2<T> defaultValue, Func<CodeStyleOption2<T>, EditorConfigValueSerializer<CodeStyleOption2<T>>>? serializerFactory = null)
            => s_allOptionsBuilder.CreateEditorConfigOption(name, defaultValue, group, languageName: null, serializerFactory);

        private static Option2<T> CreateOption<T>(
            OptionGroup group, string name, T defaultValue, EditorConfigValueSerializer<T>? serializer = null)
            => s_allOptionsBuilder.CreateEditorConfigOption(name, defaultValue, group, serializer);

        /// <remarks>
        /// When user preferences are not yet set for a style, we fall back to the default value.
        /// One such default(s), is that the feature is turned on, so that codegen consumes it,
        /// but with silent enforcement, so that the user is not prompted about their usage.
        /// </remarks>
        internal static readonly CodeStyleOption2<bool> TrueWithSilentEnforcement = new(value: true, notification: NotificationOption2.Silent);
        internal static readonly CodeStyleOption2<bool> FalseWithSilentEnforcement = new(value: false, notification: NotificationOption2.Silent);
        internal static readonly CodeStyleOption2<bool> TrueWithSuggestionEnforcement = new(value: true, notification: NotificationOption2.Suggestion);
        internal static readonly CodeStyleOption2<bool> FalseWithSuggestionEnforcement = new(value: false, notification: NotificationOption2.Suggestion);

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
            defaultValue: SimplifierOptions.DefaultPreferPredefinedTypeKeyword,
            name: "dotnet_style_predefined_type_for_locals_parameters_members")
            .WithPublicOption(PublicFeatureName, "PreferIntrinsicPredefinedTypeKeywordInDeclaration");

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Member Access Expression
        /// </summary>
        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferIntrinsicPredefinedTypeKeywordInMemberAccess = CreatePerLanguageOption(
            CodeStyleOptionGroups.PredefinedTypeNameUsage,
            defaultValue: SimplifierOptions.DefaultPreferPredefinedTypeKeyword,
            name: "dotnet_style_predefined_type_for_member_access")
            .WithPublicOption(PublicFeatureName, "PreferIntrinsicPredefinedTypeKeywordInMemberAccess");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferObjectInitializer = CreatePerLanguageOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_object_initializer",
            IdeCodeStyleOptions.CommonOptions.Default.PreferObjectInitializer);

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferCollectionInitializer = CreatePerLanguageOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_collection_initializer",
            IdeCodeStyleOptions.CommonOptions.Default.PreferCollectionInitializer);

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferSimplifiedBooleanExpressions = CreatePerLanguageOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_prefer_simplified_boolean_expressions",
            IdeCodeStyleOptions.CommonOptions.Default.PreferSimplifiedBooleanExpressions);

        internal static readonly Option2<OperatorPlacementWhenWrappingPreference> OperatorPlacementWhenWrapping = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_operator_placement_when_wrapping",
            IdeCodeStyleOptions.CommonOptions.Default.OperatorPlacementWhenWrapping,
            serializer: new(
                parseValue: str => OperatorPlacementUtilities.Parse(str, IdeCodeStyleOptions.CommonOptions.Default.OperatorPlacementWhenWrapping),
                serializeValue: OperatorPlacementUtilities.GetEditorConfigString));

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferCoalesceExpression = CreatePerLanguageOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_coalesce_expression",
            IdeCodeStyleOptions.CommonOptions.Default.PreferCoalesceExpression);

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferNullPropagation = CreatePerLanguageOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_null_propagation",
            IdeCodeStyleOptions.CommonOptions.Default.PreferNullPropagation);

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferExplicitTupleNames = CreatePerLanguageOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_explicit_tuple_names",
            IdeCodeStyleOptions.CommonOptions.Default.PreferExplicitTupleNames);

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferAutoProperties = CreatePerLanguageOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_prefer_auto_properties",
            IdeCodeStyleOptions.CommonOptions.Default.PreferAutoProperties);

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferInferredTupleNames = CreatePerLanguageOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_prefer_inferred_tuple_names",
            IdeCodeStyleOptions.CommonOptions.Default.PreferInferredTupleNames);

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferInferredAnonymousTypeMemberNames = CreatePerLanguageOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_prefer_inferred_anonymous_type_member_names",
            IdeCodeStyleOptions.CommonOptions.Default.PreferInferredAnonymousTypeMemberNames);

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferIsNullCheckOverReferenceEqualityMethod = CreatePerLanguageOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_prefer_is_null_check_over_reference_equality_method",
            IdeCodeStyleOptions.CommonOptions.Default.PreferIsNullCheckOverReferenceEqualityMethod);

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferConditionalExpressionOverAssignment = CreatePerLanguageOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_prefer_conditional_expression_over_assignment",
            IdeCodeStyleOptions.CommonOptions.Default.PreferConditionalExpressionOverAssignment);

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferConditionalExpressionOverReturn = CreatePerLanguageOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_prefer_conditional_expression_over_return",
            IdeCodeStyleOptions.CommonOptions.Default.PreferConditionalExpressionOverReturn);

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferCompoundAssignment = CreatePerLanguageOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_prefer_compound_assignment",
            IdeCodeStyleOptions.CommonOptions.Default.PreferCompoundAssignment);

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferSimplifiedInterpolation = CreatePerLanguageOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_prefer_simplified_interpolation",
            IdeCodeStyleOptions.CommonOptions.Default.PreferSimplifiedInterpolation);

        private static readonly BidirectionalMap<string, UnusedParametersPreference> s_unusedParametersPreferenceMap =
            new(new[]
            {
                KeyValuePairUtil.Create("non_public", UnusedParametersPreference.NonPublicMethods),
                KeyValuePairUtil.Create("all", UnusedParametersPreference.AllMethods),
            });

        internal static readonly PerLanguageOption2<CodeStyleOption2<UnusedParametersPreference>> UnusedParameters = CreatePerLanguageOption(
            CodeStyleOptionGroups.Parameter,
            "dotnet_code_quality_unused_parameters",
            IdeCodeStyleOptions.CommonOptions.Default.UnusedParameters,
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
            new(new[]
            {
                KeyValuePairUtil.Create("never", CodeStyle.AccessibilityModifiersRequired.Never),
                KeyValuePairUtil.Create("always", CodeStyle.AccessibilityModifiersRequired.Always),
                KeyValuePairUtil.Create("for_non_interface_members", CodeStyle.AccessibilityModifiersRequired.ForNonInterfaceMembers),
                KeyValuePairUtil.Create("omit_if_default", CodeStyle.AccessibilityModifiersRequired.OmitIfDefault),
            });

        internal static readonly PerLanguageOption2<CodeStyleOption2<AccessibilityModifiersRequired>> AccessibilityModifiersRequired = CreatePerLanguageOption(
            CodeStyleOptionGroups.Modifier, "dotnet_style_require_accessibility_modifiers",
            IdeCodeStyleOptions.CommonOptions.Default.AccessibilityModifiersRequired,
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
            IdeCodeStyleOptions.CommonOptions.Default.PreferReadonly);

        internal static readonly Option2<string> FileHeaderTemplate = CreateOption(
            CodeStyleOptionGroups.Usings,
            "file_header_template",
            DocumentFormattingOptions.Default.FileHeaderTemplate,
            EditorConfigValueSerializer.String(emptyStringRepresentation: "unset"));

        internal static readonly Option2<string> RemoveUnnecessarySuppressionExclusions = CreateOption(
            CodeStyleOptionGroups.Suppressions,
            "dotnet_remove_unnecessary_suppression_exclusions",
            IdeCodeStyleOptions.CommonOptions.Default.RemoveUnnecessarySuppressionExclusions,
            EditorConfigValueSerializer.String(emptyStringRepresentation: "none"));

        private static readonly BidirectionalMap<string, ParenthesesPreference> s_parenthesesPreferenceMap =
            new(new[]
            {
                KeyValuePairUtil.Create("always_for_clarity", ParenthesesPreference.AlwaysForClarity),
                KeyValuePairUtil.Create("never_if_unnecessary", ParenthesesPreference.NeverIfUnnecessary),
            });

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
                IdeCodeStyleOptions.CommonOptions.Default.ArithmeticBinaryParentheses,
                "dotnet_style_parentheses_in_arithmetic_binary_operators");

        internal static readonly PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> OtherBinaryParentheses =
            CreateParenthesesOption(
                IdeCodeStyleOptions.CommonOptions.Default.OtherBinaryParentheses,
                "dotnet_style_parentheses_in_other_binary_operators");

        internal static readonly PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> RelationalBinaryParentheses =
            CreateParenthesesOption(
                IdeCodeStyleOptions.CommonOptions.Default.RelationalBinaryParentheses,
                "dotnet_style_parentheses_in_relational_binary_operators");

        internal static readonly PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> OtherParentheses =
            CreateParenthesesOption(
                IdeCodeStyleOptions.CommonOptions.Default.OtherParentheses,
                "dotnet_style_parentheses_in_other_operators");

        private static readonly BidirectionalMap<string, ForEachExplicitCastInSourcePreference> s_forEachExplicitCastInSourcePreferencePreferenceMap =
            new(new[]
            {
                KeyValuePairUtil.Create("always", ForEachExplicitCastInSourcePreference.Always),
                KeyValuePairUtil.Create("when_strongly_typed", ForEachExplicitCastInSourcePreference.WhenStronglyTyped),
            });

        internal static readonly Option2<CodeStyleOption2<ForEachExplicitCastInSourcePreference>> ForEachExplicitCastInSource = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_prefer_foreach_explicit_cast_in_source",
            IdeCodeStyleOptions.CommonOptions.Default.ForEachExplicitCastInSource,
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
            "CodeStyleOptions_PreferSystemHashCode",
            IdeAnalyzerOptions.CommonDefault.PreferSystemHashCode,
            group: CodeStyleOptionGroups.ExpressionLevelPreferences);

        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferNamespaceAndFolderMatchStructure = CreatePerLanguageOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            "dotnet_style_namespace_match_folder",
            IdeCodeStyleOptions.CommonOptions.Default.PreferNamespaceAndFolderMatchStructure);

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> AllowMultipleBlankLines = CreatePerLanguageOption(
            CodeStyleOptionGroups.NewLinePreferences,
            "dotnet_style_allow_multiple_blank_lines_experimental",
            IdeCodeStyleOptions.CommonOptions.Default.AllowMultipleBlankLines);

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> AllowStatementImmediatelyAfterBlock = CreatePerLanguageOption(
            CodeStyleOptionGroups.NewLinePreferences,
            "dotnet_style_allow_statement_immediately_after_block_experimental",
            IdeCodeStyleOptions.CommonOptions.Default.AllowStatementImmediatelyAfterBlock);

        internal static readonly ImmutableArray<IOption2> AllOptions = s_allOptionsBuilder.ToImmutable();
    }

    internal static class CodeStyleOptionGroups
    {
        public static readonly OptionGroup CodeStyle = new("", priority: 1);

        public static readonly OptionGroup Usings = new(CompilerExtensionsResources.Organize_usings, priority: 1, parent: CodeStyle);
        public static readonly OptionGroup ThisOrMe = new(CompilerExtensionsResources.this_dot_and_Me_dot_preferences, priority: 2, parent: CodeStyle);
        public static readonly OptionGroup PredefinedTypeNameUsage = new(CompilerExtensionsResources.Language_keywords_vs_BCL_types_preferences, priority: 3, parent: CodeStyle);
        public static readonly OptionGroup Parentheses = new(CompilerExtensionsResources.Parentheses_preferences, priority: 4, parent: CodeStyle);
        public static readonly OptionGroup Modifier = new(CompilerExtensionsResources.Modifier_preferences, priority: 5, parent: CodeStyle);
        public static readonly OptionGroup ExpressionLevelPreferences = new(CompilerExtensionsResources.Expression_level_preferences, priority: 6, parent: CodeStyle);
        public static readonly OptionGroup Field = new(CompilerExtensionsResources.Field_preferences, priority: 7, parent: CodeStyle);
        public static readonly OptionGroup Parameter = new(CompilerExtensionsResources.Parameter_preferences, priority: 8, parent: CodeStyle);
        public static readonly OptionGroup Suppressions = new(CompilerExtensionsResources.Suppression_preferences, priority: 9, parent: CodeStyle);
        public static readonly OptionGroup NewLinePreferences = new(CompilerExtensionsResources.New_line_preferences, priority: 10, parent: CodeStyle);
    }
}
