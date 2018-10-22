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

        Protected Overrides ReadOnly Property LogicalAndSyntaxKind As Integer = SyntaxKind.AndAlsoExpression

        Protected Overrides Function IsConditionOfIfStatement(expression As SyntaxNode, ByRef ifStatementNode As SyntaxNode) As Boolean
            If TypeOf expression.Parent Is IfStatementSyntax AndAlso
               DirectCast(expression.Parent, IfStatementSyntax).Condition Is expression AndAlso
               TypeOf expression.Parent.Parent Is MultiLineIfBlockSyntax Then
                ifStatementNode = expression.Parent.Parent
                Return True
            End If

            If TypeOf expression.Parent Is ElseIfStatementSyntax AndAlso
               DirectCast(expression.Parent, ElseIfStatementSyntax).Condition Is expression AndAlso
               TypeOf expression.Parent.Parent Is ElseIfBlockSyntax Then
                ifStatementNode = expression.Parent.Parent
                Return True
            End If

            ifStatementNode = Nothing
            Return False
        End Function

        Protected Overrides Function SplitIfStatement(ifStatementNode As SyntaxNode,
                                                      condition1 As ExpressionSyntax,
                                                      condition2 As ExpressionSyntax) As SyntaxNode
            Dim innerIfStatement = SyntaxFactory.IfStatement(SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.IfKeyword),
                                                             condition2,
                                                             SyntaxFactory.Token(SyntaxKind.ThenKeyword))
            If TypeOf ifStatementNode Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(ifStatementNode, MultiLineIfBlockSyntax)

                Dim innerIfBlock = SyntaxFactory.MultiLineIfBlock(innerIfStatement,
                                                              ifBlock.Statements,
                                                              ifBlock.ElseIfBlocks,
                                                              ifBlock.ElseBlock)
                Dim outerIfBlock = ifBlock _
                              .WithIfStatement(ifBlock.IfStatement.WithCondition(condition1)) _
                              .WithStatements(SyntaxFactory.SingletonList(Of StatementSyntax)(innerIfBlock))

                Return outerIfBlock
            ElseIf TypeOf ifStatementNode Is ElseIfBlockSyntax Then
                Dim elseIfBlock = DirectCast(ifStatementNode, ElseIfBlockSyntax)
                Dim ifBlock = DirectCast(elseIfBlock.Parent, MultiLineIfBlockSyntax)
                Dim elseIfBlockIndex = ifBlock.ElseIfBlocks.IndexOf(elseIfBlock)

                Dim innerIfBlock = SyntaxFactory.MultiLineIfBlock(innerIfStatement,
                                                              elseIfBlock.Statements,
                                                              ifBlock.ElseIfBlocks.RemoveRange(0, elseIfBlockIndex + 1),
                                                              ifBlock.ElseBlock)
                Dim outerElseIfBlock = elseIfBlock _
                              .WithElseIfStatement(elseIfBlock.ElseIfStatement.WithCondition(condition1)) _
                              .WithStatements(SyntaxFactory.SingletonList(Of StatementSyntax)(innerIfBlock))

                Return outerElseIfBlock
            End If
            Throw ExceptionUtilities.UnexpectedValue(ifStatementNode)
        End Function
    End Class
End Namespace
