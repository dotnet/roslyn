' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements
    Friend Module Helpers
        Public Function IsConditionOfIfStatement(expression As SyntaxNode, ByRef ifStatementNode As SyntaxNode) As Boolean
            If TypeOf expression.Parent Is IfStatementSyntax AndAlso
               DirectCast(expression.Parent, IfStatementSyntax).Condition Is expression AndAlso
               TypeOf expression.Parent.Parent Is MultiLineIfBlockSyntax Then
                ifStatementNode = expression.Parent.Parent
                Return True
            End If

            If TypeOf expression.Parent Is ElseIfStatementSyntax AndAlso
               DirectCast(expression.Parent, ElseIfStatementSyntax).Condition Is expression AndAlso
               TypeOf expression.Parent.Parent Is ElseIfBlockSyntax Then
                ifStatementNode = expression.Parent.Parent
                Return True
            End If

            ifStatementNode = Nothing
            Return False
        End Function

        Public Function GetElseClauses(ifStatementNode As SyntaxNode) As SyntaxList(Of SyntaxNode)
            If TypeOf ifStatementNode Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(ifStatementNode, MultiLineIfBlockSyntax)
                Return AddIfNotNull(ifBlock.ElseIfBlocks, ifBlock.ElseBlock)
            ElseIf TypeOf ifStatementNode Is ElseIfBlockSyntax Then
                Dim elseIfBlock = DirectCast(ifStatementNode, ElseIfBlockSyntax)
                Dim ifBlock = DirectCast(elseIfBlock.Parent, MultiLineIfBlockSyntax)
                Dim nextElseIfBlocks = ifBlock.ElseIfBlocks.RemoveRange(0, ifBlock.ElseIfBlocks.IndexOf(elseIfBlock) + 1)
                Return AddIfNotNull(nextElseIfBlocks, ifBlock.ElseBlock)
            End If
            Throw ExceptionUtilities.UnexpectedValue(ifStatementNode)
        End Function

        Private Function AddIfNotNull(list As SyntaxList(Of SyntaxNode), node As SyntaxNode) As SyntaxList(Of SyntaxNode)
            Return If(node IsNot Nothing, list.Add(node), list)
        End Function
    End Module
End Namespace
