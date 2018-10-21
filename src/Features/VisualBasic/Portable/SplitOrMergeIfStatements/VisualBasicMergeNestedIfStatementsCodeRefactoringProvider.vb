' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.SplitOrMergeIfStatements
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.MergeNestedIfStatements), [Shared]>
    Friend NotInheritable Class VisualBasicMergeNestedIfStatementsCodeRefactoringProvider
        Inherits AbstractMergeNestedIfStatementsCodeRefactoringProvider(Of ExpressionSyntax)

        Protected Overrides ReadOnly Property IfKeywordText As String = SyntaxFacts.GetText(SyntaxKind.IfKeyword)

        Protected Overrides Function IsApplicableSpan(node As SyntaxNode, span As TextSpan, ByRef ifStatementNode As SyntaxNode) As Boolean
            If TypeOf node Is IfStatementSyntax And TypeOf node.Parent Is MultiLineIfBlockSyntax Then
                Dim ifStatement = DirectCast(node, IfStatementSyntax)
                ' Cases:
                ' 1. Position is at a direct token child of an if statement with no selection (e.g. 'If' keyword, 'Then' keyword)
                ' 2. Selection around the 'If' keyword
                ' 3. Selection around the if statement - from 'If' keyword to 'Then' keyword
                If span.Length = 0 OrElse
                   span.IsAround(ifStatement.IfKeyword) OrElse
                   span.IsAround(ifStatement) Then
                    ifStatementNode = node.Parent
                    Return True
                End If
            End If

            If TypeOf node Is MultiLineIfBlockSyntax Then
                ' 4. Selection around the whole if block
                If span.IsAround(node) Then
                    ifStatementNode = node
                    Return True
                End If
            End If

            ifStatementNode = Nothing
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
