' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
Imports Microsoft.VisualStudio.Text.Editor

Public Class MockCompletionPresenter
    Implements ICompletionPresenter

    Private ReadOnly _textView As ITextView

    Public Sub New(textView As ITextView)
        _textView = textView
    End Sub

    Public Event FiltersChanged As EventHandler(Of CompletionFilterChangedEventArgs) Implements ICompletionPresenter.FiltersChanged
    Public Event CompletionItemSelected As EventHandler(Of CompletionItemSelectedEventArgs) Implements ICompletionPresenter.CompletionItemSelected
    Public Event CommitRequested As EventHandler(Of CompletionItemEventArgs) Implements ICompletionPresenter.CommitRequested
    Public Event CompletionClosed As EventHandler(Of CompletionClosedEventArgs) Implements ICompletionPresenter.CompletionClosed

    Public Sub Open(session As IAsyncCompletionSession, presentation As CompletionPresentationViewModel) Implements ICompletionPresenter.Open
        Throw New NotImplementedException()
    End Sub

    Public Sub Update(session As IAsyncCompletionSession, presentation As CompletionPresentationViewModel) Implements ICompletionPresenter.Update
        Throw New NotImplementedException()
    End Sub

    Public Sub Close() Implements ICompletionPresenter.Close
        Throw New NotImplementedException()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Throw New NotImplementedException()
    End Sub
End Class
