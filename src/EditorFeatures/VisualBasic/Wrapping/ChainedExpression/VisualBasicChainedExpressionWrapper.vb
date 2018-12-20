' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Wrapping.ChainedExpression
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Wrapping.ChainedExpression
    Friend Class VisualBasicChainedExpressionWrapper
        Inherits AbstractChainedExpressionWrapper(Of NameSyntax, ArgumentListSyntax)

        Public Sub New()
            MyBase.New(VisualBasicSyntaxFactsService.Instance, SyntaxKind.DotToken, SyntaxKind.QuestionToken)
        End Sub

        Public Overrides Function GetNewLineBeforeOperatorTrivia(newLine As SyntaxTriviaList) As SyntaxTriviaList
            Return newLine.InsertRange(0, {SyntaxFactory.WhitespaceTrivia(" "), SyntaxFactory.LineContinuationTrivia("_")})
        End Function
    End Class
End Namespace
