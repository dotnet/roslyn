' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 9.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ConvertIfToSwitch
    Partial Class VisualBasicConvertIfToSwitchCodeRefactoringProvider
        Friend MustInherit Class Pattern
            Friend NotInheritable Class Comparison
                Inherits Pattern

                Private ReadOnly _constant As ExpressionSyntax
                Private ReadOnly _flipOperator As Boolean
                Private ReadOnly _operatorToken As SyntaxToken

                Friend Sub New(constant As ExpressionSyntax, flipOperator As Boolean, operatorToken As SyntaxToken)
                    _constant = constant
                    _flipOperator = flipOperator
                    _operatorToken = operatorToken
                End Sub

                Friend Overrides Function CreateCaseClause() As CaseClauseSyntax
                    Return SyntaxFactory.RelationalCaseClause(
                        GetRelationalCaseClauseKind(),
                        SyntaxFactory.Token(SyntaxKind.IsKeyword),
                        SyntaxFactory.Token(GetOperatorToken()), _constant)
                End Function

                Private Function GetOperatorToken() As SyntaxKind
                    If Not _flipOperator Then
                        Return _operatorToken.Kind
                    End If

                    Select Case _operatorToken.Kind
                        Case SyntaxKind.GreaterThanEqualsToken
                            Return SyntaxKind.LessThanEqualsToken

                        Case SyntaxKind.GreaterThanToken
                            Return SyntaxKind.LessThanEqualsToken

                        Case SyntaxKind.LessThanEqualsToken
                            Return SyntaxKind.GreaterThanEqualsToken

                        Case SyntaxKind.LessThanToken
                            Return SyntaxKind.GreaterThanToken

                        Case Else
                            Throw New InvalidOperationException()

                    End Select
                End Function

                Private Function GetRelationalCaseClauseKind() As SyntaxKind
                    Select Case GetOperatorToken()
                        Case SyntaxKind.GreaterThanEqualsToken
                            Return SyntaxKind.CaseGreaterThanOrEqualClause

                        Case SyntaxKind.GreaterThanToken
                            Return SyntaxKind.CaseGreaterThanClause

                        Case SyntaxKind.LessThanEqualsToken
                            Return SyntaxKind.CaseLessThanOrEqualClause

                        Case SyntaxKind.LessThanToken
                            Return SyntaxKind.CaseLessThanClause

                        Case Else
                            Throw New InvalidOperationException()

                    End Select
                End Function
            End Class

            Friend NotInheritable Class Constant
                Inherits Pattern

                Private ReadOnly _constant As ExpressionSyntax

                Friend Sub New(constant As ExpressionSyntax)
                    _constant = constant
                End Sub

                Friend Overrides Function CreateCaseClause() As CaseClauseSyntax
                    Return SyntaxFactory.SimpleCaseClause(_constant)
                End Function
            End Class

            Friend NotInheritable Class Range
                Inherits Pattern

                Private ReadOnly _rangeBounds As (lower As ExpressionSyntax, upper As ExpressionSyntax)

                Friend Sub New(rangeBounds As (ExpressionSyntax, ExpressionSyntax))
                    _rangeBounds = rangeBounds
                End Sub

                Friend Overrides Function CreateCaseClause() As CaseClauseSyntax
                    Return SyntaxFactory.RangeCaseClause(_rangeBounds.lower, _rangeBounds.upper)
                End Function
            End Class

            Friend MustOverride Function CreateCaseClause() As CaseClauseSyntax
        End Class
    End Class
End Namespace