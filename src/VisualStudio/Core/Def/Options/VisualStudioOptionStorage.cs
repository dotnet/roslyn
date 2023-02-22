// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal abstract class VisualStudioOptionStorage
{
    internal sealed class RoamingProfileStorage : VisualStudioOptionStorage
    {
        private const string LanguagePlaceholder = "%LANGUAGE%";

        /// <summary>
        /// Key may contain <see cref="LanguagePlaceholder"/> that is replaced by the language name.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// VB specific key should only be specified for backward compat for a few speciifc options.
        /// Language specific storage key should use <see cref="LanguagePlaceholder"/> in <see cref="Key"/>.
        /// </summary>
        public string? VisualBasicKey { get; }

        public RoamingProfileStorage(string key)
        {
            Key = key;
        }

        /// <summary>
        /// Backward compat only.
        /// </summary>
        [Obsolete]
        public RoamingProfileStorage(string key, string vbKey)
        {
            Debug.Assert(!vbKey.Contains(LanguagePlaceholder));

            Key = key;
            VisualBasicKey = vbKey;
        }

        public bool IsPerLanguage
            => Key.Contains(LanguagePlaceholder);

        private string GetKey(string? language)
            => (VisualBasicKey != null && language == LanguageNames.VisualBasic) ? VisualBasicKey : SubstituteLanguage(Key, language);

        private static string SubstituteLanguage(string keyName, string? language)
            => keyName.Replace(LanguagePlaceholder, language switch
            {
                LanguageNames.CSharp => "CSharp",
                LanguageNames.VisualBasic => "VisualBasic",
                _ => language // handles F#, TypeScript and Xaml
            });

        public Task PersistAsync(VisualStudioSettingsOptionPersister persister, OptionKey2 optionKey, object? value)
            => persister.PersistAsync(optionKey, GetKey(optionKey.Language), value);

        public bool TryFetch(VisualStudioSettingsOptionPersister persister, OptionKey2 optionKey, out object? value)
            => persister.TryFetch(optionKey, GetKey(optionKey.Language), out value);
    }

    internal sealed class FeatureFlagStorage : VisualStudioOptionStorage
    {
        public string FlagName { get; }

        public FeatureFlagStorage(string flagName)
        {
            FlagName = flagName;
        }

        public Task PersistAsync(FeatureFlagPersister persister, object? value)
        {
            persister.Persist(FlagName, value);
            return Task.CompletedTask;
        }

        public bool TryFetch(FeatureFlagPersister persister, OptionKey2 optionKey, out object? value)
            => persister.TryFetch(optionKey, FlagName, out value);
    }

    internal sealed class LocalUserProfileStorage : VisualStudioOptionStorage
    {
        private readonly string _path;
        private readonly string _key;

        public LocalUserProfileStorage(string path, string key)
        {
            _path = path;
            _key = key;
        }

        public Task PersistAsync(LocalUserRegistryOptionPersister persister, OptionKey2 optionKey, object? value)
        {
            persister.Persist(optionKey, _path, _key, value);
            return Task.CompletedTask;
        }

        public bool TryFetch(LocalUserRegistryOptionPersister persister, OptionKey2 optionKey, out object? value)
            => persister.TryFetch(optionKey, _path, _key, out value);
    }

    public static readonly IReadOnlyDictionary<string, VisualStudioOptionStorage> Storages = new Dictionary<string, VisualStudioOptionStorage>()
    {
        {"BlockStructureOptions_CollapseEmptyMetadataImplementationsWhenFirstOpened", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.CollapseEmptyMetadataImplementationsWhenFirstOpened")},
        {"BlockStructureOptions_CollapseImportsWhenFirstOpened", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.CollapseImportsWhenFirstOpened")},
        {"BlockStructureOptions_CollapseMetadataImplementationsWhenFirstOpened", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.CollapseMetadataImplementationsWhenFirstOpened")},
        {"BlockStructureOptions_CollapseRegionsWhenCollapsingToDefinitions", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.CollapseRegionsWhenCollapsingToDefinitions")},
        {"BlockStructureOptions_CollapseRegionsWhenFirstOpened", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.CollapseRegionsWhenFirstOpened")},
        {"BlockStructureOptions_MaximumBannerLength", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.MaximumBannerLength")},
        {"BlockStructureOptions_ShowBlockStructureGuidesForCodeLevelConstructs", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowBlockStructureGuidesForCodeLevelConstructs")},
        {"BlockStructureOptions_ShowBlockStructureGuidesForCommentsAndPreprocessorRegions", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions")},
        {"BlockStructureOptions_ShowBlockStructureGuidesForDeclarationLevelConstructs", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowBlockStructureGuidesForDeclarationLevelConstructs")},
        {"BlockStructureOptions_ShowOutliningForCodeLevelConstructs", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowOutliningForCodeLevelConstructs")},
        {"BlockStructureOptions_ShowOutliningForCommentsAndPreprocessorRegions", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowOutliningForCommentsAndPreprocessorRegions")},
        {"BlockStructureOptions_ShowOutliningForDeclarationLevelConstructs", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowOutliningForDeclarationLevelConstructs")},
        {"BraceCompletionOptions_AutoFormattingOnCloseBrace", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Close Brace")},
        {"ClassificationOptions_ClassifyReassignedVariables", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ClassificationOptions.ClassifyReassignedVariables")},
        {"CodeStyleOptions_PreferSystemHashCode", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferSystemHashCode")},
        {"ColorSchemeOptions_ColorSchemeName", new RoamingProfileStorage("TextEditor.Roslyn.ColorSchemeName")},
        {"ColorSchemeOptions_LegacyUseEnhancedColors", new RoamingProfileStorage("WindowManagement.Options.UseEnhancedColorsForManagedLanguages")},
        {"CompletionOptions_BlockForCompletionItems", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.BlockForCompletionItems")},
        {"CompletionOptions_EnableArgumentCompletionSnippets", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.EnableArgumentCompletionSnippets")},
        {"CompletionOptions_EnterKeyBehavior", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.EnterKeyBehavior")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"CompletionOptions_HideAdvancedMembers", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Hide Advanced Auto List Members", vbKey: "TextEditor.Basic.Hide Advanced Auto List Members")},
#pragma warning restore
        {"CompletionOptions_HighlightMatchingPortionsOfCompletionListItems", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.HighlightMatchingPortionsOfCompletionListItems")},
        {"CompletionOptions_ShowCompletionItemFilters", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowCompletionItemFilters")},
        {"CompletionOptions_ShowItemsFromUnimportedNamespaces", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowItemsFromUnimportedNamespaces")},
        {"CompletionOptions_ShowNameSuggestions", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowNameSuggestions")},
        {"CompletionOptions_ShowNewSnippetExperienceFeatureFlag", new FeatureFlagStorage(@"Roslyn.SnippetCompletion")},
        {"CompletionOptions_ShowNewSnippetExperienceUserOption", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowNewSnippetExperience")},
        {"CompletionOptions_SnippetsBehavior", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.SnippetsBehavior")},
        {"CompletionOptions_TriggerInArgumentLists", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.TriggerInArgumentLists")},
        {"CompletionOptions_TriggerOnDeletion", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.TriggerOnDeletion")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"CompletionOptions_TriggerOnTyping", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Auto List Members", vbKey: "TextEditor.Basic.Auto List Members")},
#pragma warning restore
        {"CompletionOptions_TriggerOnTypingLetters", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.TriggerOnTypingLetters")},
        {"CompletionOptions_UnnamedSymbolCompletionDisabledFeatureFlag", new FeatureFlagStorage(@"Roslyn.UnnamedSymbolCompletionDisabled")},
        {"csharp_indent_block_contents", new RoamingProfileStorage("TextEditor.CSharp.Specific.IndentBlock")},
        {"csharp_indent_braces", new RoamingProfileStorage("TextEditor.CSharp.Specific.OpenCloseBracesIndent")},
        {"csharp_indent_case_contents", new RoamingProfileStorage("TextEditor.CSharp.Specific.IndentSwitchCaseSection")},
        {"csharp_indent_case_contents_when_block", new RoamingProfileStorage("TextEditor.CSharp.Specific.IndentSwitchCaseSectionWhenBlock")},
        {"csharp_indent_labels", new RoamingProfileStorage("TextEditor.CSharp.Specific.LabelPositioning")},
        {"csharp_indent_switch_labels", new RoamingProfileStorage("TextEditor.CSharp.Specific.IndentSwitchSection")},
        {"csharp_new_line_before_open_brace", new RoamingProfileStorage("TextEditor.CSharp.Specific.csharp_new_line_before_open_brace") },
        {"csharp_new_line_before_catch", new RoamingProfileStorage("TextEditor.CSharp.Specific.NewLineForCatch")},
        {"csharp_new_line_before_else", new RoamingProfileStorage("TextEditor.CSharp.Specific.NewLineForElse")},
        {"csharp_new_line_before_finally", new RoamingProfileStorage("TextEditor.CSharp.Specific.NewLineForFinally")},
        {"csharp_new_line_before_members_in_anonymous_types", new RoamingProfileStorage("TextEditor.CSharp.Specific.NewLineForMembersInAnonymousTypes")},
        {"csharp_new_line_before_members_in_object_initializers", new RoamingProfileStorage("TextEditor.CSharp.Specific.NewLineForMembersInObjectInit")},
        {"csharp_new_line_between_query_expression_clauses", new RoamingProfileStorage("TextEditor.CSharp.Specific.NewLineForClausesInQuery")},
        {"csharp_prefer_braces", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferBraces")},
        {"csharp_prefer_simple_default_expression", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferSimpleDefaultExpression")},
        {"csharp_prefer_simple_using_statement", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferSimpleUsingStatement")},
        {"csharp_prefer_static_local_function", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferStaticLocalFunction")},
        {"csharp_preferred_modifier_order", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferredModifierOrder")},
        {"csharp_preserve_single_line_blocks", new RoamingProfileStorage("TextEditor.CSharp.Specific.WrappingPreserveSingleLine")},
        {"csharp_preserve_single_line_statements", new RoamingProfileStorage("TextEditor.CSharp.Specific.WrappingKeepStatementsOnSingleLine")},
        {"csharp_space_after_cast", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceAfterCast")},
        {"csharp_space_after_colon_in_inheritance_clause", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceAfterColonInBaseTypeDeclaration")},
        {"csharp_space_after_comma", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceAfterComma")},
        {"csharp_space_after_dot", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceAfterDot")},
        {"csharp_space_after_keywords_in_control_flow_statements", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceAfterControlFlowStatementKeyword")},
        {"csharp_space_after_semicolon_in_for_statement", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceAfterSemicolonsInForStatement")},
        {"csharp_space_around_binary_operators", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpacingAroundBinaryOperator")},
        {"csharp_space_around_declaration_statements", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpacesIgnoreAroundVariableDeclaration")},
        {"csharp_space_before_colon_in_inheritance_clause", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceBeforeColonInBaseTypeDeclaration")},
        {"csharp_space_before_comma", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceBeforeComma")},
        {"csharp_space_before_dot", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceBeforeDot")},
        {"csharp_space_before_open_square_brackets", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceBeforeOpenSquareBracket")},
        {"csharp_space_before_semicolon_in_for_statement", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceBeforeSemicolonsInForStatement")},
        {"csharp_space_between_empty_square_brackets", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceBetweenEmptySquareBrackets")},
        {"csharp_space_between_method_call_empty_parameter_list_parentheses", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceBetweenEmptyMethodCallParentheses")},
        {"csharp_space_between_method_call_name_and_opening_parenthesis", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceAfterMethodCallName")},
        {"csharp_space_between_method_call_parameter_list_parentheses", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceWithinMethodCallParentheses")},
        {"csharp_space_between_method_declaration_empty_parameter_list_parentheses", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceBetweenEmptyMethodDeclarationParentheses")},
        {"csharp_space_between_method_declaration_name_and_open_parenthesis", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpacingAfterMethodDeclarationName")},
        {"csharp_space_between_method_declaration_parameter_list_parentheses", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceWithinMethodDeclarationParenthesis")},
        {"csharp_space_between_parentheses", new RoamingProfileStorage("TextEditor.CSharp.Specific.csharp_space_between_parentheses") },
        {"csharp_space_between_square_brackets", new RoamingProfileStorage("TextEditor.CSharp.Specific.SpaceWithinSquareBrackets")},
        {"csharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental", new RoamingProfileStorage("TextEditor.CSharp.Specific.AllowBlankLineAfterColonInConstructorInitializer")},
        {"csharp_style_allow_blank_line_after_token_in_arrow_expression_clause_experimental", new RoamingProfileStorage("TextEditor.CSharp.Specific.AllowBlankLineAfterTokenInArrowExpressionClause")},
        {"csharp_style_allow_blank_line_after_token_in_conditional_expression_experimental", new RoamingProfileStorage("TextEditor.CSharp.Specific.AllowBlankLineAfterTokenInConditionalExpression")},
        {"csharp_style_allow_blank_lines_between_consecutive_braces_experimental", new RoamingProfileStorage("TextEditor.CSharp.Specific.AllowBlankLinesBetweenConsecutiveBraces")},
        {"csharp_style_allow_embedded_statements_on_same_line_experimental", new RoamingProfileStorage("TextEditor.CSharp.Specific.AllowEmbeddedStatementsOnSameLine")},
        {"csharp_style_conditional_delegate_call", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferConditionalDelegateCall")},
        {"csharp_style_deconstructed_variable_declaration", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferDeconstructedVariableDeclaration")},
        {"csharp_style_expression_bodied_accessors", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferExpressionBodiedAccessors")},
        {"csharp_style_expression_bodied_constructors", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferExpressionBodiedConstructors")},
        {"csharp_style_expression_bodied_indexers", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferExpressionBodiedIndexers")},
        {"csharp_style_expression_bodied_lambdas", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferExpressionBodiedLambdas")},
        {"csharp_style_expression_bodied_local_functions", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferExpressionBodiedLocalFunctions")},
        {"csharp_style_expression_bodied_methods", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferExpressionBodiedMethods")},
        {"csharp_style_expression_bodied_operators", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferExpressionBodiedOperators")},
        {"csharp_style_expression_bodied_properties", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferExpressionBodiedProperties")},
        {"csharp_style_implicit_object_creation_when_type_is_apparent", new RoamingProfileStorage("TextEditor.CSharp.Specific.ImplicitObjectCreationWhenTypeIsApparent")},
        {"csharp_style_inlined_variable_declaration", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferInlinedVariableDeclaration")},
        {"csharp_style_namespace_declarations", new RoamingProfileStorage("TextEditor.CSharp.Specific.NamespaceDeclarations")},
        {"csharp_style_pattern_matching_over_as_with_null_check", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferPatternMatchingOverAsWithNullCheck")},
        {"csharp_style_pattern_matching_over_is_with_cast_check", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferPatternMatchingOverIsWithCastCheck")},
        {"csharp_style_prefer_extended_property_pattern", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferExtendedPropertyPattern")},
        {"csharp_style_prefer_index_operator", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferIndexOperator")},
        {"csharp_style_prefer_local_over_anonymous_function", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferLocalOverAnonymousFunction")},
        {"csharp_style_prefer_method_group_conversion", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferMethodGroupConversion")},
        {"csharp_style_prefer_not_pattern", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferNotPattern")},
        {"csharp_style_prefer_null_check_over_type_check", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferNullCheckOverTypeCheck")},
        {"csharp_style_prefer_pattern_matching", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferPatternMatching")},
        {"csharp_style_prefer_range_operator", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferRangeOperator")},
        {"csharp_style_prefer_readonly_struct", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferReadOnlyStruct")},
        {"csharp_style_prefer_switch_expression", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferSwitchExpression")},
        {"csharp_style_prefer_top_level_statements", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferTopLevelStatements")},
        {"csharp_style_prefer_tuple_swap", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferTupleSwap")},
        {"csharp_style_prefer_utf8_string_literals", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferUtf8StringLiterals")},
        {"csharp_style_throw_expression", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferThrowExpression")},
        {"csharp_style_unused_value_assignment_preference", new RoamingProfileStorage("TextEditor.CSharp.Specific.UnusedValueAssignmentPreference")},
        {"csharp_style_unused_value_expression_statement_preference", new RoamingProfileStorage("TextEditor.CSharp.Specific.UnusedValueExpressionStatementPreference")},
        {"csharp_style_var_elsewhere", new RoamingProfileStorage("TextEditor.CSharp.Specific.UseImplicitTypeWherePossible")},
        {"csharp_style_var_for_built_in_types", new RoamingProfileStorage("TextEditor.CSharp.Specific.UseImplicitTypeForIntrinsicTypes")},
        {"csharp_style_var_when_type_is_apparent", new RoamingProfileStorage("TextEditor.CSharp.Specific.UseImplicitTypeWhereApparent")},
        {"csharp_using_directive_placement", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferredUsingDirectivePlacement")},
        {"DateAndTime_ProvideDateAndTimeCompletions", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ProvideDateAndTimeCompletions")},
        {"DiagnosticOptions_LogTelemetryForBackgroundAnalyzerExecution", new FeatureFlagStorage(@"Roslyn.LogTelemetryForBackgroundAnalyzerExecution")},
        {"DiagnosticOptions_LspPullDiagnosticsFeatureFlag", new FeatureFlagStorage(@"Lsp.PullDiagnostics")},
        {"DiagnosticTaggingOptions_PullDiagnosticTagging", new FeatureFlagStorage(@"Roslyn.PullDiagnosticTagging")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"DocumentationCommentOptions_AutoXmlDocCommentGeneration", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Automatic XML Doc Comment Generation", "TextEditor.VisualBasic.Specific.AutoComment")},
#pragma warning restore
        {"DocumentOutlineOptions_EnableDocumentOutline", new FeatureFlagStorage(@"Roslyn.DocumentOutline")},
        {"dotnet_code_quality_unused_parameters", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.UnusedParametersPreference")},

        {"dotnet_separate_import_directive_groups", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.SeparateImportDirectiveGroups")},
        {"dotnet_sort_system_directives_first", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PlaceSystemNamespaceFirst")},
        {"dotnet_style_allow_multiple_blank_lines_experimental", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.AllowMultipleBlankLines")},
        {"dotnet_style_allow_statement_immediately_after_block_experimental", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.AllowStatementImmediatelyAfterBlock")},
        {"dotnet_style_coalesce_expression", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferCoalesceExpression")},
        {"dotnet_style_collection_initializer", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferCollectionInitializer")},
        {"dotnet_style_explicit_tuple_names", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferExplicitTupleNames")},
        {"dotnet_style_namespace_match_folder", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferNamespaceAndFolderMatchStructure")},
        {"dotnet_style_null_propagation", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferNullPropagation")},
        {"dotnet_style_object_initializer", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferObjectInitializer")},
        {"dotnet_style_parentheses_in_arithmetic_binary_operators", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ArithmeticBinaryParenthesesPreference")},
        {"dotnet_style_parentheses_in_other_binary_operators", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.OtherBinaryParenthesesPreference")},
        {"dotnet_style_parentheses_in_other_operators", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.OtherParenthesesPreference")},
        {"dotnet_style_parentheses_in_relational_binary_operators", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.RelationalBinaryParenthesesPreference")},
        {"dotnet_style_predefined_type_for_locals_parameters_members", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferIntrinsicPredefinedTypeKeywordInDeclaration.CodeStyle")},
        {"dotnet_style_predefined_type_for_member_access", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferIntrinsicPredefinedTypeKeywordInMemberAccess.CodeStyle")},
        {"dotnet_style_prefer_auto_properties", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferAutoProperties")},
        {"dotnet_style_prefer_compound_assignment", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferCompoundAssignment")},
        {"dotnet_style_prefer_conditional_expression_over_assignment", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferConditionalExpressionOverAssignment")},
        {"dotnet_style_prefer_conditional_expression_over_return", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferConditionalExpressionOverReturn")},
        {"dotnet_style_prefer_inferred_anonymous_type_member_names", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferInferredAnonymousTypeMemberNames")},
        {"dotnet_style_prefer_inferred_tuple_names", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferInferredTupleNames")},
        {"dotnet_style_prefer_is_null_check_over_reference_equality_method", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferIsNullCheckOverReferenceEqualityMethod")},
        {"dotnet_style_prefer_simplified_boolean_expressions", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferSimplifiedBooleanExpressions")},
        {"dotnet_style_prefer_simplified_interpolation", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferSimplifiedInterpolation")},
        {"dotnet_style_qualification_for_event", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.QualifyEventAccess")},
        {"dotnet_style_qualification_for_field", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.QualifyFieldAccess")},
        {"dotnet_style_qualification_for_method", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.QualifyMethodAccess")},
        {"dotnet_style_qualification_for_property", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.QualifyPropertyAccess")},
        {"dotnet_style_readonly_field", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferReadonly")},
        {"dotnet_style_require_accessibility_modifiers", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.RequireAccessibilityModifiers")},
        {"EditorComponentOnOffOptions_Adornment", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Components", "Adornment")},
        {"EditorComponentOnOffOptions_CodeRefactorings", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Components", "Code Refactorings")},
        {"EditorComponentOnOffOptions_Tagger", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Components", "Tagger")},
        {"ExtractMethodOptions_AllowBestEffort", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Allow Best Effort")},
        {"ExtractMethodOptions_DontPutOutOrRefOnStruct", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Don't Put Out Or Ref On Strcut")},
        {"FadingOptions_FadeOutUnreachableCode", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.FadeOutUnreachableCode")},
        {"FadingOptions_FadeOutUnusedImports", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.FadeOutUnusedImports")},
        {"Storage_CloudCacheFeatureFlag", new FeatureFlagStorage(@"Roslyn.CloudCache3")},
        {"Storage_Database", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Database")},
        {"dotnet_add_imports_on_paste", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.AddImportsOnPaste2")},
        {"dotnet_always_use_default_symbol_servers", new RoamingProfileStorage("TextEditor.AlwaysUseDefaultSymbolServers")},
        {"csharp_automatically_insert_block_comment_start_string", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Auto Insert Block Comment Start String")},
        {"csharp_complete_statement_on_semicolon", new RoamingProfileStorage("TextEditor.AutomaticallyCompleteStatementOnSemicolon")},
        {"csharp_fix_string_contents_on_paste", new RoamingProfileStorage("TextEditor.%LANGUAGE%.AutomaticallyFixStringContentsOnPaste")},
        {"visual_basic_insert_abstract_or_interface_members_on_return", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.AutoRequiredMemberInsert")},
        {"visual_basic_generate_end_construct", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.AutoEndInsert")},
        {"dotnet_combine_inheritance_and_indicator_margins", new RoamingProfileStorage("TextEditor.InheritanceMarginCombinedWithIndicatorMargin")},
        {"dotnet_show_global_imports_in_inheritance_margin", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InheritanceMarginIncludeGlobalImports")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"dotnet_highlight_keywords", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Keyword Highlighting", "TextEditor.VisualBasic.Specific.EnableHighlightRelatedKeywords")},
        {"dotnet_line_separator", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Line Separator", "TextEditor.VisualBasic.Specific.DisplayLineSeparators")},
#pragma warning restore
        {"dotnet_navigate_asynchronously", new RoamingProfileStorage("TextEditor.NavigateAsynchronously")},
        {"csharp_navigate_to_decompiled_sources", new RoamingProfileStorage("TextEditor.NavigateToDecompiledSources")},
        {"csharp_navigate_to_source_link_and_embedded_sources", new RoamingProfileStorage("TextEditor.NavigateToSourceLinkAndEmbeddedSources")},
        {"dotnet_offer_remove_unused_references", new RoamingProfileStorage("TextEditor.OfferRemoveUnusedReferences")},
        {"dotnet_offer_remove_unused_references_feature_flag", new FeatureFlagStorage(@"Roslyn.RemoveUnusedReferences")},
        {"dotnet_enter_outlining_mode_when_files_open", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Outlining")},
        {"visual_basic_pretty_listing", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PrettyListing")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"dotnet_reference_highlighting", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Reference Highlighting", "TextEditor.VisualBasic.Specific.EnableHighlightReferences")},
        {"dotnet_rename_tracking_preview", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Rename Tracking Preview", "TextEditor.VisualBasic.Specific.RenameTrackingPreview")},
#pragma warning restore
        {"dotnet_show_inheritance_margin", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowInheritanceMargin")},
        {"dotnet_skip_analyzers_for_implicitly_triggered_builds", new RoamingProfileStorage("TextEditor.SkipAnalyzersForImplicitlyTriggeredBuilds")},
        {"dotnet_string_identation", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.StringIdentation")},
        {"FindUsagesOptions_DefinitionGroupingPriority", new LocalUserProfileStorage(@"Roslyn\Internal\FindUsages", "DefinitionGroupingPriority")},
        {"FormattingOptions_AutoFormattingOnReturn", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Return")},
        {"FormattingOptions_AutoFormattingOnSemicolon", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Semicolon")},
        {"FormattingOptions_AutoFormattingOnTyping", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Typing")},
        {"FormattingOptions_FormatOnPaste", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.FormatOnPaste")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"FormattingOptions_SmartIndent", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Indent Style", vbKey: "TextEditor.Basic.Indent Style")},
#pragma warning restore
        {"GenerateConstructorFromMembersOptions_AddNullChecks", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.GenerateConstructorFromMembersOptions.AddNullChecks")},
        {"GenerateEqualsAndGetHashCodeFromMembersOptions_GenerateOperators", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.GenerateEqualsAndGetHashCodeFromMembersOptions.GenerateOperators")},
        {"GenerateEqualsAndGetHashCodeFromMembersOptions_ImplementIEquatable", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.GenerateEqualsAndGetHashCodeFromMembersOptions.ImplementIEquatable")},
        {"GenerateOverridesOptions_SelectAll", new RoamingProfileStorage("TextEditor.Specific.GenerateOverridesOptions.SelectAll")},
        {"ImplementTypeOptions_InsertionBehavior", new RoamingProfileStorage("TextEditor.%LANGUAGE%.ImplementTypeOptions.InsertionBehavior")},
        {"ImplementTypeOptions_PropertyGenerationBehavior", new RoamingProfileStorage("TextEditor.%LANGUAGE%.ImplementTypeOptions.PropertyGenerationBehavior")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"indent_size", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Indent Size", vbKey: "TextEditor.Basic.Indent Size")},
        {"indent_style", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Insert Tabs", vbKey: "TextEditor.Basic.Insert Tabs")},
#pragma warning restore
        {"InlineDiagnosticsOptions_EnableInlineDiagnostics", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineDiagnostics")},
        {"InlineDiagnosticsOptions_Location", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineDiagnostics.LocationOption")},
        {"InlineHintsOptions_ColorHints", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ColorHints")},
        {"InlineHintsOptions_DisplayAllHintsWhilePressingAltF1", new RoamingProfileStorage("TextEditor.Specific.DisplayAllHintsWhilePressingAltF1")},
        {"InlineHintsOptions_EnabledForParameters", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints")},
        {"InlineHintsOptions_EnabledForTypes", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineTypeHints")},
        {"InlineHintsOptions_ForImplicitObjectCreation", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineTypeHints.ForImplicitObjectCreation")},
        {"InlineHintsOptions_ForImplicitVariableTypes", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineTypeHints.ForImplicitVariableTypes")},
        {"InlineHintsOptions_ForIndexerParameters", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForArrayIndexers")},
        {"InlineHintsOptions_ForLambdaParameterTypes", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineTypeHints.ForLambdaParameterTypes")},
        {"InlineHintsOptions_ForLiteralParameters", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForLiteralParameters")},
        {"InlineHintsOptions_ForObjectCreationParameters", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForObjectCreationParameters")},
        {"InlineHintsOptions_ForOtherParameters", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForOtherParameters")},
        {"InlineHintsOptions_SuppressForParametersThatDifferOnlyBySuffix", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.SuppressForParametersThatDifferOnlyBySuffix")},
        {"InlineHintsOptions_SuppressForParametersThatMatchArgumentName", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.SuppressForParametersThatMatchArgumentName")},
        {"InlineHintsOptions_SuppressForParametersThatMatchMethodIntent", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.SuppressForParametersThatMatchMethodIntent")},
        {"InlineRename_CollapseRenameUI", new RoamingProfileStorage("TextEditor.CollapseRenameUI")},
        {"InlineRename_UseInlineAdornment", new RoamingProfileStorage("TextEditor.RenameUseInlineAdornment")},
        {"InlineRenameSessionOptions_PreviewChanges", new RoamingProfileStorage("TextEditor.Specific.PreviewRename")},
        {"InlineRenameSessionOptions_RenameAsynchronously", new RoamingProfileStorage("TextEditor.Specific.RenameAsynchronously")},
        {"InlineRenameSessionOptions_RenameFile", new RoamingProfileStorage("TextEditor.Specific.RenameFile")},
        {"InlineRenameSessionOptions_RenameInComments", new RoamingProfileStorage("TextEditor.Specific.RenameInComments")},
        {"InlineRenameSessionOptions_RenameInStrings", new RoamingProfileStorage("TextEditor.Specific.RenameInStrings")},
        {"InlineRenameSessionOptions_RenameOverloads", new RoamingProfileStorage("TextEditor.Specific.RenameOverloads")},
        {"InternalDiagnosticsOptions_CrashOnAnalyzerException", new LocalUserProfileStorage(@"Roslyn\Internal\Diagnostics", "CrashOnAnalyzerException")},
        {"InternalDiagnosticsOptions_EnableFileLoggingForDiagnostics", new LocalUserProfileStorage(@"Roslyn\Internal\Diagnostics", "EnableFileLoggingForDiagnostics")},
        {"InternalDiagnosticsOptions_NormalDiagnosticMode", new LocalUserProfileStorage(@"Roslyn\Internal\Diagnostics", "NormalDiagnosticMode")},
        {"InternalFeatureOnOffOptions_AutomaticLineEnder", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Automatic Line Ender")},
        {"InternalFeatureOnOffOptions_BraceMatching", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Brace Matching")},
        {"InternalFeatureOnOffOptions_Classification", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Classification")},
        {"InternalFeatureOnOffOptions_EventHookup", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Event Hookup")},
        {"InternalFeatureOnOffOptions_FormatOnSave", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "FormatOnSave")},
        {"InternalFeatureOnOffOptions_FullSolutionAnalysisMemoryMonitor", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Full Solution Analysis Memory Monitor")},
        {"InternalFeatureOnOffOptions_OOP64Bit", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "OOP64Bit")},
        {"InternalFeatureOnOffOptions_OOPCoreClrFeatureFlag", new FeatureFlagStorage(@"Roslyn.ServiceHubCore")},
        {"InternalFeatureOnOffOptions_OOPServerGCFeatureFlag", new FeatureFlagStorage(@"Roslyn.OOPServerGC")},
        {"InternalFeatureOnOffOptions_RemoveRecommendationLimit", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "RemoveRecommendationLimit")},
        {"InternalFeatureOnOffOptions_RenameTracking", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Rename Tracking")},
        {"InternalFeatureOnOffOptions_SemanticColorizer", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Semantic Colorizer")},
        {"InternalFeatureOnOffOptions_ShowDebugInfo", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "ShowDebugInfo")},
        {"InternalFeatureOnOffOptions_SmartIndenter", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Smart Indenter")},
        {"InternalFeatureOnOffOptions_Snippets", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Snippets2")},
        {"InternalFeatureOnOffOptions_Squiggles", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Squiggles")},
        {"InternalFeatureOnOffOptions_SyntacticColorizer", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Syntactic Colorizer")},
        {"InternalSolutionCrawlerOptions_Solution Crawler", new LocalUserProfileStorage(@"Roslyn\Internal\SolutionCrawler", "Solution Crawler")},
        {"JsonFeatureOptions_ColorizeJsonPatterns", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ColorizeJsonPatterns")},
        {"JsonFeatureOptions_DetectAndOfferEditorFeaturesForProbableJsonStrings", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.DetectAndOfferEditorFeaturesForProbableJsonStrings")},
        {"JsonFeatureOptions_HighlightRelatedJsonComponentsUnderCursor", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.HighlightRelatedJsonComponentsUnderCursor")},
        {"JsonFeatureOptions_ReportInvalidJsonPatterns", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ReportInvalidJsonPatterns")},
        {"KeybindingResetOptions_EnabledFeatureFlag", new FeatureFlagStorage(@"Roslyn.KeybindingResetEnabled")},
        {"KeybindingResetOptions_NeedsReset", new LocalUserProfileStorage(@"Roslyn\Internal\KeybindingsStatus", "NeedsReset")},
        {"KeybindingResetOptions_NeverShowAgain", new LocalUserProfileStorage(@"Roslyn\Internal\KeybindingsStatus", "NeverShowAgain")},
        {"KeybindingResetOptions_ReSharperStatus", new LocalUserProfileStorage(@"Roslyn\Internal\KeybindingsStatus", "ReSharperStatus")},
        {"LoggerOptions_EtwLoggerKey", new LocalUserProfileStorage(@"Roslyn\Internal\Performance\Logger", "EtwLogger")},
        {"LoggerOptions_OutputWindowLoggerKey", new LocalUserProfileStorage(@"Roslyn\Internal\Performance\Logger", "OutputWindowLogger")},
        {"LoggerOptions_TraceLoggerKey", new LocalUserProfileStorage(@"Roslyn\Internal\Performance\Logger", "TraceLogger")},
        {"LspOptions_LspEditorFeatureFlag", new FeatureFlagStorage(@"Roslyn.LSP.Editor")},
        {"LspOptions_LspSemanticTokensFeatureFlag", new FeatureFlagStorage(@"Roslyn.LSP.SemanticTokens")},
        {"LspOptions_MaxCompletionListSize", new LocalUserProfileStorage(@"Roslyn\Internal\Lsp", "MaxCompletionListSize")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"NavigationBarOptions_ShowNavigationBar", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Dropdown Bar", vbKey: "TextEditor.Basic.Dropdown Bar")},
#pragma warning restore
        {"QuickInfoOptions_IncludeNavigationHintsInQuickInfo", new RoamingProfileStorage("TextEditor.Specific.IncludeNavigationHintsInQuickInfo")},
        {"QuickInfoOptions_ShowRemarksInQuickInfo", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowRemarks")},
        {"RegularExpressionsOptions_ColorizeRegexPatterns", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ColorizeRegexPatterns")},
        {"RegularExpressionsOptions_HighlightRelatedRegexComponentsUnderCursor", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.HighlightRelatedRegexComponentsUnderCursor")},
        {"RegularExpressionsOptions_ProvideRegexCompletions", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ProvideRegexCompletions")},
        {"RegularExpressionsOptions_ReportInvalidRegexPatterns", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ReportInvalidRegexPatterns")},
        {"ServiceFeatureOnOffOptions_RemoveDocumentDiagnosticsOnDocumentClose", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.RemoveDocumentDiagnosticsOnDocumentClose")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"SignatureHelpOptions_ShowSignatureHelp", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Auto List Params", vbKey: "TextEditor.Basic.Auto List Params")},
#pragma warning restore
        {"SimplificationOptions_NamingPreferences", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.NamingPreferences5")},
        {"SolutionCrawlerOptionsStorage_BackgroundAnalysisScopeOption", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.BackgroundAnalysisScopeOption")},
        {"SolutionCrawlerOptionsStorage_CompilerDiagnosticsScopeOption", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.CompilerDiagnosticsScopeOption")},
        {"SplitCommentOptions_Enabled", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.SplitComments")},
        {"SplitStringLiteralOptions_Enabled", new RoamingProfileStorage("TextEditor.CSharp.Specific.SplitStringLiterals")},
        {"StackTraceExplorerOptions_OpenOnFocus", new RoamingProfileStorage("StackTraceExplorer.Options.OpenOnFocus")},
        {"SuggestionsOptions_Asynchronous", new RoamingProfileStorage("TextEditor.Specific.Suggestions.Asynchronous4")},
        {"SuggestionsOptions_AsynchronousQuickActionsDisableFeatureFlag", new FeatureFlagStorage(@"Roslyn.AsynchronousQuickActionsDisable2")},
        {"SymbolSearchOptions_Enabled", new LocalUserProfileStorage(@"Roslyn\Features\SymbolSearch", "Enabled")},
        {"SymbolSearchOptions_SuggestForTypesInNuGetPackages", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.SuggestForTypesInNuGetPackages")},
        {"SymbolSearchOptions_SuggestForTypesInReferenceAssemblies", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.SuggestForTypesInReferenceAssemblies")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"tab_width", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Tab Size", "TextEditor.Basic.Tab Size")},
#pragma warning restore
        {"TaskListOptionsStorage_ComputeTaskListItemsForClosedFiles", new RoamingProfileStorage("TextEditor.Specific.ComputeTaskListItemsForClosedFiles")},
        {"TaskListOptionsStorage_Descriptors", new RoamingProfileStorage("Microsoft.VisualStudio.ErrorListPkg.Shims.TaskListOptions.CommentTokens")},
        {"UseConditionalExpressionOptions_ConditionalExpressionWrappingLength", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ConditionalExpressionWrappingLength")},
        {"ValidateFormatStringOption_ReportInvalidPlaceholdersInStringDotFormatCalls", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.WarnOnInvalidStringDotFormatCalls")},
        {"visual_basic_preferred_modifier_order", new RoamingProfileStorage("TextEditor.VisualBasic.Specific.PreferredModifierOrder")},
        {"visual_basic_style_prefer_isnot_expression", new RoamingProfileStorage("TextEditor.VisualBasic.Specific.PreferIsNotExpression")},
        {"visual_basic_style_prefer_simplified_object_creation", new RoamingProfileStorage("TextEditor.VisualBasic.Specific.PreferSimplifiedObjectCreation")},
        {"visual_basic_style_unused_value_assignment_preference", new RoamingProfileStorage("TextEditor.VisualBasic.Specific.UnusedValueAssignmentPreference")},
        {"visual_basic_style_unused_value_expression_statement_preference", new RoamingProfileStorage("TextEditor.VisualBasic.Specific.UnusedValueExpressionStatementPreference")},
        {"VisualStudioNavigationOptions_NavigateToObjectBrowser", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.NavigateToObjectBrowser")},
        {"VisualStudioWorkspaceStatusService_PartialLoadModeFeatureFlag", new FeatureFlagStorage(@"Roslyn.PartialLoadMode")},
        {"WorkspaceConfigurationOptions_DisableBackgroundCompilation", new FeatureFlagStorage(@"Roslyn.DisableBackgroundCompilation")},
        {"WorkspaceConfigurationOptions_DisableReferenceManagerRecoverableMetadata", new FeatureFlagStorage(@"Roslyn.DisableReferenceManagerRecoverableMetadata")},
        {"WorkspaceConfigurationOptions_DisableSharedSyntaxTrees", new FeatureFlagStorage(@"Roslyn.DisableSharedSyntaxTrees")},
        {"WorkspaceConfigurationOptions_EnableDiagnosticsInSourceGeneratedFiles", new RoamingProfileStorage("TextEditor.Roslyn.Specific.EnableDiagnosticsInSourceGeneratedFilesExperiment")},
        {"WorkspaceConfigurationOptions_EnableDiagnosticsInSourceGeneratedFilesFeatureFlag", new FeatureFlagStorage(@"Roslyn.EnableDiagnosticsInSourceGeneratedFiles")},
        {"WorkspaceConfigurationOptions_EnableOpeningSourceGeneratedFilesInWorkspace", new RoamingProfileStorage("TextEditor.Roslyn.Specific.EnableOpeningSourceGeneratedFilesInWorkspaceExperiment")},
        {"WorkspaceConfigurationOptions_EnableOpeningSourceGeneratedFilesInWorkspaceFeatureFlag", new FeatureFlagStorage(@"Roslyn.SourceGeneratorsEnableOpeningInWorkspace")},
        {"XamlOptions_EnableLspIntelliSenseFeatureFlag", new FeatureFlagStorage(@"Xaml.EnableLspIntelliSense")},
    };
}
