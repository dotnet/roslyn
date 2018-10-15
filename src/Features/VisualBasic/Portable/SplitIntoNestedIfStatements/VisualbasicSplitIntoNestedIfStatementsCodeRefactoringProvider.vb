' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.SplitIntoNestedIfStatements
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SplitIntoNestedIfStatements
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.SplitIntoNestedIfStatements), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeRefactoringProviderNames.InvertLogical, Before:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    Friend NotInheritable Class VisualBasicSplitIntoNestedIfStatementsCodeRefactoringProvider
        Inherits AbstractSplitIntoNestedIfStatementsCodeRefactoringProvider(Of MultiLineIfBlockSyntax, ExpressionSyntax)

        Protected Overrides ReadOnly Property LogicalAndSyntaxKind As Integer = SyntaxKind.AndAlsoExpression

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

        Protected Overrides Function SplitIfStatement(currentIfBlock As MultiLineIfBlockSyntax,
                                                      condition1 As ExpressionSyntax,
                                                      condition2 As ExpressionSyntax) As MultiLineIfBlockSyntax
            Dim innerIfStatement = SyntaxFactory.IfStatement(SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.IfKeyword),
                                                             condition2,
                                                             SyntaxFactory.Token(SyntaxKind.ThenKeyword))
            Dim innerIfBlock = SyntaxFactory.MultiLineIfBlock(innerIfStatement,
                                                              currentIfBlock.Statements,
                                                              SyntaxFactory.List(Of ElseIfBlockSyntax),
                                                              Nothing)
            Dim outerIfBlock = currentIfBlock _
                              .WithIfStatement(currentIfBlock.IfStatement.WithCondition(condition1)) _
                              .WithStatements(SyntaxFactory.SingletonList(Of StatementSyntax)(innerIfBlock))

            Return outerIfBlock
        End Function
    End Class
End Namespace
