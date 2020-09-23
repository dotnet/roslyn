' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Friend Class StructuredTriviaFormattingRule
        Inherits BaseFormattingRule
        Friend Const Name As String = "VisualBasic Structured Trivia Formatting Rule"

        Public Sub New()
        End Sub

        Public Overrides Function GetAdjustNewLinesOperationSlow(ByRef previousToken As SyntaxToken, ByRef currentToken As SyntaxToken, ByRef nextOperation As NextGetAdjustNewLinesOperation) As AdjustNewLinesOperation
            If UnderStructuredTrivia(previousToken, currentToken) Then
                Return Nothing
            End If

            Return nextOperation.Invoke(previousToken, currentToken)
        End Function

        Public Overrides Function GetAdjustSpacesOperationSlow(ByRef previousToken As SyntaxToken, ByRef currentToken As SyntaxToken, ByRef nextOperation As NextGetAdjustSpacesOperation) As AdjustSpacesOperation
            If UnderStructuredTrivia(previousToken, currentToken) Then
                If previousToken.Kind = SyntaxKind.HashToken AndAlso SyntaxFacts.IsPreprocessorKeyword(CType(currentToken.Kind, SyntaxKind)) Then
                    Return CreateAdjustSpacesOperation(space:=0, option:=AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                End If
            End If

            Return nextOperation.Invoke(previousToken, currentToken)
        End Function

        Private Shared Function UnderStructuredTrivia(previousToken As SyntaxToken, currentToken As SyntaxToken) As Boolean
            ' this actually doesn't check all cases but the cases where we care
            ' since checking all cases would be expansive
            If TypeOf previousToken.Parent Is StructuredTriviaSyntax OrElse TypeOf currentToken.Parent Is StructuredTriviaSyntax Then
                Return True
            End If

            If TypeOf previousToken.Parent Is DirectiveTriviaSyntax OrElse TypeOf currentToken.Parent Is DirectiveTriviaSyntax Then
                Return True
            End If

            Return False
        End Function
    End Class
End Namespace
