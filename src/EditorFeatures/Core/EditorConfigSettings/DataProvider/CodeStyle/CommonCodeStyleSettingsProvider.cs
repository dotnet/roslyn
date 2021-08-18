// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.CodeStyle
{
    internal class CommonCodeStyleSettingsProvider : SettingsProviderBase<CodeStyleSetting, OptionUpdater, IOption2, object>
    {
        public CommonCodeStyleSettingsProvider(string filePath, OptionUpdater settingsUpdater, Workspace workspace)
            : base(filePath, settingsUpdater, workspace)
        {
            Update();
        }

        protected override void UpdateOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions)
        {
            var qualifySettings = GetQualifyCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(qualifySettings);

            var predefinedTypesSettings = GetPredefinedTypesCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(predefinedTypesSettings);

            var nullCheckingSettings = GetNullCheckingCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(nullCheckingSettings);

            var modifierSettings = GetModifierCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(modifierSettings);

            var codeBlockSettings = GetCodeBlockCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(codeBlockSettings);

            var expressionSettings = GetExpressionCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(expressionSettings);

            var parameterSettings = GetParameterCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(parameterSettings);

            var parenthesesSettings = GetParenthesesCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(parenthesesSettings);

            // TODO(jmarolf): set as stable
        }

        private static IEnumerable<CodeStyleSetting> GetQualifyCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.QualifyFieldAccess,
                description: EditorFeaturesResources.Qualify_field_access_with_this_or_Me,
                trueValueDescription: EditorFeaturesResources.Prefer_this_or_Me,
                falseValueDescription: EditorFeaturesResources.Do_not_prefer_this_or_Me,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.QualifyPropertyAccess,
                description: EditorFeaturesResources.Qualify_property_access_with_this_or_Me,
                trueValueDescription: EditorFeaturesResources.Prefer_this_or_Me,
                falseValueDescription: EditorFeaturesResources.Do_not_prefer_this_or_Me,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.QualifyMethodAccess,
                description: EditorFeaturesResources.Qualify_method_access_with_this_or_Me,
                trueValueDescription: EditorFeaturesResources.Prefer_this_or_Me,
                falseValueDescription: EditorFeaturesResources.Do_not_prefer_this_or_Me,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.QualifyEventAccess,
                description: EditorFeaturesResources.Qualify_event_access_with_this_or_Me,
                trueValueDescription: EditorFeaturesResources.Prefer_this_or_Me,
                falseValueDescription: EditorFeaturesResources.Do_not_prefer_this_or_Me,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);
        }

        private static IEnumerable<CodeStyleSetting> GetPredefinedTypesCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration,
                description: EditorFeaturesResources.For_locals_parameters_and_members,
                trueValueDescription: EditorFeaturesResources.Prefer_predefined_type,
                falseValueDescription: EditorFeaturesResources.Prefer_framework_type,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration,
                description: EditorFeaturesResources.For_member_access_expressions,
                trueValueDescription: EditorFeaturesResources.Prefer_predefined_type,
                falseValueDescription: EditorFeaturesResources.Prefer_framework_type,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);
        }

        private static IEnumerable<CodeStyleSetting> GetNullCheckingCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.PreferCoalesceExpression,
                description: EditorFeaturesResources.Prefer_coalesce_expression,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.PreferNullPropagation,
                description: EditorFeaturesResources.Prefer_null_propagation,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod,
                description: EditorFeaturesResources.Prefer_is_null_for_reference_equality_checks,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);
        }

        private static IEnumerable<CodeStyleSetting> GetModifierCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.PreferReadonly,
                description: EditorFeaturesResources.Prefer_readonly_fields,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);
        }

        private static IEnumerable<CodeStyleSetting> GetCodeBlockCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.PreferAutoProperties,
                description: EditorFeaturesResources.analyzer_Prefer_auto_properties,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.PreferSystemHashCode,
                description: EditorFeaturesResources.Prefer_System_HashCode_in_GetHashCode,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);
        }

        private static IEnumerable<CodeStyleSetting> GetExpressionCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferObjectInitializer, description: EditorFeaturesResources.Prefer_object_initializer, options, visualStudioOptions, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferCollectionInitializer, description: EditorFeaturesResources.Prefer_collection_initializer, options, visualStudioOptions, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferSimplifiedBooleanExpressions, description: EditorFeaturesResources.Prefer_simplified_boolean_expressions, options, visualStudioOptions, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferConditionalExpressionOverAssignment, description: EditorFeaturesResources.Prefer_conditional_expression_over_if_with_assignments, options, visualStudioOptions, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferConditionalExpressionOverReturn, description: EditorFeaturesResources.Prefer_conditional_expression_over_if_with_returns, options, visualStudioOptions, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferExplicitTupleNames, description: EditorFeaturesResources.Prefer_explicit_tuple_name, options, visualStudioOptions, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferInferredTupleNames, description: EditorFeaturesResources.Prefer_inferred_tuple_names, options, visualStudioOptions, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames, description: EditorFeaturesResources.Prefer_inferred_anonymous_type_member_names, options, visualStudioOptions, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferCompoundAssignment, description: EditorFeaturesResources.Prefer_compound_assignments, options, visualStudioOptions, updater);
        }

        private static IEnumerable<CodeStyleSetting> GetParenthesesCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            var enumValues = new[] { ParenthesesPreference.AlwaysForClarity, ParenthesesPreference.NeverIfUnnecessary };
            var valueDescriptions = new[] { EditorFeaturesResources.Always_for_clarity, EditorFeaturesResources.Never_if_unnecessary };
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.ArithmeticBinaryParentheses,
                description: EditorFeaturesResources.In_arithmetic_binary_operators,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);

            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.OtherBinaryParentheses,
                description: EditorFeaturesResources.In_other_binary_operators,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);

            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.RelationalBinaryParentheses,
                description: EditorFeaturesResources.In_relational_binary_operators,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);

            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.OtherParentheses,
                description: EditorFeaturesResources.In_other_operators,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);

        }

        private static IEnumerable<CodeStyleSetting> GetParameterCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(
                option: CodeStyleOptions2.UnusedParameters,
                description: EditorFeaturesResources.Avoid_unused_parameters,
                enumValues: new[] { UnusedParametersPreference.NonPublicMethods, UnusedParametersPreference.AllMethods },
                new[] { EditorFeaturesResources.Non_public_methods, EditorFeaturesResources.All_methods },
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater);
        }
    }
}
