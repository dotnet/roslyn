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

        Protected Overrides Function GetIfBody(ifNode As SingleLineIfStatementSyntax) As SyntaxList(Of StatementSyntax)?
            Return ifNode.Statements
        End Function

        Protected Overrides Function GetElseBody(ifNode As SingleLineIfStatementSyntax) As SyntaxList(Of StatementSyntax)?
            Return ifNode.ElseClause.Statements
        End Function

        Protected Overrides Function UpdateIf(
                sourceText As SourceText,
                ifNode As SingleLineIfStatementSyntax,
                condition As SyntaxNode,
                Optional trueStatements As SyntaxList(Of StatementSyntax)? = Nothing,
                Optional falseStatements As SyntaxList(Of StatementSyntax)? = Nothing) As SyntaxNode

            Dim isSingleLine = sourceText.AreOnSameLine(ifNode.GetFirstToken(), ifNode.GetLastToken())
            If trueStatements?.Count > 0 AndAlso falseStatements?.Count > 0 AndAlso isSingleLine Then
                ' If statement Is on a single line, And we're swapping the true/false parts.
                ' In that case, try to swap the trailing trivia between the true/false parts.
                ' That way the trailing comments/newlines at the end of the 'if' stay there,
                ' And the spaces after the true-part stay where they are.

                Dim lastTrue = trueStatements.Value.Last()
                Dim lastFalse = falseStatements.Value.Last()

                Dim newLastTrue = lastTrue.WithTrailingTrivia(lastFalse.GetTrailingTrivia())
                Dim newLastFalse = lastFalse.WithTrailingTrivia(lastTrue.GetTrailingTrivia())

                trueStatements = trueStatements.Value.Replace(lastTrue, newLastTrue)
                falseStatements = falseStatements.Value.Replace(lastFalse, newLastFalse)
            End If

            Dim updatedIf = ifNode.WithCondition(DirectCast(condition, ExpressionSyntax))

            If trueStatements?.Count <> 0 Then
                updatedIf = updatedIf.WithStatements(trueStatements.Value)
            End If

            If falseStatements?.Count <> 0 Then
                Dim elseClause =
                    If(updatedIf.ElseClause IsNot Nothing,
                       updatedIf.ElseClause.WithStatements(falseStatements.Value),
                       SyntaxFactory.SingleLineElseClause(falseStatements.Value))

                updatedIf = updatedIf.WithElseClause(elseClause)
            End If

            Return updatedIf
        End Function
    End Class
End Namespace

