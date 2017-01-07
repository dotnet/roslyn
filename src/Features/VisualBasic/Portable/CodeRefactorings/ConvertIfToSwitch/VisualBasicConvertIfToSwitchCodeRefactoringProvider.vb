' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 9.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.ConvertIfToSwitch
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ConvertIfToSwitch
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicConvertIfToSwitchCodeRefactoringProvider)), [Shared]>
    Partial Friend Class VisualBasicConvertIfToSwitchCodeRefactoringProvider
        Inherits AbstractConvertIfToSwitchCodeRefactoringProvider(Of SyntaxList(Of StatementSyntax), MultiLineIfBlockSyntax, ExpressionSyntax, Pattern)

        Protected Overrides Function AreEquivalentCore(expression As ExpressionSyntax, switchExpression As ExpressionSyntax) As Boolean
            Return SyntaxFactory.AreEquivalent(expression, switchExpression)
        End Function

        Protected Overrides Function CreatePatternFromExpression(operand As ExpressionSyntax, semanticModel As SemanticModel, ByRef switchExpression As ExpressionSyntax) As Pattern
            Select Case operand.Kind
                Case SyntaxKind.EqualsExpression,
                     SyntaxKind.GreaterThanOrEqualExpression,
                     SyntaxKind.GreaterThanExpression,
                     SyntaxKind.LessThanExpression,
                     SyntaxKind.LessThanOrEqualExpression,
                     SyntaxKind.NotEqualsExpression
                    ' Look for the form "x = 5" where x is equivalent to the switch expression.
                    ' This will turn into a simple case clause e.g. "Case 5". For other comparison
                    ' operators, we will use the form "Case Is > 5" et cetera.
                    Dim node = DirectCast(operand, BinaryExpressionSyntax)
                    Dim constant As ExpressionSyntax = Nothing
                    Dim expression As ExpressionSyntax = Nothing

                    If Not TryDetermineConstant(node.Right, node.Left, semanticModel, constant, expression) Then
                        Return Nothing
                    End If

                    If Not AreEquivalent(expression, switchExpression) Then
                        Return Nothing
                    End If

                    If operand.Kind = SyntaxKind.EqualsExpression Then
                        Return New Pattern.Constant(constant)
                    End If

                    ' Flip the operator if the constant is on the left hand side in the original expression.
                    Dim flipOperator = constant Is node.Left
                    Return New Pattern.Comparison(constant, flipOperator, node.OperatorToken)

                Case SyntaxKind.AndAlsoExpression
                    ' Look for the from "x >= 1 AndAlso x <= 9" where x is equivalent to the switch expression.
                    ' This will turn into a range case clause e.g. "Case 1 To 10"
                    Dim node = DirectCast(operand, BinaryExpressionSyntax)
                    Dim left = TryCast(node.Left.WalkDownParentheses, BinaryExpressionSyntax)
                    Dim right = TryCast(node.Right.WalkDownParentheses, BinaryExpressionSyntax)

                    If left Is Nothing OrElse right Is Nothing Then
                        Return Nothing
                    End If

                    If Not IsRangeComparisonOperator(left.Kind) OrElse Not IsRangeComparisonOperator(right.Kind) Then
                        Return Nothing
                    End If

                    Dim leftConstant As ExpressionSyntax = Nothing
                    Dim rightConstant As ExpressionSyntax = Nothing
                    Dim leftExpression As ExpressionSyntax = Nothing
                    Dim rightExpression As ExpressionSyntax = Nothing

                    If Not TryDetermineConstant(left.Right, left.Left, semanticModel, leftConstant, leftExpression) Then
                        Return Nothing
                    End If

                    If Not TryDetermineConstant(right.Right, right.Left, semanticModel, rightConstant, rightExpression) Then
                        Return Nothing
                    End If

                    If Not AreEquivalentCore(leftExpression, rightExpression) Then
                        Return Nothing
                    End If

                    Dim leftHasLowerPosition = HasLowerPosition(leftExpression, left)
                    Dim rightHasLowerPosition = HasLowerPosition(rightExpression, right)
                    If leftHasLowerPosition = rightHasLowerPosition Then
                        Return Nothing
                    End If

                    If Not AreEquivalent(leftExpression, switchExpression) Then
                        Return Nothing
                    End If

                    Dim range = If(leftHasLowerPosition, (rightConstant, leftConstant), (leftConstant, rightConstant))
                    Return New Pattern.Range(range)

                Case Else
                    Return Nothing

            End Select
        End Function

        Private Shared Function HasLowerPosition(expression As ExpressionSyntax, node As BinaryExpressionSyntax) As Boolean
            Return If(node.IsKind(SyntaxKind.LessThanOrEqualExpression), expression Is node.Left, expression Is node.Right)
        End Function

        Private Shared Function IsRangeComparisonOperator(kind As SyntaxKind) As Boolean
            Return kind = SyntaxKind.LessThanOrEqualExpression OrElse kind = SyntaxKind.GreaterThanOrEqualExpression
        End Function

        Protected Overrides Function GetIfKeyword(node As MultiLineIfBlockSyntax) As SyntaxToken
            Return node.IfStatement.IfKeyword
        End Function

        Protected Overrides Iterator Function GetLogicalOrExpressionOperands(node As ExpressionSyntax) As IEnumerable(Of ExpressionSyntax)
            node = node.WalkDownParentheses
            While node.IsKind(SyntaxKind.OrElseExpression)
                Dim binaryExpression = DirectCast(node, BinaryExpressionSyntax)
                Yield binaryExpression.Right.WalkDownParentheses
                node = binaryExpression.Left.WalkDownParentheses
            End While

            Yield node
        End Function

        Protected Overrides Function CanConvertIfToSwitch(ifStatement As MultiLineIfBlockSyntax, semanticModel As SemanticModel) As Boolean
            ' No pre-condition
            Return True
        End Function

        Protected Overrides Iterator Function GetIfElseStatementChain(node As MultiLineIfBlockSyntax) _
            As IEnumerable(Of (SyntaxList(Of StatementSyntax), ExpressionSyntax))
            Yield (node.Statements, node.IfStatement.Condition)
            For Each item In node.ElseIfBlocks
                Yield (item.Statements, item.ElseIfStatement.Condition)
            Next

            Yield ((node.ElseBlock?.Statements).GetValueOrDefault(), Nothing)
        End Function

        Protected Overrides Function CreateSwitchStatement(
            switchDefaultBody As SyntaxList(Of StatementSyntax),
            switchExpression As ExpressionSyntax,
            semanticModel As SemanticModel,
            sections As List(Of (patterns As List(Of Pattern), body As SyntaxList(Of StatementSyntax)))) As SyntaxNode
            Dim blocks = sections.Select(Function(section) SyntaxFactory.CaseBlock(SyntaxFactory.CaseStatement(section.patterns.Select(Function(pattern) pattern.CreateCaseClause()).ToArray()), section.body))
            Dim blockList = SyntaxFactory.List(blocks)

            If Not switchDefaultBody.IsEmpty Then
                blockList = blockList.Add(SyntaxFactory.CaseElseBlock(SyntaxFactory.CaseElseStatement(SyntaxFactory.ElseCaseClause()), switchDefaultBody))
            End If

            Dim selectStatement = SyntaxFactory.SelectStatement(switchExpression)
            Return SyntaxFactory.SelectBlock(selectStatement, blockList)
        End Function
    End Class
End Namespace