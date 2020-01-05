' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.CorrectNextControlVariable
    Partial Friend Class CorrectNextControlVariableCodeFixProvider
        Private Class CorrectNextControlVariableCodeAction
            Inherits CodeAction

            Private ReadOnly _document As Document
            Private ReadOnly _newNode As SyntaxNode
            Private ReadOnly _node As SyntaxNode

            Public Sub New(document As Document, node As SyntaxNode, newNode As SyntaxNode)
                Me._document = document
                Me._newNode = newNode
                Me._node = node
            End Sub

            Public Overrides ReadOnly Property Title As String
                Get
                    Return VBFeaturesResources.Use_the_correct_control_variable
                End Get
            End Property

            Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
                Dim root = Await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
                Dim updatedRoot = root.ReplaceNode(_node, _newNode)
                Return _document.WithSyntaxRoot(updatedRoot)
            End Function
        End Class
    End Class
End Namespace
