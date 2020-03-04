' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue
    Partial Friend Class IncorrectExitContinueCodeFixProvider
        Private Class ReplaceKeywordCodeAction
            Inherits CodeAction

            Private ReadOnly _invalidToken As SyntaxToken
            Private ReadOnly _node As SyntaxNode
            Private ReadOnly _document As Document
            Private ReadOnly _updateNode As Func(Of SyntaxNode, SyntaxNode, SyntaxKind, Document, CancellationToken, StatementSyntax)
            Private ReadOnly _containingBlock As SyntaxNode
            Private ReadOnly _createBlockKind As SyntaxKind

            Public Sub New(createBlockKind As SyntaxKind, invalidToken As SyntaxToken, syntax As SyntaxNode, containingBlock As SyntaxNode, document As Document,
                updateNode As Func(Of SyntaxNode, SyntaxNode, SyntaxKind, Document, CancellationToken, StatementSyntax))
                Me._createBlockKind = createBlockKind
                Me._invalidToken = invalidToken
                Me._node = syntax
                Me._document = document
                Me._updateNode = updateNode
                Me._containingBlock = containingBlock
            End Sub

            Public Overrides ReadOnly Property Title As String
                Get
                    Return String.Format(FeaturesResources.Change_0_to_1, _invalidToken.ValueText, SyntaxFacts.GetText(BlockKindToKeywordKind(_createBlockKind)))
                End Get
            End Property

            Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
                Dim updatedNode = _updateNode(_node, _containingBlock, _createBlockKind, _document, cancellationToken)
                Dim root = Await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
                Dim updatedRoot = root.ReplaceNode(_node, updatedNode)
                Return _document.WithSyntaxRoot(updatedRoot)
            End Function
        End Class
    End Class
End Namespace
