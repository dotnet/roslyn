// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeStyle.CodeStyleHelpers;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal static class CodeStyleOptions2
    {
        private static readonly ImmutableArray<IOption2>.Builder s_allOptionsBuilder = ImmutableArray.CreateBuilder<IOption2>();

        internal static ImmutableArray<IOption2> AllOptions { get; }

        private static PerLanguageOption2<T> CreateOption<T>(OptionGroup group, string name, T defaultValue, params OptionStorageLocation2[] storageLocations)
        {
            var option = new PerLanguageOption2<T>("CodeStyleOptions", group, name, defaultValue, storageLocations);
            s_allOptionsBuilder.Add(option);
            return option;
        }

        private static Option2<T> CreateCommonOption<T>(OptionGroup group, string name, T defaultValue, params OptionStorageLocation2[] storageLocations)
        {
            var option = new Option2<T>("CodeStyleOptions", group, name, defaultValue, storageLocations);
            s_allOptionsBuilder.Add(option);
            return option;
        }

        /// <remarks>
        /// When user preferences are not yet set for a style, we fall back to the default value.
        /// One such default(s), is that the feature is turned on, so that codegen consumes it,
        /// but with silent enforcement, so that the user is not prompted about their usage.
        /// </remarks>
        internal static readonly CodeStyleOption2<bool> TrueWithSilentEnforcement = new CodeStyleOption2<bool>(value: true, notification: NotificationOption2.Silent);
        internal static readonly CodeStyleOption2<bool> FalseWithSilentEnforcement = new CodeStyleOption2<bool>(value: false, notification: NotificationOption2.Silent);
        internal static readonly CodeStyleOption2<bool> TrueWithSuggestionEnforcement = new CodeStyleOption2<bool>(value: true, notification: NotificationOption2.Suggestion);
        internal static readonly CodeStyleOption2<bool> FalseWithSuggestionEnforcement = new CodeStyleOption2<bool>(value: false, notification: NotificationOption2.Suggestion);

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in field access expressions.
        /// </summary>
        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> QualifyFieldAccess = CreateOption(
            CodeStyleOptionGroups.ThisOrMe, nameof(QualifyFieldAccess),
            defaultValue: CodeStyleOption2<bool>.Default,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_qualification_for_field"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyFieldAccess")});

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in property access expressions.
        /// </summary>
        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> QualifyPropertyAccess = CreateOption(
            CodeStyleOptionGroups.ThisOrMe, nameof(QualifyPropertyAccess),
            defaultValue: CodeStyleOption2<bool>.Default,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_qualification_for_property"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyPropertyAccess")});

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in method access expressions.
        /// </summary>
        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> QualifyMethodAccess = CreateOption(
            CodeStyleOptionGroups.ThisOrMe, nameof(QualifyMethodAccess),
            defaultValue: CodeStyleOption2<bool>.Default,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_qualification_for_method"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyMethodAccess")});

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in event access expressions.
        /// </summary>
        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> QualifyEventAccess = CreateOption(
            CodeStyleOptionGroups.ThisOrMe, nameof(QualifyEventAccess),
            defaultValue: CodeStyleOption2<bool>.Default,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_qualification_for_event"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyEventAccess")});

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Declarations
        /// </summary>
        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferIntrinsicPredefinedTypeKeywordInDeclaration = CreateOption(
            CodeStyleOptionGroups.PredefinedTypeNameUsage, nameof(PreferIntrinsicPredefinedTypeKeywordInDeclaration),
            defaultValue: TrueWithSilentEnforcement,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_predefined_type_for_locals_parameters_members"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferIntrinsicPredefinedTypeKeywordInDeclaration.CodeStyle")});

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Member Access Expression
        /// </summary>
        public static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferIntrinsicPredefinedTypeKeywordInMemberAccess = CreateOption(
            CodeStyleOptionGroups.PredefinedTypeNameUsage, nameof(PreferIntrinsicPredefinedTypeKeywordInMemberAccess),
            defaultValue: TrueWithSilentEnforcement,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_predefined_type_for_member_access"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferIntrinsicPredefinedTypeKeywordInMemberAccess.CodeStyle")});

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferObjectInitializer = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferObjectInitializer),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_object_initializer"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferObjectInitializer")});

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferCollectionInitializer = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferCollectionInitializer),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_collection_initializer"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferCollectionInitializer")});

        // TODO: Should both the below "_FadeOutCode" options be added to AllOptions?
        internal static readonly PerLanguageOption2<bool> PreferObjectInitializer_FadeOutCode = new PerLanguageOption2<bool>(
            "CodeStyleOptions", nameof(PreferObjectInitializer_FadeOutCode),
            defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferObjectInitializer_FadeOutCode"));

        internal static readonly PerLanguageOption2<bool> PreferCollectionInitializer_FadeOutCode = new PerLanguageOption2<bool>(
            "CodeStyleOptions", nameof(PreferCollectionInitializer_FadeOutCode),
            defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferCollectionInitializer_FadeOutCode"));

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferSimplifiedBooleanExpressions = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferSimplifiedBooleanExpressions),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_simplified_boolean_expressions"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferSimplifiedBooleanExpressions")});

        internal static readonly PerLanguageOption2<OperatorPlacementWhenWrappingPreference> OperatorPlacementWhenWrapping =
            CreateOption(
                CodeStyleOptionGroups.ExpressionLevelPreferences,
                nameof(OperatorPlacementWhenWrapping),
                defaultValue: OperatorPlacementWhenWrappingPreference.BeginningOfLine,
                storageLocations:
                    new EditorConfigStorageLocation<OperatorPlacementWhenWrappingPreference>(
                        "dotnet_style_operator_placement_when_wrapping",
                        OperatorPlacementUtilities.Parse,
                        OperatorPlacementUtilities.GetEditorConfigString));

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferCoalesceExpression = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferCoalesceExpression),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_coalesce_expression"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferCoalesceExpression") });

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferNullPropagation = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferNullPropagation),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_null_propagation"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferNullPropagation") });

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferExplicitTupleNames = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferExplicitTupleNames),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_explicit_tuple_names"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferExplicitTupleNames") });

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferAutoProperties = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferAutoProperties),
            defaultValue: TrueWithSilentEnforcement,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_auto_properties"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferAutoProperties") });

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferInferredTupleNames = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferInferredTupleNames),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_inferred_tuple_names"),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(PreferInferredTupleNames)}") });

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferInferredAnonymousTypeMemberNames = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferInferredAnonymousTypeMemberNames),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation2[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_inferred_anonymous_type_member_names"),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(PreferInferredAnonymousTypeMemberNames)}") });

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferIsNullCheckOverReferenceEqualityMethod = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferIsNullCheckOverReferenceEqualityMethod),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_is_null_check_over_reference_equality_method"),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(PreferIsNullCheckOverReferenceEqualityMethod)}") });

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferConditionalExpressionOverAssignment = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferConditionalExpressionOverAssignment),
            defaultValue: TrueWithSilentEnforcement,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_conditional_expression_over_assignment"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferConditionalExpressionOverAssignment")});

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferConditionalExpressionOverReturn = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferConditionalExpressionOverReturn),
            defaultValue: TrueWithSilentEnforcement,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_conditional_expression_over_return"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferConditionalExpressionOverReturn")});

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferCompoundAssignment = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            nameof(PreferCompoundAssignment),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_compound_assignment"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferCompoundAssignment") });

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferSimplifiedInterpolation = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferSimplifiedInterpolation),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_simplified_interpolation"),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(PreferSimplifiedInterpolation)}") });

        private static readonly CodeStyleOption2<UnusedParametersPreference> s_preferNoneUnusedParametersPreference =
            new CodeStyleOption2<UnusedParametersPreference>(default, NotificationOption2.None);
        private static readonly CodeStyleOption2<UnusedParametersPreference> s_preferAllMethodsUnusedParametersPreference =
            new CodeStyleOption2<UnusedParametersPreference>(UnusedParametersPreference.AllMethods, NotificationOption2.Suggestion);

        // TODO: https://github.com/dotnet/roslyn/issues/31225 tracks adding CodeQualityOption<T> and CodeQualityOptions
        // and moving this option to CodeQualityOptions.
        internal static readonly PerLanguageOption2<CodeStyleOption2<UnusedParametersPreference>> UnusedParameters = CreateOption(
            CodeStyleOptionGroups.Parameter,
            nameof(UnusedParameters),
            defaultValue: s_preferAllMethodsUnusedParametersPreference,
            storageLocations: new OptionStorageLocation2[]{
                new EditorConfigStorageLocation<CodeStyleOption2<UnusedParametersPreference>>(
                        "dotnet_code_quality_unused_parameters",
                        ParseUnusedParametersPreference,
                        o => GetUnusedParametersPreferenceEditorConfigString(o, s_preferAllMethodsUnusedParametersPreference.Value)),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(UnusedParameters)}Preference") });

        private static readonly CodeStyleOption2<AccessibilityModifiersRequired> s_requireAccessibilityModifiersDefault =
            new CodeStyleOption2<AccessibilityModifiersRequired>(AccessibilityModifiersRequired.ForNonInterfaceMembers, NotificationOption2.Silent);

        internal static readonly PerLanguageOption2<CodeStyleOption2<AccessibilityModifiersRequired>> RequireAccessibilityModifiers =
            CreateOption(
                CodeStyleOptionGroups.Modifier, nameof(RequireAccessibilityModifiers),
                defaultValue: s_requireAccessibilityModifiersDefault,
                storageLocations: new OptionStorageLocation2[]{
                    new EditorConfigStorageLocation<CodeStyleOption2<AccessibilityModifiersRequired>>(
                        "dotnet_style_require_accessibility_modifiers",
                        s => ParseAccessibilityModifiersRequired(s),
                        GetAccessibilityModifiersRequiredEditorConfigString),
                    new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.RequireAccessibilityModifiers")});

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferReadonly = CreateOption(
            CodeStyleOptionGroups.Field, nameof(PreferReadonly),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_readonly_field"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferReadonly") });

        internal static readonly Option2<string> FileHeaderTemplate = CreateCommonOption(
            CodeStyleOptionGroups.Usings, nameof(FileHeaderTemplate),
            defaultValue: "",
            EditorConfigStorageLocation.ForStringOption("file_header_template", emptyStringRepresentation: "unset"));

        internal static readonly Option2<string> RemoveUnnecessarySuppressionExclusions = CreateCommonOption(
            CodeStyleOptionGroups.Suppressions,
            nameof(RemoveUnnecessarySuppressionExclusions),
            defaultValue: "",
            storageLocations: new OptionStorageLocation2[]{
                EditorConfigStorageLocation.ForStringOption("dotnet_remove_unnecessary_suppression_exclusions", emptyStringRepresentation: "none"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.RemoveUnnecessarySuppressionExclusions") });

        private static readonly BidirectionalMap<string, AccessibilityModifiersRequired> s_accessibilityModifiersRequiredMap =
            new BidirectionalMap<string, AccessibilityModifiersRequired>(new[]
            {
                KeyValuePairUtil.Create("never", AccessibilityModifiersRequired.Never),
                KeyValuePairUtil.Create("always", AccessibilityModifiersRequired.Always),
                KeyValuePairUtil.Create("for_non_interface_members", AccessibilityModifiersRequired.ForNonInterfaceMembers),
                KeyValuePairUtil.Create("omit_if_default", AccessibilityModifiersRequired.OmitIfDefault),
            });

        private static CodeStyleOption2<AccessibilityModifiersRequired> ParseAccessibilityModifiersRequired(string optionString)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(optionString,
                    out var value, out var notificationOpt))
            {
                if (value == "never")
                {
                    // If they provide 'never', they don't need a notification level.
                    notificationOpt ??= NotificationOption2.Silent;
                }

                if (notificationOpt is object)
                {
                    Debug.Assert(s_accessibilityModifiersRequiredMap.ContainsKey(value));
                    return new CodeStyleOption2<AccessibilityModifiersRequired>(s_accessibilityModifiersRequiredMap.GetValueOrDefault(value), notificationOpt);
                }
            }

            return s_requireAccessibilityModifiersDefault;
        }

        private static string GetAccessibilityModifiersRequiredEditorConfigString(CodeStyleOption2<AccessibilityModifiersRequired> option)
        {
            // If they provide 'never', they don't need a notification level.
            if (option.Notification == null)
            {
                Debug.Assert(s_accessibilityModifiersRequiredMap.ContainsValue(AccessibilityModifiersRequired.Never));
                return s_accessibilityModifiersRequiredMap.GetKeyOrDefault(AccessibilityModifiersRequired.Never);
            }

            Debug.Assert(s_accessibilityModifiersRequiredMap.ContainsValue(option.Value));
            return $"{s_accessibilityModifiersRequiredMap.GetKeyOrDefault(option.Value)}:{option.Notification.ToEditorConfigString()}";
        }

        private static readonly CodeStyleOption2<ParenthesesPreference> s_alwaysForClarityPreference =
            new CodeStyleOption2<ParenthesesPreference>(ParenthesesPreference.AlwaysForClarity, NotificationOption2.Silent);

        private static readonly CodeStyleOption2<ParenthesesPreference> s_neverIfUnnecessaryPreference =
            new CodeStyleOption2<ParenthesesPreference>(ParenthesesPreference.NeverIfUnnecessary, NotificationOption2.Silent);

        private static PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> CreateParenthesesOption(
            string fieldName, CodeStyleOption2<ParenthesesPreference> defaultValue,
            string styleName)
        {
            return CreateOption(
                CodeStyleOptionGroups.Parentheses, fieldName, defaultValue,
                storageLocations: new OptionStorageLocation2[]{
                    new EditorConfigStorageLocation<CodeStyleOption2<ParenthesesPreference>>(
                        styleName,
                        s => ParseParenthesesPreference(s, defaultValue),
                        v => GetParenthesesPreferenceEditorConfigString(v)),
                    new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{fieldName}Preference")});
        }

        internal static readonly PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> ArithmeticBinaryParentheses =
            CreateParenthesesOption(
                nameof(ArithmeticBinaryParentheses),
                s_alwaysForClarityPreference,
                "dotnet_style_parentheses_in_arithmetic_binary_operators");

        internal static readonly PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> OtherBinaryParentheses =
            CreateParenthesesOption(
                nameof(OtherBinaryParentheses),
                s_alwaysForClarityPreference,
                "dotnet_style_parentheses_in_other_binary_operators");

        internal static readonly PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> RelationalBinaryParentheses =
            CreateParenthesesOption(
                nameof(RelationalBinaryParentheses),
                s_alwaysForClarityPreference,
                "dotnet_style_parentheses_in_relational_binary_operators");

        internal static readonly PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> OtherParentheses =
            CreateParenthesesOption(
                nameof(OtherParentheses),
                s_neverIfUnnecessaryPreference,
                "dotnet_style_parentheses_in_other_operators");

        private static readonly BidirectionalMap<string, ParenthesesPreference> s_parenthesesPreferenceMap =
            new BidirectionalMap<string, ParenthesesPreference>(new[]
            {
                KeyValuePairUtil.Create("always_for_clarity", ParenthesesPreference.AlwaysForClarity),
                KeyValuePairUtil.Create("never_if_unnecessary", ParenthesesPreference.NeverIfUnnecessary),
            });

        private static readonly BidirectionalMap<string, UnusedParametersPreference> s_unusedParametersPreferenceMap =
            new BidirectionalMap<string, UnusedParametersPreference>(new[]
            {
                KeyValuePairUtil.Create("non_public", UnusedParametersPreference.NonPublicMethods),
                KeyValuePairUtil.Create("all", UnusedParametersPreference.AllMethods),
            });

        internal static readonly PerLanguageOption2<CodeStyleOption2<bool>> PreferSystemHashCode = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            nameof(PreferSystemHashCode),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation2[]{
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferSystemHashCode") });

        static CodeStyleOptions2()
        {
            // Note that the static constructor executes after all the static field initializers for the options have executed,
            // and each field initializer adds the created option to s_allOptionsBuilder.
            AllOptions = s_allOptionsBuilder.ToImmutable();
        }

        private static Optional<CodeStyleOption2<ParenthesesPreference>> ParseParenthesesPreference(
            string optionString, Optional<CodeStyleOption2<ParenthesesPreference>> defaultValue)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(optionString,
                    out var value, out var notificationOpt))
            {
                Debug.Assert(s_parenthesesPreferenceMap.ContainsKey(value));
                return new CodeStyleOption2<ParenthesesPreference>(s_parenthesesPreferenceMap.GetValueOrDefault(value),
                                                                  notificationOpt ?? NotificationOption2.Silent);
            }

            return defaultValue;
        }

        private static string GetParenthesesPreferenceEditorConfigString(CodeStyleOption2<ParenthesesPreference> option)
        {
            Debug.Assert(s_parenthesesPreferenceMap.ContainsValue(option.Value));
            var value = s_parenthesesPreferenceMap.GetKeyOrDefault(option.Value) ?? s_parenthesesPreferenceMap.GetKeyOrDefault(ParenthesesPreference.AlwaysForClarity);
            return option.Notification == null ? value : $"{value}:{option.Notification.ToEditorConfigString()}";
        }

        private static Optional<CodeStyleOption2<UnusedParametersPreference>> ParseUnusedParametersPreference(string optionString)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(optionString,
                out var value, out var notificationOpt))
            {
                return new CodeStyleOption2<UnusedParametersPreference>(
                    s_unusedParametersPreferenceMap.GetValueOrDefault(value), notificationOpt ?? NotificationOption2.Suggestion);
            }

            return s_preferNoneUnusedParametersPreference;
        }

        private static string GetUnusedParametersPreferenceEditorConfigString(CodeStyleOption2<UnusedParametersPreference> option, UnusedParametersPreference defaultPreference)
        {
            Debug.Assert(s_unusedParametersPreferenceMap.ContainsValue(option.Value));
            var value = s_unusedParametersPreferenceMap.GetKeyOrDefault(option.Value) ?? s_unusedParametersPreferenceMap.GetKeyOrDefault(defaultPreference);
            return option.Notification == null ? value : $"{value}:{option.Notification.ToEditorConfigString()}";
        }
    }

    internal static class CodeStyleOptionGroups
    {
        public static readonly OptionGroup Usings = new OptionGroup(CompilerExtensionsResources.Organize_usings, priority: 1);
        public static readonly OptionGroup ThisOrMe = new OptionGroup(CompilerExtensionsResources.this_dot_and_Me_dot_preferences, priority: 2);
        public static readonly OptionGroup PredefinedTypeNameUsage = new OptionGroup(CompilerExtensionsResources.Language_keywords_vs_BCL_types_preferences, priority: 3);
        public static readonly OptionGroup Parentheses = new OptionGroup(CompilerExtensionsResources.Parentheses_preferences, priority: 4);
        public static readonly OptionGroup Modifier = new OptionGroup(CompilerExtensionsResources.Modifier_preferences, priority: 5);
        public static readonly OptionGroup ExpressionLevelPreferences = new OptionGroup(CompilerExtensionsResources.Expression_level_preferences, priority: 6);
        public static readonly OptionGroup Field = new OptionGroup(CompilerExtensionsResources.Field_preferences, priority: 7);
        public static readonly OptionGroup Parameter = new OptionGroup(CompilerExtensionsResources.Parameter_preferences, priority: 8);
        public static readonly OptionGroup Suppressions = new OptionGroup(CompilerExtensionsResources.Suppression_preferences, priority: 9);
    }
}
