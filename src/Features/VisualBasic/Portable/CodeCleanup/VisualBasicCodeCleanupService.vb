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
                New DiagnosticSet(VBFeaturesResources.Apply_Me_qualification_preferences,
                    IDEDiagnosticIds.AddQualificationDiagnosticId, IDEDiagnosticIds.RemoveQualificationDiagnosticId),
                New DiagnosticSet(AnalyzersResources.Add_accessibility_modifiers,
                    IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId),
                New DiagnosticSet(FeaturesResources.Sort_accessibility_modifiers,
                    IDEDiagnosticIds.OrderModifiersDiagnosticId),
                New DiagnosticSet(VBFeaturesResources.Make_private_field_ReadOnly_when_possible,
                    IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId),
                New DiagnosticSet(FeaturesResources.Remove_unnecessary_casts,
                    IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId),
                New DiagnosticSet(FeaturesResources.Remove_unused_variables,
                    VisualBasicRemoveUnusedVariableCodeFixProvider.BC42024),
                New DiagnosticSet(FeaturesResources.Apply_object_collection_initialization_preferences,
                    IDEDiagnosticIds.UseObjectInitializerDiagnosticId, IDEDiagnosticIds.UseCollectionInitializerDiagnosticId),
                New DiagnosticSet(VBFeaturesResources.Apply_Imports_directive_placement_preferences,
                    IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId),
                New DiagnosticSet(FeaturesResources.Apply_file_header_preferences,
                    IDEDiagnosticIds.FileHeaderMismatch))

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
