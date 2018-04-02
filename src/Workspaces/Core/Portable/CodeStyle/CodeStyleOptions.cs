// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Options;
using static Microsoft.CodeAnalysis.CodeStyle.CodeStyleHelpers;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    public class CodeStyleOptions
    {
        /// <remarks>
        /// When user preferences are not yet set for a style, we fall back to the default value.
        /// One such default(s), is that the feature is turned on, so that codegen consumes it,
        /// but with none enforcement, so that the user is not prompted about their usage.
        /// </remarks>
        internal static readonly CodeStyleOption<bool> TrueWithNoneEnforcement = new CodeStyleOption<bool>(value: true, notification: NotificationOption.None);
        internal static readonly CodeStyleOption<bool> FalseWithNoneEnforcement = new CodeStyleOption<bool>(value: false, notification: NotificationOption.None);
        internal static readonly CodeStyleOption<bool> TrueWithSuggestionEnforcement = new CodeStyleOption<bool>(value: true, notification: NotificationOption.Suggestion);
        internal static readonly CodeStyleOption<bool> FalseWithSuggestionEnforcement = new CodeStyleOption<bool>(value: false, notification: NotificationOption.Suggestion);

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in field access expressions.
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> QualifyFieldAccess = new PerLanguageOption<CodeStyleOption<bool>>(nameof(CodeStyleOptions), nameof(QualifyFieldAccess), defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_qualification_for_field"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyFieldAccess")});

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in property access expressions.
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> QualifyPropertyAccess = new PerLanguageOption<CodeStyleOption<bool>>(nameof(CodeStyleOptions), nameof(QualifyPropertyAccess), defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_qualification_for_property"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyPropertyAccess")});

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in method access expressions.
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> QualifyMethodAccess = new PerLanguageOption<CodeStyleOption<bool>>(nameof(CodeStyleOptions), nameof(QualifyMethodAccess), defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_qualification_for_method"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyMethodAccess")});

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in event access expressions.
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> QualifyEventAccess = new PerLanguageOption<CodeStyleOption<bool>>(nameof(CodeStyleOptions), nameof(QualifyEventAccess), defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_qualification_for_event"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyEventAccess")});

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Declarations
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> PreferIntrinsicPredefinedTypeKeywordInDeclaration = new PerLanguageOption<CodeStyleOption<bool>>(nameof(CodeStyleOptions), nameof(PreferIntrinsicPredefinedTypeKeywordInDeclaration), defaultValue: TrueWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_predefined_type_for_locals_parameters_members"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferIntrinsicPredefinedTypeKeywordInDeclaration.CodeStyle")});

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Member Access Expression
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> PreferIntrinsicPredefinedTypeKeywordInMemberAccess = new PerLanguageOption<CodeStyleOption<bool>>(nameof(CodeStyleOptions), nameof(PreferIntrinsicPredefinedTypeKeywordInMemberAccess), defaultValue: TrueWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_predefined_type_for_member_access"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferIntrinsicPredefinedTypeKeywordInMemberAccess.CodeStyle")});

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferThrowExpression = new PerLanguageOption<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions),
            nameof(PreferThrowExpression),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_throw_expression"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferThrowExpression")});

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferObjectInitializer = new PerLanguageOption<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions),
            nameof(PreferObjectInitializer),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_object_initializer"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferObjectInitializer")});

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferCollectionInitializer = new PerLanguageOption<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions),
            nameof(PreferCollectionInitializer),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_collection_initializer"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferCollectionInitializer")});

        internal static readonly PerLanguageOption<bool> PreferObjectInitializer_FadeOutCode = new PerLanguageOption<bool>(
            nameof(CodeStyleOptions),
            nameof(PreferObjectInitializer_FadeOutCode),
            defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferObjectInitializer_FadeOutCode"));

        internal static readonly PerLanguageOption<bool> PreferCollectionInitializer_FadeOutCode = new PerLanguageOption<bool>(
            nameof(CodeStyleOptions),
            nameof(PreferCollectionInitializer_FadeOutCode),
            defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferCollectionInitializer_FadeOutCode"));

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferCoalesceExpression = new PerLanguageOption<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions),
            nameof(PreferCoalesceExpression),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_coalesce_expression"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferCoalesceExpression") });

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferNullPropagation = new PerLanguageOption<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions),
            nameof(PreferNullPropagation),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_null_propagation"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferNullPropagation") });

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferInlinedVariableDeclaration = new PerLanguageOption<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions),
            nameof(PreferInlinedVariableDeclaration),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_inlined_variable_declaration"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferInlinedVariableDeclaration") });

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferDeconstructedVariableDeclaration = new PerLanguageOption<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions),
            nameof(PreferDeconstructedVariableDeclaration),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_deconstructed_variable_declaration"),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(PreferDeconstructedVariableDeclaration)}")});

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferExplicitTupleNames = new PerLanguageOption<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions),
            nameof(PreferExplicitTupleNames),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_explicit_tuple_names"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferExplicitTupleNames") });

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferAutoProperties = new PerLanguageOption<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions),
            nameof(PreferAutoProperties),
            defaultValue: TrueWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_auto_properties"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferAutoProperties") });

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferInferredTupleNames = new PerLanguageOption<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions),
            nameof(PreferInferredTupleNames),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_inferred_tuple_names"),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(PreferInferredTupleNames)}") });

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferInferredAnonymousTypeMemberNames = new PerLanguageOption<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions),
            nameof(PreferInferredAnonymousTypeMemberNames),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_inferred_anonymous_type_member_names"),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(PreferInferredAnonymousTypeMemberNames)}") });

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferIsNullCheckOverReferenceEqualityMethod = new PerLanguageOption<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions),
            nameof(PreferIsNullCheckOverReferenceEqualityMethod),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_prefer_is_null_check_over_reference_equality_method"),
                new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(PreferIsNullCheckOverReferenceEqualityMethod)}") });

        private static readonly CodeStyleOption<AccessibilityModifiersRequired> s_requireAccessibilityModifiersDefault =
            new CodeStyleOption<AccessibilityModifiersRequired>(AccessibilityModifiersRequired.ForNonInterfaceMembers, NotificationOption.None);

        internal static readonly PerLanguageOption<CodeStyleOption<AccessibilityModifiersRequired>> RequireAccessibilityModifiers = 
            new PerLanguageOption<CodeStyleOption<AccessibilityModifiersRequired>>(
                nameof(CodeStyleOptions), nameof(RequireAccessibilityModifiers), defaultValue: s_requireAccessibilityModifiersDefault,
                storageLocations: new OptionStorageLocation[]{
                    new EditorConfigStorageLocation<CodeStyleOption<AccessibilityModifiersRequired>>("dotnet_style_require_accessibility_modifiers", s => ParseAccessibilityModifiersRequired(s)),
                    new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.RequireAccessibilityModifiers")});

        internal static readonly PerLanguageOption<CodeStyleOption<bool>> PreferReadonly = new PerLanguageOption<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions),
            nameof(PreferReadonly),
            defaultValue: TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("dotnet_style_readonly_field"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferReadonly") });

        private static CodeStyleOption<AccessibilityModifiersRequired> ParseAccessibilityModifiersRequired(string optionString)
        {
            if (TryGetCodeStyleValueAndOptionalNotification(optionString,
                    out var value, out var notificationOpt))
            {
                if (value == "never")
                {
                    // If they provide 'never', they don't need a notification level.
                    notificationOpt = notificationOpt ?? NotificationOption.None;
                }

                if (notificationOpt != null)
                {
                    switch (value)
                    {
                        case "never":
                            return new CodeStyleOption<AccessibilityModifiersRequired>(AccessibilityModifiersRequired.Never, notificationOpt);
                        case "always":
                            return new CodeStyleOption<AccessibilityModifiersRequired>(AccessibilityModifiersRequired.Always, notificationOpt);
                        case "for_non_interface_members":
                            return new CodeStyleOption<AccessibilityModifiersRequired>(AccessibilityModifiersRequired.ForNonInterfaceMembers, notificationOpt);
                        case "omit_if_default":
                            return new CodeStyleOption<AccessibilityModifiersRequired>(AccessibilityModifiersRequired.OmitIfDefault, notificationOpt);
                    }
                }
            }

            return s_requireAccessibilityModifiersDefault;
        }
    }
}
