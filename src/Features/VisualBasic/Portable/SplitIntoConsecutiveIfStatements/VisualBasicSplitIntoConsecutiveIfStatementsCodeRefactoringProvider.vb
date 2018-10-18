' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.SplitIntoConsecutiveIfStatements
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SplitIntoConsecutiveIfStatements
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.SplitIntoConsecutiveIfStatements), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeRefactoringProviderNames.InvertLogical, Before:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    Friend NotInheritable Class VisualBasicSplitIntoConsecutiveIfStatementsCodeRefactoringProvider
        Inherits AbstractSplitIntoConsecutiveIfStatementsCodeRefactoringProvider(Of MultiLineIfBlockSyntax, ExpressionSyntax)

        Protected Overrides ReadOnly Property IfKeywordText As String = SyntaxFacts.GetText(SyntaxKind.IfKeyword)

        Protected Overrides ReadOnly Property LogicalOrSyntaxKind As Integer = SyntaxKind.OrElseExpression

        Protected Overrides Function IsConditionOfIfStatement(expression As SyntaxNode, ByRef ifStatement As MultiLineIfBlockSyntax) As Boolean
            If TypeOf expression.Parent Is IfStatementSyntax AndAlso
               DirectCast(expression.Parent, IfStatementSyntax).Condition Is expression AndAlso
               TypeOf expression.Parent.Parent Is MultiLineIfBlockSyntax Then
                ifStatement = DirectCast(expression.Parent.Parent, MultiLineIfBlockSyntax)
                Return True
            End If

            ifStatement = Nothing
            Return False
        End Function

        Protected Overrides Function HasElseClauses(ifStatement As MultiLineIfBlockSyntax) As Boolean
            Return ifStatement.ElseIfBlocks.Count > 0 OrElse ifStatement.ElseBlock IsNot Nothing
        End Function

        Protected Overrides Function SplitIfStatementIntoElseClause(currentIfBlock As MultiLineIfBlockSyntax,
                                                                    condition1 As ExpressionSyntax,
                                                                    condition2 As ExpressionSyntax) As MultiLineIfBlockSyntax
            Dim secondIfStatement = SyntaxFactory.ElseIfStatement(SyntaxFactory.Token(SyntaxKind.ElseIfKeyword),
                                                              condition2,
                                                              SyntaxFactory.Token(SyntaxKind.ThenKeyword))
            Dim secondIfBlock = SyntaxFactory.ElseIfBlock(secondIfStatement,
                                                          currentIfBlock.Statements)
            Dim firstIfBlock = currentIfBlock _
                               .WithIfStatement(currentIfBlock.IfStatement.WithCondition(condition1)) _
                               .WithElseIfBlocks(currentIfBlock.ElseIfBlocks.Insert(0, secondIfBlock))

            Return firstIfBlock
        End Function

        Protected Overrides Function SplitIfStatementIntoSeparateStatements(currentIfBlock As MultiLineIfBlockSyntax,
                                                                            condition1 As ExpressionSyntax,
                                                                            condition2 As ExpressionSyntax) As (MultiLineIfBlockSyntax, MultiLineIfBlockSyntax)
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
