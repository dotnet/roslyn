// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    internal static class CSharpCodeStyleOptions
    {
        public static readonly Option<CodeStyleOption<bool>> UseImplicitTypeForIntrinsicTypes = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(UseImplicitTypeForIntrinsicTypes), defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation("csharp_style_var_for_built_in_types"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.UseImplicitTypeForIntrinsicTypes")});

        public static readonly Option<CodeStyleOption<bool>> UseImplicitTypeWhereApparent = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(UseImplicitTypeWhereApparent), defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation("csharp_style_var_when_type_is_apparent"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.UseImplicitTypeWhereApparent")});

        public static readonly Option<CodeStyleOption<bool>> UseImplicitTypeWherePossible = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(UseImplicitTypeWherePossible), defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation("csharp_style_var_elsewhere"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.UseImplicitTypeWherePossible")});

        public static readonly Option<CodeStyleOption<bool>> PreferConditionalDelegateCall = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferConditionalDelegateCall), defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation("csharp_style_conditional_delegate_call"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.PreferConditionalDelegateCall")});

        public static readonly Option<CodeStyleOption<bool>> PreferPatternMatchingOverAsWithNullCheck = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferPatternMatchingOverAsWithNullCheck), defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation("csharp_style_pattern_matching_over_as_with_null_check"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferPatternMatchingOverAsWithNullCheck)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferPatternMatchingOverIsWithCastCheck = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferPatternMatchingOverIsWithCastCheck), defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation("csharp_style_pattern_matching_over_is_with_cast_check"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferPatternMatchingOverIsWithCastCheck)}")});

        public static readonly CodeStyleOption<ExpressionBodyPreference> NeverWithNoneEnforcement =
            new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.Never, NotificationOption.None);

        public static readonly CodeStyleOption<ExpressionBodyPreference> WhenPossibleWithNoneEnforcement =
            new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption.None);

        public static readonly CodeStyleOption<ExpressionBodyPreference> WhenPossibleWithSuggestionEnforcement =
            new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption.Suggestion);

        public static readonly CodeStyleOption<ExpressionBodyPreference> WhenOnSingleLineWithNoneEnforcement =
            new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.WhenOnSingleLine, NotificationOption.None);

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedConstructors = new Option<CodeStyleOption<ExpressionBodyPreference>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedConstructors),
            defaultValue: NeverWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation("csharp_style_expression_bodied_constructors", s => ParseExpressionBodyPreference(s, ExpressionBodyPreference.Never)),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedConstructors)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedMethods = new Option<CodeStyleOption<ExpressionBodyPreference>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedMethods),
            defaultValue: NeverWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation("csharp_style_expression_bodied_methods", s => ParseExpressionBodyPreference(s, ExpressionBodyPreference.Never)),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedMethods)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedOperators = new Option<CodeStyleOption<ExpressionBodyPreference>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedOperators),
            defaultValue: NeverWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation("csharp_style_expression_bodied_operators", s => ParseExpressionBodyPreference(s, ExpressionBodyPreference.Never)),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedOperators)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedProperties = new Option<CodeStyleOption<ExpressionBodyPreference>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedProperties),
            defaultValue: WhenPossibleWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation("csharp_style_expression_bodied_properties", s => ParseExpressionBodyPreference(s, ExpressionBodyPreference.WhenOnSingleLine)),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedProperties)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedIndexers = new Option<CodeStyleOption<ExpressionBodyPreference>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedIndexers),
            defaultValue: WhenPossibleWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation("csharp_style_expression_bodied_indexers", s => ParseExpressionBodyPreference(s, ExpressionBodyPreference.WhenOnSingleLine)),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedIndexers)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedAccessors = new Option<CodeStyleOption<ExpressionBodyPreference>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedAccessors), 
            defaultValue: WhenPossibleWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation("csharp_style_expression_bodied_accessors", s => ParseExpressionBodyPreference(s, ExpressionBodyPreference.WhenOnSingleLine)),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedAccessors)}")});

        private static object ParseExpressionBodyPreference(string value, ExpressionBodyPreference @default)
        {
            if (bool.TryParse(value, out var boolValue))
            {
                return boolValue ? ExpressionBodyPreference.WhenPossible : ExpressionBodyPreference.Never;
            }

            switch (value)
            {
                case "never": return ExpressionBodyPreference.Never;
                case "when_possible": return ExpressionBodyPreference.WhenPossible;
                case "when_on_single_line": return ExpressionBodyPreference.WhenOnSingleLine;
            }

            return @default;
        }

        public static readonly Option<CodeStyleOption<bool>> PreferBraces = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferBraces), defaultValue: CodeStyleOptions.TrueWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation("csharp_prefer_braces"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferBraces)}")});

        public static IEnumerable<Option<CodeStyleOption<bool>>> GetCodeStyleOptions()
        {
            yield return UseImplicitTypeForIntrinsicTypes;
            yield return UseImplicitTypeWhereApparent;
            yield return UseImplicitTypeWherePossible;
            yield return PreferConditionalDelegateCall;
            yield return PreferPatternMatchingOverAsWithNullCheck;
            yield return PreferPatternMatchingOverIsWithCastCheck;
            yield return PreferBraces;
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