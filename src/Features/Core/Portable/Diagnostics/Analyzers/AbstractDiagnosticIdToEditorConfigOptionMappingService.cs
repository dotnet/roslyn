// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract class AbstractDiagnosticIdToEditorConfigOptionMappingService : IDiagnosticIdToEditorConfigOptionMappingService
    {
        protected abstract (IOption optionOpt, bool handled) TryGetLanguageSpecificOption(string diagnosticId);

        public IOption GetMappedEditorConfigOption(string diagnosticId)
        {
            // Service currently only handles IDE Diagnostic IDs, which start with a prefix "IDE".
            if (!diagnosticId.StartsWith("IDE"))
            {
                return default;
            }

            switch (diagnosticId)
            {
                // IDE Diagnostic IDs with no editorconfig option OR no unique editorconfig option to configure
                // all diagnostics with the given diagnostic ID.
                case IDEDiagnosticIds.SimplifyNamesDiagnosticId:
                case IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId:
                case IDEDiagnosticIds.RemoveQualificationDiagnosticId:
                case IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId:
                case IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId:
                case IDEDiagnosticIds.IntellisenseBuildFailedDiagnosticId:
                case IDEDiagnosticIds.UseImplicitTypeDiagnosticId:
                case IDEDiagnosticIds.UseExplicitTypeDiagnosticId:
                case IDEDiagnosticIds.AddQualificationDiagnosticId:
                case IDEDiagnosticIds.PopulateSwitchDiagnosticId:
                case IDEDiagnosticIds.RemoveUnreachableCodeDiagnosticId:
                case IDEDiagnosticIds.OrderModifiersDiagnosticId:
                case IDEDiagnosticIds.InlineIsTypeWithoutNameCheckDiagnosticsId:
                case IDEDiagnosticIds.ValidateFormatStringDiagnosticID:
                case IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId:
                case IDEDiagnosticIds.AddRequiredParenthesesDiagnosticId:
                case IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId:
                case IDEDiagnosticIds.ConvertAnonymousTypeToTupleDiagnosticId:
                case IDEDiagnosticIds.RemoveUnusedMembersDiagnosticId:
                case IDEDiagnosticIds.RemoveUnreadMembersDiagnosticId:
                case IDEDiagnosticIds.FormattingDiagnosticId:
                case IDEDiagnosticIds.MakeStructFieldsWritable:
                case IDEDiagnosticIds.AnalyzerChangedId:
                case IDEDiagnosticIds.AnalyzerDependencyConflictId:
                case IDEDiagnosticIds.MissingAnalyzerReferenceId:
                case IDEDiagnosticIds.ErrorReadingRulesetId:
                case IDEDiagnosticIds.NamingRuleId:
                case IDEDiagnosticIds.UnboundIdentifierId:
                case IDEDiagnosticIds.UnboundConstructorId:
                    return null;

                // IDE Diagnostic IDs with a unique editorconfig option with severity to configure
                // all diagnostics with the given diagnostic ID.
                case IDEDiagnosticIds.UseThrowExpressionDiagnosticId:
                    return CodeStyleOptions.PreferThrowExpression;
                case IDEDiagnosticIds.UseObjectInitializerDiagnosticId:
                    return CodeStyleOptions.PreferObjectInitializer;
                case IDEDiagnosticIds.InlineDeclarationDiagnosticId:
                    return CodeStyleOptions.PreferInlinedVariableDeclaration;
                case IDEDiagnosticIds.UseCollectionInitializerDiagnosticId:
                    return CodeStyleOptions.PreferCollectionInitializer;
                case IDEDiagnosticIds.UseCoalesceExpressionDiagnosticId:
                    return CodeStyleOptions.PreferCoalesceExpression;
                case IDEDiagnosticIds.UseCoalesceExpressionForNullableDiagnosticId:
                    return CodeStyleOptions.PreferCoalesceExpression;
                case IDEDiagnosticIds.UseNullPropagationDiagnosticId:
                    return CodeStyleOptions.PreferNullPropagation;
                case IDEDiagnosticIds.UseAutoPropertyDiagnosticId:
                    return CodeStyleOptions.PreferAutoProperties;
                case IDEDiagnosticIds.UseExplicitTupleNameDiagnosticId:
                    return CodeStyleOptions.PreferExplicitTupleNames;
                case IDEDiagnosticIds.UseInferredMemberNameDiagnosticId:
                    return CodeStyleOptions.PreferInferredTupleNames;
                case IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId:
                    return CodeStyleOptions.RequireAccessibilityModifiers;
                case IDEDiagnosticIds.UseIsNullCheckDiagnosticId:
                    return CodeStyleOptions.PreferIsNullCheckOverReferenceEqualityMethod;
                case IDEDiagnosticIds.UseDeconstructionDiagnosticId:
                    return CodeStyleOptions.PreferDeconstructedVariableDeclaration;
                case IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId:
                    return CodeStyleOptions.PreferReadonly;
                case IDEDiagnosticIds.UseConditionalExpressionForAssignmentDiagnosticId:
                    return CodeStyleOptions.PreferConditionalExpressionOverAssignment;
                case IDEDiagnosticIds.UseConditionalExpressionForReturnDiagnosticId:
                    return CodeStyleOptions.PreferConditionalExpressionOverReturn;
                case IDEDiagnosticIds.UseCompoundAssignmentDiagnosticId:
                    return CodeStyleOptions.PreferCompoundAssignment;
                case IDEDiagnosticIds.UnusedParameterDiagnosticId:
                    return CodeStyleOptions.UnusedParameters;

                default:
                    // Check if there is a unique language specific option for the diagnostic
                    // i.e. options defined in CSharpCodeStyleOptions and VisualBasicCodeStyleOptions.
                    var (languageSpecificOptionOpt, handled) = TryGetLanguageSpecificOption(diagnosticId);
                    if (handled)
                    {
                        return languageSpecificOptionOpt;
                    }

                    Debug.Assert(languageSpecificOptionOpt == null);

                    // Unhandled "IDExxx" diagnostic ID.
                    throw new System.NotImplementedException($@"'{diagnosticId}' must be handled by {nameof(AbstractDiagnosticIdToEditorConfigOptionMappingService)}.
Ensure that every IDE diagnosticID is handled by this service as follows:
  1. If this diagnostic has a unique code style option, such that diagnostic's severity can be configured in .editorconfig with an entry such as 'option_name = option_value:severity', then ensure that this option is returned from this method.
  2. Otherwise, handle diagnostic ID in this switch statement and return null.");
            }
        }
    }
}
