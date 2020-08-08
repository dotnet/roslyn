' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
'-----------------------------------------------------------------------------
' Contains the definition of the BlockContext
'-----------------------------------------------------------------------------

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class DoLoopBlockContext
        Inherits ExecutableStatementContext

        Friend Sub New(statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(If(DirectCast(statement, DoStatementSyntax).WhileOrUntilClause Is Nothing,
                          SyntaxKind.SimpleDoLoopBlock,
                          SyntaxKind.DoWhileLoopBlock),
                       statement,
                       prevContext)
        End Sub

        Friend Overrides Function CreateBlockSyntax(statement As StatementSyntax) As VisualBasicSyntaxNode
            Dim doStmt As DoStatementSyntax = Nothing
            Dim loopStmt As LoopStatementSyntax = DirectCast(statement, LoopStatementSyntax)

            GetBeginEndStatements(doStmt, loopStmt)

            Dim kind As SyntaxKind = BlockKind

            If kind = SyntaxKind.DoWhileLoopBlock AndAlso
               loopStmt.WhileOrUntilClause IsNot Nothing Then

                Dim whileUntilClause = loopStmt.WhileOrUntilClause

                ' Error: the loop has a condition in both header and trailer.
                Dim keyword = Parser.ReportSyntaxError(whileUntilClause.WhileOrUntilKeyword, ERRID.ERR_LoopDoubleCondition)
                Dim errors = whileUntilClause.GetDiagnostics

                whileUntilClause = SyntaxFactory.WhileOrUntilClause(whileUntilClause.Kind, DirectCast(keyword, KeywordSyntax), whileUntilClause.Condition)

                If errors IsNot Nothing Then
                    whileUntilClause = DirectCast(whileUntilClause.SetDiagnostics(errors), WhileOrUntilClauseSyntax)
                End If

                loopStmt = SyntaxFactory.LoopStatement(loopStmt.Kind, loopStmt.LoopKeyword, whileUntilClause)
            End If

            If kind = SyntaxKind.SimpleDoLoopBlock AndAlso loopStmt.WhileOrUntilClause IsNot Nothing Then
                ' Set the Do Loop kind now that the bottom is known.
                kind = If(loopStmt.Kind = SyntaxKind.LoopWhileStatement, SyntaxKind.DoLoopWhileBlock, SyntaxKind.DoLoopUntilBlock)
            ElseIf doStmt.WhileOrUntilClause IsNot Nothing Then
                kind = If(doStmt.Kind = SyntaxKind.DoWhileStatement, SyntaxKind.DoWhileLoopBlock, SyntaxKind.DoUntilLoopBlock)
            End If

            Dim result = SyntaxFactory.DoLoopBlock(kind, doStmt, Body(), loopStmt)

            FreeStatements()

            Return result
        End Function

        Friend Overrides Function KindEndsBlock(kind As SyntaxKind) As Boolean
            Select Case kind
                Case SyntaxKind.SimpleLoopStatement,
                     SyntaxKind.LoopWhileStatement,
                     SyntaxKind.LoopUntilStatement
                    Return True
                Case Else
                    Return False
            End Select
        End Function

    End Class

End Namespace
