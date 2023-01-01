// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeStyle.CodeStyleHelpers;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal static class CodeStyleOptions2
    {
        private const string PublicFeatureName = "CodeStyleOptions";

        private static readonly ImmutableArray<IOption2>.Builder s_allOptionsBuilder = ImmutableArray.CreateBuilder<IOption2>();

        internal static ImmutableArray<IOption2> AllOptions { get; }

        private static PerLanguageOption2<T> CreateOption<T>(OptionGroup group, string name, T defaultValue, EditorConfigStorageLocation<T> storageLocation)
        {
            var option = new PerLanguageOption2<T>(name, defaultValue, group, storageLocation);
            s_allOptionsBuilder.Add(option);
            return option;
        }

        private static Option2<T> CreateCommonOption<T>(OptionGroup group, string name, T defaultValue, EditorConfigStorageLocation<T> storageLocation)
        {
            var option = new Option2<T>(name, defaultValue, group, storageLocation);
            s_allOptionsBuilder.Add(option);
            return option;
        }

        /// <remarks>
        /// When user preferences are not yet set for a style, we fall back to the default value.
        /// One such default(s), is that the feature is turned on, so that codegen consumes it,
        /// but with silent enforcement, so that the user is not prompted about their usage.
        /// </remarks>
        internal static readonly CodeStyleOption2<bool> TrueWithSilentEnforcement = new(value: true, notification: NotificationOption2.Silent);
        internal static readonly CodeStyleOption2<bool> FalseWithSilentEnforcement = new(value: false, notification: NotificationOption2.Silent);
        internal static readonly CodeStyleOption2<bool> TrueWithSuggestionEnforcement = new(value: true, notification: NotificationOption2.Suggestion);
        internal static readonly CodeStyleOption2<bool> FalseWithSuggestionEnforcement = new(value: false, notification: NotificationOption2.Suggestion);

        private static PerLanguageOption2<CodeStyleOption2<bool>> CreateOption(
            OptionGroup group, CodeStyleOption2<bool> defaultValue,
            string editorconfigKeyName)
            => CreateOption(
                group, editorconfigKeyName, defaultValue,
                EditorConfigStorageLocation.ForBoolCodeStyleOption(defaultValue));

        private static PerLanguageOption2<CodeStyleOption2<bool>> CreateQualifyAccessOption(string editorconfigKeyName)
            => CreateOption(
                CodeStyleOptionGroups.ThisOrMe,
                defaultValue: SimplifierOptions.DefaultQualifyAccess,
                editorconfigKeyName);

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
        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferIntrinsicPredefinedTypeKeywordInDeclaration = CreateOption(
            CodeStyleOptionGroups.PredefinedTypeNameUsage,
            defaultValue: SimplifierOptions.DefaultPreferPredefinedTypeKeyword,
            editorconfigKeyName: "dotnet_style_predefined_type_for_locals_parameters_members")
            .WithPublicOption(PublicFeatureName, "PreferIntrinsicPredefinedTypeKeywordInDeclaration");

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Member Access Expression
        /// </summary>
        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferIntrinsicPredefinedTypeKeywordInMemberAccess = CreateOption(
            CodeStyleOptionGroups.PredefinedTypeNameUsage,
            defaultValue: SimplifierOptions.DefaultPreferPredefinedTypeKeyword,
            editorconfigKeyName: "dotnet_style_predefined_type_for_member_access")
            .WithPublicOption(PublicFeatureName, "PreferIntrinsicPredefinedTypeKeywordInMemberAccess");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferObjectInitializer = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            IdeCodeStyleOptions.CommonOptions.Default.PreferObjectInitializer,
            "dotnet_style_object_initializer");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferCollectionInitializer = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            IdeCodeStyleOptions.CommonOptions.Default.PreferCollectionInitializer,
            "dotnet_style_collection_initializer");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferSimplifiedBooleanExpressions = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            IdeCodeStyleOptions.CommonOptions.Default.PreferSimplifiedBooleanExpressions,
            "dotnet_style_prefer_simplified_boolean_expressions");

        internal static readonly Option2<OperatorPlacementWhenWrappingPreference> OperatorPlacementWhenWrapping =
            CreateCommonOption(
                CodeStyleOptionGroups.ExpressionLevelPreferences,
                "dotnet_style_operator_placement_when_wrapping",
                IdeCodeStyleOptions.CommonOptions.Default.OperatorPlacementWhenWrapping,
                new EditorConfigStorageLocation<OperatorPlacementWhenWrappingPreference>(
                    OperatorPlacementUtilities.Parse,
                    OperatorPlacementUtilities.GetEditorConfigString));

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferCoalesceExpression = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            IdeCodeStyleOptions.CommonOptions.Default.PreferCoalesceExpression,
            "dotnet_style_coalesce_expression");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferNullPropagation = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            IdeCodeStyleOptions.CommonOptions.Default.PreferNullPropagation,
            "dotnet_style_null_propagation");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferExplicitTupleNames = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            IdeCodeStyleOptions.CommonOptions.Default.PreferExplicitTupleNames,
            "dotnet_style_explicit_tuple_names");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferAutoProperties = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            IdeCodeStyleOptions.CommonOptions.Default.PreferAutoProperties,
            "dotnet_style_prefer_auto_properties");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferInferredTupleNames = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            IdeCodeStyleOptions.CommonOptions.Default.PreferInferredTupleNames,
            "dotnet_style_prefer_inferred_tuple_names");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferInferredAnonymousTypeMemberNames = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            IdeCodeStyleOptions.CommonOptions.Default.PreferInferredAnonymousTypeMemberNames,
            "dotnet_style_prefer_inferred_anonymous_type_member_names");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferIsNullCheckOverReferenceEqualityMethod = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            IdeCodeStyleOptions.CommonOptions.Default.PreferIsNullCheckOverReferenceEqualityMethod,
            "dotnet_style_prefer_is_null_check_over_reference_equality_method");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferConditionalExpressionOverAssignment = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            IdeCodeStyleOptions.CommonOptions.Default.PreferConditionalExpressionOverAssignment,
            "dotnet_style_prefer_conditional_expression_over_assignment");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferConditionalExpressionOverReturn = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            IdeCodeStyleOptions.CommonOptions.Default.PreferConditionalExpressionOverReturn,
            "dotnet_style_prefer_conditional_expression_over_return");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferCompoundAssignment = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            IdeCodeStyleOptions.CommonOptions.Default.PreferCompoundAssignment,
            "dotnet_style_prefer_compound_assignment");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferSimplifiedInterpolation = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            IdeCodeStyleOptions.CommonOptions.Default.PreferSimplifiedInterpolation,
            "dotnet_style_prefer_simplified_interpolation");

        internal static readonly PerLanguageOption2<CodeStyleOption2<UnusedParametersPreference>> UnusedParameters = CreateOption(
            CodeStyleOptionGroups.Parameter,
            "dotnet_code_quality_unused_parameters",
            IdeCodeStyleOptions.CommonOptions.Default.UnusedParameters,
            new EditorConfigStorageLocation<CodeStyleOption2<UnusedParametersPreference>>(
                    s => ParseUnusedParametersPreference(s, IdeCodeStyleOptions.CommonOptions.Default.UnusedParameters),
                    o => GetUnusedParametersPreferenceEditorConfigString(o, IdeCodeStyleOptions.CommonOptions.Default.UnusedParameters)));

        internal static readonly PerLanguageOption2<CodeStyleOption2<AccessibilityModifiersRequired>> AccessibilityModifiersRequired =
            CreateOption(
                CodeStyleOptionGroups.Modifier, "dotnet_style_require_accessibility_modifiers",
                IdeCodeStyleOptions.CommonOptions.Default.AccessibilityModifiersRequired,
                new EditorConfigStorageLocation<CodeStyleOption2<AccessibilityModifiersRequired>>(
                    s => ParseAccessibilityModifiersRequired(s, IdeCodeStyleOptions.CommonOptions.Default.AccessibilityModifiersRequired),
                    v => GetAccessibilityModifiersRequiredEditorConfigString(v, IdeCodeStyleOptions.CommonOptions.Default.AccessibilityModifiersRequired)));

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferReadonly = CreateOption(
            CodeStyleOptionGroups.Field, IdeCodeStyleOptions.CommonOptions.Default.PreferReadonly,
            "dotnet_style_readonly_field");

        internal static readonly Option2<string> FileHeaderTemplate = CreateCommonOption(
            CodeStyleOptionGroups.Usings, "file_header_template",
            DocumentFormattingOptions.Default.FileHeaderTemplate,
            EditorConfigStorageLocation.ForStringOption(emptyStringRepresentation: "unset"));

        internal static readonly Option2<string> RemoveUnnecessarySuppressionExclusions = CreateCommonOption(
            CodeStyleOptionGroups.Suppressions,
            "dotnet_remove_unnecessary_suppression_exclusions",
            IdeCodeStyleOptions.CommonOptions.Default.RemoveUnnecessarySuppressionExclusions,
            EditorConfigStorageLocation.ForStringOption(emptyStringRepresentation: "none"));

        private static readonly BidirectionalMap<string, AccessibilityModifiersRequired> s_accessibilityModifiersRequiredMap =
            new(new[]
            {
                KeyValuePairUtil.Create("never", CodeStyle.AccessibilityModifiersRequired.Never),
                KeyValuePairUtil.Create("always", CodeStyle.AccessibilityModifiersRequired.Always),
                KeyValuePairUtil.Create("for_non_interface_members", CodeStyle.AccessibilityModifiersRequired.ForNonInterfaceMembers),
                KeyValuePairUtil.Create("omit_if_default", CodeStyle.AccessibilityModifiersRequired.OmitIfDefault),
            });

        private static CodeStyleOption2<AccessibilityModifiersRequired> ParseAccessibilityModifiersRequired(string optionString, CodeStyleOption2<AccessibilityModifiersRequired> defaultValue)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(optionString,
                    defaultValue.Notification, out var value, out var notificationOpt))
            {
                Debug.Assert(s_accessibilityModifiersRequiredMap.ContainsKey(value));
                return new CodeStyleOption2<AccessibilityModifiersRequired>(s_accessibilityModifiersRequiredMap.GetValueOrDefault(value), notificationOpt);
            }

            return defaultValue;
        }

        private static string GetAccessibilityModifiersRequiredEditorConfigString(CodeStyleOption2<AccessibilityModifiersRequired> option, CodeStyleOption2<AccessibilityModifiersRequired> defaultValue)
        {
            Debug.Assert(s_accessibilityModifiersRequiredMap.ContainsValue(option.Value));
            return $"{s_accessibilityModifiersRequiredMap.GetKeyOrDefault(option.Value)}{GetEditorConfigStringNotificationPart(option, defaultValue)}";
        }

        private static PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> CreateParenthesesOption(
            CodeStyleOption2<ParenthesesPreference> defaultValue, string name)
        {
            return CreateOption(
                CodeStyleOptionGroups.Parentheses, name, defaultValue,
                new EditorConfigStorageLocation<CodeStyleOption2<ParenthesesPreference>>(
                    s => ParseParenthesesPreference(s, defaultValue),
                    v => GetParenthesesPreferenceEditorConfigString(v, defaultValue)));
        }

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

        private static readonly BidirectionalMap<string, ParenthesesPreference> s_parenthesesPreferenceMap =
            new(new[]
            {
                KeyValuePairUtil.Create("always_for_clarity", ParenthesesPreference.AlwaysForClarity),
                KeyValuePairUtil.Create("never_if_unnecessary", ParenthesesPreference.NeverIfUnnecessary),
            });

        private static readonly BidirectionalMap<string, UnusedParametersPreference> s_unusedParametersPreferenceMap =
            new(new[]
            {
                KeyValuePairUtil.Create("non_public", UnusedParametersPreference.NonPublicMethods),
                KeyValuePairUtil.Create("all", UnusedParametersPreference.AllMethods),
            });

        #region dotnet_style_prefer_foreach_explicit_cast_in_source

        private static readonly BidirectionalMap<string, ForEachExplicitCastInSourcePreference> s_forEachExplicitCastInSourcePreferencePreferenceMap =
            new(new[]
            {
                KeyValuePairUtil.Create("always", ForEachExplicitCastInSourcePreference.Always),
                KeyValuePairUtil.Create("when_strongly_typed", ForEachExplicitCastInSourcePreference.WhenStronglyTyped),
            });

        internal static readonly Option2<CodeStyleOption2<ForEachExplicitCastInSourcePreference>> ForEachExplicitCastInSource =
            CreateCommonOption(
                CodeStyleOptionGroups.ExpressionLevelPreferences,
                "dotnet_style_prefer_foreach_explicit_cast_in_source",
                IdeCodeStyleOptions.CommonOptions.Default.ForEachExplicitCastInSource,
                new EditorConfigStorageLocation<CodeStyleOption2<ForEachExplicitCastInSourcePreference>>(
                    s => ParseForEachExplicitCastInSourcePreference(s, IdeCodeStyleOptions.CommonOptions.Default.ForEachExplicitCastInSource),
                    v => GetForEachExplicitCastInSourceEditorConfigString(v, IdeCodeStyleOptions.CommonOptions.Default.ForEachExplicitCastInSource)));

        private static CodeStyleOption2<ForEachExplicitCastInSourcePreference> ParseForEachExplicitCastInSourcePreference(
            string optionString, CodeStyleOption2<ForEachExplicitCastInSourcePreference> defaultValue)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(optionString,
                    defaultValue.Notification, out var value, out var notification))
            {
                Debug.Assert(s_forEachExplicitCastInSourcePreferencePreferenceMap.ContainsKey(value));
                return new CodeStyleOption2<ForEachExplicitCastInSourcePreference>(
                    s_forEachExplicitCastInSourcePreferencePreferenceMap.GetValueOrDefault(value), notification);
            }

            return defaultValue;
        }

        private static string GetForEachExplicitCastInSourceEditorConfigString(
            CodeStyleOption2<ForEachExplicitCastInSourcePreference> option,
            CodeStyleOption2<ForEachExplicitCastInSourcePreference> defaultValue)
        {
            Debug.Assert(s_forEachExplicitCastInSourcePreferencePreferenceMap.ContainsValue(option.Value));
            var value = s_forEachExplicitCastInSourcePreferencePreferenceMap.GetKeyOrDefault(option.Value) ??
                s_forEachExplicitCastInSourcePreferencePreferenceMap.GetKeyOrDefault(defaultValue.Value);
            return $"{value}{GetEditorConfigStringNotificationPart(option, defaultValue)}";
        }

        #endregion

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferSystemHashCode = new(
            "CodeStyleOptions_PreferSystemHashCode",
            IdeAnalyzerOptions.CommonDefault.PreferSystemHashCode,
            CodeStyleOptionGroups.ExpressionLevelPreferences);

        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferNamespaceAndFolderMatchStructure = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, IdeCodeStyleOptions.CommonOptions.Default.PreferNamespaceAndFolderMatchStructure,
            editorconfigKeyName: "dotnet_style_namespace_match_folder");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> AllowMultipleBlankLines = CreateOption(
            CodeStyleOptionGroups.NewLinePreferences, IdeCodeStyleOptions.CommonOptions.Default.AllowMultipleBlankLines,
            "dotnet_style_allow_multiple_blank_lines_experimental");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> AllowStatementImmediatelyAfterBlock = CreateOption(
            CodeStyleOptionGroups.NewLinePreferences, IdeCodeStyleOptions.CommonOptions.Default.AllowStatementImmediatelyAfterBlock,
            "dotnet_style_allow_statement_immediately_after_block_experimental");

        static CodeStyleOptions2()
        {
            // Note that the static constructor executes after all the static field initializers for the options have executed,
            // and each field initializer adds the created option to s_allOptionsBuilder.
            AllOptions = s_allOptionsBuilder.ToImmutable();
        }

        private static CodeStyleOption2<ParenthesesPreference> ParseParenthesesPreference(
            string optionString, CodeStyleOption2<ParenthesesPreference> defaultValue)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(optionString,
                    defaultValue.Notification, out var value, out var notification))
            {
                Debug.Assert(s_parenthesesPreferenceMap.ContainsKey(value));
                return new CodeStyleOption2<ParenthesesPreference>(s_parenthesesPreferenceMap.GetValueOrDefault(value), notification);
            }

            return defaultValue;
        }

        private static string GetParenthesesPreferenceEditorConfigString(CodeStyleOption2<ParenthesesPreference> option, CodeStyleOption2<ParenthesesPreference> defaultValue)
        {
            Debug.Assert(s_parenthesesPreferenceMap.ContainsValue(option.Value));
            var value = s_parenthesesPreferenceMap.GetKeyOrDefault(option.Value) ?? s_parenthesesPreferenceMap.GetKeyOrDefault(ParenthesesPreference.AlwaysForClarity);
            return $"{value}{GetEditorConfigStringNotificationPart(option, defaultValue)}";
        }

        private static CodeStyleOption2<UnusedParametersPreference> ParseUnusedParametersPreference(string optionString, CodeStyleOption2<UnusedParametersPreference> defaultValue)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(optionString,
                    defaultValue.Notification, out var value, out var notification))
            {
                return new CodeStyleOption2<UnusedParametersPreference>(s_unusedParametersPreferenceMap.GetValueOrDefault(value), notification);
            }

            return defaultValue;
        }

        private static string GetUnusedParametersPreferenceEditorConfigString(CodeStyleOption2<UnusedParametersPreference> option, CodeStyleOption2<UnusedParametersPreference> defaultValue)
        {
            Debug.Assert(s_unusedParametersPreferenceMap.ContainsValue(option.Value));
            var value = s_unusedParametersPreferenceMap.GetKeyOrDefault(option.Value) ?? s_unusedParametersPreferenceMap.GetKeyOrDefault(defaultValue.Value);
            return $"{value}{GetEditorConfigStringNotificationPart(option, defaultValue)}";
        }
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
