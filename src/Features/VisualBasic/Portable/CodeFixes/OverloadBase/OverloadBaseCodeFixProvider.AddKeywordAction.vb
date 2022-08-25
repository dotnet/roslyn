' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.OverloadBase
    Partial Friend Class OverloadBaseCodeFixProvider
        Private Class AddKeywordAction
            Inherits CodeAction

            Private ReadOnly _document As Document
            Private ReadOnly _node As SyntaxNode
            Private ReadOnly _title As String
            Private ReadOnly _modifier As SyntaxKind
            Private ReadOnly _fallbackOptions As SyntaxFormattingOptionsProvider

            Public Overrides ReadOnly Property Title As String
                Get
                    Return _title
                End Get
            End Property

            Public Overrides ReadOnly Property EquivalenceKey As String
                Get
                    Return _title
                End Get
            End Property

            Public Sub New(document As Document, node As SyntaxNode, title As String, modifier As SyntaxKind, fallbackOptions As SyntaxFormattingOptionsProvider)
                _document = document
                _node = node
                _title = title
                _modifier = modifier
                _fallbackOptions = fallbackOptions
            End Sub

            Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
                Dim root = Await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
                Dim options = Await _document.GetSyntaxFormattingOptionsAsync(_fallbackOptions, cancellationToken).ConfigureAwait(False)

                Dim newNode = Await GetNewNodeAsync(_document, _node, options, cancellationToken).ConfigureAwait(False)
                Dim newRoot = root.ReplaceNode(_node, newNode)

                Return _document.WithSyntaxRoot(newRoot)
            End Function

            Private Async Function GetNewNodeAsync(document As Document, node As SyntaxNode, options As SyntaxFormattingOptions, cancellationToken As CancellationToken) As Task(Of SyntaxNode)
                Dim newNode As SyntaxNode = Nothing
                Dim trivia As SyntaxTriviaList = node.GetLeadingTrivia()
                node = node.WithoutLeadingTrivia()

                Dim propertyStatement = TryCast(node, PropertyStatementSyntax)
                If propertyStatement IsNot Nothing Then
                    newNode = propertyStatement.AddModifiers(SyntaxFactory.Token(_modifier))
                End If

                Dim methodStatement = TryCast(node, MethodStatementSyntax)
                If methodStatement IsNot Nothing Then
                    newNode = methodStatement.AddModifiers(SyntaxFactory.Token(_modifier))
                End If

                'Make sure we preserve any trivia from the original node
                newNode = newNode.WithLeadingTrivia(trivia)

                'We need to perform a cleanup on the node because AddModifiers doesn't adhere to the VB modifier ordering rules
                Dim cleanupService = document.GetLanguageService(Of ICodeCleanerService)

                If cleanupService IsNot Nothing AndAlso newNode IsNot Nothing Then
                    Dim services = document.Project.Solution.Services
                    newNode = Await cleanupService.CleanupAsync(newNode, ImmutableArray.Create(newNode.Span), options, services, cleanupService.GetDefaultProviders(), cancellationToken).ConfigureAwait(False)
                End If

                Return newNode.WithAdditionalAnnotations(Formatter.Annotation)
            End Function
        End Class
    End Class
End Namespace
