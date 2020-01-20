' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 9.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertIfToSwitch
    Partial Friend NotInheritable Class VisualBasicConvertIfToSwitchCodeRefactoringProvider
        Private Shared ReadOnly s_operatorMap As Dictionary(Of BinaryOperatorKind, (CaseClauseKind As SyntaxKind, OperatorTokenKind As SyntaxKind)) =
            New Dictionary(Of BinaryOperatorKind, (SyntaxKind, SyntaxKind))() From
            {
                {BinaryOperatorKind.NotEquals, (SyntaxKind.CaseNotEqualsClause, SyntaxKind.LessThanGreaterThanToken)},
                {BinaryOperatorKind.LessThan, (SyntaxKind.CaseLessThanClause, SyntaxKind.LessThanToken)},
                {BinaryOperatorKind.GreaterThan, (SyntaxKind.CaseGreaterThanClause, SyntaxKind.GreaterThanToken)},
                {BinaryOperatorKind.LessThanOrEqual, (SyntaxKind.CaseLessThanOrEqualClause, SyntaxKind.LessThanEqualsToken)},
                {BinaryOperatorKind.GreaterThanOrEqual, (SyntaxKind.CaseGreaterThanOrEqualClause, SyntaxKind.GreaterThanEqualsToken)}
            }

        Public Overrides Function CreateSwitchExpressionStatement(target As SyntaxNode, sections As ImmutableArray(Of AnalyzedSwitchSection)) As SyntaxNode
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides Function CreateSwitchStatement(ifStatement As ExecutableStatementSyntax, expression As SyntaxNode, sectionList As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return VisualBasicSyntaxGenerator.Instance.SwitchStatement(expression, sectionList)
        End Function

        Public Overrides Function AsSwitchSectionStatements(operation As IOperation) As IEnumerable(Of SyntaxNode)
            Dim node = operation.Syntax
            Return If(node.IsStatementContainerNode(), node.GetStatements(), SpecializedCollections.SingletonEnumerable(node))
        End Function

        Public Overrides Function AsSwitchLabelSyntax(label As AnalyzedSwitchLabel) As SyntaxNode
            Debug.Assert(label.Guards.IsDefaultOrEmpty)
            Return AsCaseClauseSyntax(label.Pattern).WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker)
        End Function

        Private Shared Function AsCaseClauseSyntax(pattern As AnalyzedPattern) As CaseClauseSyntax
            Return pattern.TypeSwitch(
                Function(p As AnalyzedPattern.Constant) SyntaxFactory.SimpleCaseClause(p.ExpressionSyntax),
                Function(p As AnalyzedPattern.Range) SyntaxFactory.RangeCaseClause(p.LowerBound, p.HigherBound),
                Function(p As AnalyzedPattern.Relational)
                    Dim relationalOperator = s_operatorMap(p.OperatorKind)
                    Return SyntaxFactory.RelationalCaseClause(
                        relationalOperator.CaseClauseKind,
                        SyntaxFactory.Token(SyntaxKind.IsKeyword),
                        SyntaxFactory.Token(relationalOperator.OperatorTokenKind),
                        p.Value)
                End Function,
                Function(p) As CaseClauseSyntax
                    Throw ExceptionUtilities.UnexpectedValue(p)
                End Function)
        End Function
    End Class
End Namespace

