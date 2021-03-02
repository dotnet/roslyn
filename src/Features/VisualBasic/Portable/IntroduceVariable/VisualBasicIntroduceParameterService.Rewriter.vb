' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable
    Partial Friend Class VisualBasicIntroduceParameterService
        Private Class Rewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _replacementAnnotation As New SyntaxAnnotation
            Private ReadOnly _replacementNode As SyntaxNode
            Private ReadOnly _matches As ISet(Of ExpressionSyntax)

            Private Sub New(replacementNode As SyntaxNode, matches As ISet(Of ExpressionSyntax))
                _replacementNode = replacementNode
                _matches = matches
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                Dim expression = TryCast(node, ExpressionSyntax)
                If expression IsNot Nothing AndAlso _matches.Contains(expression) Then
                    Return _replacementNode.WithTriviaFrom(expression).WithAdditionalAnnotations(_replacementAnnotation)
                End If

                Return MyBase.Visit(node)
            End Function

            Public Overloads Shared Function Visit(node As SyntaxNode, replacementNode As SyntaxNode, matches As ISet(Of ExpressionSyntax)) As SyntaxNode
                Return New Rewriter(replacementNode, matches).Visit(node)
            End Function
        End Class
    End Class
End Namespace
