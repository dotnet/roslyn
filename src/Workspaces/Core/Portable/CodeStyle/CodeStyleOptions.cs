// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeStyle.CodeStyleHelpers;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    public class CodeStyleOptions
    {
        private static readonly ImmutableArray<IOption>.Builder s_allOptionsBuilder = ImmutableArray.CreateBuilder<IOption>();

        internal static ImmutableArray<IOption> AllOptions { get; }

        private static PerLanguageOption<T> CreateOption<T>(OptionGroup group, string name, T defaultValue, params OptionStorageLocation[] storageLocations)
        {
            var option = new PerLanguageOption<T>(nameof(CodeStyleOptions), group, name, defaultValue, storageLocations);
            s_allOptionsBuilder.Add(option);
            return option;
        }

        /// <remarks>
        /// When user preferences are not yet set for a style, we fall back to the default value.
        /// One such default(s), is that the feature is turned on, so that codegen consumes it,
        /// but with silent enforcement, so that the user is not prompted about their usage.
        /// </remarks>
        internal static readonly CodeStyleOption<bool> TrueWithSilentEnforcement = new CodeStyleOption<bool>(value: true, notification: NotificationOption.Silent);
        internal static readonly CodeStyleOption<bool> FalseWithSilentEnforcement = new CodeStyleOption<bool>(value: false, notification: NotificationOption.Silent);
        internal static readonly CodeStyleOption<bool> TrueWithSuggestionEnforcement = new CodeStyleOption<bool>(value: true, notification: NotificationOption.Suggestion);
        internal static readonly CodeStyleOption<bool> FalseWithSuggestionEnforcement = new CodeStyleOption<bool>(value: false, notification: NotificationOption.Suggestion);

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in field access expressions.
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> QualifyFieldAccess = CreateOption(
            CodeStyleOptionGroups.ThisOrMe, nameof(QualifyFieldAccess),
            defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_qualification_for_field"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyFieldAccess")});

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in property access expressions.
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> QualifyPropertyAccess = CreateOption(
            CodeStyleOptionGroups.ThisOrMe, nameof(QualifyPropertyAccess),
            defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_qualification_for_property"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyPropertyAccess")});

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in method access expressions.
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> QualifyMethodAccess = CreateOption(
            CodeStyleOptionGroups.ThisOrMe, nameof(QualifyMethodAccess),
            defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_qualification_for_method"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyMethodAccess")});

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in event access expressions.
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> QualifyEventAccess = CreateOption(
            CodeStyleOptionGroups.ThisOrMe, nameof(QualifyEventAccess),
            defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_qualification_for_event"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyEventAccess")});

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Declarations
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> PreferIntrinsicPredefinedTypeKeywordInDeclaration = CreateOption(
            CodeStyleOptionGroups.PredefinedTypeNameUsage, nameof(PreferIntrinsicPredefinedTypeKeywordInDeclaration),
            defaultValue: TrueWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_predefined_type_for_locals_parameters_members"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferIntrinsicPredefinedTypeKeywordInDeclaration.CodeStyle")});

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Member Access Expression
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> PreferIntrinsicPredefinedTypeKeywordInMemberAccess = CreateOption(
            CodeStyleOptionGroups.PredefinedTypeNameUsage, nameof(PreferIntrinsicPredefinedTypeKeywordInMemberAccess),
            defaultValue: TrueWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_predefined_type_for_member_access"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferIntrinsicPredefinedTypeKeywordInMemberAccess.CodeStyle")});

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferThrowExpression = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferThrowExpression),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_throw_expression"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferThrowExpression")});

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferObjectInitializer = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferObjectInitializer),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_object_initializer"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferObjectInitializer")});

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferCollectionInitializer = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferCollectionInitializer),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_collection_initializer"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferCollectionInitializer")});

        // TODO: Should both the below "_FadeOutCode" options be added to AllOptions?
        internal static readonly PerLanguageOption<bool> PreferObjectInitializer_FadeOutCode = new PerLanguageOption<bool>(
            nameof(CodeStyleOptions), nameof(PreferObjectInitializer_FadeOutCode),
            defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferObjectInitializer_FadeOutCode"));

        internal static readonly PerLanguageOption<bool> PreferCollectionInitializer_FadeOutCode = new PerLanguageOption<bool>(
            nameof(CodeStyleOptions), nameof(PreferCollectionInitializer_FadeOutCode),
            defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferCollectionInitializer_FadeOutCode"));

        internal static readonly PerLanguageOption<OperatorPlacementWhenWrappingPreference> OperatorPlacementWhenWrapping =
            new PerLanguageOption<OperatorPlacementWhenWrappingPreference>(
                nameof(CodeStyleOptions), nameof(OperatorPlacementWhenWrapping),
                defaultValue: OperatorPlacementWhenWrappingPreference.BeginningOfLine,
                storageLocations:
                    new EditorConfigStorageLocation<OperatorPlacementWhenWrappingPreference>(
                        "dotnet_style_operator_placement_when_wrapping",
                        OperatorPlacementUtilities.Parse,
                        OperatorPlacementUtilities.GetEditorConfigString));

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferCoalesceExpression = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferCoalesceExpression),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_coalesce_expression"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferCoalesceExpression") });

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferNullPropagation = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferNullPropagation),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_null_propagation"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferNullPropagation") });

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferInlinedVariableDeclaration = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferInlinedVariableDeclaration),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_inlined_variable_declaration"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferInlinedVariableDeclaration") });

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferDeconstructedVariableDeclaration = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferDeconstructedVariableDeclaration),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_deconstructed_variable_declaration"),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(PreferDeconstructedVariableDeclaration)}")});

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferExplicitTupleNames = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferExplicitTupleNames),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_explicit_tuple_names"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferExplicitTupleNames") });

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferAutoProperties = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferAutoProperties),
            defaultValue: TrueWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_auto_properties"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferAutoProperties") });

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferInferredTupleNames = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferInferredTupleNames),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_inferred_tuple_names"),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(PreferInferredTupleNames)}") });

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferInferredAnonymousTypeMemberNames = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferInferredAnonymousTypeMemberNames),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_inferred_anonymous_type_member_names"),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(PreferInferredAnonymousTypeMemberNames)}") });

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferIsNullCheckOverReferenceEqualityMethod = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferIsNullCheckOverReferenceEqualityMethod),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_is_null_check_over_reference_equality_method"),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(PreferIsNullCheckOverReferenceEqualityMethod)}") });

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferConditionalExpressionOverAssignment = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferConditionalExpressionOverAssignment),
            defaultValue: TrueWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_conditional_expression_over_assignment"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferConditionalExpressionOverAssignment")});

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferConditionalExpressionOverReturn = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferConditionalExpressionOverReturn),
            defaultValue: TrueWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_conditional_expression_over_return"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferConditionalExpressionOverReturn")});

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferCompoundAssignment = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            nameof(PreferCompoundAssignment),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_compound_assignment"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferCompoundAssignment") });

        private static readonly CodeStyleOption<UnusedParametersPreference> s_preferNoneUnusedParametersPreference =
            new CodeStyleOption<UnusedParametersPreference>(default, NotificationOption.None);
        private static readonly CodeStyleOption<UnusedParametersPreference> s_preferAllMethodsUnusedParametersPreference =
            new CodeStyleOption<UnusedParametersPreference>(UnusedParametersPreference.AllMethods, NotificationOption.Suggestion);

        // TODO: https://github.com/dotnet/roslyn/issues/31225 tracks adding CodeQualityOption<T> and CodeQualityOptions
        // and moving this option to CodeQualityOptions.
        internal static readonly PerLanguageOption<CodeStyleOption<UnusedParametersPreference>> UnusedParameters = CreateOption(
            CodeStyleOptionGroups.Parameter,
            nameof(UnusedParameters),
            defaultValue: s_preferAllMethodsUnusedParametersPreference,
            storageLocations: new OptionStorageLocation[]{
                new EditorConfigStorageLocation<CodeStyleOption<UnusedParametersPreference>>(
                        "dotnet_code_quality_unused_parameters",
                        ParseUnusedParametersPreference,
                        o => GetUnusedParametersPreferenceEditorConfigString(o, s_preferAllMethodsUnusedParametersPreference.Value)),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(UnusedParameters)}Preference") });

        private static readonly CodeStyleOption<AccessibilityModifiersRequired> s_requireAccessibilityModifiersDefault =
            new CodeStyleOption<AccessibilityModifiersRequired>(AccessibilityModifiersRequired.ForNonInterfaceMembers, NotificationOption.Silent);

        internal static readonly PerLanguageOption<CodeStyleOption<AccessibilityModifiersRequired>> RequireAccessibilityModifiers =
            CreateOption(
                CodeStyleOptionGroups.Modifier, nameof(RequireAccessibilityModifiers),
                defaultValue: s_requireAccessibilityModifiersDefault,
                storageLocations: new OptionStorageLocation[]{
                    new EditorConfigStorageLocation<CodeStyleOption<AccessibilityModifiersRequired>>(
                        "dotnet_style_require_accessibility_modifiers",
                        s => ParseAccessibilityModifiersRequired(s),
                        GetAccessibilityModifiersRequiredEditorConfigString),
                    new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.RequireAccessibilityModifiers")});

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferReadonly = CreateOption(
            CodeStyleOptionGroups.Field, nameof(PreferReadonly),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_readonly_field"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferReadonly") });

        private static readonly BidirectionalMap<string, AccessibilityModifiersRequired> s_accessibilityModifiersRequiredMap =
            new BidirectionalMap<string, AccessibilityModifiersRequired>(new[]
            {
                KeyValuePairUtil.Create("never", AccessibilityModifiersRequired.Never),
                KeyValuePairUtil.Create("always", AccessibilityModifiersRequired.Always),
                KeyValuePairUtil.Create("for_non_interface_members", AccessibilityModifiersRequired.ForNonInterfaceMembers),
                KeyValuePairUtil.Create("omit_if_default", AccessibilityModifiersRequired.OmitIfDefault),
            });

        private static CodeStyleOption<AccessibilityModifiersRequired> ParseAccessibilityModifiersRequired(string optionString)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(optionString,
                    out var value, out var notificationOpt))
            {
                if (value == "never")
                {
                    // If they provide 'never', they don't need a notification level.
                    notificationOpt ??= NotificationOption.Silent;
                }

                if (notificationOpt != null)
                {
                    Debug.Assert(s_accessibilityModifiersRequiredMap.ContainsKey(value));
                    return new CodeStyleOption<AccessibilityModifiersRequired>(s_accessibilityModifiersRequiredMap.GetValueOrDefault(value), notificationOpt);
                }
            }

            return s_requireAccessibilityModifiersDefault;
        }

        private static string GetAccessibilityModifiersRequiredEditorConfigString(CodeStyleOption<AccessibilityModifiersRequired> option)
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

        private static readonly CodeStyleOption<ParenthesesPreference> s_alwaysForClarityPreference =
            new CodeStyleOption<ParenthesesPreference>(ParenthesesPreference.AlwaysForClarity, NotificationOption.Silent);

        private static readonly CodeStyleOption<ParenthesesPreference> s_neverIfUnnecessaryPreference =
            new CodeStyleOption<ParenthesesPreference>(ParenthesesPreference.NeverIfUnnecessary, NotificationOption.Silent);

        private static PerLanguageOption<CodeStyleOption<ParenthesesPreference>> CreateParenthesesOption(
            string fieldName, CodeStyleOption<ParenthesesPreference> defaultValue,
            string styleName)
        {
            return CreateOption(
                CodeStyleOptionGroups.Parentheses, fieldName, defaultValue,
                storageLocations: new OptionStorageLocation[]{
                    new EditorConfigStorageLocation<CodeStyleOption<ParenthesesPreference>>(
                        styleName,
                        s => ParseParenthesesPreference(s, defaultValue),
                        v => GetParenthesesPreferenceEditorConfigString(v)),
                    new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{fieldName}Preference")});
        }

        internal static readonly PerLanguageOption<CodeStyleOption<ParenthesesPreference>> ArithmeticBinaryParentheses =
            CreateParenthesesOption(
                nameof(ArithmeticBinaryParentheses),
                s_alwaysForClarityPreference,
                "dotnet_style_parentheses_in_arithmetic_binary_operators");

        internal static readonly PerLanguageOption<CodeStyleOption<ParenthesesPreference>> OtherBinaryParentheses =
            CreateParenthesesOption(
                nameof(OtherBinaryParentheses),
                s_alwaysForClarityPreference,
                "dotnet_style_parentheses_in_other_binary_operators");

        internal static readonly PerLanguageOption<CodeStyleOption<ParenthesesPreference>> RelationalBinaryParentheses =
            CreateParenthesesOption(
                nameof(RelationalBinaryParentheses),
                s_alwaysForClarityPreference,
                "dotnet_style_parentheses_in_relational_binary_operators");

        internal static readonly PerLanguageOption<CodeStyleOption<ParenthesesPreference>> OtherParentheses =
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

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferSystemHashCode = CreateOption(
            CodeStyleOptionGroups.ExpressionLevelPreferences,
            nameof(PreferSystemHashCode),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_prefer_system_hashcode"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferSystemHashCode") });

        static CodeStyleOptions()
        {
            // Note that the static constructor executes after all the static field initializers for the options have executed,
            // and each field initializer adds the created option to s_allOptionsBuilder.
            AllOptions = s_allOptionsBuilder.ToImmutable();
        }

        private static Optional<CodeStyleOption<ParenthesesPreference>> ParseParenthesesPreference(
            string optionString, Optional<CodeStyleOption<ParenthesesPreference>> defaultValue)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(optionString,
                    out var value, out var notificationOpt))
            {
                Debug.Assert(s_parenthesesPreferenceMap.ContainsKey(value));
                return new CodeStyleOption<ParenthesesPreference>(s_parenthesesPreferenceMap.GetValueOrDefault(value),
                                                                  notificationOpt ?? NotificationOption.Silent);
            }

            return defaultValue;
        }

        private static string GetParenthesesPreferenceEditorConfigString(CodeStyleOption<ParenthesesPreference> option)
        {
            Debug.Assert(s_parenthesesPreferenceMap.ContainsValue(option.Value));
            var value = s_parenthesesPreferenceMap.GetKeyOrDefault(option.Value) ?? s_parenthesesPreferenceMap.GetKeyOrDefault(ParenthesesPreference.AlwaysForClarity);
            return option.Notification == null ? value : $"{value}:{option.Notification.ToEditorConfigString()}";
        }

        private static Optional<CodeStyleOption<UnusedParametersPreference>> ParseUnusedParametersPreference(string optionString)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(optionString,
                out var value, out var notificationOpt))
            {
                return new CodeStyleOption<UnusedParametersPreference>(
                    s_unusedParametersPreferenceMap.GetValueOrDefault(value), notificationOpt ?? NotificationOption.Suggestion);
            }

            return s_preferNoneUnusedParametersPreference;
        }

        private static string GetUnusedParametersPreferenceEditorConfigString(CodeStyleOption<UnusedParametersPreference> option, UnusedParametersPreference defaultPreference)
        {
            Debug.Assert(s_unusedParametersPreferenceMap.ContainsValue(option.Value));
            var value = s_unusedParametersPreferenceMap.GetKeyOrDefault(option.Value) ?? s_unusedParametersPreferenceMap.GetKeyOrDefault(defaultPreference);
            return option.Notification == null ? value : $"{value}:{option.Notification.ToEditorConfigString()}";
        }
    }

    internal static class CodeStyleOptionGroups
    {
        public static readonly OptionGroup Usings = new OptionGroup(WorkspacesResources.Organize_usings, priority: 1);
        public static readonly OptionGroup ThisOrMe = new OptionGroup(WorkspacesResources.this_dot_and_Me_dot_preferences, priority: 2);
        public static readonly OptionGroup PredefinedTypeNameUsage = new OptionGroup(WorkspacesResources.Language_keywords_vs_BCL_types_preferences, priority: 3);
        public static readonly OptionGroup Parentheses = new OptionGroup(WorkspacesResources.Parentheses_preferences, priority: 4);
        public static readonly OptionGroup Modifier = new OptionGroup(WorkspacesResources.Modifier_preferences, priority: 5);
        public static readonly OptionGroup ExpressionLevelPreferences = new OptionGroup(WorkspacesResources.Expression_level_preferences, priority: 6);
        public static readonly OptionGroup Field = new OptionGroup(WorkspacesResources.Field_preferences, priority: 7);
        public static readonly OptionGroup Parameter = new OptionGroup(WorkspacesResources.Parameter_preferences, priority: 8);
    }
}
