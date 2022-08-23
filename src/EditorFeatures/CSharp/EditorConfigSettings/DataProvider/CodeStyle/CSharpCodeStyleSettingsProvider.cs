// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.EditorConfigSettings;
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

        private IEnumerable<CodeStyleSetting> GetVarCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.VarForBuiltInTypes,
                editorConfigData: CSharpEditorConfigSettingsData.VarForBuiltInTypes,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.VarWhenTypeIsApparent,
                editorConfigData: CSharpEditorConfigSettingsData.VarWhenTypeIsApparent,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.VarElsewhere,
                editorConfigData: CSharpEditorConfigSettingsData.VarElsewhere,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetUsingsCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(
                option: CSharpCodeStyleOptions.PreferredUsingDirectivePlacement,
                editorConfigData: CSharpEditorConfigSettingsData.PreferredUsingDirectivePlacement,
                enumValues: new[] { AddImportPlacement.InsideNamespace, AddImportPlacement.OutsideNamespace },
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetNullCheckingCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferThrowExpression,
                editorConfigData: CSharpEditorConfigSettingsData.PreferThrowExpression,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferConditionalDelegateCall,
                editorConfigData: CSharpEditorConfigSettingsData.PreferConditionalDelegateCall,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferNullCheckOverTypeCheck,
                editorConfigData: CSharpEditorConfigSettingsData.PreferNullCheckOverTypeCheck,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetModifierCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferStaticLocalFunction,
                editorConfigData: CSharpEditorConfigSettingsData.PreferStaticLocalFunction,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetCodeBlockCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferSimpleUsingStatement,
                editorConfigData: CSharpEditorConfigSettingsData.PreferSimpleUsingStatement,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);

            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferBraces,
                editorConfigData: CSharpEditorConfigSettingsData.PreferBraces,
                enumValues: new[] { PreferBracesPreference.Always, PreferBracesPreference.None, PreferBracesPreference.WhenMultiline },
                editorConfigOptions: editorConfigOptions, visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);

            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.NamespaceDeclarations,
                editorConfigData: CSharpEditorConfigSettingsData.NamespaceDeclarations,
                enumValues: new[] { NamespaceDeclarationPreference.BlockScoped, NamespaceDeclarationPreference.FileScoped },
                editorConfigOptions: editorConfigOptions, visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);

            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferMethodGroupConversion,
                editorConfigData: CSharpEditorConfigSettingsData.PreferMethodGroupConversion,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);

            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferTopLevelStatements,
                editorConfigData: CSharpEditorConfigSettingsData.PreferTopLevelStatements,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetExpressionCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferSwitchExpression, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsData.PreferSwitchExpression);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferSimpleDefaultExpression, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsData.PreferSimpleDefaultExpression);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsData.PreferLocalOverAnonymousFunction);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferIndexOperator, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsData.PreferIndexOperator);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferRangeOperator, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsData.PreferRangeOperator);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.ImplicitObjectCreationWhenTypeIsApparent, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsData.ImplicitObjectCreationWhenTypeIsApparent);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferTupleSwap, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsData.PreferTupleSwap);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferUtf8StringLiterals, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsData.PreferUtf8StringLiterals);
        }

        private IEnumerable<CodeStyleSetting> GetPatternMatchingCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferPatternMatching, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsData.PreferPatternMatching);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsData.PreferPatternMatchingOverIsWithCastCheck);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsData.PreferPatternMatchingOverAsWithNullCheck);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferNotPattern, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsData.PreferNotPattern);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferExtendedPropertyPattern, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsData.PreferExtendedPropertyPattern);
        }

        private IEnumerable<CodeStyleSetting> GetVariableCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferInlinedVariableDeclaration, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsData.PreferInlinedVariableDeclaration);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferDeconstructedVariableDeclaration, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsData.PreferDeconstructedVariableDeclaration);
        }

        private IEnumerable<CodeStyleSetting> GetExpressionBodyCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            var enumValues = new[] { ExpressionBodyPreference.Never, ExpressionBodyPreference.WhenPossible, ExpressionBodyPreference.WhenOnSingleLine };
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedMethods,
                editorConfigData: CSharpEditorConfigSettingsData.PreferExpressionBodiedMethods,
                enumValues: enumValues,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedConstructors,
                editorConfigData: CSharpEditorConfigSettingsData.PreferExpressionBodiedConstructors,
                enumValues: enumValues,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedOperators,
                editorConfigData: CSharpEditorConfigSettingsData.PreferExpressionBodiedOperators,
                enumValues: enumValues,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedProperties,
                editorConfigData: CSharpEditorConfigSettingsData.PreferExpressionBodiedProperties,
                enumValues: enumValues,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedIndexers,
                editorConfigData: CSharpEditorConfigSettingsData.PreferExpressionBodiedIndexers,
                enumValues: enumValues,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedAccessors,
                editorConfigData: CSharpEditorConfigSettingsData.PreferExpressionBodiedAccessors,
                enumValues: enumValues,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedLambdas,
                editorConfigData: CSharpEditorConfigSettingsData.PreferExpressionBodiedLambdas,
                enumValues: enumValues,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions,
                editorConfigData: CSharpEditorConfigSettingsData.PreferExpressionBodiedLocalFunctions,
                enumValues: enumValues,
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
                enumValues,
                editorConfigOptions,
                visualStudioOptions,
                updaterService, FileName,
                editorConfigData: CSharpEditorConfigSettingsData.UnusedValueAssignment);

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.UnusedValueExpressionStatement,
                enumValues,
                editorConfigOptions,
                visualStudioOptions,
                updaterService, FileName,
                editorConfigData: CSharpEditorConfigSettingsData.UnusedValueExpressionStatement);

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine,
                editorConfigOptions,
                visualStudioOptions,
                updaterService, FileName,
                editorConfigData: CSharpEditorConfigSettingsData.AllowEmbeddedStatementsOnSameLine);

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces,
                editorConfigOptions,
                visualStudioOptions,
                updaterService, FileName,
                editorConfigData: CSharpEditorConfigSettingsData.AllowBlankLinesBetweenConsecutiveBraces);

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer,
                editorConfigOptions,
                visualStudioOptions,
                updaterService, FileName,
                editorConfigData: CSharpEditorConfigSettingsData.AllowBlankLineAfterColonInConstructorInitializer);
        }
    }
}
