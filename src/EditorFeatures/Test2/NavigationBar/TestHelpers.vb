' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar
Imports Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.NavigationBar
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.NavigationBar
Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigationBar
    Friend Module TestHelpers
        Private ReadOnly s_composition As TestComposition = EditorTestCompositions.EditorFeatures
        Private ReadOnly s_oopComposition As TestComposition = s_composition.WithTestHostParts(Remote.Testing.TestHost.OutOfProcess)

        Public Function AssertItemsAreAsync(workspaceElement As XElement, host As TestHost, ParamArray expectedItems As ExpectedItem()) As Tasks.Task
            Return AssertItemsAreAsync(workspaceElement, host, True, expectedItems)
        End Function

        Public Async Function AssertItemsAreAsync(
                workspaceElement As XElement,
                host As TestHost,
                workspaceSupportsChangeDocument As Boolean,
                ParamArray expectedItems As ExpectedItem()) As Tasks.Task
            Using workspace = EditorTestWorkspace.Create(workspaceElement, composition:=If(host = TestHost.OutOfProcess, s_oopComposition, s_composition))
                workspace.CanApplyChangeDocument = workspaceSupportsChangeDocument

                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim snapshot = (Await document.GetTextAsync()).FindCorrespondingEditorTextSnapshot()

                Dim service = document.GetLanguageService(Of INavigationBarItemService)()
                Dim actualItems = Await service.GetItemsAsync(
                    document, workspaceSupportsDocumentChanges:=True, frozenPartialSemantics:=False, snapshot.Version, Nothing)

                AssertEqual(expectedItems, actualItems, document.GetLanguageService(Of ISyntaxFactsService)().IsCaseSensitive)
            End Using
        End Function

        Public Async Function AssertSelectedItemsAreAsync(
                workspaceElement As XElement,
                host As TestHost,
                leftItem As ExpectedItem,
                leftItemGrayed As Boolean,
                rightItem As ExpectedItem,
                rightItemGrayed As Boolean) As Tasks.Task
            Using workspace = EditorTestWorkspace.Create(workspaceElement, composition:=If(host = TestHost.OutOfProcess, s_oopComposition, s_composition))
                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim snapshot = (Await document.GetTextAsync()).FindCorrespondingEditorTextSnapshot()

                Dim service = document.GetLanguageService(Of INavigationBarItemService)()
                Dim items = Await service.GetItemsAsync(
                    document, workspaceSupportsDocumentChanges:=True, frozenPartialSemantics:=False, snapshot.Version, Nothing)

                Dim hostDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
                Dim model As New NavigationBarModel(service, items)
                Dim selectedItems = NavigationBarController.ComputeSelectedTypeAndMember(model, New SnapshotPoint(hostDocument.GetTextBuffer().CurrentSnapshot, hostDocument.CursorPosition.Value), Nothing)

                Dim isCaseSensitive = document.GetLanguageService(Of ISyntaxFactsService)().IsCaseSensitive

                AssertEqual(leftItem, selectedItems.TypeItem, isCaseSensitive)
                Assert.Equal(leftItemGrayed, selectedItems.ShowTypeItemGrayed)
                AssertEqual(rightItem, selectedItems.MemberItem, isCaseSensitive)
                Assert.Equal(rightItemGrayed, selectedItems.ShowMemberItemGrayed)
            End Using
        End Function

        Public Function AssertGeneratedResultIsAsync(workspaceElement As XElement, host As TestHost, leftItemToSelectText As String, rightItemToSelectText As String, expectedText As XElement) As Tasks.Task
            Dim selectRightItem As Func(Of IList(Of NavigationBarItem), NavigationBarItem)
            selectRightItem = Function(items) items.Single(Function(i) i.Text = rightItemToSelectText)
            Return AssertGeneratedResultIsAsync(workspaceElement, host, leftItemToSelectText, selectRightItem, expectedText)
        End Function

        Public Async Function AssertGeneratedResultIsAsync(
                workspaceElement As XElement,
                host As TestHost,
                leftItemToSelectText As String,
                selectRightItem As Func(Of IList(Of NavigationBarItem), NavigationBarItem),
                expectedText As XElement) As Tasks.Task
            Using workspace = EditorTestWorkspace.Create(workspaceElement, composition:=If(host = TestHost.OutOfProcess, s_oopComposition, s_composition))
                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim snapshot = (Await document.GetTextAsync()).FindCorrespondingEditorTextSnapshot()

                Dim service = document.GetLanguageService(Of INavigationBarItemService)()

                Dim items = Await service.GetItemsAsync(
                    document, workspaceSupportsDocumentChanges:=True, frozenPartialSemantics:=False, snapshot.Version, Nothing)

                Dim leftItem = items.Single(Function(i) i.Text = leftItemToSelectText)
                Dim rightItem = selectRightItem(leftItem.ChildItems)

                Dim contextLocation = (Await document.GetSyntaxTreeAsync()).GetLocation(New TextSpan(0, 0))
                Dim generateCodeItem = DirectCast(rightItem, WrappedNavigationBarItem).UnderlyingItem
                Dim newDocument = Await VisualBasicEditorNavigationBarItemService.GetGeneratedDocumentAsync(document, generateCodeItem, CancellationToken.None)

                Dim actual = (Await newDocument.GetSyntaxRootAsync()).ToFullString().TrimEnd()
                Dim expected = expectedText.NormalizedValue.TrimEnd()
                Assert.Equal(expected, actual)
            End Using
        End Function

        Public Async Function AssertNavigationPointAsync(
                workspaceElement As XElement,
                host As TestHost,
                startingDocumentFilePath As String,
                leftItemToSelectText As String,
                rightItemToSelectText As String,
                Optional expectedVirtualSpace As Integer = 0) As Tasks.Task

            Using workspace = EditorTestWorkspace.Create(workspaceElement, composition:=If(host = TestHost.OutOfProcess, s_oopComposition, s_composition))
                Dim sourceDocument = workspace.CurrentSolution.Projects.First().Documents.First(Function(doc) doc.FilePath = startingDocumentFilePath)
                Dim snapshot = (Await sourceDocument.GetTextAsync()).FindCorrespondingEditorTextSnapshot()

                Dim service = DirectCast(sourceDocument.GetLanguageService(Of INavigationBarItemService)(), AbstractEditorNavigationBarItemService)
                Dim items = Await service.GetItemsAsync(
                    sourceDocument, workspaceSupportsDocumentChanges:=True, frozenPartialSemantics:=False, snapshot.Version, Nothing)

                Dim leftItem = items.Single(Function(i) i.Text = leftItemToSelectText)
                Dim rightItem = leftItem.ChildItems.Single(Function(i) i.Text = rightItemToSelectText)

                Dim navigationPoint = Await service.GetNavigationLocationAsync(
                    sourceDocument,
                    rightItem,
                    DirectCast(DirectCast(rightItem, WrappedNavigationBarItem).UnderlyingItem, RoslynNavigationBarItem.SymbolItem),
                    snapshot.Version,
                    cancellationToken:=Nothing)

                Dim expectedNavigationDocument = workspace.Documents.Single(Function(doc) doc.CursorPosition.HasValue)
                Assert.Equal(expectedNavigationDocument.Id, navigationPoint.documentId)

                Dim expectedNavigationPosition = expectedNavigationDocument.CursorPosition.Value
                Assert.Equal(expectedNavigationPosition, navigationPoint.position)
                Assert.Equal(expectedVirtualSpace, navigationPoint.virtualSpace)
            End Using
        End Function

        Private Sub AssertEqual(expectedItems As IEnumerable(Of ExpectedItem), actualItems As IEnumerable(Of NavigationBarItem), isCaseSensitive As Boolean)
            Assert.Equal(expectedItems.Count, actualItems.Count)

            For i = 0 To actualItems.Count - 1
                Dim expectedItem = expectedItems(i)
                Dim actualItem = actualItems(i)

                AssertEqual(expectedItem, actualItem, isCaseSensitive)
            Next
        End Sub

        Private Sub AssertEqual(expectedItem As ExpectedItem, actualItem As NavigationBarItem, isCaseSensitive As Boolean)
            If expectedItem Is Nothing AndAlso actualItem Is Nothing Then
                Return
            End If

            Assert.Equal(expectedItem.Text, actualItem.Text)
            Assert.Equal(expectedItem.Glyph, actualItem.Glyph)
            Assert.Equal(expectedItem.Bolded, actualItem.Bolded)
            Assert.Equal(expectedItem.Indent, actualItem.Indent)
            Assert.Equal(expectedItem.Grayed, actualItem.Grayed)

            Dim underlyingItem = DirectCast(actualItem, WrappedNavigationBarItem).UnderlyingItem
            If expectedItem.HasNavigationSymbolId Then
                Assert.True(TypeOf underlyingItem Is RoslynNavigationBarItem.SymbolItem)
            Else
                Assert.True(TypeOf underlyingItem IsNot RoslynNavigationBarItem.SymbolItem)
            End If

            If expectedItem.Children IsNot Nothing Then
                AssertEqual(expectedItem.Children,
                            actualItem.ChildItems, isCaseSensitive)
            End If
        End Sub

        Public Function Item(text As String,
                             glyph As Glyph,
                             Optional children As IEnumerable(Of ExpectedItem) = Nothing,
                             Optional indent As Integer = 0,
                             Optional bolded As Boolean = False,
                             Optional grayed As Boolean = False,
                             Optional hasNavigationSymbolId As Boolean = True) As ExpectedItem

            Return New ExpectedItem() With {.Text = text,
                                            .Glyph = glyph,
                                            .Children = children,
                                            .Indent = indent,
                                            .Bolded = bolded,
                                            .Grayed = grayed,
                                            .HasNavigationSymbolId = hasNavigationSymbolId}
        End Function

        Friend Class ExpectedItem
            Public Property Text As String
            Public Property Glyph As Glyph
            Public Property Children As IEnumerable(Of ExpectedItem)
            Public Property Indent As Integer
            Public Property Bolded As Boolean
            Public Property Grayed As Boolean
            Public Property HasNavigationSymbolId As Boolean
        End Class
    End Module
End Namespace
