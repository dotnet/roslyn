' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.CodeDefinitionWindow
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.MetadataAsSource
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.CodeDefinitionWindow.UnitTests

    Public MustInherit Class AbstractCodeDefinitionWindowTests
        Private Class FakeMetadataAsSourceFileService
            Implements IMetadataAsSourceFileService

            Public Shared ReadOnly LinePositionSpan As New LinePositionSpan(start:=New LinePosition(42, 0), [end]:=New LinePosition(42, 1))
            Public Shared ReadOnly Filename As String = "MetadataAsSourceFileName"

            Public Sub CleanupGeneratedFiles() Implements IMetadataAsSourceFileService.CleanupGeneratedFiles
                Throw New NotImplementedException()
            End Sub

            Public Function GetGeneratedFileAsync(project As Project, symbol As ISymbol, allowDecompilation As Boolean, Optional cancellationToken As CancellationToken = Nothing) As Task(Of MetadataAsSourceFile) Implements IMetadataAsSourceFileService.GetGeneratedFileAsync
                Return Task.FromResult(New MetadataAsSourceFile(
                    Filename,
                    Location.Create(
                        Filename,
                        New TextSpan(42, 1),
                        LinePositionSpan),
                    "Title",
                    "Tooltip"))
            End Function

            Public Function IsNavigableMetadataSymbol(symbol As ISymbol) As Boolean Implements IMetadataAsSourceFileService.IsNavigableMetadataSymbol
                Return True
            End Function

            Public Function TryAddDocumentToWorkspace(filePath As String, buffer As SourceTextContainer) As Boolean Implements IMetadataAsSourceFileService.TryAddDocumentToWorkspace
                Throw New NotImplementedException()
            End Function

            Public Function TryRemoveDocumentFromWorkspace(filePath As String) As Boolean Implements IMetadataAsSourceFileService.TryRemoveDocumentFromWorkspace
                Throw New NotImplementedException()
            End Function

            Public Function TryGetWorkspace() As Workspace Implements IMetadataAsSourceFileService.TryGetWorkspace
                Throw New NotImplementedException()
            End Function
        End Class

        Protected MustOverride Function CreateWorkspace(code As String, Optional testComposition As TestComposition = Nothing) As TestWorkspace

        Protected Async Function VerifyContextLocationInMetadataAsSource(
            code As String,
            displayName As String) As Task

            Dim testComposition = EditorTestCompositions.EditorFeatures.AddParts(GetType(FakeMetadataAsSourceFileService))

            Using workspace = CreateWorkspace(code, testComposition)
                Dim hostDocument = workspace.Documents.Single()
                Dim document As Document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim tree = Await document.GetSyntaxTreeAsync()

                Assert.Empty(tree.GetDiagnostics(CancellationToken.None))

                Dim definitionContextTracker As New DefinitionContextTracker(Nothing, Nothing)
                Dim locations = Await definitionContextTracker.GetContextFromPointAsync(
                    document,
                    hostDocument.CursorPosition.Value,
                    TaskScheduler.Current,
                    CancellationToken.None)

                Dim location = tree.GetLocation(hostDocument.SelectedSpans.Single()).GetLineSpan()
                Dim expectedLocation = New CodeDefinitionWindowLocation(
                    displayName,
                    New FileLinePositionSpan(FakeMetadataAsSourceFileService.Filename, FakeMetadataAsSourceFileService.LinePositionSpan))

                Assert.Equal(expectedLocation, locations.Single())
            End Using
        End Function

        Protected Async Function VerifyContextLocationInSameFile(code As String, displayName As String) As Task
            Using workspace = CreateWorkspace(code)
                Dim hostDocument = workspace.Documents.Single()
                Dim document As Document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim tree = Await document.GetSyntaxTreeAsync()

                Assert.Empty(tree.GetDiagnostics(CancellationToken.None))

                Dim definitionContextTracker As New DefinitionContextTracker(Nothing, Nothing)
                Dim locations = Await definitionContextTracker.GetContextFromPointAsync(
                    document,
                    hostDocument.CursorPosition.Value,
                    TaskScheduler.Current,
                    CancellationToken.None)

                Dim expectedLocation = New CodeDefinitionWindowLocation(
                    displayName,
                    tree.GetLocation(hostDocument.SelectedSpans.Single()).GetLineSpan())

                Assert.Equal(expectedLocation, locations.Single())
            End Using
        End Function

    End Class
End Namespace
