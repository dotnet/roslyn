// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp;
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

        private static IEnumerable<CodeStyleSetting> GetVarCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.VarForBuiltInTypes,
                description: CSharpEditorResources.For_built_in_types,
                trueValueDescription: CSharpEditorResources.Prefer_var,
                falseValueDescription: CSharpEditorResources.Prefer_explicit_type,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.VarWhenTypeIsApparent,
                description: CSharpEditorResources.When_variable_type_is_apparent,
                trueValueDescription: CSharpEditorResources.Prefer_var,
                falseValueDescription: CSharpEditorResources.Prefer_explicit_type,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.VarElsewhere,
                description: CSharpEditorResources.Elsewhere,
                trueValueDescription: CSharpEditorResources.Prefer_var,
                falseValueDescription: CSharpEditorResources.Prefer_explicit_type,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService);
        }

        private static IEnumerable<CodeStyleSetting> GetUsingsCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(
                option: CSharpCodeStyleOptions.PreferredUsingDirectivePlacement,
                description: CSharpEditorResources.Preferred_using_directive_placement,
                enumValues: new[] { AddImportPlacement.InsideNamespace, AddImportPlacement.OutsideNamespace },
                valueDescriptions: new[] { CSharpEditorResources.Inside_namespace, CSharpEditorResources.Outside_namespace },
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService);
        }

        private static IEnumerable<CodeStyleSetting> GetNullCheckingCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferThrowExpression,
                description: CSharpEditorResources.Prefer_throw_expression,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferConditionalDelegateCall,
                description: CSharpEditorResources.Prefer_conditional_delegate_call,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService);
        }

        private static IEnumerable<CodeStyleSetting> GetModifierCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferStaticLocalFunction,
                description: CSharpEditorResources.Prefer_static_local_functions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService);
        }

        private static IEnumerable<CodeStyleSetting> GetCodeBlockCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferSimpleUsingStatement,
                description: CSharpEditorResources.Prefer_simple_using_statement,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService);
        }

        private static IEnumerable<CodeStyleSetting> GetExpressionCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferSwitchExpression, description: CSharpEditorResources.Prefer_switch_expression, editorConfigOptions, visualStudioOptions, updaterService);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferSimpleDefaultExpression, description: CSharpEditorResources.Prefer_simple_default_expression, editorConfigOptions, visualStudioOptions, updaterService);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction, description: CSharpEditorResources.Prefer_local_function_over_anonymous_function, editorConfigOptions, visualStudioOptions, updaterService);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferIndexOperator, description: CSharpEditorResources.Prefer_index_operator, editorConfigOptions, visualStudioOptions, updaterService);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferRangeOperator, description: CSharpEditorResources.Prefer_range_operator, editorConfigOptions, visualStudioOptions, updaterService);
        }

        private static IEnumerable<CodeStyleSetting> GetPatternMatchingCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferPatternMatching, description: CSharpEditorResources.Prefer_pattern_matching, editorConfigOptions, visualStudioOptions, updaterService);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, description: CSharpEditorResources.Prefer_pattern_matching_over_is_with_cast_check, editorConfigOptions, visualStudioOptions, updaterService);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, description: CSharpEditorResources.Prefer_pattern_matching_over_as_with_null_check, editorConfigOptions, visualStudioOptions, updaterService);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferNotPattern, description: CSharpEditorResources.Prefer_pattern_matching_over_mixed_type_check, editorConfigOptions, visualStudioOptions, updaterService);
        }

        private static IEnumerable<CodeStyleSetting> GetVariableCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferInlinedVariableDeclaration, description: CSharpEditorResources.Prefer_inlined_variable_declaration, editorConfigOptions, visualStudioOptions, updaterService);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferDeconstructedVariableDeclaration, description: CSharpEditorResources.Prefer_deconstructed_variable_declaration, editorConfigOptions, visualStudioOptions, updaterService);
        }

        private static IEnumerable<CodeStyleSetting> GetExpressionBodyCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            var enumValues = new[] { ExpressionBodyPreference.Never, ExpressionBodyPreference.WhenPossible, ExpressionBodyPreference.WhenOnSingleLine };
            var valueDescriptions = new[] { CSharpEditorResources.Never, CSharpEditorResources.When_possible, CSharpEditorResources.When_on_single_line };
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedMethods,
                description: CSharpEditorResources.Use_expression_body_for_methods,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedConstructors,
                description: CSharpEditorResources.Use_expression_body_for_constructors,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedOperators,
                description: CSharpEditorResources.Use_expression_body_for_operators,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedProperties,
                description: CSharpEditorResources.Use_expression_body_for_properties,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedIndexers,
                description: CSharpEditorResources.Use_expression_body_for_indexers,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedAccessors,
                description: CSharpEditorResources.Use_expression_body_for_accessors,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedLambdas,
                description: CSharpEditorResources.Use_expression_body_for_lambdas,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions,
                description: CSharpEditorResources.Use_expression_body_for_local_functions,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService);
        }

        private static IEnumerable<CodeStyleSetting> GetUnusedValueCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            var enumValues = new[]
            {
                UnusedValuePreference.UnusedLocalVariable,
                UnusedValuePreference.DiscardVariable
            };

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.UnusedValueAssignment,
                description: CSharpEditorResources.Avoid_unused_value_assignments,
                enumValues,
                new[] { CSharpEditorResources.Unused_local, CSharpEditorResources.Discard },
                editorConfigOptions,
                visualStudioOptions,
                updaterService);

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.UnusedValueExpressionStatement,
                description: CSharpEditorResources.Avoid_expression_statements_that_implicitly_ignore_value,
                enumValues,
                new[] { CSharpEditorResources.Unused_local, CSharpEditorResources.Discard },
                editorConfigOptions,
                visualStudioOptions,
                updaterService);
        }
    }
}
