' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.CodeDefinitionWindow
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.CodeDefinitionWindow.UnitTests

    Public MustInherit Class AbstractCodeDefinitionWindowTests
        Protected Shared ReadOnly TestComposition As TestComposition =
            EditorTestCompositions.EditorFeatures _
                .AddParts(GetType(MockCodeDefinitionWindowService),
                          GetType(NoCompilationContentTypeLanguageService),
                          GetType(NoCompilationContentTypeDefinitions),
                          GetType(MockDocumentNavigationServiceProvider))

        <Export(GetType(ICodeDefinitionWindowService)), PartNotDiscoverable>
        Private Class MockCodeDefinitionWindowService
            Implements ICodeDefinitionWindowService

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function SetContextAsync(locations As ImmutableArray(Of CodeDefinitionWindowLocation), cancellationToken As CancellationToken) As Task Implements ICodeDefinitionWindowService.SetContextAsync
                Throw New NotImplementedException()
            End Function
        End Class

        Protected MustOverride Function CreateWorkspace(code As String, Optional testComposition As TestComposition = Nothing) As TestWorkspace

        Protected Async Function VerifyContextLocationInMetadataAsSource(
            code As String,
            displayName As String,
            fileName As String) As Task

            Using workspace = CreateWorkspace(code, TestComposition)
                Dim hostDocument = workspace.Documents.Single()
                Dim document As Document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim tree = Await document.GetSyntaxTreeAsync()

                Assert.Empty(tree.GetDiagnostics(CancellationToken.None))

                Dim definitionContextTracker = workspace.ExportProvider.GetExportedValue(Of DefinitionContextTracker)
                Dim locations = Await definitionContextTracker.GetContextFromPointAsync(
                    document,
                    hostDocument.CursorPosition.Value,
                    CancellationToken.None)

                Dim location = Assert.Single(locations)
                Assert.Equal(displayName, location.DisplayName)
                Assert.EndsWith(fileName, location.FilePath)
            End Using
        End Function

        Protected Async Function VerifyContextLocationInSameFile(code As String, displayName As String) As Task
            Using workspace = CreateWorkspace(code, TestComposition)
                Dim hostDocument = workspace.Documents.Single()
                Dim document As Document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim tree = Await document.GetSyntaxTreeAsync()

                Assert.Empty(tree.GetDiagnostics(CancellationToken.None))

                Dim definitionContextTracker = workspace.ExportProvider.GetExportedValue(Of DefinitionContextTracker)
                Dim locations = Await definitionContextTracker.GetContextFromPointAsync(
                    document,
                    hostDocument.CursorPosition.Value,
                    CancellationToken.None)

                Dim expectedLocation = New CodeDefinitionWindowLocation(
                    displayName,
                    tree.GetLocation(hostDocument.SelectedSpans.Single()).GetLineSpan())

                Assert.Equal(expectedLocation, locations.Single())
            End Using
        End Function

    End Class
End Namespace
