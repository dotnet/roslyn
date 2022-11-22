' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
'-----------------------------------------------------------------------------
' Contains the definition of the BlockContext
'-----------------------------------------------------------------------------

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class SingleLineIfBlockContext
        Inherits SingleLineIfOrElseBlockContext

        Private _optionalElseClause As SingleLineElseClauseSyntax
        Private _haveElseClause As Boolean

        Friend Sub New(statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(SyntaxKind.SingleLineIfStatement, statement, prevContext)

            Debug.Assert(statement.Kind = SyntaxKind.IfStatement)
            Debug.Assert(DirectCast(statement, IfStatementSyntax).ThenKeyword IsNot Nothing)
        End Sub

        Friend Overrides Function ProcessSyntax(node As VisualBasicSyntaxNode) As BlockContext
            Select Case node.Kind
                Case SyntaxKind.IfStatement
                    Dim ifStmt = DirectCast(node, IfStatementSyntax)
                    ' A single line if has a "then" on the line and is not followed by a ":", EOL or EOF.
                    ' It is OK for else to follow a single line if. i.e
                    '       "if true then if true then else else
                    If ifStmt.ThenKeyword IsNot Nothing AndAlso Not SyntaxFacts.IsTerminator(Parser.CurrentToken.Kind) Then
                        Return New SingleLineIfBlockContext(ifStmt, Me)
                    End If

                Case SyntaxKind.ElseIfStatement
                    'ElseIf is unsupported in line if. End the line if and report expected end of statement per Dev10
                    Add(Parser.ReportSyntaxError(node, ERRID.ERR_ExpectedEOS))
                    Return Me.EndBlock(Nothing)

                Case SyntaxKind.ElseStatement
                    If _haveElseClause Then
                        Throw ExceptionUtilities.Unreachable
                    End If

                    _haveElseClause = True
                    Return New SingleLineElseContext(SyntaxKind.SingleLineElseClause, DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.SingleLineElseClause
                    _optionalElseClause = DirectCast(node, SingleLineElseClauseSyntax)
                    Return Me

                Case SyntaxKind.CatchStatement, SyntaxKind.FinallyStatement
                    ' A Catch or Finally always closes a single line if
                    Add(Parser.ReportSyntaxError(node, If(node.Kind = SyntaxKind.CatchStatement, ERRID.ERR_CatchNoMatchingTry, ERRID.ERR_FinallyNoMatchingTry)))
                    Return Me.EndBlock(Nothing)

            End Select

            Return MyBase.ProcessSyntax(node)
        End Function

        Friend Overrides Function CreateBlockSyntax(endStmt As StatementSyntax) As VisualBasicSyntaxNode
            Debug.Assert(endStmt Is Nothing)
            Return CreateIfBlockSyntax()
        End Function

        Private Function CreateIfBlockSyntax() As SingleLineIfStatementSyntax
            Debug.Assert(BeginStatement IsNot Nothing)

            Dim ifStatement = DirectCast(BeginStatement, IfStatementSyntax)

            Dim result = SyntaxFactory.SingleLineIfStatement(ifStatement.IfKeyword, ifStatement.Condition, ifStatement.ThenKeyword, Body(), _optionalElseClause)

            FreeStatements()

            Return result
        End Function

        Friend Overrides Function EndBlock(statement As StatementSyntax) As BlockContext
            Debug.Assert(statement Is Nothing)
            Dim blockSyntax = CreateIfBlockSyntax()
            Return PrevBlock.ProcessSyntax(blockSyntax)
        End Function

        Friend Overrides Function ProcessStatementTerminator(lambdaContext As BlockContext) As BlockContext
            Dim token = Parser.CurrentToken
            Select Case token.Kind
                Case SyntaxKind.StatementTerminatorToken, SyntaxKind.EndOfFileToken
                    ' A single-line If is terminated at the end of the line.
                    Dim context = EndBlock(Nothing)
                    Return context.ProcessStatementTerminator(lambdaContext)

                Case SyntaxKind.ColonToken
                    ' A colon does not represent the end of the single-line if.
                    Debug.Assert(_statements.Count > 0)
                    Parser.ConsumeColonInSingleLineExpression()
                    Return Me

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(token.Kind)
            End Select
        End Function

        Friend Overrides Function ResyncAndProcessStatementTerminator(statement As StatementSyntax, lambdaContext As BlockContext) As BlockContext
            Dim token = Parser.CurrentToken
            Select Case token.Kind
                Case SyntaxKind.StatementTerminatorToken, SyntaxKind.EndOfFileToken, SyntaxKind.ColonToken
                    Return ProcessStatementTerminator(lambdaContext)

                Case SyntaxKind.ElseKeyword
                    Parser.ConsumedStatementTerminator(allowLeadingMultilineTrivia:=False)
                    Return Me

                Case Else
                    ' Terminated if we've already seen at least one statement.
                    If _statements.Count > 0 Then
                        If TreatOtherAsStatementTerminator Then
                            Return ProcessOtherAsStatementTerminator()
                        End If

                        Return MyBase.ResyncAndProcessStatementTerminator(statement, lambdaContext)
                    End If

                    Parser.ConsumedStatementTerminator(allowLeadingMultilineTrivia:=False)
                    Return Me
            End Select
        End Function

    End Class

End Namespace
