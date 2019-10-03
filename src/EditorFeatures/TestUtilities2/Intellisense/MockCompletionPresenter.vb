' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Imports System.Collections.Immutable
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class MockCompletionPresenter
        Implements ICompletionPresenter

        Private ReadOnly _textView As ITextView
        Private _filters As ImmutableArray(Of CompletionFilterWithState)
        Private _presentedItems As ImmutableArray(Of CompletionItemWithHighlight)
        Private _selectSuggestionItem As Boolean
        Private _selectedItem As CompletionItem

        Public Sub New(textView As ITextView)
            _textView = textView
        End Sub

        Public Event FiltersChanged As EventHandler(Of CompletionFilterChangedEventArgs) Implements ICompletionPresenter.FiltersChanged
        Public Event CompletionItemSelected As EventHandler(Of CompletionItemSelectedEventArgs) Implements ICompletionPresenter.CompletionItemSelected
        Public Event CommitRequested As EventHandler(Of Data.CompletionItemEventArgs) Implements ICompletionPresenter.CommitRequested
        Public Event CompletionClosed As EventHandler(Of CompletionClosedEventArgs) Implements ICompletionPresenter.CompletionClosed

        ' This event Is Not part of ICompletionPresenter. We use it to notify test that UI has updated.
        Public Event UiUpdated As EventHandler(Of CompletionItemSelectedEventArgs)

        Public Sub Open(session As IAsyncCompletionSession, presentation As CompletionPresentationViewModel) Implements ICompletionPresenter.Open
            DoUpdate(presentation)
        End Sub

        Public Sub Update(session As IAsyncCompletionSession, presentation As CompletionPresentationViewModel) Implements ICompletionPresenter.Update
            DoUpdate(presentation)
        End Sub

        Public Sub Close() Implements ICompletionPresenter.Close
            RaiseEvent CompletionClosed(Me, New CompletionClosedEventArgs(_textView))
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub

        Private Sub DoUpdate(presentation As CompletionPresentationViewModel)
            _filters = presentation.Filters
            _presentedItems = presentation.Items
            _selectSuggestionItem = presentation.SelectSuggestionItem
            If presentation.SelectSuggestionItem Then
                ProgrammaticallySelectItem(presentation.SuggestionItem, True)
            ElseIf Not presentation.Items.IsDefaultOrEmpty Then
                ProgrammaticallySelectItem(presentation.Items(presentation.SelectedItemIndex).CompletionItem, False)
            Else
                ProgrammaticallySelectItem(Nothing, False)
            End If
        End Sub

        Public Sub SetFilter(targetFilter As CompletionFilter, isSelected As Boolean)
            _filters = _filters.Select(Function(n) If(n.Filter.Equals(targetFilter), n.WithSelected(isSelected), n)).ToImmutableArray()
            If FiltersChangedEvent IsNot Nothing Then
                RaiseEvent FiltersChanged(Me, New CompletionFilterChangedEventArgs(_filters))
            End If
        End Sub

        Public Function GetFilters() As ImmutableArray(Of CompletionFilterWithState)
            Return _filters.WhereAsArray(Function(state) TypeOf state.Filter IsNot CompletionExpander)
        End Function

        Public Sub SetExpander(isSelected As Boolean)
            _filters = _filters.Select(Function(n) If(TypeOf n.Filter Is CompletionExpander, n.WithSelected(isSelected), n)).ToImmutableArray()
            RaiseEvent FiltersChanged(Me, New CompletionFilterChangedEventArgs(_filters))
        End Sub

        Public Function GetExpander() As CompletionFilterWithState
            Return _filters.SingleOrDefault(Function(state) TypeOf state.Filter Is CompletionExpander)
        End Function

        Public Sub TriggerFiltersChanged(sender As Object, args As CompletionFilterChangedEventArgs)
            RaiseEvent FiltersChanged(sender, args)
        End Sub

        Public Sub ProgrammaticallySelectItem(itemToSelect As CompletionItem, thisIsSuggestionItem As Boolean)
            If itemToSelect IsNot Nothing AndAlso Not _presentedItems.Any(Function(n) n.CompletionItem.Equals(itemToSelect)) AndAlso Not thisIsSuggestionItem Then
                Throw New ArgumentOutOfRangeException(NameOf(itemToSelect))
            End If

            _selectedItem = itemToSelect
            _selectSuggestionItem = thisIsSuggestionItem

            If UiUpdatedEvent IsNot Nothing Then
                RaiseEvent UiUpdated(Me, New CompletionItemSelectedEventArgs(itemToSelect, thisIsSuggestionItem))
            End If
        End Sub
    End Class
End Namespace
