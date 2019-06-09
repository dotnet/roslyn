' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Friend Class TestCompletionPresenterSession
        Implements ICompletionPresenterSession

        Private ReadOnly _testState As IIntelliSenseTestState

        Public TriggerSpan As ITrackingSpan
        Public CompletionItems As IList(Of CompletionItem)
        Public SelectedItem As CompletionItem
        Public IsSoftSelected As Boolean
        Public SuggestionModeItem As CompletionItem
        Public SuggestionMode As Boolean

        Public Event Dismissed As EventHandler(Of EventArgs) Implements ICompletionPresenterSession.Dismissed
        Public Event ItemSelected As EventHandler(Of CompletionItemEventArgs) Implements ICompletionPresenterSession.ItemSelected
        Public Event ItemCommitted As EventHandler(Of CompletionItemEventArgs) Implements ICompletionPresenterSession.ItemCommitted
        Public Event CompletionFiltersChanged As EventHandler(Of CompletionItemFilterStateChangedEventArgs) Implements ICompletionPresenterSession.FilterStateChanged

        Public Sub New(testState As IIntelliSenseTestState)
            Me._testState = testState
        End Sub

        Public Sub PresentItems(textSnapshot As ITextSnapshot,
                                triggerSpan As ITrackingSpan,
                                completionItems As IList(Of CompletionItem),
                                selectedItem As CompletionItem,
                                suggestionModeItem As CompletionItem,
                                suggestionMode As Boolean,
                                isSoftSelected As Boolean,
                                completionItemFilters As ImmutableArray(Of CompletionItemFilter),
                                filterText As String) Implements ICompletionPresenterSession.PresentItems
            _testState.CurrentCompletionPresenterSession = Me
            Me.TriggerSpan = triggerSpan
            Me.CompletionItems = completionItems
            Me.SelectedItem = selectedItem
            Me.IsSoftSelected = isSoftSelected
            Me.SuggestionModeItem = suggestionModeItem
            Me.SuggestionMode = suggestionMode
            Me._completionFilters = completionItemFilters
        End Sub

        Public Sub Dismiss() Implements ICompletionPresenterSession.Dismiss
            _testState.CurrentCompletionPresenterSession = Nothing
        End Sub

        Public Sub SetSelectedItem(item As CompletionItem)
            Me.SelectedItem = item
            RaiseEvent ItemSelected(Me, New CompletionItemEventArgs(item))
        End Sub

        Private Function GetFilteredItemAt(index As Integer) As CompletionItem
            index = Math.Max(0, Math.Min(CompletionItems.Count - 1, index))
            Return CompletionItems(index)
        End Function

        Private Sub SelectPreviousItem() Implements ICompletionPresenterSession.SelectPreviousItem
            If IsSoftSelected Then
                IsSoftSelected = False
            Else
                SetSelectedItem(GetFilteredItemAt(CompletionItems.IndexOf(SelectedItem) - 1))
            End If
        End Sub

        Private Sub SelectNextItem() Implements ICompletionPresenterSession.SelectNextItem
            If IsSoftSelected Then
                IsSoftSelected = False
            Else
                SetSelectedItem(GetFilteredItemAt(CompletionItems.IndexOf(SelectedItem) + 1))
            End If
        End Sub

        Private Const s_itemsPerPage = 9
        Private _completionFilters As ImmutableArray(Of CompletionItemFilter)

        Public ReadOnly Property CompletionItemFilters As ImmutableArray(Of CompletionItemFilter)
            Get
                Return _completionFilters
            End Get
        End Property

        Public Sub SelectPreviousPageItem() Implements ICompletionPresenterSession.SelectPreviousPageItem
            SetSelectedItem(GetFilteredItemAt(CompletionItems.IndexOf(SelectedItem) - s_itemsPerPage))
        End Sub

        Public Sub SelectNextPageItem() Implements ICompletionPresenterSession.SelectNextPageItem
            SetSelectedItem(GetFilteredItemAt(CompletionItems.IndexOf(SelectedItem) + s_itemsPerPage))
        End Sub

        Public Sub RaiseFiltersChanged(args As CompletionItemFilterStateChangedEventArgs)
            RaiseEvent CompletionFiltersChanged(Me, args)
        End Sub
    End Class
End Namespace
