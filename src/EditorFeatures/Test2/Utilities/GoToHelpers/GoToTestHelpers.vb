﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.GeneratedCodeRecognition
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
    Friend Module GoToTestHelpers
        Public ReadOnly Catalog As ComposableCatalog = TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithParts(
                        GetType(MockDocumentNavigationServiceFactory),
                        GetType(DefaultSymbolNavigationServiceFactory),
                        GetType(GeneratedCodeRecognitionServiceFactory))

        Public ReadOnly ExportProvider As ExportProvider = MinimalTestExportProvider.CreateExportProvider(Catalog)

        Public Async Function TestAsync(workspaceDefinition As XElement, expectedResult As Boolean, executeOnDocument As Func(Of Document, Integer, IEnumerable(Of Lazy(Of INavigableItemsPresenter)), IEnumerable(Of Lazy(Of INavigableDefinitionProvider)), Boolean)) As System.Threading.Tasks.Task
            Using workspace = Await TestWorkspace.CreateAsync(workspaceDefinition, exportProvider:=ExportProvider)
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
                Dim actualResult = executeOnDocument(document, cursorPosition, presenters, {})

                Assert.Equal(expectedResult, actualResult)

                Dim expectedLocations As New List(Of FilePathAndSpan)

                For Each testDocument In workspace.Documents
                    For Each selectedSpan In testDocument.SelectedSpans
                        expectedLocations.Add(New FilePathAndSpan(testDocument.FilePath, selectedSpan))
                    Next
                Next

                expectedLocations.Sort()

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

                        Dim actualLocations As New List(Of FilePathAndSpan)

                        For Each location In items
                            actualLocations.Add(New FilePathAndSpan(location.Document.FilePath, location.SourceSpan))
                        Next

                        actualLocations.Sort()
                        Assert.Equal(expectedLocations, actualLocations)

                        ' The IDocumentNavigationService should not have been called
                        Assert.Null(mockDocumentNavigationService._documentId)
                    End If
                Else
                    Assert.Null(mockDocumentNavigationService._documentId)
                    Assert.True(items Is Nothing OrElse items.Count = 0)
                End If
            End Using
        End Function

        Private Structure FilePathAndSpan
            Implements IComparable(Of FilePathAndSpan)

            Public ReadOnly Property FilePath As String
            Public ReadOnly Property Span As TextSpan

            Public Sub New(filePath As String, span As TextSpan)
                Me.FilePath = filePath
                Me.Span = span
            End Sub

            Public Function CompareTo(other As FilePathAndSpan) As Integer Implements IComparable(Of FilePathAndSpan).CompareTo
                Dim result = String.CompareOrdinal(FilePath, other.FilePath)

                If result <> 0 Then
                    Return result
                End If

                Return Span.CompareTo(other.Span)
            End Function

            Public Overrides Function ToString() As String
                Return $"{FilePath}, {Span}"
            End Function
        End Structure
    End Module
End Namespace