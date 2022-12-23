// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    internal static partial class CSharpCodeStyleOptions
    {
        private static readonly ImmutableArray<IOption2>.Builder s_allOptionsBuilder = ImmutableArray.CreateBuilder<IOption2>();

        internal static ImmutableArray<IOption2> AllOptions { get; }

        private static Option2<T> CreateOption<T>(
            OptionGroup group, string name, T defaultValue, EditorConfigStorageLocation<T> storageLocation)
            => CodeStyleHelpers.CreateOption(
                group, name, defaultValue,
                s_allOptionsBuilder, storageLocation, LanguageNames.CSharp);

        private static Option2<CodeStyleOption2<bool>> CreateOption(
            OptionGroup group, CodeStyleOption2<bool> defaultValue, string name)
            => CreateOption(
                group, name, defaultValue,
                EditorConfigStorageLocation.ForBoolCodeStyleOption(name, defaultValue));

        private static Option2<CodeStyleOption2<string>> CreateOption(
            OptionGroup group, CodeStyleOption2<string> defaultValue, string name)
            => CreateOption(
                group, name, defaultValue,
                EditorConfigStorageLocation.ForStringCodeStyleOption(name, defaultValue));

        public static readonly Option2<CodeStyleOption2<bool>> VarForBuiltInTypes = CreateOption(
            CSharpCodeStyleOptionGroups.VarPreferences, CSharpSimplifierOptions.Default.VarForBuiltInTypes,
            "csharp_style_var_for_built_in_types");

        public static readonly Option2<CodeStyleOption2<bool>> VarWhenTypeIsApparent = CreateOption(
            CSharpCodeStyleOptionGroups.VarPreferences, CSharpSimplifierOptions.Default.VarWhenTypeIsApparent,
            "csharp_style_var_when_type_is_apparent");

        public static readonly Option2<CodeStyleOption2<bool>> VarElsewhere = CreateOption(
            CSharpCodeStyleOptionGroups.VarPreferences, CSharpSimplifierOptions.Default.VarElsewhere,
            "csharp_style_var_elsewhere");

        public static readonly Option2<CodeStyleOption2<bool>> PreferConditionalDelegateCall = CreateOption(
            CSharpCodeStyleOptionGroups.NullCheckingPreferences, CSharpIdeCodeStyleOptions.Default.PreferConditionalDelegateCall,
            "csharp_style_conditional_delegate_call");

        public static readonly Option2<CodeStyleOption2<bool>> PreferSwitchExpression = CreateOption(
            CSharpCodeStyleOptionGroups.PatternMatching, CSharpIdeCodeStyleOptions.Default.PreferSwitchExpression,
            "csharp_style_prefer_switch_expression");

        public static readonly Option2<CodeStyleOption2<bool>> PreferPatternMatching = CreateOption(
            CSharpCodeStyleOptionGroups.PatternMatching, CSharpIdeCodeStyleOptions.Default.PreferPatternMatching,
            "csharp_style_prefer_pattern_matching");

        public static readonly Option2<CodeStyleOption2<bool>> PreferPatternMatchingOverAsWithNullCheck = CreateOption(
            CSharpCodeStyleOptionGroups.PatternMatching, CSharpIdeCodeStyleOptions.Default.PreferPatternMatchingOverAsWithNullCheck,
            "csharp_style_pattern_matching_over_as_with_null_check");

        public static readonly Option2<CodeStyleOption2<bool>> PreferPatternMatchingOverIsWithCastCheck = CreateOption(
            CSharpCodeStyleOptionGroups.PatternMatching, CSharpIdeCodeStyleOptions.Default.PreferPatternMatchingOverIsWithCastCheck,
            "csharp_style_pattern_matching_over_is_with_cast_check");

        public static readonly Option2<CodeStyleOption2<bool>> PreferNotPattern = CreateOption(
            CSharpCodeStyleOptionGroups.PatternMatching, CSharpIdeCodeStyleOptions.Default.PreferNotPattern,
            "csharp_style_prefer_not_pattern");

        public static readonly Option2<CodeStyleOption2<bool>> PreferExtendedPropertyPattern = CreateOption(
            CSharpCodeStyleOptionGroups.PatternMatching, CSharpIdeCodeStyleOptions.Default.PreferExtendedPropertyPattern,
            "csharp_style_prefer_extended_property_pattern");

        public static readonly Option2<CodeStyleOption2<bool>> PreferThrowExpression = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, CSharpSimplifierOptions.Default.PreferThrowExpression,
            "csharp_style_throw_expression");

        public static readonly Option2<CodeStyleOption2<bool>> PreferInlinedVariableDeclaration = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, CSharpIdeCodeStyleOptions.Default.PreferInlinedVariableDeclaration,
            "csharp_style_inlined_variable_declaration");

        public static readonly Option2<CodeStyleOption2<bool>> PreferDeconstructedVariableDeclaration = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, CSharpIdeCodeStyleOptions.Default.PreferDeconstructedVariableDeclaration,
            "csharp_style_deconstructed_variable_declaration");

        public static readonly Option2<CodeStyleOption2<bool>> PreferIndexOperator = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, CSharpIdeCodeStyleOptions.Default.PreferIndexOperator,
            "csharp_style_prefer_index_operator");

        public static readonly Option2<CodeStyleOption2<bool>> PreferRangeOperator = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, CSharpIdeCodeStyleOptions.Default.PreferRangeOperator,
            "csharp_style_prefer_range_operator");

        public static readonly Option2<CodeStyleOption2<bool>> PreferUtf8StringLiterals = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, CSharpIdeCodeStyleOptions.Default.PreferUtf8StringLiterals,
            "csharp_style_prefer_utf8_string_literals");

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
            CodeStyleOption2<ExpressionBodyPreference> defaultValue,
            string editorconfigKeyName)
        => CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionBodiedMembers, editorconfigKeyName,
            defaultValue,
            new EditorConfigStorageLocation<CodeStyleOption2<ExpressionBodyPreference>>(
                editorconfigKeyName,
                s => ParseExpressionBodyPreference(s, defaultValue),
                v => GetExpressionBodyPreferenceEditorConfigString(v, defaultValue)));

        public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedConstructors = CreatePreferExpressionBodyOption(
            defaultValue: NeverWithSilentEnforcement, editorconfigKeyName: "csharp_style_expression_bodied_constructors");

        public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedMethods = CreatePreferExpressionBodyOption(
            defaultValue: NeverWithSilentEnforcement, editorconfigKeyName: "csharp_style_expression_bodied_methods");

        public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedOperators = CreatePreferExpressionBodyOption(
            defaultValue: NeverWithSilentEnforcement, editorconfigKeyName: "csharp_style_expression_bodied_operators");

        public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedProperties = CreatePreferExpressionBodyOption(
            defaultValue: WhenPossibleWithSilentEnforcement, editorconfigKeyName: "csharp_style_expression_bodied_properties");

        public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedIndexers = CreatePreferExpressionBodyOption(
            defaultValue: WhenPossibleWithSilentEnforcement, editorconfigKeyName: "csharp_style_expression_bodied_indexers");

        public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedAccessors = CreatePreferExpressionBodyOption(
            defaultValue: WhenPossibleWithSilentEnforcement, editorconfigKeyName: "csharp_style_expression_bodied_accessors");

        public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedLambdas = CreatePreferExpressionBodyOption(
            defaultValue: WhenPossibleWithSilentEnforcement, editorconfigKeyName: "csharp_style_expression_bodied_lambdas");

        public static readonly Option2<CodeStyleOption2<ExpressionBodyPreference>> PreferExpressionBodiedLocalFunctions = CreatePreferExpressionBodyOption(
            defaultValue: NeverWithSilentEnforcement, editorconfigKeyName: "csharp_style_expression_bodied_local_functions");

        private static Option2<CodeStyleOption2<PreferBracesPreference>> CreatePreferBracesOption(
            CodeStyleOption2<PreferBracesPreference> defaultValue,
            string name)
        => CreateOption(
            CSharpCodeStyleOptionGroups.CodeBlockPreferences, name,
            defaultValue,
            new EditorConfigStorageLocation<CodeStyleOption2<PreferBracesPreference>>(
                name,
                s => ParsePreferBracesPreference(s, defaultValue),
                v => GetPreferBracesPreferenceEditorConfigString(v, defaultValue)));

        public static readonly Option2<CodeStyleOption2<PreferBracesPreference>> PreferBraces = CreatePreferBracesOption(
            CSharpSimplifierOptions.Default.PreferBraces, "csharp_prefer_braces");

        public static readonly Option2<CodeStyleOption2<bool>> PreferSimpleDefaultExpression = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, CSharpSimplifierOptions.Default.PreferSimpleDefaultExpression,
            "csharp_prefer_simple_default_expression");

        public static readonly Option2<CodeStyleOption2<string>> PreferredModifierOrder = CreateOption(
            CSharpCodeStyleOptionGroups.Modifier, CSharpIdeCodeStyleOptions.Default.PreferredModifierOrder,
            "csharp_preferred_modifier_order");

        public static readonly Option2<CodeStyleOption2<bool>> PreferStaticLocalFunction = CreateOption(
            CSharpCodeStyleOptionGroups.Modifier, CSharpIdeCodeStyleOptions.Default.PreferStaticLocalFunction,
            "csharp_prefer_static_local_function");

        public static readonly Option2<CodeStyleOption2<bool>> PreferReadOnlyStruct = CreateOption(
            CSharpCodeStyleOptionGroups.Modifier, CSharpIdeCodeStyleOptions.Default.PreferReadOnlyStruct,
            "csharp_style_prefer_readonly_struct");

        public static readonly Option2<CodeStyleOption2<bool>> PreferSimpleUsingStatement = CreateOption(
            CSharpCodeStyleOptionGroups.CodeBlockPreferences, CSharpIdeCodeStyleOptions.Default.PreferSimpleUsingStatement,
            "csharp_prefer_simple_using_statement");

        public static readonly Option2<CodeStyleOption2<bool>> PreferLocalOverAnonymousFunction = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, CSharpIdeCodeStyleOptions.Default.PreferLocalOverAnonymousFunction,
            "csharp_style_prefer_local_over_anonymous_function");

        public static readonly Option2<CodeStyleOption2<bool>> PreferTupleSwap = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, CSharpIdeCodeStyleOptions.Default.PreferTupleSwap,
            "csharp_style_prefer_tuple_swap");

        public static readonly CodeStyleOption2<AddImportPlacement> PreferOutsidePlacementWithSilentEnforcement =
           new(AddImportPlacement.OutsideNamespace, NotificationOption2.Silent);

        private static Option2<CodeStyleOption2<AddImportPlacement>> CreateUsingDirectivePlacementOption(CodeStyleOption2<AddImportPlacement> defaultValue, string editorconfigKeyName)
            => CreateOption(
                CSharpCodeStyleOptionGroups.UsingDirectivePreferences, editorconfigKeyName,
                defaultValue,
                new EditorConfigStorageLocation<CodeStyleOption2<AddImportPlacement>>(
                    editorconfigKeyName,
                    s => ParseUsingDirectivesPlacement(s, defaultValue),
                    v => GetUsingDirectivesPlacementEditorConfigString(v, defaultValue)));

        public static readonly Option2<CodeStyleOption2<AddImportPlacement>> PreferredUsingDirectivePlacement = CreateUsingDirectivePlacementOption(
            AddImportPlacementOptions.Default.UsingDirectivePlacement, "csharp_using_directive_placement");

        internal static readonly Option2<CodeStyleOption2<UnusedValuePreference>> UnusedValueExpressionStatement =
            CodeStyleHelpers.CreateUnusedExpressionAssignmentOption(
                CSharpCodeStyleOptionGroups.ExpressionLevelPreferences,
                editorConfigName: "csharp_style_unused_value_expression_statement_preference",
                defaultValue: CSharpIdeCodeStyleOptions.Default.UnusedValueExpressionStatement,
                optionsBuilder: s_allOptionsBuilder,
                languageName: LanguageNames.CSharp);

        internal static readonly Option2<CodeStyleOption2<UnusedValuePreference>> UnusedValueAssignment =
            CodeStyleHelpers.CreateUnusedExpressionAssignmentOption(
                CSharpCodeStyleOptionGroups.ExpressionLevelPreferences,
                editorConfigName: "csharp_style_unused_value_assignment_preference",
                defaultValue: CSharpIdeCodeStyleOptions.Default.UnusedValueAssignment,
                optionsBuilder: s_allOptionsBuilder,
                languageName: LanguageNames.CSharp);

        public static readonly Option2<CodeStyleOption2<bool>> ImplicitObjectCreationWhenTypeIsApparent = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, CSharpIdeCodeStyleOptions.Default.ImplicitObjectCreationWhenTypeIsApparent,
            "csharp_style_implicit_object_creation_when_type_is_apparent");

        internal static readonly Option2<CodeStyleOption2<bool>> PreferNullCheckOverTypeCheck = CreateOption(
            CSharpCodeStyleOptionGroups.ExpressionLevelPreferences, CSharpIdeCodeStyleOptions.Default.PreferNullCheckOverTypeCheck,
            "csharp_style_prefer_null_check_over_type_check");

        public static Option2<CodeStyleOption2<bool>> AllowEmbeddedStatementsOnSameLine { get; } = CreateOption(
            CSharpCodeStyleOptionGroups.NewLinePreferences, "csharp_style_allow_embedded_statements_on_same_line_experimental",
            CSharpSimplifierOptions.Default.AllowEmbeddedStatementsOnSameLine,
            EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_allow_embedded_statements_on_same_line_experimental", CodeStyleOptions2.TrueWithSilentEnforcement));

        public static Option2<CodeStyleOption2<bool>> AllowBlankLinesBetweenConsecutiveBraces { get; } = CreateOption(
            CSharpCodeStyleOptionGroups.NewLinePreferences, "csharp_style_allow_blank_lines_between_consecutive_braces_experimental",
            CSharpIdeCodeStyleOptions.Default.AllowBlankLinesBetweenConsecutiveBraces,
            EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_allow_blank_lines_between_consecutive_braces_experimental", CodeStyleOptions2.TrueWithSilentEnforcement));

        public static Option2<CodeStyleOption2<bool>> AllowBlankLineAfterColonInConstructorInitializer { get; } = CreateOption(
            CSharpCodeStyleOptionGroups.NewLinePreferences, "csharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental",
            CSharpIdeCodeStyleOptions.Default.AllowBlankLineAfterColonInConstructorInitializer,
            EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental", CodeStyleOptions2.TrueWithSilentEnforcement));

        public static Option2<CodeStyleOption2<bool>> AllowBlankLineAfterTokenInConditionalExpression { get; } = CreateOption(
            CSharpCodeStyleOptionGroups.NewLinePreferences, "csharp_style_allow_blank_line_after_token_in_conditional_expression_experimental",
            CSharpIdeCodeStyleOptions.Default.AllowBlankLineAfterTokenInConditionalExpression,
            EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_allow_blank_line_after_token_in_conditional_expression_experimental", CodeStyleOptions2.TrueWithSilentEnforcement));

        public static Option2<CodeStyleOption2<bool>> AllowBlankLineAfterTokenInArrowExpressionClause { get; } = CreateOption(
            CSharpCodeStyleOptionGroups.NewLinePreferences, "csharp_style_allow_blank_line_after_token_in_arrow_expression_clause_experimental",
            CSharpIdeCodeStyleOptions.Default.AllowBlankLineAfterTokenInArrowExpressionClause,
            EditorConfigStorageLocation.ForBoolCodeStyleOption("csharp_style_allow_blank_line_after_token_in_arrow_expression_clause_experimental", CodeStyleOptions2.TrueWithSilentEnforcement));

        private static Option2<CodeStyleOption2<NamespaceDeclarationPreference>> CreateNamespaceDeclarationOption(CodeStyleOption2<NamespaceDeclarationPreference> defaultValue, string editorconfigKeyName)
            => CreateOption(
                CSharpCodeStyleOptionGroups.CodeBlockPreferences, editorconfigKeyName,
                defaultValue,
                new EditorConfigStorageLocation<CodeStyleOption2<NamespaceDeclarationPreference>>(
                    editorconfigKeyName,
                    s => ParseNamespaceDeclaration(s, defaultValue),
                    v => GetNamespaceDeclarationEditorConfigString(v, defaultValue)));

        public static readonly Option2<CodeStyleOption2<NamespaceDeclarationPreference>> NamespaceDeclarations = CreateNamespaceDeclarationOption(
            CSharpSyntaxFormattingOptions.Default.NamespaceDeclarations,
            "csharp_style_namespace_declarations");

        public static readonly Option2<CodeStyleOption2<bool>> PreferMethodGroupConversion = CreateOption(
            CSharpCodeStyleOptionGroups.CodeBlockPreferences, CSharpIdeCodeStyleOptions.Default.PreferMethodGroupConversion,
            "csharp_style_prefer_method_group_conversion");

        public static readonly Option2<CodeStyleOption2<bool>> PreferTopLevelStatements = CreateOption(
            CSharpCodeStyleOptionGroups.CodeBlockPreferences, CSharpSyntaxFormattingOptions.Default.PreferTopLevelStatements,
            "csharp_style_prefer_top_level_statements");

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
            yield return AllowBlankLineAfterColonInConstructorInitializer;
            yield return AllowBlankLineAfterTokenInConditionalExpression;
            yield return AllowBlankLineAfterTokenInArrowExpressionClause;
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
        public static readonly OptionGroup VarPreferences = new(CSharpCompilerExtensionsResources.var_preferences, priority: 1, parent: CodeStyleOptionGroups.CodeStyle);
        public static readonly OptionGroup ExpressionBodiedMembers = new(CSharpCompilerExtensionsResources.Expression_bodied_members, priority: 2, parent: CodeStyleOptionGroups.CodeStyle);
        public static readonly OptionGroup PatternMatching = new(CSharpCompilerExtensionsResources.Pattern_matching_preferences, priority: 3, parent: CodeStyleOptionGroups.CodeStyle);
        public static readonly OptionGroup NullCheckingPreferences = new(CSharpCompilerExtensionsResources.Null_checking_preferences, priority: 4, parent: CodeStyleOptionGroups.CodeStyle);
        public static readonly OptionGroup Modifier = new(CompilerExtensionsResources.Modifier_preferences, priority: 5, parent: CodeStyleOptionGroups.CodeStyle);
        public static readonly OptionGroup CodeBlockPreferences = new(CSharpCompilerExtensionsResources.Code_block_preferences, priority: 6, parent: CodeStyleOptionGroups.CodeStyle);
        public static readonly OptionGroup ExpressionLevelPreferences = new(CompilerExtensionsResources.Expression_level_preferences, priority: 7, parent: CodeStyleOptionGroups.CodeStyle);
        public static readonly OptionGroup UsingDirectivePreferences = new(CSharpCompilerExtensionsResources.using_directive_preferences, priority: 8, parent: CodeStyleOptionGroups.CodeStyle);
        public static readonly OptionGroup NewLinePreferences = new(CompilerExtensionsResources.New_line_preferences, priority: 9, parent: CodeStyleOptionGroups.CodeStyle);
    }
}
