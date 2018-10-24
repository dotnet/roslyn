' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.SplitOrMergeIfStatements
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements
    <ExportLanguageService(GetType(IIfStatementSyntaxService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicIfStatementSyntaxService
        Implements IIfStatementSyntaxService

        Public ReadOnly Property IfKeywordKind As Integer =
            SyntaxKind.IfKeyword Implements IIfStatementSyntaxService.IfKeywordKind

        Public ReadOnly Property LogicalAndExpressionKind As Integer =
            SyntaxKind.AndAlsoExpression Implements IIfStatementSyntaxService.LogicalAndExpressionKind

        Public ReadOnly Property LogicalOrExpressionKind As Integer =
            SyntaxKind.OrElseExpression Implements IIfStatementSyntaxService.LogicalOrExpressionKind

        Public Function IsIfLikeStatement(node As SyntaxNode) As Boolean Implements IIfStatementSyntaxService.IsIfLikeStatement
            Return TypeOf node Is MultiLineIfBlockSyntax OrElse
                   TypeOf node Is ElseIfBlockSyntax
        End Function

        Public Function IsConditionOfIfLikeStatement(expression As SyntaxNode, ByRef ifLikeStatement As SyntaxNode) As Boolean Implements IIfStatementSyntaxService.IsConditionOfIfLikeStatement
            If TypeOf expression.Parent Is IfStatementSyntax AndAlso
               DirectCast(expression.Parent, IfStatementSyntax).Condition Is expression AndAlso
               TypeOf expression.Parent.Parent Is MultiLineIfBlockSyntax Then
                ifLikeStatement = expression.Parent.Parent
                Return True
            End If

            If TypeOf expression.Parent Is ElseIfStatementSyntax AndAlso
               DirectCast(expression.Parent, ElseIfStatementSyntax).Condition Is expression AndAlso
               TypeOf expression.Parent.Parent Is ElseIfBlockSyntax Then
                ifLikeStatement = expression.Parent.Parent
                Return True
            End If

            ifLikeStatement = Nothing
            Return False
        End Function

        Public Function GetConditionOfIfLikeStatement(ifLikeStatement As SyntaxNode) As SyntaxNode Implements IIfStatementSyntaxService.GetConditionOfIfLikeStatement
            If TypeOf ifLikeStatement Is MultiLineIfBlockSyntax Then
                Return DirectCast(ifLikeStatement, MultiLineIfBlockSyntax).IfStatement.Condition
            ElseIf TypeOf ifLikeStatement Is ElseIfBlockSyntax Then
                Return DirectCast(ifLikeStatement, ElseIfBlockSyntax).ElseIfStatement.Condition
            Else
                Throw ExceptionUtilities.UnexpectedValue(ifLikeStatement)
            End If
        End Function

        Public Function GetElseLikeClauses(ifLikeStatement As SyntaxNode) As ImmutableArray(Of SyntaxNode) Implements IIfStatementSyntaxService.GetElseLikeClauses
            If TypeOf ifLikeStatement Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(ifLikeStatement, MultiLineIfBlockSyntax)
                Return AddIfNotNull(ifBlock.ElseIfBlocks, ifBlock.ElseBlock).ToImmutableArray()
            ElseIf TypeOf ifLikeStatement Is ElseIfBlockSyntax Then
                Dim elseIfBlock = DirectCast(ifLikeStatement, ElseIfBlockSyntax)
                Dim ifBlock = DirectCast(elseIfBlock.Parent, MultiLineIfBlockSyntax)
                Dim nextElseIfBlocks = ifBlock.ElseIfBlocks.RemoveRange(0, ifBlock.ElseIfBlocks.IndexOf(elseIfBlock) + 1)
                Return AddIfNotNull(nextElseIfBlocks, ifBlock.ElseBlock).ToImmutableArray()
            Else
                Throw ExceptionUtilities.UnexpectedValue(ifLikeStatement)
            End If
        End Function

        Private Shared Function AddIfNotNull(list As SyntaxList(Of SyntaxNode), node As SyntaxNode) As SyntaxList(Of SyntaxNode)
            Return If(node IsNot Nothing, list.Add(node), list)
        End Function

        Public Function WithCondition(ifOrElseIfNode As SyntaxNode, condition As SyntaxNode) As SyntaxNode Implements IIfStatementSyntaxService.WithCondition
            If TypeOf ifOrElseIfNode Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(ifOrElseIfNode, MultiLineIfBlockSyntax)
                Return ifBlock.WithIfStatement(ifBlock.IfStatement.WithCondition(DirectCast(condition, ExpressionSyntax)))
            ElseIf TypeOf ifOrElseIfNode Is ElseIfBlockSyntax Then
                Dim elseIfBlock = DirectCast(ifOrElseIfNode, ElseIfBlockSyntax)
                Return elseIfBlock.WithElseIfStatement(elseIfBlock.ElseIfStatement.WithCondition(DirectCast(condition, ExpressionSyntax)))
            Else
                Throw ExceptionUtilities.UnexpectedValue(ifOrElseIfNode)
            End If
        End Function

        Public Function WithStatement(ifOrElseIfNode As SyntaxNode, statement As SyntaxNode) As SyntaxNode Implements IIfStatementSyntaxService.WithStatement
            Return ifOrElseIfNode.ReplaceStatements(SyntaxFactory.SingletonList(DirectCast(statement, StatementSyntax)))
        End Function

        Public Function WithStatementsOf(ifOrElseIfNode As SyntaxNode, otherIfOrElseIfNode As SyntaxNode) As SyntaxNode Implements IIfStatementSyntaxService.WithStatementsOf
            Return ifOrElseIfNode.ReplaceStatements(otherIfOrElseIfNode.GetStatements())
        End Function

        Public Function ToIfStatement(ifOrElseIfNode As SyntaxNode) As SyntaxNode Implements IIfStatementSyntaxService.ToIfStatement
            If TypeOf ifOrElseIfNode Is ElseIfBlockSyntax Then
                Dim elseIfBlock = DirectCast(ifOrElseIfNode, ElseIfBlockSyntax)
                Dim ifBlock = DirectCast(elseIfBlock.Parent, MultiLineIfBlockSyntax)
                Dim nextElseIfBlocks = ifBlock.ElseIfBlocks.RemoveRange(0, ifBlock.ElseIfBlocks.IndexOf(elseIfBlock) + 1)

                Dim newIfStatement = SyntaxFactory.IfStatement(ifBlock.IfStatement.IfKeyword,
                                                               elseIfBlock.ElseIfStatement.Condition,
                                                               elseIfBlock.ElseIfStatement.ThenKeyword)
                Dim newIfBlock = SyntaxFactory.MultiLineIfBlock(newIfStatement,
                                                                elseIfBlock.Statements,
                                                                nextElseIfBlocks,
                                                                ifBlock.ElseBlock,
                                                                ifBlock.EndIfStatement)
                Return newIfBlock
            Else
                Return ifOrElseIfNode
            End If
        End Function

        Public Function ToElseIfClause(ifOrElseIfNode As SyntaxNode) As SyntaxNode Implements IIfStatementSyntaxService.ToElseIfClause
            If TypeOf ifOrElseIfNode Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(ifOrElseIfNode, MultiLineIfBlockSyntax)

                Dim newElseIfStatement = SyntaxFactory.ElseIfStatement(SyntaxFactory.Token(SyntaxKind.ElseIfKeyword),
                                                                       ifBlock.IfStatement.Condition,
                                                                       ifBlock.IfStatement.ThenKeyword)
                Dim newElseIfBlock = SyntaxFactory.ElseIfBlock(newElseIfStatement,
                                                               ifBlock.Statements)
                Return newElseIfBlock
            Else
                Return ifOrElseIfNode
            End If
        End Function

        Public Sub InsertElseIfClause(editor As SyntaxEditor, ifOrElseIfNode As SyntaxNode, elseIfClause As SyntaxNode) Implements IIfStatementSyntaxService.InsertElseIfClause
            Dim elseIfBlockToInsert = DirectCast(elseIfClause, ElseIfBlockSyntax)
            If TypeOf ifOrElseIfNode Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(ifOrElseIfNode, MultiLineIfBlockSyntax)
                Dim newIfBlock = ifBlock.WithElseIfBlocks(ifBlock.ElseIfBlocks.Insert(0, elseIfBlockToInsert))
                editor.ReplaceNode(ifBlock, newIfBlock)
            ElseIf TypeOf ifOrElseIfNode Is ElseIfBlockSyntax Then
                editor.InsertAfter(ifOrElseIfNode, elseIfBlockToInsert)
            Else
                Throw ExceptionUtilities.UnexpectedValue(ifOrElseIfNode)
            End If
        End Sub

        Public Sub RemoveElseIfClause(editor As SyntaxEditor, elseIfClause As SyntaxNode) Implements IIfStatementSyntaxService.RemoveElseIfClause
            editor.RemoveNode(elseIfClause)
        End Sub
    End Class
End Namespace
