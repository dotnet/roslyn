' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains the definition of the BlockContext
'-----------------------------------------------------------------------------
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class IfBlockContext
        Inherits ExecutableStatementContext

        Private ReadOnly _elseIfBlocks As SyntaxListBuilder(Of ElseIfBlockSyntax)
        Private _optionalElseBlock As ElseBlockSyntax

        Friend Sub New(statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(SyntaxKind.MultiLineIfBlock, statement, prevContext)

            Debug.Assert(statement.Kind = SyntaxKind.IfStatement OrElse
                         (statement.Kind = SyntaxKind.ElseIfStatement AndAlso PrevBlock.BlockKind = SyntaxKind.SingleLineIfStatement))

            _elseIfBlocks = _parser._pool.Allocate(Of ElseIfBlockSyntax)()
        End Sub

        Friend Overrides Function ProcessSyntax(node As VisualBasicSyntaxNode) As BlockContext

            Select Case node.Kind
                Case SyntaxKind.ElseIfStatement

                    Return New IfPartContext(SyntaxKind.ElseIfBlock, DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.ElseIfBlock
                    _elseIfBlocks.Add(DirectCast(node, ElseIfBlockSyntax))

                Case SyntaxKind.ElseStatement

                    Return New IfPartContext(SyntaxKind.ElseBlock, DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.ElseBlock
                    _optionalElseBlock = DirectCast(node, ElseBlockSyntax)

                Case Else
                    Return MyBase.ProcessSyntax(node)
            End Select

            Return Me
        End Function

        Friend Overrides Function TryLinkSyntax(node As VisualBasicSyntaxNode, ByRef newContext As BlockContext) As LinkResult
            newContext = Nothing
            Select Case node.Kind

                Case _
                   SyntaxKind.ElseIfStatement,
                   SyntaxKind.ElseStatement
                    Return UseSyntax(node, newContext)

                Case _
                    SyntaxKind.ElseIfBlock,
                    SyntaxKind.ElseBlock
                    ' Skip terminator because these are not statements
                    Return UseSyntax(node, newContext) Or LinkResult.SkipTerminator

                Case Else
                    Return MyBase.TryLinkSyntax(node, newContext)
            End Select
        End Function

        Friend Overrides Function CreateBlockSyntax(endStmt As StatementSyntax) As VisualBasicSyntaxNode
            Debug.Assert(BeginStatement IsNot Nothing)

            Dim begin As StatementSyntax = BeginStatement

            If endStmt Is Nothing Then
                begin = Parser.ReportSyntaxError(begin, ERRID.ERR_ExpectedEndIf)
                endStmt = SyntaxFactory.EndIfStatement(InternalSyntaxFactory.MissingKeyword(SyntaxKind.EndKeyword), InternalSyntaxFactory.MissingKeyword(SyntaxKind.IfKeyword))
            End If

            Dim result = SyntaxFactory.MultiLineIfBlock(DirectCast(begin, IfStatementSyntax), Body(), _elseIfBlocks.ToList(), _optionalElseBlock, DirectCast(endStmt, EndBlockStatementSyntax))

            _parser._pool.Free(_elseIfBlocks)
            FreeStatements()

            Return result
        End Function

        Friend Overrides Function EndBlock(statement As StatementSyntax) As BlockContext
            Dim blockSyntax = CreateBlockSyntax(statement)
            Return PrevBlock.ProcessSyntax(blockSyntax)
        End Function

    End Class

End Namespace
