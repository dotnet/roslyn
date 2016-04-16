' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.OverloadBase
    Partial Friend Class OverloadBaseCodeFixProvider
        Private Class AddOverloadsKeywordAction
            Inherits CodeAction

            Private ReadOnly _document As Document
            Private ReadOnly _node As SyntaxNode

            Public Overrides ReadOnly Property Title As String
                Get
                    Return VBFeaturesResources.AddOverloadsKeyword
                End Get
            End Property

            Public Overrides ReadOnly Property EquivalenceKey As String
                Get
                    Return VBFeaturesResources.AddOverloadsKeyword
                End Get
            End Property

            Public Sub New(document As Document, node As SyntaxNode)
                _document = document
                _node = node
            End Sub

            Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
                Dim root = Await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

                Dim newNode = Await GetNewNodeAsync(_document, _node, cancellationToken).ConfigureAwait(False)
                Dim newRoot = root.ReplaceNode(_node, newNode)

                Return _document.WithSyntaxRoot(newRoot)
            End Function

            Private Async Function GetNewNodeAsync(document As Document, node As SyntaxNode, cancellationToken As CancellationToken) As Task(Of SyntaxNode)
                Dim newNode As SyntaxNode = Nothing

                Dim propertyStatement = TryCast(node, PropertyStatementSyntax)
                If propertyStatement IsNot Nothing Then
                    newNode = propertyStatement.AddModifiers(SyntaxFactory.Token(SyntaxKind.OverloadsKeyword))
                End If

                Dim methodStatement = TryCast(node, MethodStatementSyntax)
                If methodStatement IsNot Nothing Then
                    newNode = methodStatement.AddModifiers(SyntaxFactory.Token(SyntaxKind.OverloadsKeyword))
                End If

                'Make sure we preserve any trivia from the original node
                newNode = newNode.WithTriviaFrom(node)

                'We need to perform a cleanup on the node because AddModifiers doesn't adhere to the VB modifier ordering rules
                Dim cleanupService = document.GetLanguageService(Of ICodeCleanerService)

                If cleanupService IsNot Nothing AndAlso newNode IsNot Nothing Then
                    newNode = Await cleanupService.CleanupAsync(newNode, {newNode.Span}, document.Project.Solution.Workspace, cleanupService.GetDefaultProviders(), cancellationToken).ConfigureAwait(False)
                End If

                Return newNode.WithAdditionalAnnotations(Formatter.Annotation)
            End Function

        End Class
    End Class
End Namespace
