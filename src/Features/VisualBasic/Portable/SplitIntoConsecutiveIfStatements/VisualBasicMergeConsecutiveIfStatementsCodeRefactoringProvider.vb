' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.SplitIntoConsecutiveIfStatements
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SplitIntoConsecutiveIfStatements
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.MergeConsecutiveIfStatements), [Shared]>
    Friend NotInheritable Class VisualBasicMergeConsecutiveIfStatementsCodeRefactoringProvider
        Inherits AbstractMergeConsecutiveIfStatementsCodeRefactoringProvider(Of ExpressionSyntax)

        Protected Overrides ReadOnly Property IfKeywordText As String = SyntaxFacts.GetText(SyntaxKind.IfKeyword)

        Protected Overrides Function IsTokenOfIfStatement(token As SyntaxToken, ByRef ifStatement As SyntaxNode) As Boolean
            If TypeOf token.Parent Is IfStatementSyntax AndAlso TypeOf token.Parent.Parent Is MultiLineIfBlockSyntax Then
                ifStatement = token.Parent.Parent
                Return True
            End If

            If TypeOf token.Parent Is ElseIfStatementSyntax AndAlso TypeOf token.Parent.Parent Is ElseIfBlockSyntax Then
                ifStatement = token.Parent.Parent
                Return True
            End If

            ifStatement = Nothing
            Return False
        End Function

        Protected Overrides Function IsElseClauseOfIfStatement(statement As SyntaxNode, ByRef ifStatement As SyntaxNode) As Boolean
            If TypeOf statement Is ElseIfBlockSyntax Then
                Dim ifBlock = DirectCast(statement.Parent, MultiLineIfBlockSyntax)
                Dim index = ifBlock.ElseIfBlocks.IndexOf(DirectCast(statement, ElseIfBlockSyntax))
                ifStatement = If(index > 0, ifBlock.ElseIfBlocks(index - 1), DirectCast(ifBlock, SyntaxNode))
                Return True
            End If

            ifStatement = Nothing
            Return False
        End Function

        Protected Overrides Function IsIfStatement(statement As SyntaxNode) As Boolean
            Return TypeOf statement Is MultiLineIfBlockSyntax OrElse
                   TypeOf statement Is ElseIfBlockSyntax
        End Function

        Protected Overrides Function HasElseClauses(ifStatement As SyntaxNode) As Boolean
            If TypeOf ifStatement Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(ifStatement, MultiLineIfBlockSyntax)
                Return ifBlock.ElseIfBlocks.Count > 0 OrElse ifBlock.ElseBlock IsNot Nothing
            End If

            Return True
        End Function

        Protected Overrides Function MergeIfStatements(parentIfStatement As SyntaxNode,
                                                       ifStatement As SyntaxNode,
                                                       condition As ExpressionSyntax) As SyntaxNode
            If TypeOf parentIfStatement Is MultiLineIfBlockSyntax Then
                Dim parentIfBlock = DirectCast(parentIfStatement, MultiLineIfBlockSyntax)
                Dim newIfBlock = parentIfBlock.WithIfStatement(parentIfBlock.IfStatement.WithCondition(condition))
                Return If(newIfBlock.ElseIfBlocks.Count > 0, newIfBlock.WithElseIfBlocks(newIfBlock.ElseIfBlocks.RemoveAt(0)), newIfBlock)
            ElseIf TypeOf parentIfStatement Is ElseIfBlockSyntax Then
                Dim parentElseIfBlock = DirectCast(parentIfStatement, ElseIfBlockSyntax)
                Return parentElseIfBlock.WithElseIfStatement(parentElseIfBlock.ElseIfStatement.WithCondition(condition))
            End If
            Throw ExceptionUtilities.UnexpectedValue(parentIfStatement)
        End Function
    End Class
End Namespace
