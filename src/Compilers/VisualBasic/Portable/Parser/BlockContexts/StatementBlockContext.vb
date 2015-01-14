' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
'-----------------------------------------------------------------------------
' Contains the definition of the BlockContext
'-----------------------------------------------------------------------------

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class StatementBlockContext
        Inherits ExecutableStatementContext

        Friend Sub New(kind As SyntaxKind, statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(kind, statement, prevContext)
        End Sub

        Friend Overrides Function CreateBlockSyntax(statement As StatementSyntax) As VisualBasicSyntaxNode

            Dim endStmt As EndBlockStatementSyntax = DirectCast(statement, EndBlockStatementSyntax)
            Dim result As VisualBasicSyntaxNode
            Select Case BlockKind
                Case SyntaxKind.WhileBlock
                    Dim beginStmt As WhileStatementSyntax = Nothing
                    GetBeginEndStatements(beginStmt, endStmt)
                    result = SyntaxFactory.WhileBlock(beginStmt, Body(), endStmt)

                Case SyntaxKind.WithBlock
                    Dim beginStmt As WithStatementSyntax = Nothing
                    GetBeginEndStatements(beginStmt, endStmt)
                    result = SyntaxFactory.WithBlock(beginStmt, Body(), endStmt)

                Case SyntaxKind.SyncLockBlock
                    Dim beginStmt As SyncLockStatementSyntax = Nothing
                    GetBeginEndStatements(beginStmt, endStmt)
                    result = SyntaxFactory.SyncLockBlock(beginStmt, Body(), endStmt)

                Case SyntaxKind.UsingBlock
                    Dim beginStmt As UsingStatementSyntax = Nothing
                    GetBeginEndStatements(beginStmt, endStmt)
                    result = SyntaxFactory.UsingBlock(beginStmt, Body(), endStmt)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(BlockKind)
            End Select

            FreeStatements()

            Return result
        End Function

    End Class

End Namespace
