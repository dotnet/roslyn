' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend NotInheritable Class SelectBlockStructureProvider
        Inherits InternalStructureBlockStructureProvider(Of
            SelectBlockSyntax,
            SelectStatementSyntax,
            CaseBlockSyntax,
            SyntaxNode,
            SyntaxNode,
            EndBlockStatementSyntax)

        Friend Overrides Function HeaderOfFullBlock(selectBlock As SelectBlockSyntax) As SelectStatementSyntax
            Return selectBlock.SelectStatement
        End Function

        Friend Overrides Function GetInnerBlocks(selectBlock As SelectBlockSyntax) As SyntaxList(Of CaseBlockSyntax)
            Return selectBlock.CaseBlocks
        End Function

        Friend Overrides Function InnerBlock_Text(caseBlock As CaseBlockSyntax) As String
            Dim banner = String.Empty
            ' If the Case Statement contains more than 1 clause, use a shortened form 
            '   First Clause , ...
            ' As to prevent issues if the clauses are over multiple lines, which messed up the collapsed banner.
            Select Case caseBlock.CaseStatement.Cases.Count
                Case 1
                    banner = caseBlock.CaseStatement.ToString()
                Case Is > 1
                    banner = $"Case {caseBlock.CaseStatement.Cases(0).ToString},{SpaceEllipsis}"
            End Select

            Return banner
        End Function

        Friend Overrides Function EndOfBlockStatement(selectBlock As SelectBlockSyntax) As EndBlockStatementSyntax
            Return selectBlock.EndSelectStatement
        End Function
    End Class
End Namespace
