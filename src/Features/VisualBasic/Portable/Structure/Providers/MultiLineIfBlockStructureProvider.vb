' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class MultiLineIfBlockStructureProvider
        Inherits InternalStructureBlockStructureProvider(Of MultiLineIfBlockSyntax, IfStatementSyntax, ElseIfBlockSyntax, ElseBlockSyntax, EndBlockStatementSyntax)

        Friend Sub New(IncludeAdditionalInternalSpans As Boolean)
            MyBase.New(IncludeAdditionalInternalSpans)
        End Sub

        Friend Overrides Function FullStructuralBlockOutlining(block As MultiLineIfBlockSyntax) As BlockSpan?
            Return MyBase.FullStructuralBlockOutlining(block, block.IfStatement)
        End Function

        Friend Overrides Function GetBannerTextOfFullStructuralBlock(block As MultiLineIfBlockSyntax) As IfStatementSyntax
            Return block.IfStatement
        End Function

        Friend Overrides Function GetPreambleOutlining(block As MultiLineIfBlockSyntax, cancellationToken As CancellationToken) As BlockSpan?
            Return If(block Is Nothing OrElse block.IsMissing, Nothing, GetPreambleOutlining(block))
        End Function

        Friend Overrides Function GetFirstStatementOfPreamble(block As MultiLineIfBlockSyntax) As SyntaxNode
            Return If(block.Statements.Count > 0, block.Statements(0), Nothing)
        End Function

        Friend Overrides Function GetInternalStructuralBlocks(block As MultiLineIfBlockSyntax) As SyntaxList(Of ElseIfBlockSyntax)
            Return block.ElseIfBlocks
        End Function

        Friend Overrides Function GetBannerTextOfInternalStructuralBlock(InnerBlock As ElseIfBlockSyntax) As String
            Return InnerBlock.ElseIfStatement.ToString
        End Function

        Friend Overrides Function GetEpilogueBlockOutlining(block As MultiLineIfBlockSyntax, cancellationToken As CancellationToken) As BlockSpan?
            If (block Is Nothing) OrElse
               (block.ElseBlock Is Nothing OrElse block.ElseBlock.IsMissing) OrElse
               (block.EndIfStatement Is Nothing OrElse block.EndIfStatement.IsMissing) Then
                Return Nothing
            End If
            Return GetBlockSpan(block.ElseBlock, block.ElseBlock.ElseStatement, block.EndIfStatement, block.ElseBlock.ElseStatement.ElseKeyword.Text, IgnoreHeader:=False)
        End Function

        Friend Overrides Function GetEpilogueBlock(block As MultiLineIfBlockSyntax) As ElseBlockSyntax
            Return block.ElseBlock
        End Function

        Friend Overrides Function GetEnd_XXX_Statement(block As MultiLineIfBlockSyntax) As EndBlockStatementSyntax
            Return block.EndIfStatement
        End Function

    End Class
End Namespace
