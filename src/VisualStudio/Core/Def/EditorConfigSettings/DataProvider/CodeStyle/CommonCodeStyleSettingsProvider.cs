// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.CodeStyle
{
    internal sealed class CommonCodeStyleSettingsProvider : SettingsProviderBase<CodeStyleSetting, OptionUpdater, IOption2, object>
    {
        public CommonCodeStyleSettingsProvider(string filePath, OptionUpdater settingsUpdater, Workspace workspace, IGlobalOptionService globalOptions)
            : base(filePath, settingsUpdater, workspace, globalOptions)
        {
            Update();
        }

        protected override void UpdateOptions(TieredAnalyzerConfigOptions options, ImmutableArray<Project> projectsInScope)
        {
            var qualifySettings = GetQualifyCodeStyleOptions(options, SettingsUpdater);
            AddRange(qualifySettings);

            var predefinedTypesSettings = GetPredefinedTypesCodeStyleOptions(options, SettingsUpdater);
            AddRange(predefinedTypesSettings);

            var nullCheckingSettings = GetNullCheckingCodeStyleOptions(options, SettingsUpdater);
            AddRange(nullCheckingSettings);

            var modifierSettings = GetModifierCodeStyleOptions(options, SettingsUpdater);
            AddRange(modifierSettings);

            var codeBlockSettings = GetCodeBlockCodeStyleOptions(options, SettingsUpdater);
            AddRange(codeBlockSettings);

            var expressionSettings = GetExpressionCodeStyleOptions(options, SettingsUpdater);
            AddRange(expressionSettings);

            var parameterSettings = GetParameterCodeStyleOptions(options, SettingsUpdater);
            AddRange(parameterSettings);

            var parenthesesSettings = GetParenthesesCodeStyleOptions(options, SettingsUpdater);
            AddRange(parenthesesSettings);

            var experimentalSettings = GetExperimentalCodeStyleOptions(options, SettingsUpdater);
            AddRange(experimentalSettings);
        }

        private static IEnumerable<CodeStyleSetting> GetQualifyCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            var trueValueDescription = EditorFeaturesResources.Prefer_this_or_Me;
            var falseValueDescription = EditorFeaturesResources.Do_not_prefer_this_or_Me;

            yield return CodeStyleSetting.Create(CodeStyleOptions2.QualifyFieldAccess, EditorFeaturesResources.Qualify_field_access_with_this_or_Me, options, updater, trueValueDescription, falseValueDescription);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.QualifyPropertyAccess, EditorFeaturesResources.Qualify_property_access_with_this_or_Me, options, updater, trueValueDescription, falseValueDescription);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.QualifyMethodAccess, EditorFeaturesResources.Qualify_method_access_with_this_or_Me, options, updater, trueValueDescription, falseValueDescription);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.QualifyEventAccess, EditorFeaturesResources.Qualify_event_access_with_this_or_Me, options, updater, trueValueDescription, falseValueDescription);
        }

        private static IEnumerable<CodeStyleSetting> GetPredefinedTypesCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            var trueValueDescription = ServicesVSResources.Prefer_predefined_type;
            var falseValueDescription = ServicesVSResources.Prefer_framework_type;

            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, ServicesVSResources.For_locals_parameters_and_members, options, updater, trueValueDescription, falseValueDescription);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, ServicesVSResources.For_member_access_expressions, options, updater, trueValueDescription, falseValueDescription);
        }

        private static IEnumerable<CodeStyleSetting> GetNullCheckingCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferCoalesceExpression, ServicesVSResources.Prefer_coalesce_expression, options, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferNullPropagation, ServicesVSResources.Prefer_null_propagation, options, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod, EditorFeaturesResources.Prefer_is_null_for_reference_equality_checks, options, updater);
        }

        private static IEnumerable<CodeStyleSetting> GetModifierCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(
                CodeStyleOptions2.AccessibilityModifiersRequired,
                ServicesVSResources.Require_accessibility_modifiers,
                options,
                updater,
                enumValues: new[] { AccessibilityModifiersRequired.Always, AccessibilityModifiersRequired.ForNonInterfaceMembers, AccessibilityModifiersRequired.Never, AccessibilityModifiersRequired.OmitIfDefault },
                valueDescriptions: new[] { ServicesVSResources.Always, ServicesVSResources.For_non_interface_members, ServicesVSResources.Never, ServicesVSResources.Omit_if_default });

            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferReadonly, ServicesVSResources.Prefer_readonly_fields, options, updater);
        }

        private static IEnumerable<CodeStyleSetting> GetCodeBlockCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferAutoProperties, ServicesVSResources.analyzer_Prefer_auto_properties, options, updater);
        }

        private static IEnumerable<CodeStyleSetting> GetExpressionCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferObjectInitializer, description: ServicesVSResources.Prefer_object_initializer, options, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferCollectionExpression, description: ServicesVSResources.Prefer_collection_expression, options, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferCollectionInitializer, description: ServicesVSResources.Prefer_collection_initializer, options, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferSimplifiedBooleanExpressions, description: ServicesVSResources.Prefer_simplified_boolean_expressions, options, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferConditionalExpressionOverAssignment, description: ServicesVSResources.Prefer_conditional_expression_over_if_with_assignments, options, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferConditionalExpressionOverReturn, description: ServicesVSResources.Prefer_conditional_expression_over_if_with_returns, options, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferExplicitTupleNames, description: ServicesVSResources.Prefer_explicit_tuple_name, options, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferInferredTupleNames, description: ServicesVSResources.Prefer_inferred_tuple_names, options, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames, description: ServicesVSResources.Prefer_inferred_anonymous_type_member_names, options, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferCompoundAssignment, description: ServicesVSResources.Prefer_compound_assignments, options, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferSimplifiedInterpolation, description: ServicesVSResources.Prefer_simplified_interpolation, options, updater);
        }

        private static IEnumerable<CodeStyleSetting> GetParenthesesCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            var enumValues = new[] { ParenthesesPreference.AlwaysForClarity, ParenthesesPreference.NeverIfUnnecessary };
            var valueDescriptions = new[] { ServicesVSResources.Always_for_clarity, ServicesVSResources.Never_if_unnecessary };

            yield return CodeStyleSetting.Create(CodeStyleOptions2.ArithmeticBinaryParentheses, EditorFeaturesResources.In_arithmetic_binary_operators, options, updater, enumValues, valueDescriptions);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.OtherBinaryParentheses, EditorFeaturesResources.In_other_binary_operators, options, updater, enumValues, valueDescriptions);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.RelationalBinaryParentheses, EditorFeaturesResources.In_relational_binary_operators, options, updater, enumValues, valueDescriptions);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.OtherParentheses, ServicesVSResources.In_other_operators, options, updater, enumValues, valueDescriptions);
        }

        private static IEnumerable<CodeStyleSetting> GetParameterCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            var enumValues = new[] { UnusedParametersPreference.NonPublicMethods, UnusedParametersPreference.AllMethods };
            var valueDescriptions = new[] { ServicesVSResources.Non_public_methods, ServicesVSResources.All_methods };

            yield return CodeStyleSetting.Create(CodeStyleOptions2.UnusedParameters, ServicesVSResources.Avoid_unused_parameters, options, updater, enumValues, valueDescriptions);
        }

        private static IEnumerable<CodeStyleSetting> GetExperimentalCodeStyleOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
        {
            yield return CodeStyleSetting.Create(CodeStyleOptions2.PreferNamespaceAndFolderMatchStructure, ServicesVSResources.Prefer_namespace_and_folder_match_structure, options, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.AllowMultipleBlankLines, ServicesVSResources.Allow_multiple_blank_lines, options, updater);
            yield return CodeStyleSetting.Create(CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, ServicesVSResources.Allow_statement_immediately_after_block, options, updater);
        }
    }
}
