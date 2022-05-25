﻿// Licensed to the .NET Foundation under one or more agreements.
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
        private static readonly ImmutableArray<IOption2>.Builder s_allOptionsBuilder = ImmutableArray.CreateBuilder<IOption2>();

        internal static ImmutableArray<IOption2> AllOptions { get; }

        private static PerLanguageOption2<T> CreateOption<T>(
            OptionGroup group, string name, T defaultValue,
            OptionStorageLocation2 storageLocation)
        {
            var option = new PerLanguageOption2<T>(
                "CodeStyleOptions",
                group, name, defaultValue,
                ImmutableArray.Create(storageLocation));

            s_allOptionsBuilder.Add(option);
            return option;
        }

        private static PerLanguageOption2<T> CreateOption<T>(
            OptionGroup group, string name, T defaultValue,
            OptionStorageLocation2 storageLocation1, OptionStorageLocation2 storageLocation2)
        {
            var option = new PerLanguageOption2<T>(
                "CodeStyleOptions",
                group, name, defaultValue,
                ImmutableArray.Create(storageLocation1, storageLocation2));

            s_allOptionsBuilder.Add(option);
            return option;
        }

        private static Option2<T> CreateCommonOption<T>(OptionGroup group, string name, T defaultValue, OptionStorageLocation2 storageLocation)
        {
            var option = new Option2<T>(
                "CodeStyleOptions",
                group, name, defaultValue,
                ImmutableArray.Create(storageLocation));

            s_allOptionsBuilder.Add(option);
            return option;
        }

        private static Option2<T> CreateCommonOption<T>(
            OptionGroup group, string name, T defaultValue,
            OptionStorageLocation2 storageLocation1, OptionStorageLocation2 storageLocation2)
        {
            var option = new Option2<T>(
                "CodeStyleOptions",
                group, name, defaultValue,
                ImmutableArray.Create(storageLocation1, storageLocation2));

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
            OptionGroup group, string name, CodeStyleOption2<bool> defaultValue,
            string editorconfigKeyName, string roamingProfileStorageKeyName)
            => CreateOption(
                group, name, defaultValue,
                EditorConfigStorageLocation.ForBoolCodeStyleOption(editorconfigKeyName, defaultValue),
                new RoamingProfileStorageLocation(roamingProfileStorageKeyName));

        private static PerLanguageOption2<CodeStyleOption2<bool>> CreateQualifyAccessOption(string optionName, string editorconfigKeyName)
            => CreateOption(
                CodeStyleOptionGroups.ThisOrMe,
                optionName,
                defaultValue: SimplifierOptions.DefaultQualifyAccess,
                editorconfigKeyName,
                $"TextEditor.%LANGUAGE%.Specific.{optionName}");

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in field access expressions.
        /// </summary>
        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> QualifyFieldAccess = CreateQualifyAccessOption(
            nameof(QualifyFieldAccess), "dotnet_style_qualification_for_field");

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in property access expressions.
        /// </summary>
        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> QualifyPropertyAccess = CreateQualifyAccessOption(
            nameof(QualifyPropertyAccess), "dotnet_style_qualification_for_property");

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in method access expressions.
        /// </summary>
        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> QualifyMethodAccess = CreateQualifyAccessOption(
            nameof(QualifyMethodAccess), "dotnet_style_qualification_for_method");

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in event access expressions.
        /// </summary>
        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> QualifyEventAccess = CreateQualifyAccessOption(
            nameof(QualifyEventAccess), "dotnet_style_qualification_for_event");

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Declarations
        /// </summary>
        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferIntrinsicPredefinedTypeKeywordInDeclaration = CreateOption(
            CodeStyleOptionGroups.PredefinedTypeNameUsage, nameof(PreferIntrinsicPredefinedTypeKeywordInDeclaration),
            defaultValue: SimplifierOptions.DefaultPreferPredefinedTypeKeyword,
            "dotnet_style_predefined_type_for_locals_parameters_members",
            "TextEditor.%LANGUAGE%.Specific.PreferIntrinsicPredefinedTypeKeywordInDeclaration.CodeStyle");

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Member Access Expression
        /// </summary>
        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferIntrinsicPredefinedTypeKeywordInMemberAccess = CreateOption(
            CodeStyleOptionGroups.PredefinedTypeNameUsage, nameof(PreferIntrinsicPredefinedTypeKeywordInMemberAccess),
            defaultValue: SimplifierOptions.DefaultPreferPredefinedTypeKeyword,
            "dotnet_style_predefined_type_for_member_access",
            "TextEditor.%LANGUAGE%.Specific.PreferIntrinsicPredefinedTypeKeywordInMemberAccess.CodeStyle");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferObjectInitializer = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferObjectInitializer),
            IdeCodeStyleOptions.CommonOptions.Default.PreferObjectInitializer,
            "dotnet_style_object_initializer",
            "TextEditor.%LANGUAGE%.Specific.PreferObjectInitializer");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferCollectionInitializer = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferCollectionInitializer),
            IdeCodeStyleOptions.CommonOptions.Default.PreferCollectionInitializer,
            "dotnet_style_collection_initializer",
            "TextEditor.%LANGUAGE%.Specific.PreferCollectionInitializer");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferSimplifiedBooleanExpressions = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferSimplifiedBooleanExpressions),
            IdeCodeStyleOptions.CommonOptions.Default.PreferSimplifiedBooleanExpressions,
            "dotnet_style_prefer_simplified_boolean_expressions",
            "TextEditor.%LANGUAGE%.Specific.PreferSimplifiedBooleanExpressions");

        internal static readonly PerLanguageOption2<OperatorPlacementWhenWrappingPreference> OperatorPlacementWhenWrapping =
            CreateOption(
                CodeStyleOptionGroups.ExpressionLevelPreferences,
                nameof(OperatorPlacementWhenWrapping),
                IdeCodeStyleOptions.CommonOptions.Default.OperatorPlacementWhenWrapping,
                new EditorConfigStorageLocation<OperatorPlacementWhenWrappingPreference>(
                    "dotnet_style_operator_placement_when_wrapping",
                    OperatorPlacementUtilities.Parse,
                    OperatorPlacementUtilities.GetEditorConfigString));

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferCoalesceExpression = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferCoalesceExpression),
            IdeCodeStyleOptions.CommonOptions.Default.PreferCoalesceExpression,
            "dotnet_style_coalesce_expression",
            "TextEditor.%LANGUAGE%.Specific.PreferCoalesceExpression");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferNullPropagation = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferNullPropagation),
            IdeCodeStyleOptions.CommonOptions.Default.PreferNullPropagation,
            "dotnet_style_null_propagation",
            "TextEditor.%LANGUAGE%.Specific.PreferNullPropagation");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferExplicitTupleNames = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferExplicitTupleNames),
            IdeCodeStyleOptions.CommonOptions.Default.PreferExplicitTupleNames,
            "dotnet_style_explicit_tuple_names",
            "TextEditor.%LANGUAGE%.Specific.PreferExplicitTupleNames");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferAutoProperties = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferAutoProperties),
            IdeCodeStyleOptions.CommonOptions.Default.PreferAutoProperties,
            "dotnet_style_prefer_auto_properties",
            "TextEditor.%LANGUAGE%.Specific.PreferAutoProperties");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferInferredTupleNames = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferInferredTupleNames),
            IdeCodeStyleOptions.CommonOptions.Default.PreferInferredTupleNames,
            "dotnet_style_prefer_inferred_tuple_names",
            $"TextEditor.%LANGUAGE%.Specific.{nameof(PreferInferredTupleNames)}");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferInferredAnonymousTypeMemberNames = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferInferredAnonymousTypeMemberNames),
            IdeCodeStyleOptions.CommonOptions.Default.PreferInferredAnonymousTypeMemberNames,
            "dotnet_style_prefer_inferred_anonymous_type_member_names",
            $"TextEditor.%LANGUAGE%.Specific.PreferInferredAnonymousTypeMemberNames");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferIsNullCheckOverReferenceEqualityMethod = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferIsNullCheckOverReferenceEqualityMethod),
            IdeCodeStyleOptions.CommonOptions.Default.PreferIsNullCheckOverReferenceEqualityMethod,
            "dotnet_style_prefer_is_null_check_over_reference_equality_method",
            $"TextEditor.%LANGUAGE%.Specific.PreferIsNullCheckOverReferenceEqualityMethod");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferConditionalExpressionOverAssignment = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferConditionalExpressionOverAssignment),
            IdeCodeStyleOptions.CommonOptions.Default.PreferConditionalExpressionOverAssignment,
            "dotnet_style_prefer_conditional_expression_over_assignment",
            "TextEditor.%LANGUAGE%.Specific.PreferConditionalExpressionOverAssignment");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferConditionalExpressionOverReturn = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferConditionalExpressionOverReturn),
            IdeCodeStyleOptions.CommonOptions.Default.PreferConditionalExpressionOverReturn,
            "dotnet_style_prefer_conditional_expression_over_return",
            "TextEditor.%LANGUAGE%.Specific.PreferConditionalExpressionOverReturn");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferCompoundAssignment = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            nameof(PreferCompoundAssignment),
            IdeCodeStyleOptions.CommonOptions.Default.PreferCompoundAssignment,
            "dotnet_style_prefer_compound_assignment",
            "TextEditor.%LANGUAGE%.Specific.PreferCompoundAssignment");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferSimplifiedInterpolation = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferSimplifiedInterpolation),
            IdeCodeStyleOptions.CommonOptions.Default.PreferSimplifiedInterpolation,
            "dotnet_style_prefer_simplified_interpolation",
            $"TextEditor.%LANGUAGE%.Specific.PreferSimplifiedInterpolation");

        // TODO: https://github.com/dotnet/roslyn/issues/31225 tracks adding CodeQualityOption<T> and CodeQualityOptions
        // and moving this option to CodeQualityOptions.
        internal static readonly PerLanguageOption2<CodeStyleOption2<UnusedParametersPreference>> UnusedParameters = CreateOption(
            CodeStyleOptionGroups.Parameter,
            nameof(UnusedParameters),
            IdeCodeStyleOptions.CommonOptions.Default.UnusedParameters,
            new EditorConfigStorageLocation<CodeStyleOption2<UnusedParametersPreference>>(
                    "dotnet_code_quality_unused_parameters",
                    s => ParseUnusedParametersPreference(s, IdeCodeStyleOptions.CommonOptions.Default.UnusedParameters),
                    o => GetUnusedParametersPreferenceEditorConfigString(o, IdeCodeStyleOptions.CommonOptions.Default.UnusedParameters)),
            new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.UnusedParametersPreference"));

        internal static readonly PerLanguageOption2<CodeStyleOption2<AccessibilityModifiersRequired>> AccessibilityModifiersRequired =
            CreateOption(
                CodeStyleOptionGroups.Modifier, "RequireAccessibilityModifiers",
                IdeCodeStyleOptions.CommonOptions.Default.AccessibilityModifiersRequired,
                new EditorConfigStorageLocation<CodeStyleOption2<AccessibilityModifiersRequired>>(
                    "dotnet_style_require_accessibility_modifiers",
                    s => ParseAccessibilityModifiersRequired(s, IdeCodeStyleOptions.CommonOptions.Default.AccessibilityModifiersRequired),
                    v => GetAccessibilityModifiersRequiredEditorConfigString(v, IdeCodeStyleOptions.CommonOptions.Default.AccessibilityModifiersRequired)),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.RequireAccessibilityModifiers"));

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferReadonly = CreateOption(
            CodeStyleOptionGroups.Field, nameof(PreferReadonly),
            IdeCodeStyleOptions.CommonOptions.Default.PreferReadonly,
            "dotnet_style_readonly_field",
            "TextEditor.%LANGUAGE%.Specific.PreferReadonly");

        internal static readonly Option2<string> FileHeaderTemplate = CreateCommonOption(
            CodeStyleOptionGroups.Usings, nameof(FileHeaderTemplate),
            DocumentFormattingOptions.Default.FileHeaderTemplate,
            EditorConfigStorageLocation.ForStringOption("file_header_template", emptyStringRepresentation: "unset"));

        internal static readonly Option2<string> RemoveUnnecessarySuppressionExclusions = CreateCommonOption(
            CodeStyleOptionGroups.Suppressions,
            nameof(RemoveUnnecessarySuppressionExclusions),
            IdeCodeStyleOptions.CommonOptions.Default.RemoveUnnecessarySuppressionExclusions,
            EditorConfigStorageLocation.ForStringOption("dotnet_remove_unnecessary_suppression_exclusions", emptyStringRepresentation: "none"),
            new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.RemoveUnnecessarySuppressionExclusions"));

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
            string fieldName, CodeStyleOption2<ParenthesesPreference> defaultValue,
            string styleName)
        {
            return CreateOption(
                CodeStyleOptionGroups.Parentheses, fieldName, defaultValue,
                new EditorConfigStorageLocation<CodeStyleOption2<ParenthesesPreference>>(
                    styleName,
                    s => ParseParenthesesPreference(s, defaultValue),
                    v => GetParenthesesPreferenceEditorConfigString(v, defaultValue)),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{fieldName}Preference"));
        }

        internal static readonly PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> ArithmeticBinaryParentheses =
            CreateParenthesesOption(
                nameof(ArithmeticBinaryParentheses),
                IdeCodeStyleOptions.CommonOptions.Default.ArithmeticBinaryParentheses,
                "dotnet_style_parentheses_in_arithmetic_binary_operators");

        internal static readonly PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> OtherBinaryParentheses =
            CreateParenthesesOption(
                nameof(OtherBinaryParentheses),
                IdeCodeStyleOptions.CommonOptions.Default.OtherBinaryParentheses,
                "dotnet_style_parentheses_in_other_binary_operators");

        internal static readonly PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> RelationalBinaryParentheses =
            CreateParenthesesOption(
                nameof(RelationalBinaryParentheses),
                IdeCodeStyleOptions.CommonOptions.Default.RelationalBinaryParentheses,
                "dotnet_style_parentheses_in_relational_binary_operators");

        internal static readonly PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> OtherParentheses =
            CreateParenthesesOption(
                nameof(OtherParentheses),
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

        internal static readonly PerLanguageOption2<CodeStyleOption2<ForEachExplicitCastInSourcePreference>> ForEachExplicitCastInSource =
            CreateOption(
                CodeStyleOptionGroups.ExpressionLevelPreferences,
                nameof(ForEachExplicitCastInSource),
                IdeCodeStyleOptions.CommonOptions.Default.ForEachExplicitCastInSource,
                new EditorConfigStorageLocation<CodeStyleOption2<ForEachExplicitCastInSourcePreference>>(
                    "dotnet_style_prefer_foreach_explicit_cast_in_source",
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

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferSystemHashCode = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            nameof(PreferSystemHashCode),
            IdeAnalyzerOptions.DefaultPreferSystemHashCode,
            new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferSystemHashCode"));

        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferNamespaceAndFolderMatchStructure = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferNamespaceAndFolderMatchStructure),
            IdeCodeStyleOptions.CommonOptions.Default.PreferNamespaceAndFolderMatchStructure,
            editorconfigKeyName: "dotnet_style_namespace_match_folder",
            roamingProfileStorageKeyName: "TextEditor.%LANGUAGE%.Specific.PreferNamespaceAndFolderMatchStructure");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> AllowMultipleBlankLines = CreateOption(
            CodeStyleOptionGroups.NewLinePreferences, nameof(AllowMultipleBlankLines),
            IdeCodeStyleOptions.CommonOptions.Default.AllowMultipleBlankLines,
            "dotnet_style_allow_multiple_blank_lines_experimental",
            "TextEditor.%LANGUAGE%.Specific.AllowMultipleBlankLines");

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> AllowStatementImmediatelyAfterBlock = CreateOption(
            CodeStyleOptionGroups.NewLinePreferences, nameof(AllowStatementImmediatelyAfterBlock),
            IdeCodeStyleOptions.CommonOptions.Default.AllowStatementImmediatelyAfterBlock,
            "dotnet_style_allow_statement_immediately_after_block_experimental",
            "TextEditor.%LANGUAGE%.Specific.AllowStatementImmediatelyAfterBlock");

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
        public static readonly OptionGroup Usings = new(CompilerExtensionsResources.Organize_usings, priority: 1);
        public static readonly OptionGroup ThisOrMe = new(CompilerExtensionsResources.this_dot_and_Me_dot_preferences, priority: 2);
        public static readonly OptionGroup PredefinedTypeNameUsage = new(CompilerExtensionsResources.Language_keywords_vs_BCL_types_preferences, priority: 3);
        public static readonly OptionGroup Parentheses = new(CompilerExtensionsResources.Parentheses_preferences, priority: 4);
        public static readonly OptionGroup Modifier = new(CompilerExtensionsResources.Modifier_preferences, priority: 5);
        public static readonly OptionGroup ExpressionLevelPreferences = new(CompilerExtensionsResources.Expression_level_preferences, priority: 6);
        public static readonly OptionGroup Field = new(CompilerExtensionsResources.Field_preferences, priority: 7);
        public static readonly OptionGroup Parameter = new(CompilerExtensionsResources.Parameter_preferences, priority: 8);
        public static readonly OptionGroup Suppressions = new(CompilerExtensionsResources.Suppression_preferences, priority: 9);
        public static readonly OptionGroup NewLinePreferences = new(CompilerExtensionsResources.New_line_preferences, priority: 10);
    }
}
