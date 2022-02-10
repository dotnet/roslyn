// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EditorConfigSettings.DataProvider.CodeStyle
{
    internal class CSharpCodeStyleSettingsProvider : SettingsProviderBase<CodeStyleSetting, OptionUpdater, IOption2, object>
    {
        public CSharpCodeStyleSettingsProvider(string fileName, OptionUpdater settingsUpdater, Workspace workspace)
            : base(fileName, settingsUpdater, workspace)
        {
            Update();
        }

        protected override void UpdateOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions)
        {
            var varSettings = GetVarCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(varSettings);

            var usingSettings = GetUsingsCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(usingSettings);

            var modifierSettings = GetModifierCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(modifierSettings);

            var codeBlockSettings = GetCodeBlockCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(codeBlockSettings);

            var nullCheckingSettings = GetNullCheckingCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(nullCheckingSettings);

            var expressionSettings = GetExpressionCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(expressionSettings);

            var patternMatchingSettings = GetPatternMatchingCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(patternMatchingSettings);

            var variableSettings = GetVariableCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(variableSettings);

            var expressionBodySettings = GetExpressionBodyCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(expressionBodySettings);

            var unusedValueSettings = GetUnusedValueCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(unusedValueSettings);
        }

        private IEnumerable<CodeStyleSetting> GetVarCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.VarForBuiltInTypes,
                description: CSharpVSResources.For_built_in_types,
                trueValueDescription: CSharpVSResources.Prefer_var,
                falseValueDescription: CSharpVSResources.Prefer_explicit_type,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.VarWhenTypeIsApparent,
                description: CSharpVSResources.When_variable_type_is_apparent,
                trueValueDescription: CSharpVSResources.Prefer_var,
                falseValueDescription: CSharpVSResources.Prefer_explicit_type,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.VarElsewhere,
                description: CSharpVSResources.Elsewhere,
                trueValueDescription: CSharpVSResources.Prefer_var,
                falseValueDescription: CSharpVSResources.Prefer_explicit_type,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetUsingsCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(
                option: CSharpCodeStyleOptions.PreferredUsingDirectivePlacement,
                description: CSharpVSResources.Preferred_using_directive_placement,
                enumValues: new[] { AddImportPlacement.InsideNamespace, AddImportPlacement.OutsideNamespace },
                valueDescriptions: new[] { CSharpVSResources.Inside_namespace, CSharpVSResources.Outside_namespace },
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetNullCheckingCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferThrowExpression,
                description: CSharpVSResources.Prefer_throw_expression,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferConditionalDelegateCall,
                description: CSharpVSResources.Prefer_conditional_delegate_call,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferNullCheckOverTypeCheck,
                description: CSharpVSResources.Prefer_null_check_over_type_check,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferParameterNullChecking,
                description: CSharpVSResources.Prefer_parameter_null_checking,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetModifierCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferStaticLocalFunction,
                description: ServicesVSResources.Prefer_static_local_functions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetCodeBlockCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferSimpleUsingStatement,
                description: ServicesVSResources.Prefer_simple_using_statement,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);

            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferBraces,
                description: ServicesVSResources.Prefer_braces,
                enumValues: new[] { PreferBracesPreference.Always, PreferBracesPreference.None, PreferBracesPreference.WhenMultiline },
                valueDescriptions: new[] { ServicesVSResources.Yes, ServicesVSResources.No, CSharpVSResources.When_on_multiple_lines },
                editorConfigOptions: editorConfigOptions, visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);

            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.NamespaceDeclarations,
                description: ServicesVSResources.Namespace_declarations,
                enumValues: new[] { NamespaceDeclarationPreference.BlockScoped, NamespaceDeclarationPreference.FileScoped },
                valueDescriptions: new[] { CSharpVSResources.Block_scoped, CSharpVSResources.File_scoped },
                editorConfigOptions: editorConfigOptions, visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);

            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferMethodGroupConversion,
                description: ServicesVSResources.Prefer_method_group_conversion,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetExpressionCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferSwitchExpression, description: CSharpVSResources.Prefer_switch_expression, editorConfigOptions, visualStudioOptions, updaterService, FileName);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferSimpleDefaultExpression, description: ServicesVSResources.Prefer_simple_default_expression, editorConfigOptions, visualStudioOptions, updaterService, FileName);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction, description: ServicesVSResources.Prefer_local_function_over_anonymous_function, editorConfigOptions, visualStudioOptions, updaterService, FileName);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferIndexOperator, description: ServicesVSResources.Prefer_index_operator, editorConfigOptions, visualStudioOptions, updaterService, FileName);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferRangeOperator, description: ServicesVSResources.Prefer_range_operator, editorConfigOptions, visualStudioOptions, updaterService, FileName);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.ImplicitObjectCreationWhenTypeIsApparent, description: CSharpVSResources.Prefer_implicit_object_creation_when_type_is_apparent, editorConfigOptions, visualStudioOptions, updaterService, FileName);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferTupleSwap, description: ServicesVSResources.Prefer_tuple_swap, editorConfigOptions, visualStudioOptions, updaterService, FileName);
        }

        private IEnumerable<CodeStyleSetting> GetPatternMatchingCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferPatternMatching, description: CSharpVSResources.Prefer_pattern_matching, editorConfigOptions, visualStudioOptions, updaterService, FileName);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, description: CSharpVSResources.Prefer_pattern_matching_over_is_with_cast_check, editorConfigOptions, visualStudioOptions, updaterService, FileName);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, description: CSharpVSResources.Prefer_pattern_matching_over_as_with_null_check, editorConfigOptions, visualStudioOptions, updaterService, FileName);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferNotPattern, description: CSharpVSResources.Prefer_pattern_matching_over_mixed_type_check, editorConfigOptions, visualStudioOptions, updaterService, FileName);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferExtendedPropertyPattern, description: CSharpVSResources.Prefer_extended_property_pattern, editorConfigOptions, visualStudioOptions, updaterService, FileName);
        }

        private IEnumerable<CodeStyleSetting> GetVariableCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferInlinedVariableDeclaration, description: ServicesVSResources.Prefer_inlined_variable_declaration, editorConfigOptions, visualStudioOptions, updaterService, FileName);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferDeconstructedVariableDeclaration, description: ServicesVSResources.Prefer_deconstructed_variable_declaration, editorConfigOptions, visualStudioOptions, updaterService, FileName);
        }

        private IEnumerable<CodeStyleSetting> GetExpressionBodyCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            var enumValues = new[] { ExpressionBodyPreference.Never, ExpressionBodyPreference.WhenPossible, ExpressionBodyPreference.WhenOnSingleLine };
            var valueDescriptions = new[] { CSharpVSResources.Never, CSharpVSResources.When_possible, CSharpVSResources.When_on_single_line };
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedMethods,
                description: ServicesVSResources.Use_expression_body_for_methods,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedConstructors,
                description: ServicesVSResources.Use_expression_body_for_constructors,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedOperators,
                description: ServicesVSResources.Use_expression_body_for_operators,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedProperties,
                description: ServicesVSResources.Use_expression_body_for_properties,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedIndexers,
                description: ServicesVSResources.Use_expression_body_for_indexers,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedAccessors,
                description: ServicesVSResources.Use_expression_body_for_accessors,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedLambdas,
                description: ServicesVSResources.Use_expression_body_for_lambdas,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions,
                description: ServicesVSResources.Use_expression_body_for_local_functions,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetUnusedValueCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            var enumValues = new[]
            {
                UnusedValuePreference.UnusedLocalVariable,
                UnusedValuePreference.DiscardVariable
            };

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.UnusedValueAssignment,
                description: ServicesVSResources.Avoid_unused_value_assignments,
                enumValues,
                new[] { CSharpVSResources.Unused_local, CSharpVSResources.Discard },
                editorConfigOptions,
                visualStudioOptions,
                updaterService, FileName);

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.UnusedValueExpressionStatement,
                description: ServicesVSResources.Avoid_expression_statements_that_implicitly_ignore_value,
                enumValues,
                new[] { CSharpVSResources.Unused_local, CSharpVSResources.Discard },
                editorConfigOptions,
                visualStudioOptions,
                updaterService, FileName);

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine,
                description: CSharpVSResources.Allow_embedded_statements_on_same_line,
                editorConfigOptions,
                visualStudioOptions,
                updaterService, FileName);

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces,
                description: CSharpVSResources.Allow_blank_lines_between_consecutive_braces,
                editorConfigOptions,
                visualStudioOptions,
                updaterService, FileName);

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer,
                description: CSharpVSResources.Allow_bank_line_after_colon_in_constructor_initializer,
                editorConfigOptions,
                visualStudioOptions,
                updaterService, FileName);
        }
    }
}
