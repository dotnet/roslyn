' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedVariable

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeCleanup
    <ExportLanguageService(GetType(ICodeCleanupService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicCodeCleanupService
        Inherits AbstractCodeCleanupService

        ''' <summary>
        ''' Maps format document code cleanup options to DiagnosticId[]
        ''' </summary>
        Private Shared ReadOnly s_diagnosticSets As ImmutableArray(Of DiagnosticSet) = ImmutableArray.Create(
                New DiagnosticSet(FeaturesResources.Apply_using_directive_placement_preferences,
                    IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_file_header_preferences,
                    IDEDiagnosticIds.FileHeaderMismatch),
                New DiagnosticSet(AnalyzersResources.Add_this_or_Me_qualification,
                    IDEDiagnosticIds.AddQualificationDiagnosticId, IDEDiagnosticIds.RemoveQualificationDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_language_framework_type_preferences,
                    IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_parentheses_preferences,
                    IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId, IDEDiagnosticIds.AddRequiredParenthesesDiagnosticId),
                New DiagnosticSet(AnalyzersResources.Add_accessibility_modifiers,
                    IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_coalesce_expression_preferences,
                    IDEDiagnosticIds.UseCoalesceExpressionDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_object_collection_initialization_preferences,
                    IDEDiagnosticIds.UseCollectionInitializerDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_tuple_name_preferences,
                    IDEDiagnosticIds.UseExplicitTupleNameDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_namespace_matches_folder_preferences,
                    IDEDiagnosticIds.MatchFolderAndNamespaceDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_null_propagation_preferences,
                    IDEDiagnosticIds.UseNullPropagationDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_object_initializer_preferences,
                    IDEDiagnosticIds.UseObjectInitializerDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_auto_property_preferences,
                    IDEDiagnosticIds.UseAutoPropertyDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_compound_assignment_preferences,
                    IDEDiagnosticIds.UseCoalesceCompoundAssignmentDiagnosticId, IDEDiagnosticIds.UseCompoundAssignmentDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_conditional_expression_preferences,
                    IDEDiagnosticIds.UseConditionalExpressionForAssignmentDiagnosticId, IDEDiagnosticIds.UseConditionalExpressionForReturnDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_inferred_anonymous_type_member_names_preferences,
                    IDEDiagnosticIds.UseInferredMemberNameDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_null_checking_preferences,
                    IDEDiagnosticIds.UseIsNullCheckDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_simplify_boolean_expression_preferences,
                    IDEDiagnosticIds.SimplifyConditionalExpressionDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_string_interpolation_preferences,
                    IDEDiagnosticIds.SimplifyInterpolationId),
                New DiagnosticSet(AnalyzersResources.Make_field_readonly,
                    IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId),
                New DiagnosticSet(FeaturesResources.Remove_unused_parameters,
                    IDEDiagnosticIds.UnusedParameterDiagnosticId),
                New DiagnosticSet(FeaturesResources.Remove_unused_suppressions,
                    IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_blank_line_preferences_experimental,
                    IDEDiagnosticIds.MultipleBlankLinesDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_statement_after_block_preferences_experimental,
                    IDEDiagnosticIds.ConsecutiveStatementPlacementDiagnosticId),
                New DiagnosticSet(FeaturesResources.Sort_accessibility_modifiers,
                    IDEDiagnosticIds.OrderModifiersDiagnosticId),
                New DiagnosticSet(VBFeaturesResources.Apply_isnot_preferences,
                    IDEDiagnosticIds.UseIsNotExpressionDiagnosticId),
                New DiagnosticSet(VBFeaturesResources.Apply_object_creation_preferences,
                    IDEDiagnosticIds.SimplifyObjectCreationDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_unused_value_preferences,
                    IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId, IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId),
                New DiagnosticSet(FeaturesResources.Remove_unnecessary_casts,
                    IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId),
                New DiagnosticSet(FeaturesResources.Remove_unused_variables,
                    VisualBasicRemoveUnusedVariableCodeFixProvider.BC42024))

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(codeFixService As ICodeFixService)
            MyBase.New(codeFixService)
        End Sub

        Protected Overrides ReadOnly Property OrganizeImportsDescription As String = VBFeaturesResources.Organize_Imports

        Protected Overrides Function GetDiagnosticSets() As ImmutableArray(Of DiagnosticSet)
            Return s_diagnosticSets
        End Function
    End Class
End Namespace
