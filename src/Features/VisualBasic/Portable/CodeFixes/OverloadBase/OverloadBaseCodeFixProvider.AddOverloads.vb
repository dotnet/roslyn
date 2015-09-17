' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.Formatting

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.OverloadBase
    Partial Friend Class OverloadBaseCodeFixProvider
        Private Class AddOverloadsKeywordAction
            Inherits CodeAction

            Private ReadOnly _document As Document
            Private ReadOnly _node As SyntaxNode
            Private ReadOnly _newNode As SyntaxNode

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

            Public Sub New(document As Document, node As SyntaxNode, newNode As SyntaxNode)
                _document = document
                _node = node
                _newNode = newNode
            End Sub

            Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
                Dim root = Await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

                Dim newRoot = root.ReplaceNode(_node, _newNode)
                Dim newDocument = Await Formatter.FormatAsync(_document.WithSyntaxRoot(newRoot), cancellationToken:=cancellationToken).ConfigureAwait(False)

                Return newDocument
            End Function

        End Class
    End Class
End Namespace
