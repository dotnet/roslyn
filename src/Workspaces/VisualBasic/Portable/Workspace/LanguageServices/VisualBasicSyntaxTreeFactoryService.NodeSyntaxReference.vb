' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class VisualBasicSyntaxTreeFactoryService
        Private NotInheritable Class NodeSyntaxReference
            Inherits SyntaxReference
            Private ReadOnly _node As SyntaxNode

            Friend Sub New(node As SyntaxNode)
                _node = node
            End Sub

            Public Overrides ReadOnly Property SyntaxTree As SyntaxTree
                Get
                    Return _node.SyntaxTree
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
    End Class
End Namespace
