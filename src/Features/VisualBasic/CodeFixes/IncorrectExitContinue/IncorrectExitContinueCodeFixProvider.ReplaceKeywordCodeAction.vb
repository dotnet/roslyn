' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue
    Partial Friend Class IncorrectExitContinueCodeFixProvider
        Private Class ReplaceKeywordCodeAction
            Inherits CodeAction

            Private ReadOnly invalidToken As SyntaxToken
            Private ReadOnly node As SyntaxNode
            Private ReadOnly document As Document
            Private ReadOnly updateNode As Func(Of SyntaxNode, SyntaxNode, SyntaxKind, Document, CancellationToken, StatementSyntax)
            Private ReadOnly containingBlock As SyntaxNode
            Private ReadOnly createBlockKind As SyntaxKind

            Sub New(createBlockKind As SyntaxKind, invalidToken As SyntaxToken, syntax As SyntaxNode, containingBlock As SyntaxNode, document As Document,
                updateNode As Func(Of SyntaxNode, SyntaxNode, SyntaxKind, Document, CancellationToken, StatementSyntax))
                Me.createBlockKind = createBlockKind
                Me.invalidToken = invalidToken
                Me.node = syntax
                Me.document = document
                Me.updateNode = updateNode
                Me.containingBlock = containingBlock
            End Sub

            Public Overrides ReadOnly Property Title As String
                Get
                    Return String.Format(VBFeaturesResources.ChangeTo, invalidToken.ValueText, SyntaxFacts.GetText(BlockKindToKeywordKind(createBlockKind)))
                End Get
            End Property

            Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
                Dim updatedNode = updateNode(node, containingBlock, createBlockKind, document, cancellationToken)
                Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
                Dim updatedRoot = root.ReplaceNode(node, updatedNode)
                Return document.WithSyntaxRoot(updatedRoot)
            End Function
        End Class
    End Class
End Namespace
