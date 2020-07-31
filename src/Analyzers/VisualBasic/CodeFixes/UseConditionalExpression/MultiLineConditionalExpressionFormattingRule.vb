' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.UseConditionalExpression
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseConditionalExpression
    ''' <summary>
    ''' Special formatting rule that will convert a conditional expression into the following form
    ''' if it has the <see cref="UseConditionalExpressionCodeFixHelpers.SpecializedFormattingAnnotation"/>
    ''' on it:
    '''
    ''' <code>
    '''     Dim v = If(expr,
    '''         whenTrue,
    '''         whenFalse)
    ''' </code>
    '''
    ''' i.e. both branches will be on a newline, indented once from the parent indentation.
    ''' </summary>
    Friend Class MultiLineConditionalExpressionFormattingRule
        Inherits CompatAbstractFormattingRule

        Public Shared ReadOnly Instance As New MultiLineConditionalExpressionFormattingRule()

        Private Sub New()
        End Sub

        Private Shared Function IsCommaOfNewConditional(token As SyntaxToken) As Boolean
            If token.Kind() = SyntaxKind.CommaToken Then
                Return token.Parent.HasAnnotation(
                        UseConditionalExpressionCodeFixHelpers.SpecializedFormattingAnnotation)
            End If

            Return False
        End Function

        Public Overrides Function GetAdjustNewLinesOperationSlow(
                ByRef previousToken As SyntaxToken, ByRef currentToken As SyntaxToken, ByRef nextOperation As NextGetAdjustNewLinesOperation) As AdjustNewLinesOperation
            If IsCommaOfNewConditional(previousToken) Then
                ' We want to force the expressions after the commas to be put on the 
                ' next line.
                Return FormattingOperations.CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLines)
            End If

            Return nextOperation.Invoke(previousToken, currentToken)
        End Function

        Public Overrides Sub AddIndentBlockOperationsSlow(
                list As List(Of IndentBlockOperation), node As SyntaxNode, ByRef nextOperation As NextIndentBlockOperationAction)

            If node.HasAnnotation(UseConditionalExpressionCodeFixHelpers.SpecializedFormattingAnnotation) AndAlso
               TypeOf node Is TernaryConditionalExpressionSyntax Then

                Dim conditional = TryCast(node, TernaryConditionalExpressionSyntax)
                Dim statement = conditional.FirstAncestorOrSelf(Of StatementSyntax)()
                If statement IsNot Nothing Then
                    Dim baseToken = statement.GetFirstToken()

                    ' we want to indent the true and false conditions in one level from the
                    ' containing statement.
                    list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
                            baseToken, conditional.WhenTrue.GetFirstToken(), conditional.WhenTrue.GetLastToken(),
                            indentationDelta:=1, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine))
                    list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
                            baseToken, conditional.WhenFalse.GetFirstToken(), conditional.WhenFalse.GetLastToken(),
                            indentationDelta:=1, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine))
                    Return
                End If
            End If

            nextOperation.Invoke()
        End Sub
    End Class

End Namespace
