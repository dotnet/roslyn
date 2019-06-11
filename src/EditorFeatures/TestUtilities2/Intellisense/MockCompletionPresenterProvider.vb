' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    <Export(GetType(ICompletionPresenterProvider))>
    <PartNotDiscoverable>
    <Name(NameOf(MockCompletionPresenterProvider))>
    <ContentType(ContentTypeNames.RoslynContentType)>
    <Order(Before:=NameOf(PredefinedCompletionNames.DefaultCompletionPresenter))>
    Public Class MockCompletionPresenterProvider
        Implements ICompletionPresenterProvider

        Private _presenters As Dictionary(Of ITextView, ICompletionPresenter) = New Dictionary(Of ITextView, ICompletionPresenter)()

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public ReadOnly Property Options As CompletionPresenterOptions Implements ICompletionPresenterProvider.Options
            Get
                ' resultsPerPage can be set for any reasonable value corresponding to the number of lines in popup.
                ' It is used in some tests involving Up/Down keystrokes.
                Return New CompletionPresenterOptions(resultsPerPage:=10)
            End Get
        End Property

        Public Function GetOrCreate(textView As ITextView) As ICompletionPresenter Implements ICompletionPresenterProvider.GetOrCreate
            If Not _presenters.ContainsKey(textView) Then
                _presenters(textView) = New MockCompletionPresenter(textView)
            End If
            Return _presenters(textView)
        End Function
    End Class
End Namespace
