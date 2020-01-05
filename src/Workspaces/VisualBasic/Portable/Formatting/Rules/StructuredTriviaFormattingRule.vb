' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Friend Class StructuredTriviaFormattingRule
        Inherits BaseFormattingRule
        Friend Const Name As String = "VisualBasic Structured Trivia Formatting Rule"

        Public Sub New()
        End Sub

        Public Overrides Function GetAdjustNewLinesOperationSlow(previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, ByRef nextOperation As NextGetAdjustNewLinesOperation) As AdjustNewLinesOperation
            If UnderStructuredTrivia(previousToken, currentToken) Then
                Return Nothing
            End If

            Return nextOperation.Invoke()
        End Function


        Public Overrides Function GetAdjustSpacesOperationSlow(previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, ByRef nextOperation As NextGetAdjustSpacesOperation) As AdjustSpacesOperation
            If UnderStructuredTrivia(previousToken, currentToken) Then
                If previousToken.Kind = SyntaxKind.HashToken AndAlso SyntaxFacts.IsPreprocessorKeyword(CType(currentToken.Kind, SyntaxKind)) Then
                    Return CreateAdjustSpacesOperation(space:=0, option:=AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                End If
            End If

            Return nextOperation.Invoke()
        End Function

        Private Function UnderStructuredTrivia(previousToken As SyntaxToken, currentToken As SyntaxToken) As Boolean
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
