' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar
Imports Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.NavigationBar
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigationBar
    Friend Module TestHelpers
        Public Function AssertItemsAreAsync(workspaceElement As XElement, ParamArray expectedItems As ExpectedItem()) As Tasks.Task
            Return AssertItemsAreAsync(workspaceElement, True, expectedItems)
        End Function

        Public Async Function AssertItemsAreAsync(workspaceElement As XElement, workspaceSupportsChangeDocument As Boolean, ParamArray expectedItems As ExpectedItem()) As Tasks.Task
            Using workspace = TestWorkspace.Create(workspaceElement)
                workspace.CanApplyChangeDocument = workspaceSupportsChangeDocument

                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim snapshot = (Await document.GetTextAsync()).FindCorrespondingEditorTextSnapshot()

                Dim service = document.GetLanguageService(Of INavigationBarItemService)()
                Dim actualItems = Await service.GetItemsAsync(document, Nothing)
                actualItems.Do(Sub(i) i.InitializeTrackingSpans(snapshot))

                AssertEqual(expectedItems, actualItems, document.GetLanguageService(Of ISyntaxFactsService)().IsCaseSensitive)
            End Using
        End Function

        Public Async Function AssertSelectedItemsAreAsync(workspaceElement As XElement, leftItem As ExpectedItem, leftItemGrayed As Boolean, rightItem As ExpectedItem, rightItemGrayed As Boolean) As Tasks.Task
            Using workspace = TestWorkspace.Create(workspaceElement)
                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim snapshot = (Await document.GetTextAsync()).FindCorrespondingEditorTextSnapshot()

                Dim service = document.GetLanguageService(Of INavigationBarItemService)()
                Dim items = Await service.GetItemsAsync(document, Nothing)
                items.Do(Sub(i) i.InitializeTrackingSpans(snapshot))

                Dim hostDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
                Dim model As New NavigationBarModel(items, VersionStamp.Create(), service)
                Dim selectedItems = NavigationBarController.ComputeSelectedTypeAndMember(model, New SnapshotPoint(hostDocument.TextBuffer.CurrentSnapshot, hostDocument.CursorPosition.Value), Nothing)

                Dim isCaseSensitive = document.GetLanguageService(Of ISyntaxFactsService)().IsCaseSensitive

                AssertEqual(leftItem, selectedItems.TypeItem, isCaseSensitive)
                Assert.Equal(leftItemGrayed, selectedItems.ShowTypeItemGrayed)
                AssertEqual(rightItem, selectedItems.MemberItem, isCaseSensitive)
                Assert.Equal(rightItemGrayed, selectedItems.ShowMemberItemGrayed)
            End Using
        End Function

        Public Function AssertGeneratedResultIsAsync(workspaceElement As XElement, leftItemToSelectText As String, rightItemToSelectText As String, expectedText As XElement) As Tasks.Task
            Dim selectRightItem As Func(Of IList(Of NavigationBarItem), NavigationBarItem)
            selectRightItem = Function(items) items.Single(Function(i) i.Text = rightItemToSelectText)
            Return AssertGeneratedResultIsAsync(workspaceElement, leftItemToSelectText, selectRightItem, expectedText)
        End Function

        Public Async Function AssertGeneratedResultIsAsync(workspaceElement As XElement, leftItemToSelectText As String, selectRightItem As Func(Of IList(Of NavigationBarItem), NavigationBarItem), expectedText As XElement) As Tasks.Task
            Using workspace = TestWorkspace.Create(workspaceElement)
                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim snapshot = (Await document.GetTextAsync()).FindCorrespondingEditorTextSnapshot()

                Dim service = document.GetLanguageService(Of INavigationBarItemService)()

                Dim items = Await service.GetItemsAsync(document, Nothing)
                items.Do(Sub(i) i.InitializeTrackingSpans(snapshot))

                Dim leftItem = items.Single(Function(i) i.Text = leftItemToSelectText)
                Dim rightItem = selectRightItem(leftItem.ChildItems)

                Dim contextLocation = (Await document.GetSyntaxTreeAsync()).GetLocation(New TextSpan(0, 0))
                Dim generateCodeItem = DirectCast(rightItem, AbstractGenerateCodeItem)
                Dim newDocument = Await generateCodeItem.GetGeneratedDocumentAsync(document, CancellationToken.None)

                Dim actual = (Await newDocument.GetSyntaxRootAsync()).ToFullString().TrimEnd()
                Dim expected = expectedText.NormalizedValue.TrimEnd()
                Assert.Equal(expected, actual)
            End Using
        End Function

        Public Async Function AssertNavigationPointAsync(workspaceElement As XElement,
                                         startingDocumentFilePath As String,
                                         leftItemToSelectText As String,
                                         rightItemToSelectText As String,
                                         Optional expectedVirtualSpace As Integer = 0) As Tasks.Task

            Using workspace = TestWorkspace.Create(workspaceElement)
                Dim sourceDocument = workspace.CurrentSolution.Projects.First().Documents.First(Function(doc) doc.FilePath = startingDocumentFilePath)
                Dim snapshot = (Await sourceDocument.GetTextAsync()).FindCorrespondingEditorTextSnapshot()

                Dim service = DirectCast(sourceDocument.GetLanguageService(Of INavigationBarItemService)(), AbstractNavigationBarItemService)
                Dim items = Await service.GetItemsAsync(sourceDocument, Nothing)
                items.Do(Sub(i) i.InitializeTrackingSpans(snapshot))

                Dim leftItem = items.Single(Function(i) i.Text = leftItemToSelectText)
                Dim rightItem = leftItem.ChildItems.Single(Function(i) i.Text = rightItemToSelectText)

                Dim navigationPoint = service.GetSymbolItemNavigationPoint(sourceDocument, DirectCast(rightItem, NavigationBarSymbolItem), CancellationToken.None).Value

                Dim expectedNavigationDocument = workspace.Documents.Single(Function(doc) doc.CursorPosition.HasValue)
                Assert.Equal(expectedNavigationDocument.FilePath, navigationPoint.Tree.FilePath)

                Dim expectedNavigationPosition = expectedNavigationDocument.CursorPosition.Value
                Assert.Equal(expectedNavigationPosition, navigationPoint.Position)
                Assert.Equal(expectedVirtualSpace, navigationPoint.VirtualSpaces)
            End Using
        End Function

        Private Sub AssertEqual(expectedItems As IEnumerable(Of ExpectedItem), actualItems As IEnumerable(Of NavigationBarItem), isCaseSensitive As Boolean)
            Assert.Equal(expectedItems.Count, actualItems.Count)

            For i = 0 To actualItems.Count - 1
                Dim expectedItem = expectedItems(i)
                Dim actualItem = actualItems(i)

                AssertEqual(expectedItem, actualItem, isCaseSensitive)
            Next

            ' Ensure all the actual items that have navigation are distinct
            Dim navigableItems = actualItems.OfType(Of NavigationBarSymbolItem).ToList()

            Assert.True(navigableItems.Count() = navigableItems.Distinct(New NavigationBarItemNavigationSymbolComparer(isCaseSensitive)).Count(), "The items were not unique by SymbolID and index.")
        End Sub

        Private Class NavigationBarItemNavigationSymbolComparer
            Implements IEqualityComparer(Of NavigationBarSymbolItem)

            Private ReadOnly _symbolIdComparer As IEqualityComparer(Of SymbolKey)

            Public Sub New(ignoreCase As Boolean)
                _symbolIdComparer = If(ignoreCase, SymbolKey.GetComparer(ignoreCase:=True, ignoreAssemblyKeys:=False), SymbolKey.GetComparer(ignoreCase:=False, ignoreAssemblyKeys:=False))
            End Sub

            Public Function IEqualityComparer_Equals(x As NavigationBarSymbolItem, y As NavigationBarSymbolItem) As Boolean Implements IEqualityComparer(Of NavigationBarSymbolItem).Equals
                Return _symbolIdComparer.Equals(x.NavigationSymbolId, y.NavigationSymbolId) AndAlso x.NavigationSymbolIndex.Value = y.NavigationSymbolIndex.Value
            End Function

            Public Function IEqualityComparer_GetHashCode(obj As NavigationBarSymbolItem) As Integer Implements IEqualityComparer(Of NavigationBarSymbolItem).GetHashCode
                Return _symbolIdComparer.GetHashCode(obj.NavigationSymbolId) Xor obj.NavigationSymbolIndex.Value
            End Function
        End Class

        Private Sub AssertEqual(expectedItem As ExpectedItem, actualItem As NavigationBarItem, isCaseSensitive As Boolean)
            If expectedItem Is Nothing AndAlso actualItem Is Nothing Then
                Return
            End If

            Assert.Equal(expectedItem.Text, actualItem.Text)
            Assert.Equal(expectedItem.Glyph, actualItem.Glyph)
            Assert.Equal(expectedItem.Bolded, actualItem.Bolded)
            Assert.Equal(expectedItem.Indent, actualItem.Indent)
            Assert.Equal(expectedItem.Grayed, actualItem.Grayed)

            If expectedItem.HasNavigationSymbolId Then
                ' Assert.True(DirectCast(actualItem, NavigationBarSymbolItem).NavigationSymbolId IsNot Nothing)
                Assert.Equal(expectedItem.HasNavigationSymbolId, DirectCast(actualItem, NavigationBarSymbolItem).NavigationSymbolIndex.HasValue)
            Else
                Assert.True(TypeOf actualItem IsNot NavigationBarSymbolItem)
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
