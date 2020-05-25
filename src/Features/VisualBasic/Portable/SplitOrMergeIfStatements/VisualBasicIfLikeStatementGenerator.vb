' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function IsIfOrElseIf(node As SyntaxNode) As Boolean Implements IIfLikeStatementGenerator.IsIfOrElseIf
            Return TypeOf node Is MultiLineIfBlockSyntax OrElse
                   TypeOf node Is ElseIfBlockSyntax
        End Function

        Public Function IsCondition(expression As SyntaxNode, ByRef ifOrElseIf As SyntaxNode) As Boolean Implements IIfLikeStatementGenerator.IsCondition
            If TypeOf expression.Parent Is IfStatementSyntax AndAlso
               DirectCast(expression.Parent, IfStatementSyntax).Condition Is expression AndAlso
               TypeOf expression.Parent.Parent Is MultiLineIfBlockSyntax Then
                ifOrElseIf = expression.Parent.Parent
                Return True
            End If

            If TypeOf expression.Parent Is ElseIfStatementSyntax AndAlso
               DirectCast(expression.Parent, ElseIfStatementSyntax).Condition Is expression AndAlso
               TypeOf expression.Parent.Parent Is ElseIfBlockSyntax Then
                ifOrElseIf = expression.Parent.Parent
                Return True
            End If

            ifOrElseIf = Nothing
            Return False
        End Function

        Public Function IsElseIfClause(node As SyntaxNode, ByRef parentIfOrElseIf As SyntaxNode) As Boolean Implements IIfLikeStatementGenerator.IsElseIfClause
            If TypeOf node Is ElseIfBlockSyntax Then
                Dim ifBlock = DirectCast(node.Parent, MultiLineIfBlockSyntax)
                Dim index = ifBlock.ElseIfBlocks.IndexOf(DirectCast(node, ElseIfBlockSyntax))
                parentIfOrElseIf = If(index > 0, ifBlock.ElseIfBlocks(index - 1), DirectCast(ifBlock, SyntaxNode))
                Return True
            End If

            parentIfOrElseIf = Nothing
            Return False
        End Function

        Public Function HasElseIfClause(ifOrElseIf As SyntaxNode, ByRef elseIfClause As SyntaxNode) As Boolean Implements IIfLikeStatementGenerator.HasElseIfClause
            Dim ifBlock As MultiLineIfBlockSyntax
            Dim nextElseIfIndex As Integer

            If TypeOf ifOrElseIf Is MultiLineIfBlockSyntax Then
                ifBlock = DirectCast(ifOrElseIf, MultiLineIfBlockSyntax)
                nextElseIfIndex = 0
            ElseIf TypeOf ifOrElseIf Is ElseIfBlockSyntax Then
                ifBlock = DirectCast(ifOrElseIf.Parent, MultiLineIfBlockSyntax)
                Dim index = ifBlock.ElseIfBlocks.IndexOf(DirectCast(ifOrElseIf, ElseIfBlockSyntax))
                nextElseIfIndex = index + 1
            Else
                Throw ExceptionUtilities.UnexpectedValue(ifOrElseIf)
            End If

            elseIfClause = ifBlock.ElseIfBlocks.ElementAtOrDefault(nextElseIfIndex)
            Return elseIfClause IsNot Nothing
        End Function

        Public Function GetCondition(ifOrElseIf As SyntaxNode) As SyntaxNode Implements IIfLikeStatementGenerator.GetCondition
            If TypeOf ifOrElseIf Is MultiLineIfBlockSyntax Then
                Return DirectCast(ifOrElseIf, MultiLineIfBlockSyntax).IfStatement.Condition
            ElseIf TypeOf ifOrElseIf Is ElseIfBlockSyntax Then
                Return DirectCast(ifOrElseIf, ElseIfBlockSyntax).ElseIfStatement.Condition
            Else
                Throw ExceptionUtilities.UnexpectedValue(ifOrElseIf)
            End If
        End Function

        Public Function GetRootIfStatement(ifOrElseIf As SyntaxNode) As SyntaxNode Implements IIfLikeStatementGenerator.GetRootIfStatement
            If TypeOf ifOrElseIf Is MultiLineIfBlockSyntax Then
                Return ifOrElseIf
            ElseIf TypeOf ifOrElseIf Is ElseIfBlockSyntax Then
                Return ifOrElseIf.Parent
            Else
                Throw ExceptionUtilities.UnexpectedValue(ifOrElseIf)
            End If
        End Function

        Public Function GetElseIfAndElseClauses(ifOrElseIf As SyntaxNode) As ImmutableArray(Of SyntaxNode) Implements IIfLikeStatementGenerator.GetElseIfAndElseClauses
            If TypeOf ifOrElseIf Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(ifOrElseIf, MultiLineIfBlockSyntax)
                Return AddIfNotNull(ifBlock.ElseIfBlocks, ifBlock.ElseBlock).ToImmutableArray()
            ElseIf TypeOf ifOrElseIf Is ElseIfBlockSyntax Then
                Dim elseIfBlock = DirectCast(ifOrElseIf, ElseIfBlockSyntax)
                Dim ifBlock = DirectCast(elseIfBlock.Parent, MultiLineIfBlockSyntax)
                Dim nextElseIfBlocks = ifBlock.ElseIfBlocks.RemoveRange(0, ifBlock.ElseIfBlocks.IndexOf(elseIfBlock) + 1)
                Return AddIfNotNull(nextElseIfBlocks, ifBlock.ElseBlock).ToImmutableArray()
            Else
                Throw ExceptionUtilities.UnexpectedValue(ifOrElseIf)
            End If
        End Function

        Private Shared Function AddIfNotNull(list As SyntaxList(Of SyntaxNode), node As SyntaxNode) As SyntaxList(Of SyntaxNode)
            Return If(node IsNot Nothing, list.Add(node), list)
        End Function

        Public Function WithCondition(ifOrElseIf As SyntaxNode, condition As SyntaxNode) As SyntaxNode Implements IIfLikeStatementGenerator.WithCondition
            If TypeOf ifOrElseIf Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(ifOrElseIf, MultiLineIfBlockSyntax)
                Return ifBlock.WithIfStatement(ifBlock.IfStatement.WithCondition(DirectCast(condition, ExpressionSyntax)))
            ElseIf TypeOf ifOrElseIf Is ElseIfBlockSyntax Then
                Dim elseIfBlock = DirectCast(ifOrElseIf, ElseIfBlockSyntax)
                Return elseIfBlock.WithElseIfStatement(elseIfBlock.ElseIfStatement.WithCondition(DirectCast(condition, ExpressionSyntax)))
            Else
                Throw ExceptionUtilities.UnexpectedValue(ifOrElseIf)
            End If
        End Function

        Public Function WithStatementInBlock(ifOrElseIf As SyntaxNode, statement As SyntaxNode) As SyntaxNode Implements IIfLikeStatementGenerator.WithStatementInBlock
            Return ifOrElseIf.ReplaceStatements(SyntaxFactory.SingletonList(DirectCast(statement, StatementSyntax)))
        End Function

        Public Function WithStatementsOf(ifOrElseIf As SyntaxNode, otherIfOrElseIf As SyntaxNode) As SyntaxNode Implements IIfLikeStatementGenerator.WithStatementsOf
            Return ifOrElseIf.ReplaceStatements(otherIfOrElseIf.GetStatements())
        End Function

        Public Function WithElseIfAndElseClausesOf(ifStatement As SyntaxNode, otherIfStatement As SyntaxNode) As SyntaxNode Implements IIfLikeStatementGenerator.WithElseIfAndElseClausesOf
            Dim ifBlock = DirectCast(ifStatement, MultiLineIfBlockSyntax)
            Dim otherIfBlock = DirectCast(otherIfStatement, MultiLineIfBlockSyntax)
            Return ifBlock.WithElseIfBlocks(otherIfBlock.ElseIfBlocks).WithElseBlock(otherIfBlock.ElseBlock)
        End Function

        Public Function ToIfStatement(ifOrElseIf As SyntaxNode) As SyntaxNode Implements IIfLikeStatementGenerator.ToIfStatement
            If TypeOf ifOrElseIf Is MultiLineIfBlockSyntax Then
                Return ifOrElseIf
            ElseIf TypeOf ifOrElseIf Is ElseIfBlockSyntax Then
                Dim elseIfBlock = DirectCast(ifOrElseIf, ElseIfBlockSyntax)
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
                Throw ExceptionUtilities.UnexpectedValue(ifOrElseIf)
            End If
        End Function

        Public Function ToElseIfClause(ifOrElseIf As SyntaxNode) As SyntaxNode Implements IIfLikeStatementGenerator.ToElseIfClause
            If TypeOf ifOrElseIf Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(ifOrElseIf, MultiLineIfBlockSyntax)

                Dim newElseIfStatement = SyntaxFactory.ElseIfStatement(SyntaxFactory.Token(SyntaxKind.ElseIfKeyword),
                                                                       ifBlock.IfStatement.Condition,
                                                                       ifBlock.IfStatement.ThenKeyword)
                Dim newElseIfBlock = SyntaxFactory.ElseIfBlock(newElseIfStatement,
                                                               ifBlock.Statements)
                Return newElseIfBlock
            ElseIf TypeOf ifOrElseIf Is ElseIfBlockSyntax Then
                Return ifOrElseIf
            Else
                Throw ExceptionUtilities.UnexpectedValue(ifOrElseIf)
            End If
        End Function

        Public Sub InsertElseIfClause(editor As SyntaxEditor, afterIfOrElseIf As SyntaxNode, elseIfClause As SyntaxNode) Implements IIfLikeStatementGenerator.InsertElseIfClause
            Dim elseIfBlockToInsert = DirectCast(elseIfClause, ElseIfBlockSyntax)
            If TypeOf afterIfOrElseIf Is MultiLineIfBlockSyntax Then
                editor.ReplaceNode(afterIfOrElseIf,
                                   Function(currentNode, g)
                                       Dim ifBlock = DirectCast(currentNode, MultiLineIfBlockSyntax)
                                       Dim newIfBlock = ifBlock.WithElseIfBlocks(ifBlock.ElseIfBlocks.Insert(0, elseIfBlockToInsert))
                                       Return newIfBlock
                                   End Function)
            ElseIf TypeOf afterIfOrElseIf Is ElseIfBlockSyntax Then
                editor.InsertAfter(afterIfOrElseIf, elseIfBlockToInsert)
            Else
                Throw ExceptionUtilities.UnexpectedValue(afterIfOrElseIf)
            End If
        End Sub

        Public Sub RemoveElseIfClause(editor As SyntaxEditor, elseIfClause As SyntaxNode) Implements IIfLikeStatementGenerator.RemoveElseIfClause
            editor.RemoveNode(elseIfClause)
        End Sub
    End Class
End Namespace
