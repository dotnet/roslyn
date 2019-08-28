' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.UseConditionalExpression
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.UseConditionalExpression
    ''' <summary>
    ''' Special formatting rule that will convert a conditional expression into the following form
    ''' if it has the <see cref="UseConditionalExpressionHelpers.SpecializedFormattingAnnotation"/>
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

        Private Function IsCommaOfNewConditional(token As SyntaxToken) As Boolean
            If token.Kind() = SyntaxKind.CommaToken Then
                Return token.Parent.HasAnnotation(
                        UseConditionalExpressionHelpers.SpecializedFormattingAnnotation)
            End If

            Return False
        End Function

        Public Overrides Function GetAdjustNewLinesOperationSlow(
                previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, ByRef nextOperation As NextGetAdjustNewLinesOperation) As AdjustNewLinesOperation
            If IsCommaOfNewConditional(previousToken) Then
                ' We want to force the expressions after the commas to be put on the 
                ' next line.
                Return FormattingOperations.CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLines)
            End If

            Return nextOperation.Invoke()
        End Function

        Public Overrides Sub AddIndentBlockOperationsSlow(
                list As List(Of IndentBlockOperation), node As SyntaxNode, optionSet As OptionSet, ByRef nextOperation As NextIndentBlockOperationAction)

            If node.HasAnnotation(UseConditionalExpressionHelpers.SpecializedFormattingAnnotation) AndAlso
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
