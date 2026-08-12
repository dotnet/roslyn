' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic

<ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:="RemoveByValVB"), [Shared]>
Class RemoveByValCodeRefactoringProvider
    Inherits CodeRefactoringProvider

    Public NotOverridable Overrides Async Function ComputeRefactoringsAsync(context As CodeRefactoringContext) As Task
        Dim document = context.Document
        Dim textSpan = context.Span
        Dim cancellationToken = context.CancellationToken

        Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
        Dim token = root.FindToken(textSpan.Start)

        If token.Kind = SyntaxKind.ByValKeyword AndAlso token.Span.IntersectsWith(textSpan.Start) Then
            context.RegisterRefactoring(New RemoveByValCodeAction("Remove unnecessary ByVal keyword",
                                                                  Function(c) RemoveOccuranceAsync(document, token, c)))
            context.RegisterRefactoring(New RemoveByValCodeAction("Remove all occurrences",
                                                                  Function(c) RemoveAllOccurancesAsync(document, c)))
        End If
    End Function

    Private Async Function RemoveOccuranceAsync(document As Document, token As SyntaxToken, cancellationToken As CancellationToken) As Task(Of Document)
        Dim oldRoot = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
        Dim rewriter = New Rewriter(Function(t) t = token)
        Dim newRoot = rewriter.Visit(oldRoot)
        Return document.WithSyntaxRoot(newRoot)
    End Function

    Private Async Function RemoveAllOccurancesAsync(document As Document, cancellationToken As CancellationToken) As Task(Of Document)
        Dim oldRoot = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
        Dim rewriter = New Rewriter(Function(current) True)
        Dim newRoot = rewriter.Visit(oldRoot)
        Return document.WithSyntaxRoot(newRoot)
    End Function

    Class Rewriter
        Inherits VisualBasicSyntaxRewriter

        Private ReadOnly _predicate As Func(Of SyntaxToken, Boolean)

        Public Sub New(predicate As Func(Of SyntaxToken, Boolean))
            _predicate = predicate
        End Sub

        Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
            If token.Kind = SyntaxKind.ByValKeyword AndAlso _predicate(token) Then
                Return SyntaxFactory.Token(token.LeadingTrivia, SyntaxKind.ByValKeyword, Nothing, String.Empty)
            End If

            Return token
        End Function
    End Class

    Class RemoveByValCodeAction
        Inherits CodeAction

        Private ReadOnly createChangedDocument As Func(Of Object, Task(Of Document))
        Private ReadOnly _title As String

        Public Sub New(title As String, createChangedDocument As Func(Of Object, Task(Of Document)))
            _title = title
            Me.createChangedDocument = createChangedDocument
        End Sub

        Public Overrides ReadOnly Property Title As String
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Protected Overrides Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
            Return createChangedDocument(cancellationToken)
        End Function
    End Class
End Class
