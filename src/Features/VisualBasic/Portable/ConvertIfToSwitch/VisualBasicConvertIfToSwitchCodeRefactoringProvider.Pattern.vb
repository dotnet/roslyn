' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 9.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertIfToSwitch
    Partial Class VisualBasicConvertIfToSwitchCodeRefactoringProvider
        Private MustInherit Class Pattern
            Implements IPattern(Of CaseClauseSyntax)
            Friend NotInheritable Class Comparison
                Inherits Pattern

                Private ReadOnly _constant As ExpressionSyntax
                Private ReadOnly _inverted As Boolean
                Private ReadOnly _operatorTokenKind As SyntaxKind

                Private Shared ReadOnly s_comparisonTokenMap As Dictionary(Of SyntaxKind, (caseClause As SyntaxKind, inverse As SyntaxKind)) =
                    New Dictionary(Of SyntaxKind, (SyntaxKind, SyntaxKind))(SyntaxFacts.EqualityComparer) From
                    {
                        {SyntaxKind.LessThanToken, (SyntaxKind.CaseLessThanClause, SyntaxKind.GreaterThanToken)},
                        {SyntaxKind.GreaterThanToken, (SyntaxKind.CaseGreaterThanClause, SyntaxKind.LessThanToken)},
                        {SyntaxKind.LessThanEqualsToken, (SyntaxKind.CaseLessThanOrEqualClause, SyntaxKind.GreaterThanEqualsToken)},
                        {SyntaxKind.GreaterThanEqualsToken, (SyntaxKind.CaseGreaterThanOrEqualClause, SyntaxKind.LessThanEqualsToken)},
                        {SyntaxKind.LessThanGreaterThanToken, (SyntaxKind.CaseNotEqualsClause, SyntaxKind.LessThanGreaterThanToken)}
                    }

                Friend Sub New(constant As ExpressionSyntax, inverted As Boolean, operatorTokenKind As SyntaxKind)
                    _constant = constant
                    _inverted = inverted
                    _operatorTokenKind = operatorTokenKind
                End Sub

                Protected Overrides Function CreateSwitchLabelWorker() As CaseClauseSyntax
                    Dim comparisonToken = If(_inverted, s_comparisonTokenMap(_operatorTokenKind).inverse, _operatorTokenKind)
                    Return SyntaxFactory.RelationalCaseClause(
                        s_comparisonTokenMap(comparisonToken).caseClause,
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

                Protected Overrides Function CreateSwitchLabelWorker() As CaseClauseSyntax
                    Return SyntaxFactory.SimpleCaseClause(_constant)
                End Function
            End Class

            Friend NotInheritable Class Range
                Inherits Pattern

                Private ReadOnly _rangeBounds As (lower As ExpressionSyntax, upper As ExpressionSyntax)

                Friend Sub New(rangeBounds As (ExpressionSyntax, ExpressionSyntax))
                    _rangeBounds = rangeBounds
                End Sub

                Protected Overrides Function CreateSwitchLabelWorker() As CaseClauseSyntax
                    Return SyntaxFactory.RangeCaseClause(_rangeBounds.lower, _rangeBounds.upper)
                End Function
            End Class

            Public Function CreateSwitchLabel() As CaseClauseSyntax Implements IPattern(Of CaseClauseSyntax).CreateSwitchLabel
                Return CreateSwitchLabelWorker().WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker)
            End Function

            Protected MustOverride Function CreateSwitchLabelWorker() As CaseClauseSyntax

        End Class
    End Class
End Namespace
