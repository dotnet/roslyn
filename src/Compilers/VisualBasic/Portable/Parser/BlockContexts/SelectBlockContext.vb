' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------
' Contains the definition of the BlockContext
'-----------------------------------------------------------------------------
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class SelectBlockContext
        Inherits ExecutableStatementContext

        Private _caseBlocks As SyntaxListBuilder(Of CaseBlockSyntax)

        Friend Sub New(statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(SyntaxKind.SelectBlock, statement, prevContext)

            Debug.Assert(statement.Kind = SyntaxKind.SelectStatement)

            _caseBlocks = _parser._pool.Allocate(Of CaseBlockSyntax)()
        End Sub

        Friend Overrides Function ProcessSyntax(node As VisualBasicSyntaxNode) As BlockContext

            Select Case node.Kind
                Case SyntaxKind.CaseStatement
                    Return New CaseBlockContext(SyntaxKind.CaseBlock, DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.CaseElseStatement
                    Return New CaseBlockContext(SyntaxKind.CaseElseBlock, DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.CaseBlock,
                    SyntaxKind.CaseElseBlock
                    _caseBlocks.Add(DirectCast(node, CaseBlockSyntax))

                Case Else
                    ' TODO - davidsch
                    ' 1. Dev10 the error is reported on the keyword that introduces the statement.  If the error is
                    ' reported here it is easier to mark the whole statement and much harder to just mark the keyword.
                    ' 2. The bad statement is going into an empty case block.  Is this the correct error model.  Do we need
                    ' a BadCaseStatement? Compile a list of all uses of missing statements.

                    node = Parser.ReportSyntaxError(node, ERRID.ERR_ExpectedCase)
                    Dim caseStmt = SyntaxFactory.CaseStatement(InternalSyntaxFactory.MissingKeyword(SyntaxKind.CaseKeyword), New SeparatedSyntaxList(Of CaseClauseSyntax)())
                    Dim context = New CaseBlockContext(SyntaxKind.CaseBlock, caseStmt, Me)
                    ' Previously we explicitly added a missing terminator.  Now, missing terminators are added automatically if a statement
                    ' is added next to a statement.
                    Return context.ProcessSyntax(node)
            End Select

            Return Me
        End Function

        Friend Overrides Function TryLinkSyntax(node As VisualBasicSyntaxNode, ByRef newContext As BlockContext) As LinkResult
            newContext = Nothing

            If KindEndsBlock(node.Kind) Then
                Return UseSyntax(node, newContext)
            End If

            Select Case node.Kind

                Case _
                    SyntaxKind.CaseStatement,
                    SyntaxKind.CaseElseStatement
                    Return UseSyntax(node, newContext)

                ' Reuse SyntaxKind.CaseBlock but do not reuse CaseElseBlock.  These need to be crumbled so that the 
                ' error check for multiple case else statements is done.
                Case SyntaxKind.CaseBlock
                    Return UseSyntax(node, newContext) Or LinkResult.SkipTerminator

                Case Else
                    ' Don't reuse other statements.  It is always an error and if a block statement is reused then the error is attached to the
                    ' block instead of the statement.
                    Return LinkResult.NotUsed
            End Select
        End Function

        Friend Overrides Function CreateBlockSyntax(endStmt As StatementSyntax) As VisualBasicSyntaxNode

            Debug.Assert(BeginStatement IsNot Nothing)
            Dim beginBlockStmt As SelectStatementSyntax = Nothing
            Dim endBlockStmt As EndBlockStatementSyntax = DirectCast(endStmt, EndBlockStatementSyntax)
            GetBeginEndStatements(beginBlockStmt, endBlockStmt)

            Dim result = SyntaxFactory.SelectBlock(beginBlockStmt, _caseBlocks.ToList, endBlockStmt)
            _parser._pool.Free(_caseBlocks)
            FreeStatements()
            Return result
        End Function

    End Class

End Namespace
