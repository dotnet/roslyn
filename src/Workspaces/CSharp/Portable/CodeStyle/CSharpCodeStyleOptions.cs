// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    internal static partial class CSharpCodeStyleOptions
    {
        private static readonly ImmutableArray<IOption>.Builder s_allOptionsBuilder = ImmutableArray.CreateBuilder<IOption>();

        internal static ImmutableArray<IOption> AllOptions { get; }

        private static Option<T> CreateOption<T>(OptionGroup group, string name, T defaultValue, params OptionStorageLocation[] storageLocations)
            => CodeStyleHelpers.CreateOption(group, nameof(CSharpCodeStyleOptions), name, defaultValue, s_allOptionsBuilder, storageLocations);

        public static readonly Option<CodeStyleOption<bool>> VarForBuiltInTypes = CreateOption(
            CSharpCodeStyleOptionGroups.VarPreferences, nameof(VarForBuiltInTypes),
            defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_var_for_built_in_types"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.UseImplicitTypeForIntrinsicTypes")});

        public static readonly Option<CodeStyleOption<bool>> VarWhenTypeIsApparent = CreateOption(
            CSharpCodeStyleOptionGroups.VarPreferences, nameof(VarWhenTypeIsApparent),
            defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_var_when_type_is_apparent"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.UseImplicitTypeWhereApparent")});

        public static readonly Option<CodeStyleOption<bool>> VarElsewhere = CreateOption(
            CSharpCodeStyleOptionGroups.VarPreferences, nameof(VarElsewhere),
            defaultValue: CodeStyleOption<bool>.Default,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_var_elsewhere"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.UseImplicitTypeWherePossible")});

        public static readonly Option<CodeStyleOption<bool>> PreferConditionalDelegateCall = CreateOption(
            CSharpCodeStyleOptionGroups.NullCheckingPreferences, nameof(PreferConditionalDelegateCall),
            defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_conditional_delegate_call"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.PreferConditionalDelegateCall")});

        public static readonly Option<CodeStyleOption<bool>> PreferSwitchExpression = CreateOption(
            CSharpCodeStyleOptionGroups.PatternMatching, nameof(PreferSwitchExpression),
            defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_prefer_switch_expression"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferSwitchExpression)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferPatternMatchingOverAsWithNullCheck = CreateOption(
            CSharpCodeStyleOptionGroups.PatternMatching, nameof(PreferPatternMatchingOverAsWithNullCheck),
            defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_pattern_matching_over_as_with_null_check"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferPatternMatchingOverAsWithNullCheck)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferPatternMatchingOverIsWithCastCheck = CreateOption(
            CSharpCodeStyleOptionGroups.PatternMatching, nameof(PreferPatternMatchingOverIsWithCastCheck),
            defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_pattern_matching_over_is_with_cast_check"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferPatternMatchingOverIsWithCastCheck)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferThrowExpression = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferThrowExpression),
            defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_throw_expression"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.PreferThrowExpression")});

        public static readonly Option<CodeStyleOption<bool>> PreferInlinedVariableDeclaration = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferInlinedVariableDeclaration),
            defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[]{
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_inlined_variable_declaration"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.PreferInlinedVariableDeclaration") });

        public static readonly Option<CodeStyleOption<bool>> PreferDeconstructedVariableDeclaration = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferDeconstructedVariableDeclaration),
            defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_deconstructed_variable_declaration"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferDeconstructedVariableDeclaration)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferIndexOperator = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferIndexOperator),
            defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_prefer_index_operator"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.PreferIndexOperator")});

        public static readonly Option<CodeStyleOption<bool>> PreferRangeOperator = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferRangeOperator),
            defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_prefer_range_operator"),
                new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.PreferRangeOperator")});

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

        public static readonly CodeStyleOption<PreferBracesPreference> UseBracesWithSilentEnforcement =
            new CodeStyleOption<PreferBracesPreference>(PreferBracesPreference.Always, NotificationOption.Silent);

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedConstructors = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionBodiedMembers, nameof(PreferExpressionBodiedConstructors),
            defaultValue: NeverWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<CodeStyleOption<ExpressionBodyPreference>>(
                    "csharp_style_expression_bodied_constructors",
                    s => ParseExpressionBodyPreference(s, NeverWithSilentEnforcement),
                    GetExpressionBodyPreferenceEditorConfigString),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedConstructors)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedMethods = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionBodiedMembers, nameof(PreferExpressionBodiedMethods),
            defaultValue: NeverWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<CodeStyleOption<ExpressionBodyPreference>>(
                    "csharp_style_expression_bodied_methods",
                    s => ParseExpressionBodyPreference(s, NeverWithSilentEnforcement),
                    GetExpressionBodyPreferenceEditorConfigString),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedMethods)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedOperators = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionBodiedMembers, nameof(PreferExpressionBodiedOperators),
            defaultValue: NeverWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<CodeStyleOption<ExpressionBodyPreference>>(
                    "csharp_style_expression_bodied_operators",
                    s => ParseExpressionBodyPreference(s, NeverWithSilentEnforcement),
                    GetExpressionBodyPreferenceEditorConfigString),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedOperators)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedProperties = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionBodiedMembers, nameof(PreferExpressionBodiedProperties),
            defaultValue: WhenPossibleWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<CodeStyleOption<ExpressionBodyPreference>>(
                    "csharp_style_expression_bodied_properties",
                    s => ParseExpressionBodyPreference(s, WhenPossibleWithSilentEnforcement),
                    GetExpressionBodyPreferenceEditorConfigString),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedProperties)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedIndexers = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionBodiedMembers, nameof(PreferExpressionBodiedIndexers),
            defaultValue: WhenPossibleWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<CodeStyleOption<ExpressionBodyPreference>>(
                    "csharp_style_expression_bodied_indexers",
                    s => ParseExpressionBodyPreference(s, WhenPossibleWithSilentEnforcement),
                    GetExpressionBodyPreferenceEditorConfigString),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedIndexers)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedAccessors = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionBodiedMembers, nameof(PreferExpressionBodiedAccessors),
            defaultValue: WhenPossibleWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<CodeStyleOption<ExpressionBodyPreference>>(
                    "csharp_style_expression_bodied_accessors",
                    s => ParseExpressionBodyPreference(s, WhenPossibleWithSilentEnforcement),
                    GetExpressionBodyPreferenceEditorConfigString),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedAccessors)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedLambdas = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionBodiedMembers, nameof(PreferExpressionBodiedLambdas),
            defaultValue: WhenPossibleWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<CodeStyleOption<ExpressionBodyPreference>>(
                    "csharp_style_expression_bodied_lambdas",
                    s => ParseExpressionBodyPreference(s, WhenPossibleWithSilentEnforcement),
                    GetExpressionBodyPreferenceEditorConfigString),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedLambdas)}")});

        public static readonly Option<CodeStyleOption<ExpressionBodyPreference>> PreferExpressionBodiedLocalFunctions = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionBodiedMembers, nameof(PreferExpressionBodiedLocalFunctions),
            defaultValue: NeverWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<CodeStyleOption<ExpressionBodyPreference>>(
                    "csharp_style_expression_bodied_local_functions",
                    s => ParseExpressionBodyPreference(s, NeverWithSilentEnforcement),
                    GetExpressionBodyPreferenceEditorConfigString),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferExpressionBodiedLocalFunctions)}")});

        public static readonly Option<CodeStyleOption<PreferBracesPreference>> PreferBraces = CreateOption(
            CSharpCodeStyleOptionGroups.CodeBlockPreferences, nameof(PreferBraces),
            defaultValue: UseBracesWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[] {
                new EditorConfigStorageLocation<CodeStyleOption<PreferBracesPreference>>(
                    "csharp_prefer_braces",
                    s => ParsePreferBracesPreference(s, UseBracesWithSilentEnforcement),
                    GetPreferBracesPreferenceEditorConfigString),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferBraces)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferSimpleDefaultExpression = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferSimpleDefaultExpression),
            defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
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

        public static readonly Option<CodeStyleOption<string>> PreferredModifierOrder = CreateOption(
            CSharpCodeStyleOptionGroups.Modifier, nameof(PreferredModifierOrder),
            defaultValue: new CodeStyleOption<string>(string.Join(",", s_preferredModifierOrderDefault.Select(SyntaxFacts.GetText)), NotificationOption.Silent),
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForStringCodeStyleOption("csharp_preferred_modifier_order"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferredModifierOrder)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferStaticLocalFunction = CreateOption(
            CSharpCodeStyleOptionGroups.Modifier, nameof(PreferStaticLocalFunction),
            defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_prefer_static_local_function"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferStaticLocalFunction)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferSimpleUsingStatement = CreateOption(
            CSharpCodeStyleOptionGroups.CodeBlockPreferences, nameof(PreferSimpleUsingStatement),
            defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_prefer_simple_using_statement"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferSimpleUsingStatement)}")});

        public static readonly Option<CodeStyleOption<bool>> PreferLocalOverAnonymousFunction = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, nameof(PreferLocalOverAnonymousFunction),
            defaultValue: CodeStyleOptions.TrueWithSuggestionEnforcement,
            storageLocations: new OptionStorageLocation[] {
                EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_pattern_local_over_anonymous_function"),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferLocalOverAnonymousFunction)}")});

        public static readonly CodeStyleOption<AddImportPlacement> PreferOutsidePlacementWithSilentEnforcement =
           new CodeStyleOption<AddImportPlacement>(AddImportPlacement.OutsideNamespace, NotificationOption.Silent);

        public static readonly Option<CodeStyleOption<AddImportPlacement>> PreferredUsingDirectivePlacement = CreateOption(
            CSharpCodeStyleOptionGroups.UsingDirectivePreferences, nameof(PreferredUsingDirectivePlacement),
            defaultValue: PreferOutsidePlacementWithSilentEnforcement,
            storageLocations: new OptionStorageLocation[]{
                new EditorConfigStorageLocation<CodeStyleOption<AddImportPlacement>>(
                    "csharp_using_directive_placement",
                    s => ParseUsingDirectivesPlacement(s, PreferOutsidePlacementWithSilentEnforcement),
                    GetUsingDirectivesPlacementEditorConfigString),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{nameof(PreferredUsingDirectivePlacement)}") });

        internal static readonly Option<CodeStyleOption<UnusedValuePreference>> UnusedValueExpressionStatement =
            CodeStyleHelpers.CreateUnusedExpressionAssignmentOption(
                CSharpCodeStyleOptionGroups.ExpressionLevelPreferences,
                feature: nameof(CSharpCodeStyleOptions),
                name: nameof(UnusedValueExpressionStatement),
                editorConfigName: "csharp_style_unused_value_expression_statement_preference",
                defaultValue: new CodeStyleOption<UnusedValuePreference>(UnusedValuePreference.DiscardVariable, NotificationOption.Silent),
                s_allOptionsBuilder);

        internal static readonly Option<CodeStyleOption<UnusedValuePreference>> UnusedValueAssignment =
            CodeStyleHelpers.CreateUnusedExpressionAssignmentOption(
                CSharpCodeStyleOptionGroups.ExpressionLevelPreferences,
                feature: nameof(CSharpCodeStyleOptions),
                name: nameof(UnusedValueAssignment),
                editorConfigName: "csharp_style_unused_value_assignment_preference",
                defaultValue: new CodeStyleOption<UnusedValuePreference>(UnusedValuePreference.DiscardVariable, NotificationOption.Suggestion),
                s_allOptionsBuilder);

        static CSharpCodeStyleOptions()
        {
            // Note that the static constructor executes after all the static field initializers for the options have executed,
            // and each field initializer adds the created option to s_allOptionsBuilder.
            AllOptions = s_allOptionsBuilder.ToImmutable();
        }

        public static IEnumerable<Option<CodeStyleOption<bool>>> GetCodeStyleOptions()
        {
            yield return VarForBuiltInTypes;
            yield return VarWhenTypeIsApparent;
            yield return VarElsewhere;
            yield return PreferConditionalDelegateCall;
            yield return PreferSwitchExpression;
            yield return PreferPatternMatchingOverAsWithNullCheck;
            yield return PreferPatternMatchingOverIsWithCastCheck;
            yield return PreferSimpleDefaultExpression;
            yield return PreferLocalOverAnonymousFunction;
            yield return PreferThrowExpression;
            yield return PreferInlinedVariableDeclaration;
            yield return PreferDeconstructedVariableDeclaration;
            yield return PreferIndexOperator;
            yield return PreferRangeOperator;
        }

        public static IEnumerable<Option<CodeStyleOption<ExpressionBodyPreference>>> GetExpressionBodyOptions()
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
        public static readonly OptionGroup VarPreferences = new OptionGroup(CSharpWorkspaceResources.var_preferences, priority: 1);
        public static readonly OptionGroup ExpressionBodiedMembers = new OptionGroup(CSharpWorkspaceResources.Expression_bodied_members, priority: 2);
        public static readonly OptionGroup PatternMatching = new OptionGroup(CSharpWorkspaceResources.Pattern_matching_preferences, priority: 3);
        public static readonly OptionGroup NullCheckingPreferences = new OptionGroup(CSharpWorkspaceResources.Null_checking_preferences, priority: 4);
        public static readonly OptionGroup Modifier = new OptionGroup(WorkspacesResources.Modifier_preferences, priority: 5);
        public static readonly OptionGroup CodeBlockPreferences = new OptionGroup(CSharpWorkspaceResources.Code_block_preferences, priority: 6);
        public static readonly OptionGroup ExpressionLevelPreferences = new OptionGroup(WorkspacesResources.Expression_level_preferences, priority: 7);
        public static readonly OptionGroup UsingDirectivePreferences = new OptionGroup(CSharpWorkspaceResources.using_directive_preferences, priority: 8);
    }
}
