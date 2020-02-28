' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Class VisualBasicSyntaxFactsService
        Inherits VisualBasicSyntaxFacts
        Implements ISyntaxFactsService

        Public Shared Shadows ReadOnly Property Instance As New VisualBasicSyntaxFactsService

        Private Sub New()
        End Sub

        Public Function ToIdentifierToken(name As String) As SyntaxToken Implements ISyntaxFactsService.ToIdentifierToken
            Return name.ToIdentifierToken()
        End Function

        Public Function Parenthesize(expression As SyntaxNode, Optional includeElasticTrivia As Boolean = True, Optional addSimplifierAnnotation As Boolean = True) As SyntaxNode Implements ISyntaxFactsService.Parenthesize
            Return DirectCast(expression, ExpressionSyntax).Parenthesize(addSimplifierAnnotation)
        End Function

        Public Sub AddFirstMissingCloseBrace(Of TContextNode As SyntaxNode)(
                root As SyntaxNode, contextNode As TContextNode,
                ByRef newRoot As SyntaxNode, ByRef newContextNode As TContextNode) Implements ISyntaxFactsService.AddFirstMissingCloseBrace
            ' Nothing to be done.  VB doesn't have close braces
            newRoot = root
            newContextNode = contextNode
        End Sub
    End Class
End Namespace
