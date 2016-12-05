// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using static Microsoft.CodeAnalysis.CodeStyle.CodeStyleHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    internal static class CSharpCodeStyleOptions
    {
        // TODO: get sign off on public api changes.
        public static readonly Option<bool> UseVarWhenDeclaringLocals = new Option<bool>(
            nameof(CodeStyleOptions), nameof(UseVarWhenDeclaringLocals), defaultValue: true,
            storageLocations: new OptionStorageLocation[]{
                new EditorConfigStorageLocation("csharp_style_var_for_locals", ParseEditorConfigCodeStyleOption),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.UseVarWhenDeclaringLocals")});

        public static readonly Option<CodeStyleOption<bool>> UseImplicitTypeForIntrinsicTypes = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(UseImplicitTypeForIntrinsicTypes), defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[]{
                new EditorConfigStorageLocation("csharp_style_var_for_built_in_types", ParseEditorConfigCodeStyleOption),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.UseImplicitTypeForIntrinsicTypes")});

        public static readonly Option<CodeStyleOption<bool>> UseImplicitTypeWhereApparent = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(UseImplicitTypeWhereApparent), defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[]{
                new EditorConfigStorageLocation("csharp_style_var_when_type_is_apparent", ParseEditorConfigCodeStyleOption),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.UseImplicitTypeWhereApparent")});

        public static readonly Option<CodeStyleOption<bool>> UseImplicitTypeWherePossible = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(UseImplicitTypeWherePossible), defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[]{
                new EditorConfigStorageLocation("csharp_style_var_elsewhere", ParseEditorConfigCodeStyleOption),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.UseImplicitTypeWherePossible")});

        public static readonly Option<CodeStyleOption<bool>> PreferConditionalDelegateCall = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferConditionalDelegateCall), defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                new EditorConfigStorageLocation("csharp_style_conditional_delegate_call", ParseEditorConfigCodeStyleOption),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.PreferConditionalDelegateCall")});

        public static readonly Option<CodeStyleOption<bool>> PreferPatternMatchingOverAsWithNullCheck = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferPatternMatchingOverAsWithNullCheck), defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                new EditorConfigStorageLocation("csharp_style_pattern_matching_over_as_with_null_check", ParseEditorConfigCodeStyleOption),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferPatternMatchingOverAsWithNullCheck)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferPatternMatchingOverIsWithCastCheck = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferPatternMatchingOverIsWithCastCheck), defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                new EditorConfigStorageLocation("csharp_style_pattern_matching_over_is_with_cast_check", ParseEditorConfigCodeStyleOption),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferPatternMatchingOverIsWithCastCheck)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferExpressionBodiedConstructors = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedConstructors), defaultValue: CodeStyleOptions.FalseWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[]{
                new EditorConfigStorageLocation("csharp_style_expression_bodied_constructors", ParseEditorConfigCodeStyleOption),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedConstructors)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferExpressionBodiedMethods = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedMethods), defaultValue: CodeStyleOptions.FalseWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[]{
                new EditorConfigStorageLocation("csharp_style_expression_bodied_methods", ParseEditorConfigCodeStyleOption),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedMethods)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferExpressionBodiedOperators = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedOperators), defaultValue: CodeStyleOptions.FalseWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[]{
                new EditorConfigStorageLocation("csharp_style_expression_bodied_operators", ParseEditorConfigCodeStyleOption),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedOperators)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferExpressionBodiedProperties = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedProperties), defaultValue: CodeStyleOptions.TrueWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[]{
                new EditorConfigStorageLocation("csharp_style_expression_bodied_properties", ParseEditorConfigCodeStyleOption),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedProperties)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferExpressionBodiedIndexers = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedIndexers), defaultValue: CodeStyleOptions.TrueWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[]{
                new EditorConfigStorageLocation("csharp_style_expression_bodied_indexers", ParseEditorConfigCodeStyleOption),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedIndexers)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferExpressionBodiedAccessors = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferExpressionBodiedAccessors), defaultValue: CodeStyleOptions.TrueWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[]{
                new EditorConfigStorageLocation("csharp_style_expression_bodied_accessors", ParseEditorConfigCodeStyleOption),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedAccessors)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferBraces = new Option<CodeStyleOption<bool>>(
            nameof(CodeStyleOptions), nameof(PreferBraces), defaultValue: CodeStyleOptions.TrueWithNoneEnforcement,
            storageLocations: new OptionStorageLocation[]{
                new EditorConfigStorageLocation("csharp_prefer_braces", ParseEditorConfigCodeStyleOption),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferBraces)}")});

        public static IEnumerable<Option<CodeStyleOption<bool>>> GetCodeStyleOptions()
        {
            yield return UseImplicitTypeForIntrinsicTypes;
            yield return UseImplicitTypeWhereApparent;
            yield return UseImplicitTypeWherePossible;
            yield return PreferConditionalDelegateCall;
            yield return PreferPatternMatchingOverAsWithNullCheck;
            yield return PreferPatternMatchingOverIsWithCastCheck;
            yield return PreferExpressionBodiedConstructors;
            yield return PreferExpressionBodiedMethods;
            yield return PreferExpressionBodiedOperators;
            yield return PreferExpressionBodiedProperties;
            yield return PreferExpressionBodiedIndexers;
            yield return PreferExpressionBodiedAccessors;
            yield return PreferBraces;
        }
    }
}
