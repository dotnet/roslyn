' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------
' Contains the definition of the BlockContext
'-----------------------------------------------------------------------------
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class IfBlockContext
        Inherits ExecutableStatementContext

        Private _elseParts As SyntaxListBuilder(Of IfPartSyntax)
        Private _optionalElsePart As ElsePartSyntax

        Friend Sub New(statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(SyntaxKind.MultiLineIfBlock, statement, prevContext)

            Debug.Assert(statement.Kind = SyntaxKind.IfStatement OrElse
                         (statement.Kind = SyntaxKind.ElseIfStatement AndAlso PrevBlock.BlockKind = SyntaxKind.SingleLineIfStatement))

            _elseParts = _parser._pool.Allocate(Of IfPartSyntax)()
        End Sub

        Friend Overrides Function ProcessSyntax(node As VisualBasicSyntaxNode) As BlockContext

            Select Case node.Kind
                Case SyntaxKind.ElseIfStatement
                    Return New IfPartContext(SyntaxKind.ElseIfPart, DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.ElseIfPart
                    _elseParts.Add(DirectCast(node, IfPartSyntax))

                Case SyntaxKind.ElsePart
                    _optionalElsePart = DirectCast(node, ElsePartSyntax)

                Case Else
                    If node.Kind = SyntaxKind.ElseStatement Then
                        Return New IfPartContext(SyntaxKind.ElsePart, DirectCast(node, StatementSyntax), Me)
                    End If

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
                    SyntaxKind.ElseIfPart,
                    SyntaxKind.ElsePart
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

            Dim ifPart = SyntaxFactory.IfPart(DirectCast(begin, IfStatementSyntax), Body())

            Dim result = SyntaxFactory.MultiLineIfBlock(ifPart, _elseParts.ToList, _optionalElsePart, DirectCast(endStmt, EndBlockStatementSyntax))

            _parser._pool.Free(_elseParts)
            FreeStatements()

            Return result
        End Function

        Friend Overrides Function EndBlock(statement As StatementSyntax) As BlockContext
            Dim blockSyntax = CreateBlockSyntax(statement)
            Return PrevBlock.ProcessSyntax(blockSyntax)
        End Function

    End Class

End Namespace