' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.SplitOrMergeIfStatements
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.SplitIntoConsecutiveIfStatements), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeRefactoringProviderNames.InvertLogical, Before:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    Friend NotInheritable Class VisualBasicSplitIntoConsecutiveIfStatementsCodeRefactoringProvider
        Inherits AbstractSplitIntoConsecutiveIfStatementsCodeRefactoringProvider(Of ExpressionSyntax)

        Protected Overrides Function IsConditionOfIfStatement(expression As SyntaxNode, ByRef ifStatementNode As SyntaxNode) As Boolean
            Return Helpers.IsConditionOfIfStatement(expression, ifStatementNode)
        End Function

        Protected Overrides Function HasElseClauses(ifStatementNode As SyntaxNode) As Boolean
            Return Helpers.GetElseClauses(ifStatementNode).Count > 0
        End Function

        Protected Overrides Function SplitIfStatementIntoElseClause(ifStatementNode As SyntaxNode,
                                                                    condition1 As ExpressionSyntax,
                                                                    condition2 As ExpressionSyntax) As (SyntaxNode, SyntaxNode)
            Dim secondIfStatement = SyntaxFactory.ElseIfStatement(SyntaxFactory.Token(SyntaxKind.ElseIfKeyword),
                                                                  condition2,
                                                                  SyntaxFactory.Token(SyntaxKind.ThenKeyword))
            If TypeOf ifStatementNode Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(ifStatementNode, MultiLineIfBlockSyntax)

                Dim secondIfBlock = SyntaxFactory.ElseIfBlock(secondIfStatement, ifBlock.Statements)
                Dim firstIfBlock = ifBlock _
                                   .WithIfStatement(ifBlock.IfStatement.WithCondition(condition1)) _
                                   .WithElseIfBlocks(ifBlock.ElseIfBlocks.Insert(0, secondIfBlock))

                Return (firstIfBlock, Nothing)
            ElseIf TypeOf ifStatementNode Is ElseIfBlockSyntax Then
                Dim elseIfBlock = DirectCast(ifStatementNode, ElseIfBlockSyntax)

                Dim secondIfBlock = SyntaxFactory.ElseIfBlock(secondIfStatement, elseIfBlock.Statements)
                Dim firstIfblock = elseIfBlock _
                                   .WithElseIfStatement(elseIfBlock.ElseIfStatement.WithCondition(condition1))

                Return (firstIfblock, secondIfBlock)
            End If
            Throw ExceptionUtilities.UnexpectedValue(ifStatementNode)
        End Function

        Protected Overrides Function SplitIfStatementIntoSeparateStatements(ifStatementNode As SyntaxNode,
                                                                            condition1 As ExpressionSyntax,
                                                                            condition2 As ExpressionSyntax) As (SyntaxNode, SyntaxNode)
            Dim ifBlock = DirectCast(ifStatementNode, MultiLineIfBlockSyntax)

            Dim secondIfStatement = SyntaxFactory.IfStatement(SyntaxFactory.Token(SyntaxKind.IfKeyword),
                                                              condition2,
                                                              SyntaxFactory.Token(SyntaxKind.ThenKeyword))
            Dim secondIfBlock = SyntaxFactory.MultiLineIfBlock(secondIfStatement,
                                                               ifBlock.Statements,
                                                               SyntaxFactory.List(Of ElseIfBlockSyntax),
                                                               Nothing)
            Dim firstIfBlock = ifBlock.
                               WithIfStatement(ifBlock.IfStatement.WithCondition(condition1))

            Return (firstIfBlock, secondIfBlock)
        End Function
    End Class
End Namespace
