' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Navigation
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.GeneratedCodeRecognition
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
    Friend Module GoToTestHelpers
        Public ReadOnly Catalog As ComposableCatalog = TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithParts(
                        GetType(MockDocumentNavigationServiceFactory),
                        GetType(DefaultSymbolNavigationServiceFactory),
                        GetType(GeneratedCodeRecognitionServiceFactory))

        Public ReadOnly ExportProvider As ExportProvider = MinimalTestExportProvider.CreateExportProvider(Catalog)

        Public Async Function TestAsync(workspaceDefinition As XElement, expectedResult As Boolean, executeOnDocument As Func(Of Document, Integer, IEnumerable(Of Lazy(Of INavigableItemsPresenter)), Boolean)) As System.Threading.Tasks.Task
            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceDefinition, exportProvider:=ExportProvider)
                Dim solution = workspace.CurrentSolution
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                Dim items As IList(Of INavigableItem) = Nothing

                ' Set up mocks. The IDocumentNavigationService should be called if there is one,
                ' location and the INavigableItemsPresenter should be called if there are 
                ' multiple locations.

                ' prepare a notification listener
                Dim textView = cursorDocument.GetTextView()
                Dim textBuffer = textView.TextBuffer
                textView.Caret.MoveTo(New SnapshotPoint(textBuffer.CurrentSnapshot, cursorPosition))

                Dim cursorBuffer = cursorDocument.TextBuffer
                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim mockDocumentNavigationService = DirectCast(workspace.Services.GetService(Of IDocumentNavigationService)(), MockDocumentNavigationService)

                Dim presenter = New MockNavigableItemsPresenter(Sub(i) items = i)
                Dim presenters = {New Lazy(Of INavigableItemsPresenter)(Function() presenter)}
                Dim actualResult = executeOnDocument(document, cursorPosition, presenters)

                Assert.Equal(expectedResult, actualResult)

                If expectedResult Then
                    If mockDocumentNavigationService._triedNavigationToSpan Then
                        Dim definitionDocument = workspace.GetTestDocument(mockDocumentNavigationService._documentId)
                        Assert.Single(definitionDocument.SelectedSpans)
                        Assert.Equal(definitionDocument.SelectedSpans.Single(), mockDocumentNavigationService._span)

                        ' The INavigableItemsPresenter should not have been called
                        Assert.Null(items)
                    Else
                        Assert.False(mockDocumentNavigationService._triedNavigationToPosition)
                        Assert.False(mockDocumentNavigationService._triedNavigationToLineAndOffset)
                        Assert.NotEmpty(items)

                        For Each location In items
                            Dim definitionDocument = workspace.GetTestDocument(location.Document.Id)
                            Assert.True(definitionDocument.SelectedSpans.Contains(location.SourceSpan))
                        Next

                        ' The IDocumentNavigationService should not have been called
                        Assert.Null(mockDocumentNavigationService._documentId)
                    End If
                Else
                    Assert.Null(mockDocumentNavigationService._documentId)
                    Assert.True(items Is Nothing OrElse items.Count = 0)
                End If
            End Using
        End Function
    End Module
End Namespace