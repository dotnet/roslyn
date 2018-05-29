// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    internal static class CSharpCodeStyleOptions
    {
        public static readonly Option<CodeStyleOption<bool>> UseImplicitTypeForIntrinsicTypes = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(UseImplicitTypeForIntrinsicTypes), defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_var_for_built_in_types"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.UseImplicitTypeForIntrinsicTypes")});

        public static readonly Option<CodeStyleOption<bool>> UseImplicitTypeWhereApparent = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(UseImplicitTypeWhereApparent), defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_var_when_type_is_apparent"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.UseImplicitTypeWhereApparent")});

        public static readonly Option<CodeStyleOption<bool>> UseImplicitTypeWherePossible = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(UseImplicitTypeWherePossible), defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_var_elsewhere"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.UseImplicitTypeWherePossible")});

        public static readonly Option<CodeStyleOption<bool>> PreferConditionalDelegateCall = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferConditionalDelegateCall), defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_conditional_delegate_call"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.PreferConditionalDelegateCall")});

        public static readonly Option<CodeStyleOption<bool>> PreferPatternMatchingOverAsWithNullCheck = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferPatternMatchingOverAsWithNullCheck), defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_pattern_matching_over_as_with_null_check"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferPatternMatchingOverAsWithNullCheck)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferPatternMatchingOverIsWithCastCheck = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferPatternMatchingOverIsWithCastCheck), defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_pattern_matching_over_is_with_cast_check"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferPatternMatchingOverIsWithCastCheck)}")});

        public static readonly CodeStyleOption<ExpressionBodyPreference> NeverWithSilentEnforcement =
            new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.Never, NotificationOption.Silent);

        public static readonly CodeStyleOption<ExpressionBodyPreference> NeverWithSuggestionEnforcement =
            new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.Never, NotificationOption.Suggestion);

        public static readonly CodeStyleOption<ExpressionBodyPreference> WhenPossibleWithSilentEnforcement =
            new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption.Silent);

        public static readonly CodeStyleOption<ExpressionBodyPreference> WhenPossibleWithSuggestionEnforcement =
            new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption.Suggestion);

        public static readonly CodeStyleOption<ExpressionBodyPreference> WhenOnSingleLineWithSilentEnforcement =
            new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.WhenOnSingleLine, NotificationOption.Silent);

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedConstructors = new Option<CodeStyleOption<ExpressionBodyPreference>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedConstructors),
            defaultValue: NeverWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<CodeStyleOption<ExpressionBodyPreference>>("csharp_style_expression_bodied_constructors", s => ParseExpressionBodyPreference(s, NeverWithSilentEnforcement)),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedConstructors)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedMethods = new Option<CodeStyleOption<ExpressionBodyPreference>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedMethods),
            defaultValue: NeverWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<CodeStyleOption<ExpressionBodyPreference>>("csharp_style_expression_bodied_methods", s => ParseExpressionBodyPreference(s, NeverWithSilentEnforcement)),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedMethods)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedOperators = new Option<CodeStyleOption<ExpressionBodyPreference>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedOperators),
            defaultValue: NeverWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<CodeStyleOption<ExpressionBodyPreference>>("csharp_style_expression_bodied_operators", s => ParseExpressionBodyPreference(s, NeverWithSilentEnforcement)),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedOperators)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedProperties = new Option<CodeStyleOption<ExpressionBodyPreference>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedProperties),
            defaultValue: WhenPossibleWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<CodeStyleOption<ExpressionBodyPreference>>("csharp_style_expression_bodied_properties", s => ParseExpressionBodyPreference(s, WhenPossibleWithSilentEnforcement)),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedProperties)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedIndexers = new Option<CodeStyleOption<ExpressionBodyPreference>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedIndexers),
            defaultValue: WhenPossibleWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<CodeStyleOption<ExpressionBodyPreference>>("csharp_style_expression_bodied_indexers", s => ParseExpressionBodyPreference(s, WhenPossibleWithSilentEnforcement)),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedIndexers)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedAccessors = new Option<CodeStyleOption<ExpressionBodyPreference>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedAccessors), 
            defaultValue: WhenPossibleWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<CodeStyleOption<ExpressionBodyPreference>>("csharp_style_expression_bodied_accessors", s => ParseExpressionBodyPreference(s, WhenPossibleWithSilentEnforcement)),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedAccessors)}")});

        public static CodeStyleOption<ExpressionBodyPreference> ParseExpressionBodyPreference(
            string optionString, CodeStyleOption<ExpressionBodyPreference> @default)
        {
            // optionString must be similar to true:error or when_on_single_line:suggestion.
            if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(optionString,
                    out var value, out var notificationOpt))
            {
                // A notification value must be provided.
                if (notificationOpt != null)
                {
                    if (bool.TryParse(value, out var boolValue))
                    {
                        return boolValue
                            ? new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, notificationOpt)
                            : new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.Never, notificationOpt);
                    }

                    if (value == "when_on_single_line")
                    {
                        return new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.WhenOnSingleLine, notificationOpt);
                    }
                }
            }

            return @default;
        }

        public static readonly Option<CodeStyleOption<bool>> PreferBraces = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferBraces), defaultValue: CodeStyleOptions.TrueWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_prefer_braces"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferBraces)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferSimpleDefaultExpression = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferSimpleDefaultExpression), defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_prefer_simple_default_expression"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferSimpleDefaultExpression)}")});

        private static readonly SyntaxKind[] s_preferredModifierOrderDefault =
            {
                SyntaxKind.PublicKeyword, SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword,
                SyntaxKind.StaticKeyword,
                SyntaxKind.ExternKeyword,
                SyntaxKind.NewKeyword,
                SyntaxKind.VirtualKeyword, SyntaxKind.AbstractKeyword, SyntaxKind.SealedKeyword, SyntaxKind.OverrideKeyword,
                SyntaxKind.ReadOnlyKeyword,
                SyntaxKind.UnsafeKeyword,
                SyntaxKind.VolatileKeyword,
                SyntaxKind.AsyncKeyword
            };

        public static readonly Option<CodeStyleOption<string>> PreferredModifierOrder = new Option<CodeStyleOption<string>>(
            nameof(CodeStyleOptions), nameof(PreferredModifierOrder),
            defaultValue: new CodeStyleOption<string>(string.Join(",", s_preferredModifierOrderDefault.Select(SyntaxFacts.GetText)), NotificationOption.Silent),
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForStringCodeStyleOption("csharp_preferred_modifier_order"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferredModifierOrder)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferLocalOverAnonymousFunction = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferLocalOverAnonymousFunction), defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_pattern_local_over_anonymous_function"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferLocalOverAnonymousFunction)}")});

        public static IEnumerable<Option<CodeStyleOption<bool>>> GetCodeStyleOptions()
        {
            yield return UseImplicitTypeForIntrinsicTypes;
            yield return UseImplicitTypeWhereApparent;
            yield return UseImplicitTypeWherePossible;
            yield return PreferConditionalDelegateCall;
            yield return PreferPatternMatchingOverAsWithNullCheck;
            yield return PreferPatternMatchingOverIsWithCastCheck;
            yield return PreferBraces;
            yield return PreferSimpleDefaultExpression;
            yield return PreferLocalOverAnonymousFunction;
        }

        public static IEnumerable<Option<CodeStyleOption<ExpressionBodyPreference>>> GetExpressionBodyOptions()
        {
            yield return PreferExpressionBodiedConstructors;
            yield return PreferExpressionBodiedMethods;
            yield return PreferExpressionBodiedOperators;
            yield return PreferExpressionBodiedProperties;
            yield return PreferExpressionBodiedIndexers;
            yield return PreferExpressionBodiedAccessors;
        }
    }
}
