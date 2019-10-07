' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Indentation
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Wrapping.BinaryExpression

Namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping.BinaryExpression
    Friend Class VisualBasicBinaryExpressionWrapper
        Inherits AbstractBinaryExpressionWrapper(Of BinaryExpressionSyntax)

        Public Sub New()
            ' Override default indentation behavior.  The special indentation rule tries to 
            ' align parameters.  But that's what we're actually trying to control, so we need
            ' to remove this.
            MyBase.New(VisualBasicIndentationService.WithoutParameterAlignmentInstance,
                       VisualBasicSyntaxFactsService.Instance,
                       VisualBasicPrecedenceService.Instance)
        End Sub

        Protected Overrides Function GetNewLineBeforeOperatorTrivia(newLine As SyntaxTriviaList) As SyntaxTriviaList
            Return newLine.InsertRange(0, {SyntaxFactory.WhitespaceTrivia(" "), SyntaxFactory.LineContinuationTrivia("_")})
        End Function
    End Class
End Namespace
