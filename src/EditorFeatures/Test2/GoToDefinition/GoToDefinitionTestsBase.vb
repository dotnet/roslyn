' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.CSharp.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.GoToDefinition
Imports Microsoft.CodeAnalysis.GoToDefinition
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToDefinition
    Public Class GoToDefinitionTestsBase
        Public Shared Async Function TestAsync(
                workspaceDefinition As XElement,
                Optional expectedResult As Boolean = True) As Task
            Using workspace = TestWorkspace.Create(workspaceDefinition, composition:=GoToTestHelpers.Composition)
                Dim solution = workspace.CurrentSolution
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                ' Set up mocks. The IDocumentNavigationService should be called if there is one,
                ' location and the INavigableItemsPresenter should be called if there are
                ' multiple locations.

                ' prepare a notification listener
                Dim textView = cursorDocument.GetTextView()
                Dim textBuffer = textView.TextBuffer
                textView.Caret.MoveTo(New SnapshotPoint(textBuffer.CurrentSnapshot, cursorPosition))

                Dim cursorBuffer = cursorDocument.GetTextBuffer()
                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim mockDocumentNavigationService = DirectCast(workspace.Services.GetService(Of IDocumentNavigationService)(), MockDocumentNavigationService)
                Dim mockSymbolNavigationService = DirectCast(workspace.Services.GetService(Of ISymbolNavigationService)(), MockSymbolNavigationService)

                Dim presenterCalled As Boolean = False
                Dim threadingContext = workspace.ExportProvider.GetExportedValue(Of IThreadingContext)()
                Dim presenter = New MockStreamingFindUsagesPresenter(workspace.GlobalOptions, Sub() presenterCalled = True)

                Dim goToDefService = If(document.Project.Language = LanguageNames.CSharp,
                    DirectCast(New CSharpAsyncGoToDefinitionService(threadingContext, presenter), IAsyncGoToDefinitionService),
                    New VisualBasicAsyncGoToDefinitionService(threadingContext, presenter))

                Dim defLocation = Await goToDefService.FindDefinitionLocationAsync(document, cursorPosition, CancellationToken.None)
                Dim actualResult = defLocation IsNot Nothing AndAlso
                    Await defLocation.NavigateToAsync(NavigationOptions.Default, CancellationToken.None)
                Assert.Equal(expectedResult, actualResult)

                Dim expectedLocations As New List(Of FilePathAndSpan)

                For Each testDocument In workspace.Documents
                    For Each selectedSpan In testDocument.SelectedSpans
                        expectedLocations.Add(New FilePathAndSpan(testDocument.FilePath, selectedSpan))
                    Next
                Next

                expectedLocations.Sort()

                Dim context = presenter.Context
                If expectedResult Then
                    If expectedLocations.Count = 0 Then
                        ' if there is not expected locations, it means symbol navigation is used
                        Assert.True(mockSymbolNavigationService._triedNavigationToSymbol, "a navigation took place")
                        Assert.Null(mockDocumentNavigationService._documentId)
                        Assert.False(presenterCalled)
                    Else
                        Assert.False(mockSymbolNavigationService._triedNavigationToSymbol)

                        If mockDocumentNavigationService._triedNavigationToSpan Then
                            Dim definitionDocument = workspace.GetTestDocument(mockDocumentNavigationService._documentId)
                            Assert.Single(definitionDocument.SelectedSpans)
                            Assert.Equal(definitionDocument.SelectedSpans.Single(), mockDocumentNavigationService._span)

                            ' The INavigableItemsPresenter should not have been called
                            Assert.False(presenterCalled)
                        ElseIf mockDocumentNavigationService._triedNavigationToPosition Then
                            Dim definitionDocument = workspace.GetTestDocument(mockDocumentNavigationService._documentId)
                            Assert.Single(definitionDocument.SelectedSpans)
                            Dim expected = definitionDocument.SelectedSpans.Single()
                            Assert.True(expected.Length = 0)
                            Assert.Equal(expected.Start, mockDocumentNavigationService._position)

                            ' The INavigableItemsPresenter should not have been called
                            Assert.False(presenterCalled)
                        Else
                            Assert.False(mockDocumentNavigationService._triedNavigationToLineAndOffset)
                            Assert.True(presenterCalled)

                            Dim actualLocations As New List(Of FilePathAndSpan)

                            Dim items = context.GetDefinitions()

                            For Each location In items
                                For Each docSpan In location.SourceSpans
                                    actualLocations.Add(New FilePathAndSpan(docSpan.Document.FilePath, docSpan.SourceSpan))
                                Next
                            Next

                            actualLocations.Sort()
                            Assert.Equal(expectedLocations, actualLocations)

                            ' The IDocumentNavigationService should not have been called
                            Assert.Null(mockDocumentNavigationService._documentId)
                        End If
                    End If
                Else
                    Assert.False(mockSymbolNavigationService._triedNavigationToSymbol)
                    Assert.Null(mockDocumentNavigationService._documentId)
                    Assert.False(presenterCalled)
                End If

            End Using
        End Function
    End Class
End Namespace
