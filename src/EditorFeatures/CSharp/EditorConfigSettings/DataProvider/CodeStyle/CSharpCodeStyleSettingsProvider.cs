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
                editorConfigData: CSharpEditorConfigSettingsValueHolder.VarForBuiltInTypes,
                trueValueDescription: CSharpEditorResources.Prefer_var,
                falseValueDescription: CSharpEditorResources.Prefer_explicit_type,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.VarWhenTypeIsApparent,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.VarWhenTypeIsApparent,
                trueValueDescription: CSharpEditorResources.Prefer_var,
                falseValueDescription: CSharpEditorResources.Prefer_explicit_type,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.VarElsewhere,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.VarElsewhere,
                trueValueDescription: CSharpEditorResources.Prefer_var,
                falseValueDescription: CSharpEditorResources.Prefer_explicit_type,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetUsingsCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(
                option: CSharpCodeStyleOptions.PreferredUsingDirectivePlacement,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferredUsingDirectivePlacement,
                enumValues: new[] { AddImportPlacement.InsideNamespace, AddImportPlacement.OutsideNamespace },
                valueDescriptions: new[] { CSharpEditorResources.Inside_namespace, CSharpEditorResources.Outside_namespace },
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetNullCheckingCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferThrowExpression,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferThrowExpression,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferConditionalDelegateCall,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferConditionalDelegateCall,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferNullCheckOverTypeCheck,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferNullCheckOverTypeCheck,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetModifierCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferStaticLocalFunction,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferStaticLocalFunction,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetCodeBlockCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferSimpleUsingStatement,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferSimpleUsingStatement,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);

            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferBraces,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferBraces,
                enumValues: new[] { PreferBracesPreference.Always, PreferBracesPreference.None, PreferBracesPreference.WhenMultiline },
                valueDescriptions: new[] { EditorFeaturesResources.Yes, EditorFeaturesResources.No, CSharpEditorResources.When_on_multiple_lines },
                editorConfigOptions: editorConfigOptions, visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);

            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.NamespaceDeclarations,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.NamespaceDeclarations,
                enumValues: new[] { NamespaceDeclarationPreference.BlockScoped, NamespaceDeclarationPreference.FileScoped },
                valueDescriptions: new[] { CSharpEditorResources.Block_scoped, CSharpEditorResources.File_scoped },
                editorConfigOptions: editorConfigOptions, visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);

            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferMethodGroupConversion,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferMethodGroupConversion,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);

            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferTopLevelStatements,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferTopLevelStatements,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
        }

        private IEnumerable<CodeStyleSetting> GetExpressionCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferSwitchExpression, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferSwitchExpression);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferSimpleDefaultExpression, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferSimpleDefaultExpression);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferLocalOverAnonymousFunction);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferIndexOperator, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferIndexOperator);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferRangeOperator, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferRangeOperator);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.ImplicitObjectCreationWhenTypeIsApparent, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsValueHolder.ImplicitObjectCreationWhenTypeIsApparent);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferTupleSwap, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferTupleSwap);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferUtf8StringLiterals, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferUtf8StringLiterals);
        }

        private IEnumerable<CodeStyleSetting> GetPatternMatchingCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferPatternMatching, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferPatternMatching);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferPatternMatchingOverIsWithCastCheck);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferPatternMatchingOverAsWithNullCheck);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferNotPattern, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferNotPattern);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferExtendedPropertyPattern, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferExtendedPropertyPattern);
        }

        private IEnumerable<CodeStyleSetting> GetVariableCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferInlinedVariableDeclaration, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferInlinedVariableDeclaration);
            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.PreferDeconstructedVariableDeclaration, editorConfigOptions, visualStudioOptions, updaterService, FileName, editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferDeconstructedVariableDeclaration);
        }

        private IEnumerable<CodeStyleSetting> GetExpressionBodyCodeStyleOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updaterService)
        {
            var enumValues = new[] { ExpressionBodyPreference.Never, ExpressionBodyPreference.WhenPossible, ExpressionBodyPreference.WhenOnSingleLine };
            var valueDescriptions = new[] { CSharpEditorResources.Never, CSharpEditorResources.When_possible, CSharpEditorResources.When_on_single_line };
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedMethods,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferExpressionBodiedMethods,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedConstructors,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferExpressionBodiedConstructors,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedOperators,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferExpressionBodiedOperators,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedProperties,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferExpressionBodiedProperties,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedIndexers,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferExpressionBodiedIndexers,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedAccessors,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferExpressionBodiedAccessors,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedLambdas,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferExpressionBodiedLambdas,
                enumValues: enumValues,
                valueDescriptions: valueDescriptions,
                editorConfigOptions: editorConfigOptions,
                visualStudioOptions: visualStudioOptions, updater: updaterService, fileName: FileName);
            yield return CodeStyleSetting.Create(option: CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.PreferExpressionBodiedLocalFunctions,
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
                enumValues,
                new[] { CSharpEditorResources.Unused_local, CSharpEditorResources.Discard },
                editorConfigOptions,
                visualStudioOptions,
                updaterService, FileName,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.UnusedValueAssignment);

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.UnusedValueExpressionStatement,
                enumValues,
                new[] { CSharpEditorResources.Unused_local, CSharpEditorResources.Discard },
                editorConfigOptions,
                visualStudioOptions,
                updaterService, FileName,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.UnusedValueExpressionStatement);

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine,
                editorConfigOptions,
                visualStudioOptions,
                updaterService, FileName,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.AllowEmbeddedStatementsOnSameLine);

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces,
                editorConfigOptions,
                visualStudioOptions,
                updaterService, FileName,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.AllowBlankLinesBetweenConsecutiveBraces);

            yield return CodeStyleSetting.Create(CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer,
                editorConfigOptions,
                visualStudioOptions,
                updaterService, FileName,
                editorConfigData: CSharpEditorConfigSettingsValueHolder.AllowBlankLineAfterColonInConstructorInitializer);
        }
    }
}
