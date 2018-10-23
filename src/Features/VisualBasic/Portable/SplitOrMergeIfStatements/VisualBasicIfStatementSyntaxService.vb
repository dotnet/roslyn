' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
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

        Public Function GetElseLikeClauses(ifLikeStatement As SyntaxNode) As ImmutableArray(Of SyntaxNode) Implements IIfStatementSyntaxService.GetElseLikeClauses
            If TypeOf ifLikeStatement Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(ifLikeStatement, MultiLineIfBlockSyntax)
                Return AddIfNotNull(ifBlock.ElseIfBlocks, ifBlock.ElseBlock).ToImmutableArray()
            ElseIf TypeOf ifLikeStatement Is ElseIfBlockSyntax Then
                Dim elseIfBlock = DirectCast(ifLikeStatement, ElseIfBlockSyntax)
                Dim ifBlock = DirectCast(elseIfBlock.Parent, MultiLineIfBlockSyntax)
                Dim nextElseIfBlocks = ifBlock.ElseIfBlocks.RemoveRange(0, ifBlock.ElseIfBlocks.IndexOf(elseIfBlock) + 1)
                Return AddIfNotNull(nextElseIfBlocks, ifBlock.ElseBlock).ToImmutableArray()
            End If
            Throw ExceptionUtilities.UnexpectedValue(ifLikeStatement)
        End Function

        Private Shared Function AddIfNotNull(list As SyntaxList(Of SyntaxNode), node As SyntaxNode) As SyntaxList(Of SyntaxNode)
            Return If(node IsNot Nothing, list.Add(node), list)
        End Function
    End Class
End Namespace
