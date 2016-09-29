' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Friend Class TestCompletionPresenterSession
        Implements ICompletionPresenterSession

        Private ReadOnly _testState As IIntelliSenseTestState

        Public TriggerSpan As ITrackingSpan
        Public PresentationItems As IList(Of PresentationItem)
        Public SelectedItem As PresentationItem
        Public IsSoftSelected As Boolean
        Public SuggestionModeItem As PresentationItem

        Public Event Dismissed As EventHandler(Of EventArgs) Implements ICompletionPresenterSession.Dismissed
        Public Event ItemSelected As EventHandler(Of PresentationItemEventArgs) Implements ICompletionPresenterSession.ItemSelected
        Public Event ItemCommitted As EventHandler(Of PresentationItemEventArgs) Implements ICompletionPresenterSession.ItemCommitted
        Public Event CompletionFiltersChanged As EventHandler(Of CompletionItemFilterStateChangedEventArgs) Implements ICompletionPresenterSession.FilterStateChanged

        Public Sub New(testState As IIntelliSenseTestState)
            Me._testState = testState
        End Sub

        Public Sub PresentItems(triggerSpan As ITrackingSpan,
                                presentationItems As IList(Of PresentationItem),
                                selectedItem As PresentationItem,
                                suggestionModeItem As PresentationItem,
                                suggestionMode As Boolean,
                                isSoftSelected As Boolean,
                                completionItemFilters As ImmutableArray(Of CompletionItemFilter),
                                filterText As String) Implements ICompletionPresenterSession.PresentItems
            _testState.CurrentCompletionPresenterSession = Me
            Me.TriggerSpan = triggerSpan
            Me.PresentationItems = presentationItems
            Me.SelectedItem = selectedItem
            Me.IsSoftSelected = isSoftSelected
            Me.SuggestionModeItem = suggestionModeItem
        End Sub

        Public Sub Dismiss() Implements ICompletionPresenterSession.Dismiss
            _testState.CurrentCompletionPresenterSession = Nothing
        End Sub

        Public Sub SetSelectedItem(item As PresentationItem)
            Me.SelectedItem = item
            RaiseEvent ItemSelected(Me, New PresentationItemEventArgs(item))
        End Sub

        Private Function GetFilteredItemAt(index As Integer) As PresentationItem
            index = Math.Max(0, Math.Min(PresentationItems.Count - 1, index))
            Return PresentationItems(index)
        End Function

        Private Sub SelectPreviousItem() Implements ICompletionPresenterSession.SelectPreviousItem
            If IsSoftSelected Then
                IsSoftSelected = False
            Else
                SetSelectedItem(GetFilteredItemAt(PresentationItems.IndexOf(SelectedItem) - 1))
            End If
        End Sub

        Private Sub SelectNextItem() Implements ICompletionPresenterSession.SelectNextItem
            If IsSoftSelected Then
                IsSoftSelected = False
            Else
                SetSelectedItem(GetFilteredItemAt(PresentationItems.IndexOf(SelectedItem) + 1))
            End If
        End Sub

        Private Const s_itemsPerPage = 9

        Public Sub SelectPreviousPageItem() Implements ICompletionPresenterSession.SelectPreviousPageItem
            SetSelectedItem(GetFilteredItemAt(PresentationItems.IndexOf(SelectedItem) - s_itemsPerPage))
        End Sub

        Public Sub SelectNextPageItem() Implements ICompletionPresenterSession.SelectNextPageItem
            SetSelectedItem(GetFilteredItemAt(PresentationItems.IndexOf(SelectedItem) + s_itemsPerPage))
        End Sub
    End Class
End Namespace
