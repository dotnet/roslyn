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
using Microsoft.CodeAnalysis.EditorConfigSettings;

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

            var experimentalSettings = GetExperimentalCodeStyleOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(experimentalSettings);
        }

        private IEnumerable<CodeStyleSetting> GetQualifyCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.QualifyFieldAccess,
                editorConfigData: EditorConfigSettingsData.QualifyFieldAccess,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.QualifyPropertyAccess,
                editorConfigData: EditorConfigSettingsData.QualifyPropertyAccess,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.QualifyMethodAccess,
                editorConfigData: EditorConfigSettingsData.QualifyMethodAccess,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.QualifyEventAccess,
                editorConfigData: EditorConfigSettingsData.QualifyEventAccess,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetPredefinedTypesCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration,
                editorConfigData: EditorConfigSettingsData.PreferIntrinsicPredefinedTypeKeywordInDeclaration,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess,
                editorConfigData: EditorConfigSettingsData.PreferIntrinsicPredefinedTypeKeywordInMemberAccess,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetNullCheckingCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.PreferCoalesceExpression,
                editorConfigData: EditorConfigSettingsData.PreferCoalesceExpression,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.PreferNullPropagation,
                editorConfigData: EditorConfigSettingsData.PreferNullPropagation,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod,
                editorConfigData: EditorConfigSettingsData.PreferIsNullCheckOverReferenceEqualityMethod,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetModifierCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.AccessibilityModifiersRequired,
                editorConfigData: EditorConfigSettingsData.RequireAccessibilityModifiers,
                enumValues: new[] { AccessibilityModifiersRequired.Always, AccessibilityModifiersRequired.ForNonInterfaceMembers, AccessibilityModifiersRequired.Never, AccessibilityModifiersRequired.OmitIfDefault },
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);

            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.PreferReadonly,
                editorConfigData: EditorConfigSettingsData.PreferReadonly,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetCodeBlockCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.PreferAutoProperties,
                editorConfigData: EditorConfigSettingsData.PreferAutoProperties,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.PreferSystemHashCode,
                editorConfigData: EditorConfigSettingsData.PreferSystemHashCode,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetExpressionCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferObjectInitializer, options, visualStudioOptions, updater, FileName, editorConfigData: EditorConfigSettingsData.PreferObjectInitializer);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferCollectionInitializer, options, visualStudioOptions, updater, FileName, editorConfigData: EditorConfigSettingsData.PreferCollectionInitializer);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferSimplifiedBooleanExpressions, options, visualStudioOptions, updater, FileName, editorConfigData: EditorConfigSettingsData.PreferSimplifiedBooleanExpressions);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferConditionalExpressionOverAssignment, options, visualStudioOptions, updater, FileName, editorConfigData: EditorConfigSettingsData.PreferConditionalExpressionOverAssignment);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferConditionalExpressionOverReturn, options, visualStudioOptions, updater, FileName, editorConfigData: EditorConfigSettingsData.PreferConditionalExpressionOverReturn);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferExplicitTupleNames, options, visualStudioOptions, updater, FileName, editorConfigData: EditorConfigSettingsData.PreferExplicitTupleNames);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferInferredTupleNames, options, visualStudioOptions, updater, FileName, editorConfigData: EditorConfigSettingsData.PreferInferredTupleNames);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames, options, visualStudioOptions, updater, FileName, editorConfigData: EditorConfigSettingsData.PreferInferredAnonymousTypeMemberNames);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferCompoundAssignment, options, visualStudioOptions, updater, FileName, editorConfigData: EditorConfigSettingsData.PreferCompoundAssignment);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferSimplifiedInterpolation, options, visualStudioOptions, updater, FileName, editorConfigData: EditorConfigSettingsData.PreferSimplifiedInterpolation);
        }

        private IEnumerable<CodeStyleSetting> GetParenthesesCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            var enumValues = new[] { ParenthesesPreference.AlwaysForClarity, ParenthesesPreference.NeverIfUnnecessary };
            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.ArithmeticBinaryParentheses,
                editorConfigData: EditorConfigSettingsData.ArithmeticBinaryParentheses,
                enumValues: enumValues,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);

            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.OtherBinaryParentheses,
                editorConfigData: EditorConfigSettingsData.OtherBinaryParentheses,
                enumValues: enumValues,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);

            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.RelationalBinaryParentheses,
                editorConfigData: EditorConfigSettingsData.RelationalBinaryParentheses,
                enumValues: enumValues,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);

            yield return CodeStyleSetting.Create(option: CodeStyleOptions2.OtherParentheses,
                editorConfigData: EditorConfigSettingsData.OtherParentheses,
                enumValues: enumValues,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);

        }

        private IEnumerable<CodeStyleSetting> GetParameterCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(
                option: CodeStyleOptions2.UnusedParameters,
                enumValues: new[] { UnusedParametersPreference.NonPublicMethods, UnusedParametersPreference.AllMethods },
                editorConfigData: EditorConfigSettingsData.UnusedParameters,
                editorConfigOptions: options,
                visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetExperimentalCodeStyleOptions(AnalyzerConfigOptions options, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(
                option: CodeStyleOptions2.PreferNamespaceAndFolderMatchStructure,
                editorConfigData: EditorConfigSettingsData.PreferNamespaceAndFolderMatchStructure,
                editorConfigOptions: options, visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);

            yield return CodeStyleSetting.Create(
                option: CodeStyleOptions2.AllowMultipleBlankLines,
                editorConfigData: EditorConfigSettingsData.AllowMultipleBlankLines,
                editorConfigOptions: options, visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);

            yield return CodeStyleSetting.Create(
                option: CodeStyleOptions2.AllowStatementImmediatelyAfterBlock,
                editorConfigData: EditorConfigSettingsData.AllowStatementImmediatelyAfterBlock,
                editorConfigOptions: options, visualStudioOptions: visualStudioOptions, updater: updater, fileName: FileName);
        }
    }
}
