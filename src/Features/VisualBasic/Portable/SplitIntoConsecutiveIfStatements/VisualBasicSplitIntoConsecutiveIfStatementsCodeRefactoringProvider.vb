' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.SplitIntoConsecutiveIfStatements
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SplitIntoConsecutiveIfStatements
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.SplitIntoConsecutiveIfStatements), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeRefactoringProviderNames.InvertLogical, Before:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    Friend NotInheritable Class VisualBasicSplitIntoConsecutiveIfStatementsCodeRefactoringProvider
        Inherits AbstractSplitIntoConsecutiveIfStatementsCodeRefactoringProvider(Of ExpressionSyntax)

        Protected Overrides ReadOnly Property IfKeywordText As String = SyntaxFacts.GetText(SyntaxKind.IfKeyword)

        Protected Overrides ReadOnly Property LogicalOrSyntaxKind As Integer = SyntaxKind.OrElseExpression

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

        Protected Overrides Function HasElseClauses(ifStatement As SyntaxNode) As Boolean
            If TypeOf ifStatement Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(ifStatement, MultiLineIfBlockSyntax)
                Return ifBlock.ElseIfBlocks.Count > 0 OrElse ifBlock.ElseBlock IsNot Nothing
            End If

            Return True
        End Function

        Protected Overrides Function SplitIfStatementIntoElseClause(currentIfStatementNode As SyntaxNode,
                                                                    condition1 As ExpressionSyntax,
                                                                    condition2 As ExpressionSyntax) As (SyntaxNode, SyntaxNode)
            Dim secondIfStatement = SyntaxFactory.ElseIfStatement(SyntaxFactory.Token(SyntaxKind.ElseIfKeyword),
                                                                  condition2,
                                                                  SyntaxFactory.Token(SyntaxKind.ThenKeyword))
            If TypeOf currentIfStatementNode Is MultiLineIfBlockSyntax Then
                Dim currentIfBlock = DirectCast(currentIfStatementNode, MultiLineIfBlockSyntax)

                Dim secondIfBlock = SyntaxFactory.ElseIfBlock(secondIfStatement, currentIfBlock.Statements)
                Dim firstIfBlock = currentIfBlock _
                                   .WithIfStatement(currentIfBlock.IfStatement.WithCondition(condition1)) _
                                   .WithElseIfBlocks(currentIfBlock.ElseIfBlocks.Insert(0, secondIfBlock))

                Return (firstIfBlock, Nothing)
            ElseIf TypeOf currentIfStatementNode Is ElseIfBlockSyntax Then
                Dim currentElseIfBlock = DirectCast(currentIfStatementNode, ElseIfBlockSyntax)

                Dim secondIfBlock = SyntaxFactory.ElseIfBlock(secondIfStatement, currentElseIfBlock.Statements)
                Dim firstIfblock = currentElseIfBlock _
                                   .WithElseIfStatement(currentElseIfBlock.ElseIfStatement.WithCondition(condition1))

                Return (firstIfblock, secondIfBlock)
            End If
            Throw ExceptionUtilities.UnexpectedValue(currentIfStatementNode)
        End Function

        Protected Overrides Function SplitIfStatementIntoSeparateStatements(currentIfStatementNode As SyntaxNode,
                                                                            condition1 As ExpressionSyntax,
                                                                            condition2 As ExpressionSyntax) As (SyntaxNode, SyntaxNode)
            Dim currentIfBlock = DirectCast(currentIfStatementNode, MultiLineIfBlockSyntax)

            Dim secondIfStatement = SyntaxFactory.IfStatement(SyntaxFactory.Token(SyntaxKind.IfKeyword),
                                                              condition2,
                                                              SyntaxFactory.Token(SyntaxKind.ThenKeyword))
            Dim secondIfBlock = SyntaxFactory.MultiLineIfBlock(secondIfStatement,
                                                               currentIfBlock.Statements,
                                                               SyntaxFactory.List(Of ElseIfBlockSyntax),
                                                               Nothing)
            Dim firstIfBlock = currentIfBlock.
                               WithIfStatement(currentIfBlock.IfStatement.WithCondition(condition1))

            Return (firstIfBlock, secondIfBlock)
        End Function
    End Class
End Namespace
