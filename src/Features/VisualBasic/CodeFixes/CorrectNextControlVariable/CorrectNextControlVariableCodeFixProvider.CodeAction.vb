' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.CorrectNextControlVariable

    Partial Friend Class CorrectNextControlVariableCodeFixProvider
        Private Class CorrectNextControlVariableCodeAction
            Inherits CodeAction

            Private ReadOnly document As Document
            Private ReadOnly newNode As SyntaxNode
            Private ReadOnly node As SyntaxNode

            Sub New(document As Document, node As SyntaxNode, newNode As SyntaxNode)
                Me.document = document
                Me.newNode = newNode
                Me.node = node
            End Sub

            Public Overrides ReadOnly Property Title As String
                Get
                    Return VBFeaturesResources.CorrectNextControlVariable
                End Get
            End Property

            Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
                Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
                Dim updatedRoot = root.ReplaceNode(node, newNode)
                Return document.WithSyntaxRoot(updatedRoot)
            End Function
        End Class
    End Class
End Namespace
