// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EditorConfigSettings.DataProvider.CodeStyle
{
    internal class CSharpCodeStyleSettingsProvider : SettingsProviderBase<CodeStyleSetting, OptionUpdater, IOption2, object>
    {
        public CSharpCodeStyleSettingsProvider(string fileName, OptionUpdater settingsUpdater, Workspace workspace, IGlobalOptionService globalOptions)
            : base(fileName, settingsUpdater, workspace, globalOptions)
        {
            Update();
        }

        protected override void UpdateOptions(TieredAnalyzerConfigOptions options, ImmutableArray<Project> projectsInScope)
        {
            var varSettings = GetVarCodeStyleOptions(options, SettingsUpdater);
            AddRange(varSettings);

            var usingSettings = GetUsingsCodeStyleOptions(options, SettingsUpdater);
            AddRange(usingSettings);

            var modifierSettings = GetModifierCodeStyleOptions(options, SettingsUpdater);
            AddRange(modifierSettings);

            var codeBlockSettings = GetCodeBlockCodeStyleOptions(options, SettingsUpdater);
            AddRange(codeBlockSettings);

            var nullCheckingSettings = GetNullCheckingCodeStyleOptions(options, SettingsUpdater);
            AddRange(nullCheckingSettings);

            var expressionSettings = GetExpressionCodeStyleOptions(options, SettingsUpdater);
            AddRange(expressionSettings);

            var patternMatchingSettings = GetPatternMatchingCodeStyleOptions(options, SettingsUpdater);
            AddRange(patternMatchingSettings);

            var variableSettings = GetVariableCodeStyleOptions(options, SettingsUpdater);
            AddRange(variableSettings);

            var expressionBodySettings = GetExpressionBodyCodeStyleOptions(options, SettingsUpdater);
            AddRange(expressionBodySettings);

            var unusedValueSettings = GetUnusedValueCodeStyleOptions(options, SettingsUpdater);
            AddRange(unusedValueSettings);
        }

        private static IEnumerable<CodeStyleSetting> GetVarCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            var trueValueDescription = CSharpVSResources.Prefer_var;
            var falseValueDescription = CSharpVSResources.Prefer_explicit_type;

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.VarForBuiltInTypes, CSharpVSResources.For_built_in_types, options, updater, trueValueDescription, falseValueDescription);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.VarWhenTypeIsApparent, CSharpVSResources.When_variable_type_is_apparent, options, updater, trueValueDescription, falseValueDescription);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.VarElsewhere, CSharpVSResources.Elsewhere, options, updater, trueValueDescription, falseValueDescription);
        }

        private static IEnumerable<CodeStyleSetting> GetUsingsCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, CSharpVSResources.Preferred_using_directive_placement, options, updater,
                enumValues: [AddImportPlacement.InsideNamespace, AddImportPlacement.OutsideNamespace],
                valueDescriptions: [CSharpVSResources.Inside_namespace, CSharpVSResources.Outside_namespace]);
        }

        private static IEnumerable<CodeStyleSetting> GetNullCheckingCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferThrowExpression, CSharpVSResources.Prefer_throw_expression, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferConditionalDelegateCall, CSharpVSResources.Prefer_conditional_delegate_call, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferNullCheckOverTypeCheck, CSharpVSResources.Prefer_null_check_over_type_check, options, updater);
        }

        private static IEnumerable<CodeStyleSetting> GetModifierCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferStaticLocalFunction, ServicesVSResources.Prefer_static_local_functions, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferStaticAnonymousFunction, ServicesVSResources.Prefer_static_anonymous_functions, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferReadOnlyStruct, ServicesVSResources.Prefer_read_only_struct, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferReadOnlyStructMember, ServicesVSResources.Prefer_read_only_struct_member, options, updater);
        }

        private static IEnumerable<CodeStyleSetting> GetCodeBlockCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferSimpleUsingStatement, ServicesVSResources.Prefer_simple_using_statement, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferBraces, ServicesVSResources.Prefer_braces, options, updater,
                enumValues: [PreferBracesPreference.Always, PreferBracesPreference.None, PreferBracesPreference.WhenMultiline],
                valueDescriptions: [ServicesVSResources.Yes, ServicesVSResources.No, CSharpVSResources.When_on_multiple_lines]);

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.NamespaceDeclarations, ServicesVSResources.Namespace_declarations, options, updater,
                enumValues: [NamespaceDeclarationPreference.BlockScoped, NamespaceDeclarationPreference.FileScoped],
                valueDescriptions: [CSharpVSResources.Block_scoped, CSharpVSResources.File_scoped]);

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferMethodGroupConversion, ServicesVSResources.Prefer_method_group_conversion, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferTopLevelStatements, ServicesVSResources.Prefer_top_level_statements, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferPrimaryConstructors, ServicesVSResources.Prefer_primary_constructors, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferSystemThreadingLock, ServicesVSResources.Prefer_System_Threading_Lock, options, updater);
        }

        private static IEnumerable<CodeStyleSetting> GetExpressionCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferSwitchExpression, CSharpVSResources.Prefer_switch_expression, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferSimpleDefaultExpression, ServicesVSResources.Prefer_simple_default_expression, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction, ServicesVSResources.Prefer_local_function_over_anonymous_function, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferIndexOperator, ServicesVSResources.Prefer_index_operator, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferRangeOperator, ServicesVSResources.Prefer_range_operator, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.ImplicitObjectCreationWhenTypeIsApparent, CSharpVSResources.Prefer_implicit_object_creation_when_type_is_apparent, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferTupleSwap, ServicesVSResources.Prefer_tuple_swap, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferUtf8StringLiterals, ServicesVSResources.Prefer_Utf8_string_literals, options, updater);
        }

        private static IEnumerable<CodeStyleSetting> GetPatternMatchingCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferPatternMatching, CSharpVSResources.Prefer_pattern_matching, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, CSharpVSResources.Prefer_pattern_matching_over_is_with_cast_check, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, CSharpVSResources.Prefer_pattern_matching_over_as_with_null_check, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferNotPattern, CSharpVSResources.Prefer_pattern_matching_over_mixed_type_check, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferExtendedPropertyPattern, CSharpVSResources.Prefer_extended_property_pattern, options, updater);
        }

        private static IEnumerable<CodeStyleSetting> GetVariableCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferInlinedVariableDeclaration, ServicesVSResources.Prefer_inlined_variable_declaration, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferDeconstructedVariableDeclaration, ServicesVSResources.Prefer_deconstructed_variable_declaration, options, updater);
        }

        private static IEnumerable<CodeStyleSetting> GetExpressionBodyCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            var enumValues = new[] { ExpressionBodyPreference.Never, ExpressionBodyPreference.WhenPossible, ExpressionBodyPreference.WhenOnSingleLine };
            var valueDescriptions = new[] { CSharpVSResources.Never, CSharpVSResources.When_possible, CSharpVSResources.When_on_single_line };

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, ServicesVSResources.Use_expression_body_for_methods, options, updater, enumValues, valueDescriptions);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, ServicesVSResources.Use_expression_body_for_constructors, options, updater, enumValues, valueDescriptions);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferExpressionBodiedOperators, ServicesVSResources.Use_expression_body_for_operators, options, updater, enumValues, valueDescriptions);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ServicesVSResources.Use_expression_body_for_properties, options, updater, enumValues, valueDescriptions);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ServicesVSResources.Use_expression_body_for_indexers, options, updater, enumValues, valueDescriptions);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ServicesVSResources.Use_expression_body_for_accessors, options, updater, enumValues, valueDescriptions);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, ServicesVSResources.Use_expression_body_for_lambdas, options, updater, enumValues, valueDescriptions);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, ServicesVSResources.Use_expression_body_for_local_functions, options, updater, enumValues, valueDescriptions);
        }

        private static IEnumerable<CodeStyleSetting> GetUnusedValueCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            var enumValues = new[] { UnusedValuePreference.UnusedLocalVariable, UnusedValuePreference.DiscardVariable };
            var valueDescriptions = new[] { CSharpVSResources.Unused_local, CSharpVSResources.Discard };

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.UnusedValueAssignment, ServicesVSResources.Avoid_unused_value_assignments, options, updater, enumValues, valueDescriptions);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.UnusedValueExpressionStatement, ServicesVSResources.Avoid_expression_statements_that_implicitly_ignore_value, options, updater, enumValues, valueDescriptions);

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CSharpVSResources.Allow_embedded_statements_on_same_line, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CSharpVSResources.Allow_blank_lines_between_consecutive_braces, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CSharpVSResources.Allow_blank_line_after_colon_in_constructor_initializer, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CSharpVSResources.Allow_blank_line_after_token_in_conditional_expression, options, updater);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CSharpVSResources.Allow_blank_line_after_token_in_arrow_expression_clause, options, updater);
        }
    }
}
