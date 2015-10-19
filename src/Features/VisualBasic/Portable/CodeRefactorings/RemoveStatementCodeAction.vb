' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeActions
    Friend Class RemoveStatementCodeAction
        Inherits CodeAction

        Private ReadOnly _document As Document
        Private ReadOnly _node As SyntaxNode
        Private ReadOnly _cancellationToken As CancellationToken
        Private ReadOnly _title As String

        Public Sub New(document As Document, node As SyntaxNode, title As String)
            Me._document = document
            Me._node = node
            _title = title
        End Sub

        Public Overrides ReadOnly Property Title As String
            Get
                Return _title
            End Get
        End Property

        Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = Await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim updatedRoot = root.RemoveNode(_node, SyntaxRemoveOptions.KeepUnbalancedDirectives)
            Return _document.WithSyntaxRoot(updatedRoot)
        End Function
    End Class
End Namespace
