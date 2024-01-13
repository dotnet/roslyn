' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeDefinitionWindow
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.CodeDefinitionWindow.UnitTests

    Public MustInherit Class AbstractCodeDefinitionWindowTests
        Public Shared ReadOnly TestComposition As TestComposition =
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

            Public Function IsWindowOpenAsync(cancellationToken As CancellationToken) As Task(Of Boolean) Implements ICodeDefinitionWindowService.IsWindowOpenAsync
                Throw New NotImplementedException()
            End Function

            Public Function SetContextAsync(locations As ImmutableArray(Of CodeDefinitionWindowLocation), cancellationToken As CancellationToken) As Task Implements ICodeDefinitionWindowService.SetContextAsync
                Throw New NotImplementedException()
            End Function
        End Class

        Protected MustOverride Function CreateWorkspace(code As String, testComposition As TestComposition) As EditorTestWorkspace

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
                    workspace,
                    document,
                    hostDocument.CursorPosition.Value,
                    CancellationToken.None)

                Dim location = Assert.Single(locations)
                Assert.Equal(displayName, location.DisplayName)
                Assert.EndsWith(fileName, location.FilePath)
            End Using
        End Function

        Protected Async Function VerifyContextLocationAsync(code As String, displayName As String) As Task
            Using workspace = CreateWorkspace(code, TestComposition)
                Await VerifyContextLocationAsync(displayName, workspace)
            End Using
        End Function

        Public Shared Async Function VerifyContextLocationAsync(displayName As String, workspace As EditorTestWorkspace) As Task
            Dim triggerHostDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
            Dim triggerDocument = workspace.CurrentSolution.GetDocument(triggerHostDocument.Id)
            Dim triggerTree = Await triggerDocument.GetSyntaxTreeAsync()

            Assert.Empty(triggerTree.GetDiagnostics(CancellationToken.None))

            Dim definitionContextTracker = workspace.ExportProvider.GetExportedValue(Of DefinitionContextTracker)
            Dim locations = Await definitionContextTracker.GetContextFromPointAsync(
                workspace,
                triggerDocument,
                triggerHostDocument.CursorPosition.Value,
                CancellationToken.None)

            Dim expectedHostDocument = workspace.Documents.Single(Function(d) d.SelectedSpans.Any())
            Dim expectedDocument = workspace.CurrentSolution.GetDocument(expectedHostDocument.Id)
            Dim expectedSpan = (Await expectedDocument.GetSyntaxTreeAsync()).GetLocation(expectedHostDocument.SelectedSpans.Single()).GetLineSpan()
            Dim expectedLocation = New CodeDefinitionWindowLocation(
                displayName,
                expectedSpan.Path,
                expectedSpan.StartLinePosition)

            Assert.Equal(expectedLocation, locations.Single())
        End Function
    End Class
End Namespace
