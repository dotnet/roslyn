' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Friend Class TestCompletionPresenterSession
        Implements ICompletionPresenterSession

        Private ReadOnly _testState As IIntelliSenseTestState

        Public TriggerSpan As ITrackingSpan
        Public CompletionItems As IList(Of CompletionItem)
        Public SelectedItem As CompletionItem
        Public IsSoftSelected As Boolean
        Public Builder As CompletionItem

        Public Event Dismissed As EventHandler(Of EventArgs) Implements ICompletionPresenterSession.Dismissed
        Public Event ItemSelected As EventHandler(Of CompletionItemEventArgs) Implements ICompletionPresenterSession.ItemSelected
        Public Event ItemCommitted As EventHandler(Of CompletionItemEventArgs) Implements ICompletionPresenterSession.ItemCommitted
        Public Event CompletionFiltersChanged As EventHandler(Of CompletionItemFilterStateChangedEventArgs) Implements ICompletionPresenterSession.FilterStateChanged

        Public Sub New(testState As IIntelliSenseTestState)
            Me._testState = testState
        End Sub

        Public Sub PresentItems(triggerSpan As ITrackingSpan,
                                completionItems As IList(Of CompletionItem),
                                selectedItem As CompletionItem,
                                presetBuilder As CompletionItem,
                                suggestionMode As Boolean,
                                isSoftSelected As Boolean,
                                completionItemFilters As ImmutableArray(Of CompletionItemFilter)) Implements ICompletionPresenterSession.PresentItems
            _testState.CurrentCompletionPresenterSession = Me
            Me.TriggerSpan = triggerSpan
            Me.CompletionItems = completionItems
            Me.SelectedItem = selectedItem
            Me.IsSoftSelected = isSoftSelected
            Me.Builder = presetBuilder
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

        Public Sub SelectPreviousPageItem() Implements ICompletionPresenterSession.SelectPreviousPageItem
            SetSelectedItem(GetFilteredItemAt(CompletionItems.IndexOf(SelectedItem) - s_itemsPerPage))
        End Sub

        Public Sub SelectNextPageItem() Implements ICompletionPresenterSession.SelectNextPageItem
            SetSelectedItem(GetFilteredItemAt(CompletionItems.IndexOf(SelectedItem) + s_itemsPerPage))
        End Sub
    End Class
End Namespace
