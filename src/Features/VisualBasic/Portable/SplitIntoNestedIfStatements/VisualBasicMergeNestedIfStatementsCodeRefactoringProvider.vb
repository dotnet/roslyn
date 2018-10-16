' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.SplitIntoNestedIfStatements
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SplitIntoNestedIfStatements
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.MergeNestedIfStatements), [Shared]>
    Friend NotInheritable Class VisualBasicMergeNestedIfStatementsCodeRefactoringProvider
        Inherits AbstractMergeNestedIfStatementsCodeRefactoringProvider(Of MultiLineIfBlockSyntax)

        Protected Overrides ReadOnly Property IfKeywordText As String = SyntaxFacts.GetText(SyntaxKind.IfKeyword)

        Protected Overrides Function IsTokenOfIfStatement(token As SyntaxToken, ByRef ifStatement As MultiLineIfBlockSyntax) As Boolean
            If TypeOf token.Parent Is IfStatementSyntax AndAlso TypeOf token.Parent.Parent Is MultiLineIfBlockSyntax Then
                ifStatement = DirectCast(token.Parent.Parent, MultiLineIfBlockSyntax)
                Return True
            End If

            ifStatement = Nothing
            Return False
        End Function

        Protected Overrides Function IsFirstStatementOfIfStatement(statement As SyntaxNode, ByRef ifStatement As MultiLineIfBlockSyntax) As Boolean
            If TypeOf statement.Parent Is MultiLineIfBlockSyntax Then
                ifStatement = DirectCast(statement.Parent, MultiLineIfBlockSyntax)
                Return ifStatement.Statements.FirstOrDefault() Is statement
            End If

            ifStatement = Nothing
            Return False
        End Function

        Protected Overrides Function GetElseClauses(ifStatement As MultiLineIfBlockSyntax) As ImmutableArray(Of SyntaxNode)
            Return ImmutableArray.ToImmutableArray(Of SyntaxNode)(ifStatement.ElseIfBlocks).Add(ifStatement.ElseBlock)
        End Function

        Protected Overrides Function MergeIfStatements(outerIfBlock As MultiLineIfBlockSyntax, innerIfBlock As MultiLineIfBlockSyntax, generator As SyntaxGenerator) As MultiLineIfBlockSyntax
            Dim newCondition = SyntaxFactory.BinaryExpression(SyntaxKind.AndAlsoExpression,
                                                              DirectCast(generator.AddParentheses(outerIfBlock.IfStatement.Condition), ExpressionSyntax),
                                                              SyntaxFactory.Token(SyntaxKind.AndAlsoKeyword),
                                                              DirectCast(generator.AddParentheses(innerIfBlock.IfStatement.Condition), ExpressionSyntax))
            Return outerIfBlock.WithIfStatement(outerIfBlock.IfStatement.WithCondition(newCondition)).WithStatements(innerIfBlock.Statements)
        End Function
    End Class
End Namespace
