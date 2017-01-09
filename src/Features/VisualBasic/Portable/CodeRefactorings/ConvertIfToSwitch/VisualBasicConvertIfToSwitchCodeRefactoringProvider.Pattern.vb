' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 9.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ConvertIfToSwitch
    Partial Class VisualBasicConvertIfToSwitchCodeRefactoringProvider
        Private MustInherit Class Pattern
            Implements IPattern
            Friend NotInheritable Class Comparison
                Inherits Pattern

                Private ReadOnly _constant As ExpressionSyntax
                Private ReadOnly _inverted As Boolean
                Private ReadOnly _operatorTokenKind As SyntaxKind

                Private Shared ReadOnly s_comparisonInversesMap As Dictionary(Of SyntaxKind, SyntaxKind) =
                    New Dictionary(Of SyntaxKind, SyntaxKind)(SyntaxFacts.EqualityComparer) From
                    {
                        {SyntaxKind.GreaterThanEqualsToken, SyntaxKind.LessThanEqualsToken},
                        {SyntaxKind.LessThanEqualsToken, SyntaxKind.GreaterThanEqualsToken},
                        {SyntaxKind.GreaterThanToken, SyntaxKind.LessThanToken},
                        {SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken}
                    }

                Private Shared ReadOnly s_caseClausesMap As Dictionary(Of SyntaxKind, SyntaxKind) =
                    New Dictionary(Of SyntaxKind, SyntaxKind)(SyntaxFacts.EqualityComparer) From
                    {
                        {SyntaxKind.GreaterThanEqualsToken, SyntaxKind.CaseGreaterThanOrEqualClause},
                        {SyntaxKind.LessThanEqualsToken, SyntaxKind.CaseLessThanOrEqualClause},
                        {SyntaxKind.GreaterThanToken, SyntaxKind.CaseGreaterThanClause},
                        {SyntaxKind.LessThanToken, SyntaxKind.CaseLessThanClause}
                    }

                Friend Sub New(constant As ExpressionSyntax, inverted As Boolean, operatorTokenKind As SyntaxKind)
                    _constant = constant
                    _inverted = inverted
                    _operatorTokenKind = operatorTokenKind
                End Sub

                Public Overrides Function CreateSwitchLabel() As SyntaxNode
                    Dim comparisonToken = If(_inverted, s_comparisonInversesMap(_operatorTokenKind), _operatorTokenKind)
                    Return SyntaxFactory.RelationalCaseClause(
                        s_caseClausesMap(comparisonToken),
                        SyntaxFactory.Token(SyntaxKind.IsKeyword),
                        SyntaxFactory.Token(comparisonToken), _constant)
                End Function
            End Class

            Friend NotInheritable Class Constant
                Inherits Pattern

                Private ReadOnly _constant As ExpressionSyntax

                Friend Sub New(constant As ExpressionSyntax)
                    _constant = constant
                End Sub

                Public Overrides Function CreateSwitchLabel() As SyntaxNode
                    Return SyntaxFactory.SimpleCaseClause(_constant)
                End Function
            End Class

            Friend NotInheritable Class Range
                Inherits Pattern

                Private ReadOnly _rangeBounds As (lower As ExpressionSyntax, upper As ExpressionSyntax)

                Friend Sub New(rangeBounds As (ExpressionSyntax, ExpressionSyntax))
                    _rangeBounds = rangeBounds
                End Sub

                Public Overrides Function CreateSwitchLabel() As SyntaxNode
                    Return SyntaxFactory.RangeCaseClause(_rangeBounds.lower, _rangeBounds.upper)
                End Function
            End Class

            Public MustOverride Function CreateSwitchLabel() As SyntaxNode Implements IPattern.CreateSwitchLabel
        End Class
    End Class
End Namespace