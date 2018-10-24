' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.SplitOrMergeIfStatements
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements
    <ExportLanguageService(GetType(IIfLikeStatementGenerator), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicIfLikeStatementGenerator
        Implements IIfLikeStatementGenerator

        Public Function IsIfLikeStatement(node As SyntaxNode) As Boolean Implements IIfLikeStatementGenerator.IsIfLikeStatement
            Return TypeOf node Is MultiLineIfBlockSyntax OrElse
                   TypeOf node Is ElseIfBlockSyntax
        End Function

        Public Function IsCondition(expression As SyntaxNode, ByRef ifLikeStatement As SyntaxNode) As Boolean Implements IIfLikeStatementGenerator.IsCondition
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

        Public Function IsElseIfClause(node As SyntaxNode, ByRef parentIfLikeStatement As SyntaxNode) As Boolean Implements IIfLikeStatementGenerator.IsElseIfClause
            If TypeOf node Is ElseIfBlockSyntax Then
                Dim ifBlock = DirectCast(node.Parent, MultiLineIfBlockSyntax)
                Dim index = ifBlock.ElseIfBlocks.IndexOf(DirectCast(node, ElseIfBlockSyntax))
                parentIfLikeStatement = If(index > 0, ifBlock.ElseIfBlocks(index - 1), DirectCast(ifBlock, SyntaxNode))
                Return True
            End If

            parentIfLikeStatement = Nothing
            Return False
        End Function

        Public Function GetCondition(ifLikeStatement As SyntaxNode) As SyntaxNode Implements IIfLikeStatementGenerator.GetCondition
            If TypeOf ifLikeStatement Is MultiLineIfBlockSyntax Then
                Return DirectCast(ifLikeStatement, MultiLineIfBlockSyntax).IfStatement.Condition
            ElseIf TypeOf ifLikeStatement Is ElseIfBlockSyntax Then
                Return DirectCast(ifLikeStatement, ElseIfBlockSyntax).ElseIfStatement.Condition
            Else
                Throw ExceptionUtilities.UnexpectedValue(ifLikeStatement)
            End If
        End Function

        Public Function GetElseLikeClauses(ifLikeStatement As SyntaxNode) As ImmutableArray(Of SyntaxNode) Implements IIfLikeStatementGenerator.GetElseLikeClauses
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

        Public Function WithCondition(ifLikeStatement As SyntaxNode, condition As SyntaxNode) As SyntaxNode Implements IIfLikeStatementGenerator.WithCondition
            If TypeOf ifLikeStatement Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(ifLikeStatement, MultiLineIfBlockSyntax)
                Return ifBlock.WithIfStatement(ifBlock.IfStatement.WithCondition(DirectCast(condition, ExpressionSyntax)))
            ElseIf TypeOf ifLikeStatement Is ElseIfBlockSyntax Then
                Dim elseIfBlock = DirectCast(ifLikeStatement, ElseIfBlockSyntax)
                Return elseIfBlock.WithElseIfStatement(elseIfBlock.ElseIfStatement.WithCondition(DirectCast(condition, ExpressionSyntax)))
            Else
                Throw ExceptionUtilities.UnexpectedValue(ifLikeStatement)
            End If
        End Function

        Public Function WithStatement(ifLikeStatement As SyntaxNode, statement As SyntaxNode) As SyntaxNode Implements IIfLikeStatementGenerator.WithStatement
            Return ifLikeStatement.ReplaceStatements(SyntaxFactory.SingletonList(DirectCast(statement, StatementSyntax)))
        End Function

        Public Function WithStatementsOf(ifLikeStatement As SyntaxNode, otherIfLikeStatement As SyntaxNode) As SyntaxNode Implements IIfLikeStatementGenerator.WithStatementsOf
            Return ifLikeStatement.ReplaceStatements(otherIfLikeStatement.GetStatements())
        End Function

        Public Function ToIfStatement(ifLikeStatement As SyntaxNode) As SyntaxNode Implements IIfLikeStatementGenerator.ToIfStatement
            If TypeOf ifLikeStatement Is ElseIfBlockSyntax Then
                Dim elseIfBlock = DirectCast(ifLikeStatement, ElseIfBlockSyntax)
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
                Return ifLikeStatement
            End If
        End Function

        Public Function ToElseIfClause(ifLikeStatement As SyntaxNode) As SyntaxNode Implements IIfLikeStatementGenerator.ToElseIfClause
            If TypeOf ifLikeStatement Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(ifLikeStatement, MultiLineIfBlockSyntax)

                Dim newElseIfStatement = SyntaxFactory.ElseIfStatement(SyntaxFactory.Token(SyntaxKind.ElseIfKeyword),
                                                                       ifBlock.IfStatement.Condition,
                                                                       ifBlock.IfStatement.ThenKeyword)
                Dim newElseIfBlock = SyntaxFactory.ElseIfBlock(newElseIfStatement,
                                                               ifBlock.Statements)
                Return newElseIfBlock
            Else
                Return ifLikeStatement
            End If
        End Function

        Public Sub InsertElseIfClause(editor As SyntaxEditor, afterIfLikeStatement As SyntaxNode, elseIfClause As SyntaxNode) Implements IIfLikeStatementGenerator.InsertElseIfClause
            Dim elseIfBlockToInsert = DirectCast(elseIfClause, ElseIfBlockSyntax)
            If TypeOf afterIfLikeStatement Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(afterIfLikeStatement, MultiLineIfBlockSyntax)
                Dim newIfBlock = ifBlock.WithElseIfBlocks(ifBlock.ElseIfBlocks.Insert(0, elseIfBlockToInsert))
                editor.ReplaceNode(ifBlock, newIfBlock)
            ElseIf TypeOf afterIfLikeStatement Is ElseIfBlockSyntax Then
                editor.InsertAfter(afterIfLikeStatement, elseIfBlockToInsert)
            Else
                Throw ExceptionUtilities.UnexpectedValue(afterIfLikeStatement)
            End If
        End Sub

        Public Sub RemoveElseIfClause(editor As SyntaxEditor, elseIfClause As SyntaxNode) Implements IIfLikeStatementGenerator.RemoveElseIfClause
            editor.RemoveNode(elseIfClause)
        End Sub
    End Class
End Namespace
