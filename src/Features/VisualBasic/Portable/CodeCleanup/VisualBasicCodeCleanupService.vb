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
                    {IDEDiagnosticIds.AddQualificationDiagnosticId, IDEDiagnosticIds.RemoveQualificationDiagnosticId}),
                New DiagnosticSet(AnalyzersResources.Add_accessibility_modifiers,
                    {IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId}),
                New DiagnosticSet(FeaturesResources.Sort_accessibility_modifiers,
                    {IDEDiagnosticIds.OrderModifiersDiagnosticId}),
                New DiagnosticSet(VBFeaturesResources.Make_private_field_ReadOnly_when_possible,
                    {IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId}),
                New DiagnosticSet(FeaturesResources.Remove_unnecessary_casts,
                    {IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId}),
                New DiagnosticSet(FeaturesResources.Remove_unused_variables,
                    {VisualBasicRemoveUnusedVariableCodeFixProvider.BC42024}),
                New DiagnosticSet(VBFeaturesResources.Apply_object_collection_initialization_preferences,
                    {IDEDiagnosticIds.UseObjectInitializerDiagnosticId, IDEDiagnosticIds.UseCollectionInitializerDiagnosticId}),
                New DiagnosticSet(VBFeaturesResources.Apply_Imports_directive_placement_preferences,
                    {IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId}),
                New DiagnosticSet(VBFeaturesResources.Apply_file_header_preferences,
                    {IDEDiagnosticIds.FileHeaderMismatch}))

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(
            <Import(AllowDefault:=True)> codeFixService As ICodeFixService _ ' will remove the AllowDefault once CodeFixService is moved to Features
 _ ' https://github.com/dotnet/roslyn/issues/27369
)
            MyBase.New(codeFixService)
        End Sub

        ' and one for 'remove/sort Imports' if we're going to run that.
        Dim organizeUsings As Boolean = enabledDiagnostics.OrganizeUsings.IsRemoveUnusedImportEnabled OrElse
            enabledDiagnostics.OrganizeUsings.IsSortImportsEnabled
        If organizeUsings Then
                progressTracker.AddItems(1)
            End If

        If _codeFixServiceOpt IsNot Nothing Then
                currentDocument = Await ApplyCodeFixesAsync(currentDocument,
                                                            enabledDiagnostics.Diagnostics,
                                                            progressTracker,
                                                            cancellationToken).ConfigureAwait(False)
            End If

        ' do the remove Imports after code fix, as code fix might remove some code which can results in unused Imports.
        If organizeUsings Then
                progressTracker.Description = VBFeaturesResources.Organize_Usings
                currentDocument = Await RemoveSortUsingsAsync(currentDocument,
                                                              enabledDiagnostics.OrganizeUsings,
                                                              cancellationToken).ConfigureAwait(False)
                progressTracker.ItemCompleted()
            End If

        If enabledDiagnostics.FormatDocument Then
                progressTracker.Description = FeaturesResources.Formatting_document
                Using Logger.LogBlock(FunctionId.CodeCleanup_Format, cancellationToken)
                    currentDocument = Await Formatter.FormatAsync(currentDocument, cancellationToken:=cancellationToken).ConfigureAwait(False)
                    progressTracker.ItemCompleted()
                End Using
        End If

        Return currentDocument
        End Function

        Private Shared Async Function RemoveSortUsingsAsync(currentDocument As Document,
                                                            organizeImportsSet As OrganizeUsingsSet,
                                                            cancelToken As CancellationToken) As Task(Of Document)
            If organizeImportsSet.IsRemoveUnusedImportEnabled Then
                Dim removeUsingsService As IRemoveUnnecessaryImportsService = currentDocument.GetLanguageService(Of IRemoveUnnecessaryImportsService)()
                If removeUsingsService IsNot Nothing Then
                    Using Logger.LogBlock(FunctionId.CodeCleanup_RemoveUnusedImports, cancelToken)
                        currentDocument = Await removeUsingsService.RemoveUnnecessaryImportsAsync(currentDocument, cancelToken).ConfigureAwait(False)
                    End Using
                End If
            End If

            If organizeImportsSet.IsSortImportsEnabled Then
                Using Logger.LogBlock(FunctionId.CodeCleanup_SortImports, cancelToken)
                    currentDocument = Await Formatter.OrganizeImportsAsync(currentDocument, cancelToken).ConfigureAwait(False)
                End Using
            End If

            Return currentDocument
        End Function

        Private Async Function ApplyCodeFixesAsync(currentDocument As Document,
                                                   enabledDiagnosticSets As ImmutableArray(Of DiagnosticSet),
                                                   progressTracker As IProgressTracker,
                                                   cancelToken As CancellationToken) As Task(Of Document)
            ' Add a progress item for each enabled option we're going to fix-up.
            progressTracker.AddItems(enabledDiagnosticSets.Length)

            For Each diagnosticSet1 As DiagnosticSet In enabledDiagnosticSets
                cancelToken.ThrowIfCancellationRequested()

                progressTracker.Description = diagnosticSet1.Description
                currentDocument = Await ApplyCodeFixesForSpecificDiagnosticIdsAsync(currentDocument,
                                                                                    diagnosticSet1.DiagnosticIds,
                                                                                    progressTracker,
                                                                                    cancelToken).ConfigureAwait(False)

                ' Mark this option as being completed.
                progressTracker.ItemCompleted()
            Next

            Return currentDocument
        End Function

        Private Async Function ApplyCodeFixesForSpecificDiagnosticIdsAsync(currentDocument As Document,
                                                                           diagnosticIds As ImmutableArray(Of String),
                                                                           progressTracker As IProgressTracker,
                                                                           cancelToken As CancellationToken) As Task(Of Document)
            For Each diagnosticId As String In diagnosticIds
                Using Logger.LogBlock(FunctionId.CodeCleanup_ApplyCodeFixesAsync, diagnosticId, cancelToken)
                    currentDocument = Await _codeFixServiceOpt.ApplyCodeFixesForSpecificDiagnosticIdAsync(currentDocument,
                                                                                                          diagnosticId,
                                                                                                          progressTracker,
                                                                                                          cancelToken).ConfigureAwait(False)
                End Using
            Next

            Return currentDocument
        End Function

        Public Function GetAllDiagnostics() As EnabledDiagnosticOptions Implements ICodeCleanupService.GetAllDiagnostics
            Return New EnabledDiagnosticOptions(formatDocument:=True,
                                                s_diagnosticSets,
                                                New OrganizeUsingsSet(isRemoveUnusedImportEnabled:=True, isSortImportsEnabled:=True))
        End Function
    End Class
End Namespace
