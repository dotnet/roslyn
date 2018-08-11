' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertIf), [Shared]>
    Friend NotInheritable Class VisualBasicInvertSingleLineIfCodeRefactoringProvider
        Inherits VisualBasicInvertIfCodeRefactoringProvider(Of SingleLineIfStatementSyntax)

        Protected Overrides Function GetHeaderSpan(ifNode As SingleLineIfStatementSyntax) As TextSpan
            Return TextSpan.FromBounds(
                    ifNode.IfKeyword.SpanStart,
                    ifNode.Condition.Span.End)
        End Function

        Protected Overrides Function IsElseless(ifNode As SingleLineIfStatementSyntax) As Boolean
            Return ifNode.ElseClause Is Nothing
        End Function

        Protected Overrides Function CanInvert(ifNode As SingleLineIfStatementSyntax) As Boolean
            Return TypeOf ifNode.Parent IsNot SingleLineLambdaExpressionSyntax AndAlso
                Not ifNode.Statements.Any(Function(n) n.IsKind(SyntaxKind.LocalDeclarationStatement)) AndAlso
                Not If(ifNode.ElseClause?.Statements.Any(Function(n) n.IsKind(SyntaxKind.LocalDeclarationStatement)), False)
        End Function

        Protected Overrides Function GetCondition(ifNode As SingleLineIfStatementSyntax) As SyntaxNode
            Return ifNode.Condition
        End Function

        Protected Overrides Function GetIfBody(ifNode As SingleLineIfStatementSyntax) As SyntaxList(Of StatementSyntax)
            Return ifNode.Statements
        End Function

        Protected Overrides Function GetElseBody(ifNode As SingleLineIfStatementSyntax) As SyntaxList(Of StatementSyntax)
            Return ifNode.ElseClause.Statements
        End Function

        Protected Overrides Function UpdateIf(
                sourceText As SourceText,
                ifNode As SingleLineIfStatementSyntax,
                condition As SyntaxNode,
                Optional trueStatement As SyntaxList(Of StatementSyntax) = Nothing,
                Optional falseStatement As SyntaxList(Of StatementSyntax) = Nothing) As SyntaxNode
            Dim updatedIf = ifNode.WithCondition(DirectCast(condition, ExpressionSyntax))

            If Not trueStatement.IsEmpty Then
                updatedIf = updatedIf.WithStatements(trueStatement)
            End If

            If Not falseStatement.IsEmpty Then
                updatedIf = updatedIf.WithElseClause(SyntaxFactory.SingleLineElseClause(falseStatement))
            End If

            Return updatedIf
        End Function
    End Class
End Namespace

