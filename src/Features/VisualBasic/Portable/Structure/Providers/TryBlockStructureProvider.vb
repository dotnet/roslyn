' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class TryBlockStructureProvider
        Inherits InternalStructureBlockStructureProvider(Of TryBlockSyntax, TryStatementSyntax, CatchBlockSyntax, FinallyBlockSyntax, EndBlockStatementSyntax)

        Friend Sub New(IncludeAdditionalInternalSpans As Boolean)
            MyBase.New(IncludeAdditionalInternalSpans)
        End Sub

        Friend Overrides Function FullStructuralBlockOutlining(block As TryBlockSyntax) As BlockSpan?
            Return MyBase.FullStructuralBlockOutlining(block, block.TryStatement)
        End Function

        Friend Overrides Function GetBannerTextOfFullStructuralBlock(block As TryBlockSyntax) As TryStatementSyntax
            Return block.TryStatement
        End Function

        Friend Overrides Function GetPreambleOutlining(block As TryBlockSyntax, cancellationToken As Threading.CancellationToken) As BlockSpan?
            Return If(block Is Nothing OrElse block.IsMissing, Nothing, GetPreambleOutlining(block))
        End Function

        Friend Overrides Function GetFirstStatementOfPreamble(block As TryBlockSyntax) As SyntaxNode
            Return If(block.Statements.Count > 0, block.Statements(0), Nothing)
        End Function

        Friend Overrides Function GetInternalStructuralBlocks(block As TryBlockSyntax) As SyntaxList(Of CatchBlockSyntax)
            Return block.CatchBlocks
        End Function

        Friend Overrides Function GetBannerTextOfInternalStructuralBlock(InnerBlock As CatchBlockSyntax) As String
            Return If(InnerBlock IsNot Nothing,
                      If(InnerBlock.CatchStatement IsNot Nothing, InnerBlock.CatchStatement.ToString, String.Empty),
                      String.Empty)
        End Function

        Friend Overrides Function GetEpilogueBlockOutlining(block As TryBlockSyntax, cancellationToken As Threading.CancellationToken) As BlockSpan?
            With block
                If (block Is Nothing) OrElse
                   (.FinallyBlock Is Nothing OrElse .FinallyBlock.IsMissing) OrElse
                   (.EndTryStatement Is Nothing OrElse .EndTryStatement.IsMissing) Then
                    Return Nothing
                End If
                Return GetBlockSpan(.FinallyBlock, .FinallyBlock.FinallyStatement, .EndTryStatement, .FinallyBlock.FinallyStatement.FinallyKeyword.Text, IgnoreHeader:=False)
            End With
        End Function

        Friend Overrides Function GetEpilogueBlock(block As TryBlockSyntax) As FinallyBlockSyntax
            Return block.FinallyBlock
        End Function

        Friend Overrides Function GetEnd_XXX_Statement(block As TryBlockSyntax) As EndBlockStatementSyntax
            Return block.EndTryStatement
        End Function

    End Class
End Namespace
