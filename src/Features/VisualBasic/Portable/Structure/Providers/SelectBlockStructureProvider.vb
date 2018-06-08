' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class SelectBlockStructureProvider
        Inherits BlockStructureProvider(Of SelectBlockSyntax, SelectStatementSyntax, CaseBlockSyntax, CaseBlockSyntax, EndBlockStatementSyntax)

        Friend Sub New(IncludeAdditionalInternalSpans As Boolean)
            MyBase.New(IncludeAdditionalInternalSpans)
        End Sub

        Friend Overrides Function GetFullBlockSpan(block As SelectBlockSyntax) As BlockSpan?
            Return GetFullBlockSpan(block, block.SelectStatement)
        End Function

        Friend Overrides Function GetBlockHeader(block As SelectBlockSyntax) As SelectStatementSyntax
            Return block.SelectStatement
        End Function

        Friend Overrides Function GetInnerBlocks(block As SelectBlockSyntax) As SyntaxList(Of CaseBlockSyntax)
            Return block.CaseBlocks
        End Function

        Friend Overrides Function GetEndOfBlockStatement(block As SelectBlockSyntax) As EndBlockStatementSyntax
            Return block.EndSelectStatement
        End Function

        Friend Overrides Function GetInnerBlockBanner(InnerBlock As CaseBlockSyntax) As String
            Dim banner = String.Empty
            ' If the Case Statement contains more than 1 clause, use a shortened form 
            '   First Clause , ...
            ' As to prevent issues if the clauses are over multiple lines, which messed up the collapsed banner.
            Select Case InnerBlock.CaseStatement.Cases.Count
                Case 1
                    banner = InnerBlock.CaseStatement.ToString
                Case Is > 1
                    banner = $"Case {InnerBlock.CaseStatement.Cases(0).ToString},{SpaceEllipsis}"
            End Select
            Return banner
        End Function

    End Class

End Namespace
