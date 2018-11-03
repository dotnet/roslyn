' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    '<Export>
    '<Export(GetType(ICompletionPresenterProvider))>
    '<Name(NameOf(MockCompletionPresenterProvider))>
    '<ContentType(ContentTypeNames.RoslynContentType)>
    '<Order(Before:=NameOf(PredefinedCompletionNames.DefaultCompletionPresenter))>
    Public Class MockCompletionPresenterProvider
        Implements ICompletionPresenterProvider

        Public Shared CompletionPresenter As MockCompletionPresenter

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public ReadOnly Property Options As CompletionPresenterOptions Implements ICompletionPresenterProvider.Options
            Get
                Return New CompletionPresenterOptions(resultsPerPage:=1)
            End Get
        End Property

        Public Function GetOrCreate(textView As ITextView) As ICompletionPresenter Implements ICompletionPresenterProvider.GetOrCreate
            CompletionPresenter = New MockCompletionPresenter(textView)
            Return CompletionPresenter
        End Function
    End Class
End Namespace
