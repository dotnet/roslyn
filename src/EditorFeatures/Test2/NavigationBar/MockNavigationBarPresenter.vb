' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigationBar
    Friend Class MockNavigationBarPresenter
        Implements INavigationBarPresenter

        Private ReadOnly _textView As ITextView
        Private ReadOnly _presentItemsCallback As Action
        Private ReadOnly _presentItemsWithValuesCallback As Action(Of IList(Of NavigationBarProjectItem), NavigationBarProjectItem, IList(Of NavigationBarItem), NavigationBarItem, NavigationBarItem)

        Public Sub New(textView As ITextView, presentItemsCallback As Action)
            _textView = textView
            _presentItemsCallback = presentItemsCallback
        End Sub

        Public Sub New(textView As ITextView, presentItemsWithValuesCallback As Action(Of IList(Of NavigationBarProjectItem), NavigationBarProjectItem, IList(Of NavigationBarItem), NavigationBarItem, NavigationBarItem))
            _textView = textView
            _presentItemsWithValuesCallback = presentItemsWithValuesCallback
        End Sub

        Public Event CaretMovedOrActiveViewChanged As EventHandler(Of EventArgs) Implements INavigationBarPresenter.CaretMovedOrActiveViewChanged
        Public Event ItemSelected As EventHandler(Of NavigationBarItemSelectedEventArgs) Implements INavigationBarPresenter.ItemSelected

        Public Sub Disconnect() Implements INavigationBarPresenter.Disconnect

        End Sub

        Public Sub PresentItems(
                projects As ImmutableArray(Of NavigationBarProjectItem),
                selectedProject As NavigationBarProjectItem,
                typesWithMembers As ImmutableArray(Of NavigationBarItem),
                selectedType As NavigationBarItem,
                selectedMember As NavigationBarItem) Implements INavigationBarPresenter.PresentItems
            If _presentItemsCallback IsNot Nothing Then
                _presentItemsCallback()
            End If

            If _presentItemsWithValuesCallback IsNot Nothing Then
                _presentItemsWithValuesCallback(projects, selectedProject, typesWithMembers, selectedType, selectedMember)
            End If
        End Sub

        Public Function TryGetCurrentView() As ITextView Implements INavigationBarPresenter.TryGetCurrentView
            Return _textView
        End Function
    End Class
End Namespace
