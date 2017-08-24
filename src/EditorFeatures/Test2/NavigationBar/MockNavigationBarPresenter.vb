' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigationBar
    Friend Class MockNavigationBarPresenter
        Implements INavigationBarPresenter

        Private _textView As ITextView
        Private _presentItemsCallback As Action
        Private _presentItemsWithValuesCallback As Action(Of IList(Of NavigationBarProjectItem), NavigationBarProjectItem, IList(Of NavigationBarItem), NavigationBarItem, NavigationBarItem)

        Public Sub New(textView As ITextView, presentItemsCallback As Action)
            _textView = textView
            _presentItemsCallback = presentItemsCallback
        End Sub

        Public Sub New(textView As ITextView, presentItemsWithValuesCallback As Action(Of IList(Of NavigationBarProjectItem), NavigationBarProjectItem, IList(Of NavigationBarItem), NavigationBarItem, NavigationBarItem))
            _textView = textView
            _presentItemsWithValuesCallback = presentItemsWithValuesCallback
        End Sub

        Public Sub RaiseDropDownFocused()
            RaiseEvent DropDownFocused(Nothing, EventArgs.Empty)
        End Sub

        Event CaretMoved As EventHandler(Of CaretPositionChangedEventArgs) Implements INavigationBarPresenter.CaretMoved
        Event DropDownFocused As EventHandler Implements INavigationBarPresenter.DropDownFocused
        Event ItemSelected As EventHandler(Of NavigationBarItemSelectedEventArgs) Implements INavigationBarPresenter.ItemSelected
        Event ViewFocused As EventHandler(Of EventArgs) Implements INavigationBarPresenter.ViewFocused

        Public Sub Disconnect() Implements INavigationBarPresenter.Disconnect

        End Sub

        Public Sub PresentItems(projects As IList(Of NavigationBarProjectItem),
                         selectedProject As NavigationBarProjectItem,
                         typesWithMembers As IList(Of NavigationBarItem),
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
