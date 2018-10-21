' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.SplitIntoNestedIfStatements
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SplitIntoNestedIfStatements
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.MergeNestedIfStatements), [Shared]>
    Friend NotInheritable Class VisualBasicMergeNestedIfStatementsCodeRefactoringProvider
        Inherits AbstractMergeNestedIfStatementsCodeRefactoringProvider(Of ExpressionSyntax)

        Protected Overrides ReadOnly Property IfKeywordText As String = SyntaxFacts.GetText(SyntaxKind.IfKeyword)

        Protected Overrides Function IsTokenOfIfStatement(token As SyntaxToken, ByRef ifStatement As SyntaxNode) As Boolean
            If TypeOf token.Parent Is IfStatementSyntax AndAlso TypeOf token.Parent.Parent Is MultiLineIfBlockSyntax Then
                ifStatement = DirectCast(token.Parent.Parent, MultiLineIfBlockSyntax)
                Return True
            End If

            ifStatement = Nothing
            Return False
        End Function

        Protected Overrides Function IsIfStatement(statement As SyntaxNode) As Boolean
            Return TypeOf statement Is MultiLineIfBlockSyntax OrElse
                   TypeOf statement Is ElseIfBlockSyntax
        End Function

        Protected Overrides Function GetElseClauses(ifStatementNode As SyntaxNode) As ImmutableArray(Of SyntaxNode)
            If TypeOf ifStatementNode Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(ifStatementNode, MultiLineIfBlockSyntax)
                Return ImmutableArray.ToImmutableArray(Of SyntaxNode)(ifBlock.ElseIfBlocks).Add(ifBlock.ElseBlock)
            ElseIf TypeOf ifStatementNode Is ElseIfBlockSyntax Then
                Dim elseIfBlock = DirectCast(ifStatementNode, ElseIfBlockSyntax)
                Dim ifBlock = DirectCast(elseIfBlock.Parent, MultiLineIfBlockSyntax)
                Dim nextElseIfBlocks = ifBlock.ElseIfBlocks.RemoveRange(0, ifBlock.ElseIfBlocks.IndexOf(elseIfBlock) + 1)
                Return ImmutableArray.ToImmutableArray(Of SyntaxNode)(nextElseIfBlocks).Add(ifBlock.ElseBlock)
            End If
            Throw ExceptionUtilities.UnexpectedValue(ifStatementNode)
        End Function

        Protected Overrides Function MergeIfStatements(outerIfStatementNode As SyntaxNode,
                                                       innerIfStatementNode As SyntaxNode,
                                                       condition As ExpressionSyntax) As SyntaxNode
            Dim innerIfBlock = DirectCast(innerIfStatementNode, MultiLineIfBlockSyntax)
            If TypeOf outerIfStatementNode Is MultiLineIfBlockSyntax Then
                Dim outerIfBlock = DirectCast(outerIfStatementNode, MultiLineIfBlockSyntax)

                Return outerIfBlock.WithIfStatement(outerIfBlock.IfStatement.WithCondition(condition)) _
                                   .WithStatements(innerIfBlock.Statements)
            ElseIf TypeOf outerIfStatementNode Is ElseIfBlockSyntax Then
                Dim outerElseIfBlock = DirectCast(outerIfStatementNode, ElseIfBlockSyntax)

                Return outerElseIfBlock.WithElseIfStatement(outerElseIfBlock.ElseIfStatement.WithCondition(condition)) _
                                       .WithStatements(innerIfBlock.Statements)
            End If
            Throw ExceptionUtilities.UnexpectedValue(outerIfStatementNode)
        End Function
    End Class
End Namespace
