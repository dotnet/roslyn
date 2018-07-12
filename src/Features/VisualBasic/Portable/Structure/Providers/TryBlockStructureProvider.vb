' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class TryBlockStructureProvider
        Inherits BlockStructureProvider(Of TryBlockSyntax, TryStatementSyntax, CatchBlockSyntax, FinallyBlockSyntax, EndBlockStatementSyntax)

        Friend Sub New(IncludeAdditionalInternalSpans As Boolean)
            MyBase.New(IncludeAdditionalInternalSpans)
        End Sub

        Friend Overrides Function GetFullBlockSpan(block As TryBlockSyntax) As BlockSpan?
            Return MyBase.GetFullBlockSpan(block, block.TryStatement)
        End Function

        Friend Overrides Function GetBlockHeader(block As TryBlockSyntax) As TryStatementSyntax
            Return block.TryStatement
        End Function

        Friend Overrides Function GetPreBlock(block As TryBlockSyntax, cancellationToken As Threading.CancellationToken) As BlockSpan?
            Return If(block Is Nothing OrElse block.IsMissing, Nothing, GetFirstBlockSpan(block))
        End Function

        Friend Overrides Function GetFirstStatementOfPreBlock(block As TryBlockSyntax) As SyntaxNode
            Return If(block.Statements.Count > 0, block.Statements(0), Nothing)
        End Function

        Friend Overrides Function GetInnerBlocks(block As TryBlockSyntax) As SyntaxList(Of CatchBlockSyntax)
            Return block.CatchBlocks
        End Function

        Friend Overrides Function GetInnerBlockBanner(InnerBlock As CatchBlockSyntax) As String
            Return If(InnerBlock IsNot Nothing,
                      If(InnerBlock.CatchStatement IsNot Nothing, InnerBlock.CatchStatement.ToString, String.Empty),
                      String.Empty)
        End Function

        Friend Overrides Function GetPostBlockSpan(block As TryBlockSyntax, cancellationToken As Threading.CancellationToken) As BlockSpan?
            If block Is Nothing Then
                Return Nothing
            End If
            If block.FinallyBlock Is Nothing OrElse block.FinallyBlock.IsMissing Then
                Return Nothing
            End If
            If block.EndTryStatement Is Nothing OrElse block.EndTryStatement.IsMissing Then
                Return Nothing
            End If
            Return GetBlockSpan(block.FinallyBlock, block.FinallyBlock.FinallyStatement, block.EndTryStatement, block.FinallyBlock.FinallyStatement.FinallyKeyword.Text, IgnoreHeader:=False)
        End Function

        Friend Overrides Function GetPostBlock(block As TryBlockSyntax) As FinallyBlockSyntax
            Return block.FinallyBlock
        End Function

        Friend Overrides Function GetEndOfBlockStatement(block As TryBlockSyntax) As EndBlockStatementSyntax
            Return block.EndTryStatement
        End Function

    End Class
End Namespace
