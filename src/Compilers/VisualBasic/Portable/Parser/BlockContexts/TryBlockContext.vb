' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains the definition of the BlockContext
'-----------------------------------------------------------------------------
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class TryBlockContext
        Inherits ExecutableStatementContext

        Private ReadOnly _catchParts As SyntaxListBuilder(Of CatchBlockSyntax)
        Private _optionalFinallyPart As FinallyBlockSyntax

        Friend Sub New(statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(SyntaxKind.TryBlock, statement, prevContext)

            Debug.Assert(statement.Kind = SyntaxKind.TryStatement)

            _catchParts = _parser._pool.Allocate(Of CatchBlockSyntax)()

        End Sub

        Friend Overrides Function ProcessSyntax(node As VisualBasicSyntaxNode) As BlockContext

            Select Case node.Kind
                Case SyntaxKind.CatchStatement
                    Return New CatchPartContext(DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.FinallyStatement
                    Return New FinallyPartContext(DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.CatchBlock
                    _catchParts.Add(DirectCast(node, CatchBlockSyntax))

                Case SyntaxKind.FinallyBlock
                    _optionalFinallyPart = DirectCast(node, FinallyBlockSyntax)

                Case Else
                    Return MyBase.ProcessSyntax(node)
            End Select

            Return Me
        End Function

        Friend Overrides Function TryLinkSyntax(node As VisualBasicSyntaxNode, ByRef newContext As BlockContext) As LinkResult
            newContext = Nothing
            Select Case node.Kind

                Case _
                    SyntaxKind.CatchStatement,
                    SyntaxKind.FinallyStatement
                    Return UseSyntax(node, newContext)

                Case _
                    SyntaxKind.CatchBlock,
                    SyntaxKind.FinallyBlock
                    ' Skip terminator because these are not statements
                    Return UseSyntax(node, newContext) Or LinkResult.SkipTerminator

                Case Else
                    Return MyBase.TryLinkSyntax(node, newContext)
            End Select
        End Function

        Friend Overrides Function CreateBlockSyntax(endStmt As StatementSyntax) As VisualBasicSyntaxNode

            Debug.Assert(BeginStatement IsNot Nothing)
            Dim beginStmt As TryStatementSyntax = DirectCast(BeginStatement, TryStatementSyntax)

            If endStmt Is Nothing Then
                beginStmt = Parser.ReportSyntaxError(beginStmt, ERRID.ERR_ExpectedEndTry)
                endStmt = SyntaxFactory.EndTryStatement(InternalSyntaxFactory.MissingKeyword(SyntaxKind.EndKeyword), InternalSyntaxFactory.MissingKeyword(SyntaxKind.TryKeyword))
            End If

            Dim result = SyntaxFactory.TryBlock(beginStmt, Body(), _catchParts.ToList(), _optionalFinallyPart, DirectCast(endStmt, EndBlockStatementSyntax))

            _parser._pool.Free(_catchParts)
            FreeStatements()

            Return result
        End Function

        Friend Overrides Function EndBlock(statement As StatementSyntax) As BlockContext
            Dim blockSyntax = CreateBlockSyntax(statement)
            Return PrevBlock.ProcessSyntax(blockSyntax)
        End Function

    End Class

End Namespace
