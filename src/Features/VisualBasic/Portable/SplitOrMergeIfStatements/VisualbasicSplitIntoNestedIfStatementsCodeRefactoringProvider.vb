' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.SplitOrMergeIfStatements
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.SplitIntoNestedIfStatements), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeRefactoringProviderNames.InvertLogical, Before:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    Friend NotInheritable Class VisualBasicSplitIntoNestedIfStatementsCodeRefactoringProvider
        Inherits AbstractSplitIntoNestedIfStatementsCodeRefactoringProvider(Of ExpressionSyntax)

        Protected Overrides ReadOnly Property IfKeywordText As String = SyntaxFacts.GetText(SyntaxKind.IfKeyword)

        Protected Overrides ReadOnly Property LogicalExpressionSyntaxKind As Integer = SyntaxKind.AndAlsoExpression

        Protected Overrides Function IsConditionOfIfStatement(expression As SyntaxNode, ByRef ifStatement As SyntaxNode) As Boolean
            If TypeOf expression.Parent Is IfStatementSyntax AndAlso
               DirectCast(expression.Parent, IfStatementSyntax).Condition Is expression AndAlso
               TypeOf expression.Parent.Parent Is MultiLineIfBlockSyntax Then
                ifStatement = expression.Parent.Parent
                Return True
            End If

            If TypeOf expression.Parent Is ElseIfStatementSyntax AndAlso
               DirectCast(expression.Parent, ElseIfStatementSyntax).Condition Is expression AndAlso
               TypeOf expression.Parent.Parent Is ElseIfBlockSyntax Then
                ifStatement = expression.Parent.Parent
                Return True
            End If

            ifStatement = Nothing
            Return False
        End Function

        Protected Overrides Function SplitIfStatement(currentIfStatementNode As SyntaxNode,
                                                      condition1 As ExpressionSyntax,
                                                      condition2 As ExpressionSyntax) As SyntaxNode
            Dim innerIfStatement = SyntaxFactory.IfStatement(SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.IfKeyword),
                                                             condition2,
                                                             SyntaxFactory.Token(SyntaxKind.ThenKeyword))
            If TypeOf currentIfStatementNode Is MultiLineIfBlockSyntax Then
                Dim currentIfBlock = DirectCast(currentIfStatementNode, MultiLineIfBlockSyntax)

                Dim innerIfBlock = SyntaxFactory.MultiLineIfBlock(innerIfStatement,
                                                              currentIfBlock.Statements,
                                                              currentIfBlock.ElseIfBlocks,
                                                              currentIfBlock.ElseBlock)
                Dim outerIfBlock = currentIfBlock _
                              .WithIfStatement(currentIfBlock.IfStatement.WithCondition(condition1)) _
                              .WithStatements(SyntaxFactory.SingletonList(Of StatementSyntax)(innerIfBlock))

                Return outerIfBlock
            ElseIf TypeOf currentIfStatementNode Is ElseIfBlockSyntax Then
                Dim currentElseIfBlock = DirectCast(currentIfStatementNode, ElseIfBlockSyntax)
                Dim currentIfBlock = DirectCast(currentElseIfBlock.Parent, MultiLineIfBlockSyntax)
                Dim currentElseIfBlockIndex = currentIfBlock.ElseIfBlocks.IndexOf(currentElseIfBlock)

                Dim innerIfBlock = SyntaxFactory.MultiLineIfBlock(innerIfStatement,
                                                              currentElseIfBlock.Statements,
                                                              currentIfBlock.ElseIfBlocks.RemoveRange(0, currentElseIfBlockIndex + 1),
                                                              currentIfBlock.ElseBlock)
                Dim outerElseIfBlock = currentElseIfBlock _
                              .WithElseIfStatement(currentElseIfBlock.ElseIfStatement.WithCondition(condition1)) _
                              .WithStatements(SyntaxFactory.SingletonList(Of StatementSyntax)(innerIfBlock))

                Return outerElseIfBlock
            End If
            Throw ExceptionUtilities.UnexpectedValue(currentIfStatementNode)
        End Function
    End Class
End Namespace
