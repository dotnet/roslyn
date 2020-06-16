' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit
    Friend Class VisualBasicLambdaSyntaxFacts
        Inherits LambdaSyntaxFacts

        Public Shared ReadOnly Instance As LambdaSyntaxFacts = New VisualBasicLambdaSyntaxFacts()

        Private Sub New()
        End Sub

        Public Overrides Function GetLambda(lambdaOrLambdaBodySyntax As SyntaxNode) As SyntaxNode
            Return LambdaUtilities.GetLambda(lambdaOrLambdaBodySyntax)
        End Function

        Public Overrides Function TryGetCorrespondingLambdaBody(previousLambdaSyntax As SyntaxNode, lambdaOrLambdaBodySyntax As SyntaxNode) As SyntaxNode
            Return LambdaUtilities.GetCorrespondingLambdaBody(lambdaOrLambdaBodySyntax, previousLambdaSyntax)
        End Function

        Public Overrides Function GetDeclaratorPosition(node As SyntaxNode) As Integer
            Return node.SpanStart
        End Function
    End Class
End Namespace
