' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    ' this is a basic implementation of a syntax reference
    Friend Class SimpleSyntaxReference
        Inherits SyntaxReference

        Private ReadOnly _tree As SyntaxTree
        Private ReadOnly _node As SyntaxNode

        Friend Sub New(tree As SyntaxTree, node As SyntaxNode)
            _tree = tree
            _node = node
        End Sub

        Public Overrides ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return _tree
            End Get
        End Property

        Public Overrides ReadOnly Property Span As TextSpan
            Get
                Return _node.Span
            End Get
        End Property

        Public Overrides Function GetSyntax(Optional cancellationToken As CancellationToken = Nothing) As SyntaxNode
            Return _node
        End Function
    End Class
End Namespace
