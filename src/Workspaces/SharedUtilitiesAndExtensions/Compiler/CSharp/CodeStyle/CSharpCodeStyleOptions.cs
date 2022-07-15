﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    internal static partial class CSharpCodeStyleOptions
    {
        private static readonly CodeStyleOption2<bool> s_trueWithSuggestionEnforcement = CodeStyleOptions2.TrueWithSuggestionEnforcement;
        private static readonly CodeStyleOption2<bool> s_trueWithSilentEnforcement = CodeStyleOptions2.TrueWithSilentEnforcement;

        private static readonly ImmutableArray<IOption2>.Builder s_allOptionsBuilder = ImmutableArray.CreateBuilder<IOption2>();

        internal static ImmutableArray<IOption2> AllOptions { get; }

        private static Option2<T> CreateOption<T>(
            OptionGroup group, string name, T defaultValue, OptionStorageLocation2 storageLocation)
            => CodeStyleHelpers.CreateOption(
                group, nameof(CSharpCodeStyleOptions), name, defaultValue,
                s_allOptionsBuilder, storageLocation);

        private static Option2<T> CreateOption<T>(
            OptionGroup group, string name, T defaultValue, OptionStorageLocation2 storageLocation1, OptionStorageLocation2 storageLocation2)
            => CodeStyleHelpers.CreateOption(
                group, nameof(CSharpCodeStyleOptions), name, defaultValue,
                s_allOptionsBuilder, storageLocation1, storageLocation2);

        private static Option2<CodeStyleOption2<bool>> CreateOption(
            OptionGroup group, string name, CodeStyleOption2<bool> defaultValue, string editorconfigKeyName, string roamingProfileStorageKeyName)
            => CreateOption(
                group, name, defaultValue,
                EditorConfigStorageLocation.ForBoolCodeStyleOption(editorconfigKeyName, defaultValue),
                new RoamingProfileStorageLocation(roamingProfileStorageKeyName));

        private static Option2<CodeStyleOption2<string>> CreateOption(
            OptionGroup group, string name, CodeStyleOption2<string> defaultValue, string editorconfigKeyName, string roamingProfileStorageKeyName)
            => CreateOption(
                group, name, defaultValue,
                EditorConfigStorageLocation.ForStringCodeStyleOption(editorconfigKeyName, defaultValue),
                new RoamingProfileStorageLocation(roamingProfileStorageKeyName));

        public static readonly Option2<CodeStyleOption2<bool>> VarForBuiltInTypes = CreateOption(
            CSharpCodeStyleOptionGroups.VarPreferences, nameof(VarForBuiltInTypes),
            CSharpSimplifierOptions.Default.VarForBuiltInTypes,
            "csharp_style_var_for_built_in_types",
            "TextEditor.CSharp.Specific.UseImplicitTypeForIntrinsicTypes");

        public static readonly Option2<CodeStyleOption2<bool>> VarWhenTypeIsApparent = CreateOption(
            CSharpCodeStyleOptionGroups.VarPreferences, nameof(VarWhenTypeIsApparent),
            CSharpSimplifierOptions.Default.VarWhenTypeIsApparent,
            "csharp_style_var_when_type_is_apparent",
            "TextEditor.CSharp.Specific.UseImplicitTypeWhereApparent");

        public static readonly Option2<CodeStyleOption2<bool>> VarElsewhere = CreateOption(
            CSharpCodeStyleOptionGroups.VarPreferences, nameof(VarElsewhere),
            CSharpSimplifierOptions.Default.VarElsewhere,
            "csharp_style_var_elsewhere",
            "TextEditor.CSharp.Specific.UseImplicitTypeWherePossible");

        public static readonly Option2<CodeStyleOption2<bool>> PreferConditionalDelegateCall = CreateOption(
            CSharpCodeStyleOptionGroups.NullCheckingPreferences, nameof(PreferConditionalDelegateCall),
            CSharpIdeCodeStyleOptions.Default.PreferConditionalDelegateCall,
            "csharp_style_conditional_delegate_call",
            "TextEditor.CSharp.Specific.PreferConditionalDelegateCall");

        public static readonly Option2<CodeStyleOption2<bool>> PreferSwitchExpression = CreateOption(
            CSharpCodeStyleOptionGroups.PatternMatching, nameof(PreferSwitchExpression),
            CSharpIdeCodeStyleOptions.Default.PreferSwitchExpression,
            "csharp_style_prefer_switch_expression",
            "TextEditor.CSharp.Specific.PreferSwitchExpression");

        public static readonly Option2<CodeStyleOption2<bool>> PreferPatternMatching = CreateOption(
            CSharpCodeStyleOptionGroups.PatternMatching, nameof(PreferPatternMatching),
            CSharpIdeCodeStyleOptions.Default.PreferPatternMatching,
            "csharp_style_prefer_pattern_matching",
            "TextEditor.CSharp.Specific.PreferPatternMatching");

        public static readonly Option2<CodeStyleOption2<bool>> PreferPatternMatchingOverAsWithNullCheck = CreateOption(
            CSharpCodeStyleOptionGroups.PatternMatching, nameof(PreferPatternMatchingOverAsWithNullCheck),
            CSharpIdeCodeStyleOptions.Default.PreferPatternMatchingOverAsWithNullCheck,
            "csharp_style_pattern_matching_over_as_with_null_check",
            "TextEditor.CSharp.Specific.PreferPatternMatchingOverAsWithNullCheck");

        public static readonly Option2<CodeStyleOption2<bool>> PreferPatternMatchingOverIsWithCastCheck = CreateOption(
            CSharpCodeStyleOptionGroups.PatternMatching, nameof(PreferPatternMatchingOverIsWithCastCheck),
            CSharpIdeCodeStyleOptions.Default.PreferPatternMatchingOverIsWithCastCheck,
            "csharp_style_pattern_matching_over_is_with_cast_check",
            "TextEditor.CSharp.Specific.PreferPatternMatchingOverIsWithCastCheck");

        public static readonly Option2<CodeStyleOption2<bool>> PreferNotPattern = CreateOption(
            CSharpCodeStyleOptionGroups.PatternMatching, nameof(PreferNotPattern),
            CSharpIdeCodeStyleOptions.Default.PreferNotPattern,
            "csharp_style_prefer_not_pattern",
            "TextEditor.CSharp.Specific.PreferNotPattern");

        public static readonly Option2<CodeStyleOption2<bool>> PreferExtendedPropertyPattern = CreateOption(
            CSharpCodeStyleOptionGroups.PatternMatching, nameof(PreferExtendedPropertyPattern),
            CSharpIdeCodeStyleOptions.Default.PreferExtendedPropertyPattern,
            "csharp_style_prefer_extended_property_pattern",
            "TextEditor.CSharp.Specific.PreferExtendedPropertyPattern");

        public static readonly Option2<CodeStyleOption2<bool>> PreferThrowExpression = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferThrowExpression),
            CSharpSimplifierOptions.Default.PreferThrowExpression,
            "csharp_style_throw_expression",
            "TextEditor.CSharp.Specific.PreferThrowExpression");

        public static readonly Option2<CodeStyleOption2<bool>> PreferInlinedVariableDeclaration = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferInlinedVariableDeclaration),
            CSharpIdeCodeStyleOptions.Default.PreferInlinedVariableDeclaration,
            "csharp_style_inlined_variable_declaration",
            "TextEditor.CSharp.Specific.PreferInlinedVariableDeclaration");

        public static readonly Option2<CodeStyleOption2<bool>> PreferDeconstructedVariableDeclaration = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferDeconstructedVariableDeclaration),
            CSharpIdeCodeStyleOptions.Default.PreferDeconstructedVariableDeclaration,
            "csharp_style_deconstructed_variable_declaration",
            "TextEditor.CSharp.Specific.PreferDeconstructedVariableDeclaration");

        public static readonly Option2<CodeStyleOption2<bool>> PreferIndexOperator = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferIndexOperator),
            CSharpIdeCodeStyleOptions.Default.PreferIndexOperator,
            "csharp_style_prefer_index_operator",
            "TextEditor.CSharp.Specific.PreferIndexOperator");

        public static readonly Option2<CodeStyleOption2<bool>> PreferRangeOperator = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferRangeOperator),
            CSharpIdeCodeStyleOptions.Default.PreferRangeOperator,
            "csharp_style_prefer_range_operator",
            "TextEditor.CSharp.Specific.PreferRangeOperator");

        public static readonly Option2<CodeStyleOption2<bool>> PreferUtf8StringLiterals = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, "PreferUtf8StringLiterals",
            CSharpIdeCodeStyleOptions.Default.PreferUtf8StringLiterals,
            "csharp_style_prefer_utf8_string_literals",
            $"TextEditor.CSharp.Specific.PreferUtf8StringLiterals");

        public static readonly CodeStyleOption2<ExpressionBodyPreference> NeverWithSilentEnforcement =
            new(ExpressionBodyPreference.Never, NotificationOption2.Silent);

        public static readonly CodeStyleOption2<ExpressionBodyPreference> NeverWithSuggestionEnforcement =
            new(ExpressionBodyPreference.Never, NotificationOption2.Suggestion);

        public static readonly CodeStyleOption2<ExpressionBodyPreference> WhenPossibleWithSilentEnforcement =
            new(ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent);

        public static readonly CodeStyleOption2<ExpressionBodyPreference> WhenPossibleWithSuggestionEnforcement =
            new(ExpressionBodyPreference.WhenPossible, NotificationOption2.Suggestion);

        public static readonly CodeStyleOption2<ExpressionBodyPreference> WhenOnSingleLineWithSilentEnforcement =
            new(ExpressionBodyPreference.WhenOnSingleLine, NotificationOption2.Silent);

        private static Option2<CodeStyleOption2<ExpressionBodyPreference>> CreatePreferExpressionBodyOption(
            string optionName,
            CodeStyleOption2<ExpressionBodyPreference> defaultValue,
            string editorconfigKeyName)
        => CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionBodiedMembers, optionName,
            defaultValue,
            new EditorConfigStorageLocation<CodeStyleOption2<ExpressionBodyPreference>>(
                editorconfigKeyName,
                s => ParseExpressionBodyPreference(s, defaultValue),
                v => GetExpressionBodyPreferenceEditorConfigString(v, defaultValue)),
            new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{optionName}"));

        public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedConstructors = CreatePreferExpressionBodyOption(
            nameof(PreferExpressionBodiedConstructors), defaultValue: NeverWithSilentEnforcement, "csharp_style_expression_bodied_constructors");

        public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedMethods = CreatePreferExpressionBodyOption(
            nameof(PreferExpressionBodiedMethods), defaultValue: NeverWithSilentEnforcement, "csharp_style_expression_bodied_methods");

        public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedOperators = CreatePreferExpressionBodyOption(
            nameof(PreferExpressionBodiedOperators), defaultValue: NeverWithSilentEnforcement, "csharp_style_expression_bodied_operators");

        public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedProperties = CreatePreferExpressionBodyOption(
            nameof(PreferExpressionBodiedProperties), defaultValue: WhenPossibleWithSilentEnforcement, "csharp_style_expression_bodied_properties");

        public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedIndexers = CreatePreferExpressionBodyOption(
            nameof(PreferExpressionBodiedIndexers), defaultValue: WhenPossibleWithSilentEnforcement, "csharp_style_expression_bodied_indexers");

        public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedAccessors = CreatePreferExpressionBodyOption(
            nameof(PreferExpressionBodiedAccessors), defaultValue: WhenPossibleWithSilentEnforcement, "csharp_style_expression_bodied_accessors");

        public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedLambdas = CreatePreferExpressionBodyOption(
            nameof(PreferExpressionBodiedLambdas), defaultValue: WhenPossibleWithSilentEnforcement, "csharp_style_expression_bodied_lambdas");

        public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedLocalFunctions = CreatePreferExpressionBodyOption(
            nameof(PreferExpressionBodiedLocalFunctions), defaultValue: NeverWithSilentEnforcement, "csharp_style_expression_bodied_local_functions");

        private static Option2<CodeStyleOption2<PreferBracesPreference>> CreatePreferBracesOption(
            string optionName,
            CodeStyleOption2<PreferBracesPreference> defaultValue,
            string editorconfigKeyName)
        => CreateOption(
            CSharpCodeStyleOptionGroups.CodeBlockPreferences, optionName,
            defaultValue,
            new EditorConfigStorageLocation<CodeStyleOption2<PreferBracesPreference>>(
                editorconfigKeyName,
                s => ParsePreferBracesPreference(s, defaultValue),
                v => GetPreferBracesPreferenceEditorConfigString(v, defaultValue)),
            new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{optionName}"));

        public static readonly Option2<CodeStyleOption2<PreferBracesPreference>> PreferBraces = CreatePreferBracesOption(
            nameof(PreferBraces), CSharpSimplifierOptions.Default.PreferBraces, "csharp_prefer_braces");

        public static readonly Option2<CodeStyleOption2<bool>> PreferSimpleDefaultExpression = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferSimpleDefaultExpression),
            CSharpSimplifierOptions.Default.PreferSimpleDefaultExpression,
            "csharp_prefer_simple_default_expression",
            "TextEditor.CSharp.Specific.PreferSimpleDefaultExpression");

        public static readonly Option2<CodeStyleOption2<string>> PreferredModifierOrder = CreateOption(
            CSharpCodeStyleOptionGroups.Modifier, nameof(PreferredModifierOrder),
            CSharpIdeCodeStyleOptions.Default.PreferredModifierOrder,
            "csharp_preferred_modifier_order",
            "TextEditor.CSharp.Specific.PreferredModifierOrder");

        public static readonly Option2<CodeStyleOption2<bool>> PreferStaticLocalFunction = CreateOption(
            CSharpCodeStyleOptionGroups.Modifier, nameof(PreferStaticLocalFunction),
            CSharpIdeCodeStyleOptions.Default.PreferStaticLocalFunction,
            "csharp_prefer_static_local_function",
            "TextEditor.CSharp.Specific.PreferStaticLocalFunction");

        public static readonly Option2<CodeStyleOption2<bool>> PreferSimpleUsingStatement = CreateOption(
            CSharpCodeStyleOptionGroups.CodeBlockPreferences, nameof(PreferSimpleUsingStatement),
            CSharpIdeCodeStyleOptions.Default.PreferSimpleUsingStatement,
            "csharp_prefer_simple_using_statement",
            "TextEditor.CSharp.Specific.PreferSimpleUsingStatement");

        public static readonly Option2<CodeStyleOption2<bool>> PreferLocalOverAnonymousFunction = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferLocalOverAnonymousFunction),
            CSharpIdeCodeStyleOptions.Default.PreferLocalOverAnonymousFunction,
            "csharp_style_prefer_local_over_anonymous_function",
            "TextEditor.CSharp.Specific.PreferLocalOverAnonymousFunction");

        public static readonly Option2<CodeStyleOption2<bool>> PreferTupleSwap = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferTupleSwap),
            CSharpIdeCodeStyleOptions.Default.PreferTupleSwap,
            "csharp_style_prefer_tuple_swap",
            "TextEditor.CSharp.Specific.PreferTupleSwap");

        public static readonly CodeStyleOption2<AddImportPlacement> PreferOutsidePlacementWithSilentEnforcement =
           new(AddImportPlacement.OutsideNamespace, NotificationOption2.Silent);

        private static Option2<CodeStyleOption2<AddImportPlacement>> CreateUsingDirectivePlacementOption(string optionName, CodeStyleOption2<AddImportPlacement> defaultValue, string editorconfigKeyName)
            => CreateOption(
                CSharpCodeStyleOptionGroups.UsingDirectivePreferences, optionName,
                defaultValue,
                new EditorConfigStorageLocation<CodeStyleOption2<AddImportPlacement>>(
                    editorconfigKeyName,
                    s => ParseUsingDirectivesPlacement(s, defaultValue),
                    v => GetUsingDirectivesPlacementEditorConfigString(v, defaultValue)),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{optionName}"));

        public static readonly Option2<CodeStyleOption2<AddImportPlacement>> PreferredUsingDirectivePlacement = CreateUsingDirectivePlacementOption(
            "PreferredUsingDirectivePlacement", AddImportPlacementOptions.Default.UsingDirectivePlacement, "csharp_using_directive_placement");

        internal static readonly Option2<CodeStyleOption2<UnusedValuePreference>> UnusedValueExpressionStatement =
            CodeStyleHelpers.CreateUnusedExpressionAssignmentOption(
                CSharpCodeStyleOptionGroups.ExpressionLevelPreferences,
                feature: nameof(CSharpCodeStyleOptions),
                name: "UnusedValueExpressionStatement",
                editorConfigName: "csharp_style_unused_value_expression_statement_preference",
                CSharpIdeCodeStyleOptions.Default.UnusedValueExpressionStatement,
                s_allOptionsBuilder);

        internal static readonly Option2<CodeStyleOption2<UnusedValuePreference>> UnusedValueAssignment =
            CodeStyleHelpers.CreateUnusedExpressionAssignmentOption(
                CSharpCodeStyleOptionGroups.ExpressionLevelPreferences,
                feature: nameof(CSharpCodeStyleOptions),
                name: "UnusedValueAssignment",
                editorConfigName: "csharp_style_unused_value_assignment_preference",
                CSharpIdeCodeStyleOptions.Default.UnusedValueAssignment,
                s_allOptionsBuilder);

        public static readonly Option2<CodeStyleOption2<bool>> ImplicitObjectCreationWhenTypeIsApparent = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(ImplicitObjectCreationWhenTypeIsApparent),
            CSharpIdeCodeStyleOptions.Default.ImplicitObjectCreationWhenTypeIsApparent,
            "csharp_style_implicit_object_creation_when_type_is_apparent",
            "TextEditor.CSharp.Specific.ImplicitObjectCreationWhenTypeIsApparent");

        internal static readonly Option2<CodeStyleOption2<bool>> PreferNullCheckOverTypeCheck = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferNullCheckOverTypeCheck),
            CSharpIdeCodeStyleOptions.Default.PreferNullCheckOverTypeCheck,
            "csharp_style_prefer_null_check_over_type_check",
            "TextEditor.CSharp.Specific.PreferNullCheckOverTypeCheck");

        public static Option2<CodeStyleOption2<bool>> AllowEmbeddedStatementsOnSameLine { get; } = CreateOption(
            CSharpCodeStyleOptionGroups.NewLinePreferences, nameof(AllowEmbeddedStatementsOnSameLine),
            CSharpSimplifierOptions.Default.AllowEmbeddedStatementsOnSameLine,
            EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_allow_embedded_statements_on_same_line_experimental", CodeStyleOptions2.TrueWithSilentEnforcement),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.AllowEmbeddedStatementsOnSameLine"));

        public static Option2<CodeStyleOption2<bool>> AllowBlankLinesBetweenConsecutiveBraces { get; } = CreateOption(
            CSharpCodeStyleOptionGroups.NewLinePreferences, nameof(AllowBlankLinesBetweenConsecutiveBraces),
            CSharpIdeCodeStyleOptions.Default.AllowBlankLinesBetweenConsecutiveBraces,
            EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_allow_blank_lines_between_consecutive_braces_experimental", CodeStyleOptions2.TrueWithSilentEnforcement),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.AllowBlankLinesBetweenConsecutiveBraces"));

        public static Option2<CodeStyleOption2<bool>> AllowBlankLineAfterColonInConstructorInitializer { get; } = CreateOption(
            CSharpCodeStyleOptionGroups.NewLinePreferences, nameof(AllowBlankLineAfterColonInConstructorInitializer),
            CSharpIdeCodeStyleOptions.Default.AllowBlankLineAfterColonInConstructorInitializer,
            EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental", CodeStyleOptions2.TrueWithSilentEnforcement),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.AllowBlankLineAfterColonInConstructorInitializer"));

        private static Option2<CodeStyleOption2<NamespaceDeclarationPreference>> CreateNamespaceDeclarationOption(string optionName, CodeStyleOption2<NamespaceDeclarationPreference> defaultValue, string editorconfigKeyName)
            => CreateOption(
                CSharpCodeStyleOptionGroups.CodeBlockPreferences, optionName,
                defaultValue,
                new EditorConfigStorageLocation<CodeStyleOption2<NamespaceDeclarationPreference>>(
                    editorconfigKeyName,
                    s => ParseNamespaceDeclaration(s, defaultValue),
                    v => GetNamespaceDeclarationEditorConfigString(v, defaultValue)),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{optionName}"));

        public static readonly Option2<CodeStyleOption2<NamespaceDeclarationPreference>> NamespaceDeclarations = CreateNamespaceDeclarationOption(
            "NamespaceDeclarations",
            CSharpSyntaxFormattingOptions.Default.NamespaceDeclarations,
            "csharp_style_namespace_declarations");

        public static readonly Option2<CodeStyleOption2<bool>> PreferMethodGroupConversion = CreateOption(
            CSharpCodeStyleOptionGroups.CodeBlockPreferences, nameof(PreferMethodGroupConversion),
            CSharpIdeCodeStyleOptions.Default.PreferMethodGroupConversion,
            "csharp_style_prefer_method_group_conversion",
            "TextEditor.CSharp.Specific.PreferMethodGroupConversion");

        public static readonly Option2<CodeStyleOption2<bool>> PreferTopLevelStatements = CreateOption(
            CSharpCodeStyleOptionGroups.CodeBlockPreferences, nameof(PreferTopLevelStatements),
            CSharpSyntaxFormattingOptions.Default.PreferTopLevelStatements,
            "csharp_style_prefer_top_level_statements",
            "TextEditor.CSharp.Specific.PreferTopLevelStatements");

#if false

        public static readonly Option2<CodeStyleOption2<bool>> VarElsewhere = CreateOption(
            CSharpCodeStyleOptionGroups.VarPreferences, nameof(VarElsewhere),
            defaultValue: s_trueWithSuggestionEnforcement,
            "csharp_style_var_elsewhere",
            "TextEditor.CSharp.Specific.UseImplicitTypeWherePossible");

#endif

        static CSharpCodeStyleOptions()
        {
            // Note that the static constructor executes after all the static field initializers for the options have executed,
            // and each field initializer adds the created option to s_allOptionsBuilder.
            AllOptions = s_allOptionsBuilder.ToImmutable();
        }

        public static IEnumerable<Option2<CodeStyleOption2<bool>>> GetCodeStyleOptions()
        {
            yield return VarForBuiltInTypes;
            yield return VarWhenTypeIsApparent;
            yield return VarElsewhere;
            yield return PreferConditionalDelegateCall;
            yield return PreferSwitchExpression;
            yield return PreferPatternMatching;
            yield return PreferPatternMatchingOverAsWithNullCheck;
            yield return PreferPatternMatchingOverIsWithCastCheck;
            yield return PreferSimpleDefaultExpression;
            yield return PreferLocalOverAnonymousFunction;
            yield return PreferThrowExpression;
            yield return PreferInlinedVariableDeclaration;
            yield return PreferDeconstructedVariableDeclaration;
            yield return PreferIndexOperator;
            yield return PreferRangeOperator;
            yield return AllowEmbeddedStatementsOnSameLine;
            yield return AllowBlankLinesBetweenConsecutiveBraces;
        }

        public static IEnumerable<Option2<CodeStyleOption2<ExpressionBodyPreference>>> GetExpressionBodyOptions()
        {
            yield return PreferExpressionBodiedConstructors;
            yield return PreferExpressionBodiedMethods;
            yield return PreferExpressionBodiedOperators;
            yield return PreferExpressionBodiedProperties;
            yield return PreferExpressionBodiedIndexers;
            yield return PreferExpressionBodiedAccessors;
            yield return PreferExpressionBodiedLambdas;
        }
    }

    internal static class CSharpCodeStyleOptionGroups
    {
        public static readonly OptionGroup VarPreferences = new(CSharpCompilerExtensionsResources.var_preferences, priority: 1);
        public static readonly OptionGroup ExpressionBodiedMembers = new(CSharpCompilerExtensionsResources.Expression_bodied_members, priority: 2);
        public static readonly OptionGroup PatternMatching = new(CSharpCompilerExtensionsResources.Pattern_matching_preferences, priority: 3);
        public static readonly OptionGroup NullCheckingPreferences = new(CSharpCompilerExtensionsResources.Null_checking_preferences, priority: 4);
        public static readonly OptionGroup Modifier = new(CompilerExtensionsResources.Modifier_preferences, priority: 5);
        public static readonly OptionGroup CodeBlockPreferences = new(CSharpCompilerExtensionsResources.Code_block_preferences, priority: 6);
        public static readonly OptionGroup ExpressionLevelPreferences = new(CompilerExtensionsResources.Expression_level_preferences, priority: 7);
        public static readonly OptionGroup UsingDirectivePreferences = new(CSharpCompilerExtensionsResources.using_directive_preferences, priority: 8);
        public static readonly OptionGroup NewLinePreferences = new(CompilerExtensionsResources.New_line_preferences, priority: 9);
    }
}
