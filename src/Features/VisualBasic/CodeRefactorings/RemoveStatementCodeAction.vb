' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeActions
    Friend Class RemoveStatementCodeAction
        Inherits CodeAction

        Private ReadOnly document As Document
        Private ReadOnly node As SyntaxNode
        Private ReadOnly cancellationToken As CancellationToken
        Private ReadOnly _title As LocalizableString

        Sub New(document As Document, node As SyntaxNode, title As LocalizableString)
            Me.document = document
            Me.node = node
            _title = title
        End Sub

        Public Overrides ReadOnly Property Title As LocalizableString
            Get
                Return _title
            End Get
        End Property

        Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim updatedRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepUnbalancedDirectives)
            Return document.WithSyntaxRoot(updatedRoot)
        End Function
    End Class
End Namespace
