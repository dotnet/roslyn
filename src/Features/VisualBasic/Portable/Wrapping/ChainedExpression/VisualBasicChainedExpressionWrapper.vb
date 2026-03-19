' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Indentation
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Wrapping.ChainedExpression

Namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping.ChainedExpression
    Friend Class VisualBasicChainedExpressionWrapper
        Inherits AbstractChainedExpressionWrapper(Of NameSyntax, ArgumentListSyntax)

        Public Sub New()
            MyBase.New(VisualBasicIndentationService.WithoutParameterAlignmentInstance, VisualBasicSyntaxFacts.Instance)
        End Sub

        Protected Overrides Function GetNewLineBeforeOperatorTrivia(newLine As SyntaxTriviaList) As SyntaxTriviaList
            Return newLine.InsertRange(0, {SyntaxFactory.WhitespaceTrivia(" "), SyntaxFactory.LineContinuationTrivia("_")})
        End Function
    End Class
End Namespace
