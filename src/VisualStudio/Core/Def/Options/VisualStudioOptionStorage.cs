// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal abstract class VisualStudioOptionStorage
{
    internal sealed class UnifiedSettingsManagerStorage(string key) : VisualStudioOptionStorage
    {
        private const string LanguagePlaceholder = "%LANGUAGE%";

        /// <summary>
        /// Key may contain <see cref="LanguagePlaceholder"/> that is replaced by the language name.
        /// </summary>
        public string Key { get; } = key;

        private string GetKey(string? language)
            => SubstituteLanguage(Key, language);

        private static string SubstituteLanguage(string keyName, string? language)
            // These constants come from the platform themselves.  It is how they store things in settings.json
            => keyName.Replace(LanguagePlaceholder, language switch
            {
                LanguageNames.CSharp => "csharp",
                LanguageNames.VisualBasic => "basic",
                LanguageNames.FSharp => "fsharp",
                "Xaml" => "xaml",
                // 'S' in 'typeScript' being capitalized is intentional.
                "TypeScript" => "typeScript",
                _ => language,
            });

        public Task PersistAsync(VisualStudioUnifiedSettingsOptionPersister persister, OptionKey2 optionKey, object? value)
            => persister.PersistAsync(optionKey, GetKey(optionKey.Language), value);

        public bool TryFetch(VisualStudioUnifiedSettingsOptionPersister persister, OptionKey2 optionKey, out object? value)
            => persister.TryFetch(optionKey, GetKey(optionKey.Language), out value);
    }

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
            => persister.PersistAsync(GetKey(optionKey.Language), value);

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

    internal sealed class LocalUserProfileStorage(string path, string key) : VisualStudioOptionStorage
    {
        public string Path => path;
        public string Key => key;

        public Task PersistAsync(LocalUserRegistryOptionPersister persister, OptionKey2 optionKey, object? value)
        {
            persister.Persist(optionKey, path, key, value);
            return Task.CompletedTask;
        }

        public bool TryFetch(LocalUserRegistryOptionPersister persister, OptionKey2 optionKey, out object? value)
            => persister.TryFetch(optionKey, path, key, out value);
    }

    public static readonly IReadOnlyDictionary<string, VisualStudioOptionStorage> Storages = new Dictionary<string, VisualStudioOptionStorage>()
    {
        // Modern settings-manager stored options.
        {"indent_size", new UnifiedSettingsManagerStorage("languages.%LANGUAGE%.tabs.indentSize")},
        {"indent_style", new UnifiedSettingsManagerStorage("languages.%LANGUAGE%.tabs.character")},
        {"tab_width", new UnifiedSettingsManagerStorage("languages.%LANGUAGE%.tabs.tabSize")},
        {"smart_indent", new UnifiedSettingsManagerStorage("languages.%LANGUAGE%arp.tabs.indenting")},

        {"dotnet_analyze_copilot_changes", new FeatureFlagStorage(@"Roslyn.AnalyzeCopilotChanges")},
        {"dotnet_copilot_fix_code_format", new FeatureFlagStorage(@"Roslyn.Copilot.FixCodeFormat")},
        {"dotnet_copilot_fix_add_missing_tokens", new FeatureFlagStorage(@"Roslyn.Copilot.FixAddMissingTokens")},
        {"dotnet_copilot_fix_add_missing_imports", new FeatureFlagStorage(@"Roslyn.Copilot.FixAddMissingImports")},
        {"dotnet_collapse_empty_metadata_implementations_when_first_opened", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.CollapseEmptyMetadataImplementationsWhenFirstOpened")},
        {"dotnet_collapse_imports_when_first_opened", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.CollapseImportsWhenFirstOpened")},
        {"dotnet_collapse_metadata_implementations_when_first_opened", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.CollapseMetadataImplementationsWhenFirstOpened")},
        {"dotnet_collapse_regions_when_collapsing_to_definitions", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.CollapseRegionsWhenCollapsingToDefinitions")},
        {"dotnet_collapse_local_functions_when_collapsing_to_definitions", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.CollapseLocalFunctionsWhenCollapsingToDefinitions")},
        {"dotnet_collapse_regions_when_first_opened", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.CollapseRegionsWhenFirstOpened")},
        {"dotnet_maximum_block_banner_length", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.MaximumBannerLength")},
        {"dotnet_show_block_structure_guides_for_code_level_constructs", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowBlockStructureGuidesForCodeLevelConstructs")},
        {"dotnet_show_block_structure_guides_for_comments_and_preprocessor_regions", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions")},
        {"dotnet_show_block_structure_guides_for_declaration_level_constructs", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowBlockStructureGuidesForDeclarationLevelConstructs")},
        {"dotnet_show_outlining_for_code_level_constructs", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowOutliningForCodeLevelConstructs")},
        {"dotnet_show_outlining_for_comments_and_preprocessor_regions", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowOutliningForCommentsAndPreprocessorRegions")},
        {"dotnet_show_outlining_for_declaration_level_constructs", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowOutliningForDeclarationLevelConstructs")},
        {"csharp_format_on_close_brace", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Close Brace")},
        {"dotnet_classify_reassigned_variables", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ClassificationOptions.ClassifyReassignedVariables")},
        {"dotnet_classify_obsolete_symbols", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ClassificationOptions.ClassifyObsoleteSymbols")},
        {"dotnet_prefer_system_hash_code", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferSystemHashCode")},
        {"block_for_completion_items", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.BlockForCompletionItems")},
        {"dotnet_enable_argument_completion_snippets", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.EnableArgumentCompletionSnippets")},
        {"dotnet_return_key_completion_behavior", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.EnterKeyBehavior")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"dotnet_hide_advanced_members", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Hide Advanced Auto List Members", vbKey: "TextEditor.Basic.Hide Advanced Auto List Members")},
#pragma warning restore
        {"dotnet_highlight_matching_portions_of_completion_list_items", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.HighlightMatchingPortionsOfCompletionListItems")},
        {"dotnet_show_completion_item_filters", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowCompletionItemFilters")},
        {"dotnet_show_completion_items_from_unimported_namespaces", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowItemsFromUnimportedNamespaces")},
        {"dotnet_show_name_completion_suggestions", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowNameSuggestions")},
        {"dotnet_show_new_snippet_experience_feature_flag", new FeatureFlagStorage(@"Roslyn.SnippetCompletion")},
        {"dotnet_show_new_snippet_experience", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowNewSnippetExperience")},
        {"dotnet_snippets_behavior", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.SnippetsBehavior")},
        {"dotnet_trigger_completion_in_argument_lists", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.TriggerInArgumentLists")},
        {"dotnet_trigger_completion_on_deletion", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.TriggerOnDeletion")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"dotnet_trigger_completion_on_typing", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Auto List Members", vbKey: "TextEditor.Basic.Auto List Members")},
#pragma warning restore
        {"dotnet_trigger_completion_on_typing_letters", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.TriggerOnTypingLetters")},
        {"dotnet_disable_unnamed_symbol_completion", new FeatureFlagStorage(@"Roslyn.UnnamedSymbolCompletionDisabled")},
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
        {"csharp_prefer_static_anonymous_function", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferStaticAnonymousFunction")},
        {"csharp_prefer_system_threading_lock",  new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferSystemThreadingLock")},
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
        {"csharp_style_prefer_implicitly_typed_lambda_expression", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferImplicitlyTypedLambdaExpression")},
        {"csharp_style_prefer_index_operator", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferIndexOperator")},
        {"csharp_style_prefer_local_over_anonymous_function", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferLocalOverAnonymousFunction")},
        {"csharp_style_prefer_method_group_conversion", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferMethodGroupConversion")},
        {"csharp_style_prefer_not_pattern", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferNotPattern")},
        {"csharp_style_prefer_null_check_over_type_check", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferNullCheckOverTypeCheck")},
        {"csharp_style_prefer_pattern_matching", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferPatternMatching")},
        {"csharp_style_prefer_primary_constructors", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferPrimaryConstructors")},
        {"csharp_style_prefer_range_operator", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferRangeOperator")},
        {"csharp_style_prefer_readonly_struct", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferReadOnlyStruct")},
        {"csharp_style_prefer_readonly_struct_member", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferReadOnlyStructMember")},
        {"csharp_style_prefer_simple_property_accessors", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferSimplePropertyAccessors")},
        {"csharp_style_prefer_switch_expression", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferSwitchExpression")},
        {"csharp_style_prefer_top_level_statements", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferTopLevelStatements")},
        {"csharp_style_prefer_tuple_swap", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferTupleSwap")},
        {"csharp_style_prefer_unbound_generic_type_in_nameof", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferUnboundGenericTypeInNameOf")},
        {"csharp_style_prefer_utf8_string_literals", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferUtf8StringLiterals")},
        {"csharp_style_throw_expression", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferThrowExpression")},
        {"csharp_style_unused_value_assignment_preference", new RoamingProfileStorage("TextEditor.CSharp.Specific.UnusedValueAssignmentPreference")},
        {"csharp_style_unused_value_expression_statement_preference", new RoamingProfileStorage("TextEditor.CSharp.Specific.UnusedValueExpressionStatementPreference")},
        {"csharp_style_var_elsewhere", new RoamingProfileStorage("TextEditor.CSharp.Specific.UseImplicitTypeWherePossible")},
        {"csharp_style_var_for_built_in_types", new RoamingProfileStorage("TextEditor.CSharp.Specific.UseImplicitTypeForIntrinsicTypes")},
        {"csharp_style_var_when_type_is_apparent", new RoamingProfileStorage("TextEditor.CSharp.Specific.UseImplicitTypeWhereApparent")},
        {"csharp_using_directive_placement", new RoamingProfileStorage("TextEditor.CSharp.Specific.PreferredUsingDirectivePlacement")},
        {"dotnet_provide_date_and_time_completions", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ProvideDateAndTimeCompletions")},
        {"dotnet_log_telemetry_for_background_analyzer_execution", new FeatureFlagStorage(@"Roslyn.LogTelemetryForBackgroundAnalyzerExecution")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"dotnet_auto_xml_doc_comment_generation", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Automatic XML Doc Comment Generation", "TextEditor.VisualBasic.Specific.AutoComment")},
#pragma warning restore
        {"dotnet_code_quality_unused_parameters", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.UnusedParametersPreference")},
        {"dotnet_separate_import_directive_groups", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.SeparateImportDirectiveGroups")},
        {"dotnet_sort_system_directives_first", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PlaceSystemNamespaceFirst")},
        {"dotnet_style_allow_multiple_blank_lines_experimental", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.AllowMultipleBlankLines")},
        {"dotnet_style_allow_statement_immediately_after_block_experimental", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.AllowStatementImmediatelyAfterBlock")},
        {"dotnet_style_coalesce_expression", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferCoalesceExpression")},
        {"dotnet_style_prefer_collection_expression", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PreferCollectionExpression")},
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
        {"dotnet_enable_editor_adornment", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Components", "Adornment")},
        {"dotnet_enable_code_refactorings", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Components", "Code Refactorings")},
        {"dotnet_enable_editor_tagger", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Components", "Tagger")},
        {"dotnet_allow_best_effort_when_extracting_method", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Allow Best Effort")},
        {"dotnet_fade_out_unreachable_code", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.FadeOutUnreachableCode")},
        {"dotnet_fade_out_unused_imports", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.FadeOutUnusedImports")},
        {"dotnet_fade_out_unused_members", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.FadeOutUnusedMembers")},
        {"dotnet_add_imports_on_paste", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.AddImportsOnPaste2")},
        {"dotnet_always_use_default_symbol_servers", new RoamingProfileStorage("TextEditor.AlwaysUseDefaultSymbolServers")},
        {"csharp_insert_block_comment_start_string", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Auto Insert Block Comment Start String")},
        {"csharp_complete_statement_on_semicolon", new RoamingProfileStorage("TextEditor.AutomaticallyCompleteStatementOnSemicolon")},
        {"dotnet_fix_string_contents_on_paste", new RoamingProfileStorage("TextEditor.%LANGUAGE%.AutomaticallyFixStringContentsOnPaste")},
        {"visual_basic_insert_abstract_or_interface_members_on_return", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.AutoRequiredMemberInsert")},
        {"visual_basic_generate_end_construct", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.AutoEndInsert")},
        {"dotnet_combine_inheritance_and_indicator_margins", new RoamingProfileStorage("TextEditor.InheritanceMarginCombinedWithIndicatorMargin")},
        {"dotnet_show_global_imports_in_inheritance_margin", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InheritanceMarginIncludeGlobalImports")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"dotnet_highlight_keywords", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Keyword Highlighting", "TextEditor.VisualBasic.Specific.EnableHighlightRelatedKeywords")},
        {"dotnet_display_line_separators", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Line Separator", "TextEditor.VisualBasic.Specific.DisplayLineSeparators")},
#pragma warning restore
        {"dotnet_navigate_to_decompiled_sources", new RoamingProfileStorage("TextEditor.NavigateToDecompiledSources")},
        {"dotnet_navigate_to_source_link_and_embedded_sources", new RoamingProfileStorage("TextEditor.NavigateToSourceLinkAndEmbeddedSources")},
        {"dotnet_offer_remove_unused_references", new RoamingProfileStorage("TextEditor.OfferRemoveUnusedReferences")},
        {"dotnet_enter_outlining_mode_on_file_open", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Outlining")},
        {"visual_basic_pretty_listing", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.PrettyListing")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"dotnet_highlight_references", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Reference Highlighting", "TextEditor.VisualBasic.Specific.EnableHighlightReferences")},
        {"dotnet_show_preview_for_rename_tracking", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Rename Tracking Preview", "TextEditor.VisualBasic.Specific.RenameTrackingPreview")},
#pragma warning restore
        {"dotnet_show_inheritance_margin", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowInheritanceMargin")},
        {"dotnet_skip_analyzers_for_implicitly_triggered_builds", new RoamingProfileStorage("TextEditor.SkipAnalyzersForImplicitlyTriggeredBuilds")},
        {"dotnet_indent_strings", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.StringIdentation")},
        {"dotnet_find_usage_definition_grouping_priority", new LocalUserProfileStorage(@"Roslyn\Internal\FindUsages", "DefinitionGroupingPriority")},
        {"csharp_format_on_return", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Return")},
        {"csharp_format_on_semicolon", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Semicolon")},
        {"csharp_format_on_typing", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Typing")},
        {"dotnet_format_on_paste", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.FormatOnPaste")},
        {"dotnet_generate_constructor_parameter_null_checks", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.GenerateConstructorFromMembersOptions.AddNullChecks")},
        {"dotnet_generate_equality_operators", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.GenerateEqualsAndGetHashCodeFromMembersOptions.GenerateOperators")},
        {"dotnet_generate_iequatable_implementation", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.GenerateEqualsAndGetHashCodeFromMembersOptions.ImplementIEquatable")},
        {"dotnet_generate_overrides_for_all_members", new RoamingProfileStorage("TextEditor.Specific.GenerateOverridesOptions.SelectAll")},
        {"dotnet_member_insertion_location", new RoamingProfileStorage("TextEditor.%LANGUAGE%.ImplementTypeOptions.InsertionBehavior")},
        {"dotnet_property_generation_behavior", new RoamingProfileStorage("TextEditor.%LANGUAGE%.ImplementTypeOptions.PropertyGenerationBehavior")},
        {"dotnet_enable_inline_diagnostics", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineDiagnostics")},
        {"dotnet_inline_diagnostics_location", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineDiagnostics.LocationOption")},
        {"dotnet_colorize_inline_hints", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ColorHints")},
        {"dotnet_display_inline_hints_while_pressing_alt_f1", new RoamingProfileStorage("TextEditor.Specific.DisplayAllHintsWhilePressingAltF1")},
        {"dotnet_enable_inlay_hints_for_parameters", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints")},
        {"csharp_enable_inlay_hints_for_types", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineTypeHints")},
        {"csharp_enable_inlay_hints_for_implicit_object_creation", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineTypeHints.ForImplicitObjectCreation")},
        {"csharp_enable_inlay_hints_for_collection_expressions", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineTypeHints.ForCollectionExpressions")},
        {"csharp_enable_inlay_hints_for_implicit_variable_types", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineTypeHints.ForImplicitVariableTypes")},
        {"dotnet_enable_inlay_hints_for_indexer_parameters", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForArrayIndexers")},
        {"csharp_enable_inlay_hints_for_lambda_parameter_types", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineTypeHints.ForLambdaParameterTypes")},
        {"dotnet_enable_inlay_hints_for_literal_parameters", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForLiteralParameters")},
        {"dotnet_enable_inlay_hints_for_object_creation_parameters", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForObjectCreationParameters")},
        {"dotnet_enable_inlay_hints_for_other_parameters", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.ForOtherParameters")},
        {"dotnet_suppress_inlay_hints_for_parameters_that_differ_only_by_suffix", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.SuppressForParametersThatDifferOnlyBySuffix")},
        {"dotnet_suppress_inlay_hints_for_parameters_that_match_argument_name", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.SuppressForParametersThatMatchArgumentName")},
        {"dotnet_suppress_inlay_hints_for_parameters_that_match_method_intent", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.InlineParameterNameHints.SuppressForParametersThatMatchMethodIntent")},
        {"dotnet_collapse_inline_rename_ui", new RoamingProfileStorage("TextEditor.CollapseRenameUI")},
        {"dotnet_preview_inline_rename_changes", new RoamingProfileStorage("TextEditor.Specific.PreviewRename")},
        {"dotnet_collapse_suggestions_in_inline_rename_ui", new RoamingProfileStorage("TextEditor.CollapseRenameSuggestionsUI")},
        {"dotnet_rename_get_suggestions_automatically", new FeatureFlagStorage(@"Editor.AutoSmartRenameSuggestions")},
        // Option is deprecated, don't use the same RoamingProfileStorage key
        {"dotnet_rename_file", new RoamingProfileStorage("TextEditor.Specific.RenameFile")},
        {"dotnet_rename_in_comments", new RoamingProfileStorage("TextEditor.Specific.RenameInComments")},
        {"dotnet_rename_in_strings", new RoamingProfileStorage("TextEditor.Specific.RenameInStrings")},
        {"dotnet_rename_overloads", new RoamingProfileStorage("TextEditor.Specific.RenameOverloads")},
        {"dotnet_crash_on_analyzer_exception", new LocalUserProfileStorage(@"Roslyn\Internal\Diagnostics", "CrashOnAnalyzerException")},
        {"visual_studio_enable_file_logging_for_diagnostics", new LocalUserProfileStorage(@"Roslyn\Internal\Diagnostics", "EnableFileLoggingForDiagnostics")},
        {"dotnet_enable_automatic_line_ender", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Automatic Line Ender")},
        {"dotnet_enable_brace_matching", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Brace Matching")},
        {"dotnet_enable_event_hook_up", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Event Hookup")},
        {"dotnet_format_on_save", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "FormatOnSave")},
        {"dotnet_code_analysis_in_separate_process", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "DotnetCodeAnalysisInSeparateProcess")},
        {"dotnet_enable_server_garbage_collection_in_code_analysis_process", new FeatureFlagStorage(@"Roslyn.OOPServerGC")},
        {"dotnet_remove_intellicode_recommendation_limit", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "RemoveRecommendationLimit")},
        {"dotnet_enable_rename_tracking", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Rename Tracking")},
        {"dotnet_enable_semantic_colorizer", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Semantic Colorizer")},
        {"dotnet_show_intellicode_debug_info", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "ShowDebugInfo")},
        {"dotnet_enable_smart_indenter", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Smart Indenter")},
        {"dotnet_enable_snippets", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Snippets2")},
        {"dotnet_enable_syntactic_colorizer", new LocalUserProfileStorage(@"Roslyn\Internal\OnOff\Features", "Syntactic Colorizer")},
        {"dotnet_enable_solution_crawler", new LocalUserProfileStorage(@"Roslyn\Internal\SolutionCrawler", "Solution Crawler")},
        {"dotnet_colorize_json_patterns", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ColorizeJsonPatterns")},
        {"dotnet_unsupported_detect_and_offer_editor_features_for_probable_json_strings", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.DetectAndOfferEditorFeaturesForProbableJsonStrings")},
        {"dotnet_highlight_related_json_components", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.HighlightRelatedJsonComponentsUnderCursor")},
        {"dotnet_unsupported_report_invalid_json_patterns", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ReportInvalidJsonPatterns")},
        {"visual_studio_enable_key_binding_reset", new FeatureFlagStorage("Roslyn.KeybindingResetEnabled")},
        {"visual_studio_enable_copilot_rename_context", new FeatureFlagStorage("Roslyn.CopilotRenameGetContext")},
        {"visual_studio_key_binding_needs_reset", new LocalUserProfileStorage(@"Roslyn\Internal\KeybindingsStatus", "NeedsReset")},
        {"visual_studio_key_binding_reset_never_show_again", new LocalUserProfileStorage(@"Roslyn\Internal\KeybindingsStatus", "NeverShowAgain")},
        {"visual_studio_resharper_key_binding_status", new LocalUserProfileStorage(@"Roslyn\Internal\KeybindingsStatus", "ReSharperStatus")},
        {"visual_studio_etw_logger_key", new LocalUserProfileStorage(@"Roslyn\Internal\Performance\Logger", "EtwLogger")},
        {"visual_studio_output_window_logger_key", new LocalUserProfileStorage(@"Roslyn\Internal\Performance\Logger", "OutputWindowLogger")},
        {"visual_studio_trace_logger_key", new LocalUserProfileStorage(@"Roslyn\Internal\Performance\Logger", "TraceLogger")},
        {"dotnet_enable_lsp_editor", new FeatureFlagStorage(@"Roslyn.LSP.Editor")},
        {"dotnet_enable_lsp_semantic_tokens", new FeatureFlagStorage(@"Roslyn.LSP.SemanticTokens")},
        {"dotnet_lsp_max_completion_list_size", new LocalUserProfileStorage(@"Roslyn\Internal\Lsp", "MaxCompletionListSize")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"dotnet_show_navigation_bar", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Dropdown Bar", vbKey: "TextEditor.Basic.Dropdown Bar")},
#pragma warning restore
        {"dotnet_show_navigation_hints_in_quick_info", new RoamingProfileStorage("TextEditor.Specific.IncludeNavigationHintsInQuickInfo")},
        {"dotnet_show_remarks_in_quick_info", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ShowRemarks")},
        {"dotnet_colorize_regex_patterns", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ColorizeRegexPatterns")},
        {"dotnet_highlight_related_regex_components", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.HighlightRelatedRegexComponentsUnderCursor")},
        {"dotnet_provide_regex_completions", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ProvideRegexCompletions")},
        {"dotnet_unsupported_report_invalid_regex_patterns", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ReportInvalidRegexPatterns")},
#pragma warning disable CS0612 // Type or member is obsolete
        {"dotnet_show_signature_help", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Auto List Params", vbKey: "TextEditor.Basic.Auto List Params")},
#pragma warning restore
        {"dotnet_naming_preferences", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.NamingPreferences5")},
        {"dotnet_analyzer_diagnostics_scope", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.BackgroundAnalysisScopeOption")},
        {"dotnet_compiler_diagnostics_scope", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.CompilerDiagnosticsScopeOption")},
        {"dotnet_split_comments", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.SplitComments")},
        {"csharp_split_string_literal_on_return", new RoamingProfileStorage("TextEditor.CSharp.Specific.SplitStringLiterals")},
        {"visual_studio_open_stack_trace_explorer_on_focus", new RoamingProfileStorage("StackTraceExplorer.Options.OpenOnFocus")},
        {"visual_studio_document_outline_sort_order", new RoamingProfileStorage(@"DocumentOutline.SortOrder")},
        {"visual_studio_enable_symbol_search", new LocalUserProfileStorage(@"Roslyn\Features\SymbolSearch", "Enabled")},
        {"dotnet_unsupported_search_nuget_packages", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.SuggestForTypesInNuGetPackages")},
        {"dotnet_search_reference_assemblies", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.SuggestForTypesInReferenceAssemblies")},
        {"dotnet_unsupported_search_referenced_project_symbols", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.SearchReferencedProjectSymbols")},
        {"dotnet_unsupported_search_unreferenced_project_symbols", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.SearchUnreferencedProjectSymbols")},
        {"dotnet_unsupported_search_unreferenced_metadata_symbols", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.SearchUnreferencedMetadataSymbols")},
        {"dotnet_compute_task_list_items_for_closed_files", new RoamingProfileStorage("TextEditor.Specific.ComputeTaskListItemsForClosedFiles")},
        {"dotnet_task_list_storage_descriptors", new RoamingProfileStorage("Microsoft.VisualStudio.ErrorListPkg.Shims.TaskListOptions.CommentTokens")},
        {"dotnet_unsupported_conditional_expression_wrapping_length", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.ConditionalExpressionWrappingLength")},
        {"csharp_unsupported_collection_expression_wrapping_length", new RoamingProfileStorage("TextEditor.CSharp.Specific.CollectionExpressionWrappingLength")},
        {"dotnet_unsupported_report_invalid_placeholders_in_string_dot_format_calls", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.WarnOnInvalidStringDotFormatCalls")},
        {"visual_basic_preferred_modifier_order", new RoamingProfileStorage("TextEditor.VisualBasic.Specific.PreferredModifierOrder")},
        {"visual_basic_style_prefer_isnot_expression", new RoamingProfileStorage("TextEditor.VisualBasic.Specific.PreferIsNotExpression")},
        {"visual_basic_style_prefer_simplified_object_creation", new RoamingProfileStorage("TextEditor.VisualBasic.Specific.PreferSimplifiedObjectCreation")},
        {"visual_basic_style_unused_value_assignment_preference", new RoamingProfileStorage("TextEditor.VisualBasic.Specific.UnusedValueAssignmentPreference")},
        {"visual_basic_style_unused_value_expression_statement_preference", new RoamingProfileStorage("TextEditor.VisualBasic.Specific.UnusedValueExpressionStatementPreference")},
        {"visual_studio_navigate_to_object_browser", new RoamingProfileStorage("TextEditor.%LANGUAGE%.Specific.NavigateToObjectBrowser")},
        {"dotnet_validate_compilation_tracker_states", new FeatureFlagStorage(@"Roslyn.ValidateCompilationTrackerStates")},
        {"dotnet_source_generator_execution", new RoamingProfileStorage("TextEditor.Roslyn.Specific.SourceGeneratorExecution")},
        {"xaml_enable_lsp_intellisense", new FeatureFlagStorage(@"Xaml.EnableLspIntelliSense")},
        {"razor_use_cohosting", new FeatureFlagStorage("Razor.LSP.UseRazorCohostServer")},
    };
}
