' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
'-----------------------------------------------------------------------------
' Contains the definition of the BlockContext
'-----------------------------------------------------------------------------

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class SingleLineElseContext
        Inherits SingleLineIfOrElseBlockContext

        Friend Sub New(kind As SyntaxKind, statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(kind, statement, prevContext)

            Debug.Assert(kind = SyntaxKind.SingleLineElseClause)
        End Sub

        Friend Overrides Function ProcessSyntax(node As VisualBasicSyntaxNode) As BlockContext

            Select Case node.Kind
                Case SyntaxKind.IfStatement
                    ' Single line if statements can always open a new context. 
                    ' A single line if has a "then" on the line and is not followed by a ":", EOL or EOF.
                    ' Multi line if's can only open a new context if they are followed by ":"
                    Dim ifStmt = DirectCast(node, IfStatementSyntax)

                    ' single line if 
                    If ifStmt.ThenKeyword IsNot Nothing AndAlso Not SyntaxFacts.IsTerminator(Parser.CurrentToken.Kind) Then
                        Return MyBase.ProcessSyntax(node)
                    End If

                ' multi-line if handled after select

                Case SyntaxKind.ElseIfStatement
                    ' ElseIf ends the context. This is an error case. Let the outer context process them.
                    ' Previously we explicitly added a missing terminator.  Now, terminators are added automatically 
                    ' if a statement is added next to a statement.
                    Return EndBlock(Nothing).ProcessSyntax(node)

                Case SyntaxKind.CatchStatement, SyntaxKind.FinallyStatement
                    ' A Catch or Finally always closes a single line else
                    Add(Parser.ReportSyntaxError(node, If(node.Kind = SyntaxKind.CatchStatement, ERRID.ERR_CatchNoMatchingTry, ERRID.ERR_FinallyNoMatchingTry)))
                    Return Me.EndBlock(Nothing)

            End Select

            Return MyBase.ProcessSyntax(node)
        End Function

        Friend Overrides Function CreateBlockSyntax(endStmt As StatementSyntax) As VisualBasicSyntaxNode
            Debug.Assert(endStmt Is Nothing)
            Return CreateElseBlockSyntax()
        End Function

        Private Function CreateElseBlockSyntax() As SingleLineElseClauseSyntax
            Debug.Assert(BeginStatement IsNot Nothing)

            Dim elseStatement = DirectCast(BeginStatement, ElseStatementSyntax)

            Dim result = SyntaxFactory.SingleLineElseClause(elseStatement.ElseKeyword, OptionalBody())

            FreeStatements()

            Return result
        End Function

        Friend Overrides Function EndBlock(statement As StatementSyntax) As BlockContext
            Debug.Assert(statement Is Nothing)

            Dim context = PrevBlock.ProcessSyntax(CreateElseBlockSyntax())
            Debug.Assert(context Is PrevBlock)

            Return context.EndBlock(Nothing)
        End Function

        Friend Overrides Function ProcessStatementTerminator(lambdaContext As BlockContext) As BlockContext
            Dim token = Parser.CurrentToken
            Select Case token.Kind
                Case SyntaxKind.StatementTerminatorToken, SyntaxKind.EndOfFileToken
EndBlock:
                    ' A single-line Else is terminated at the end of the line.
                    Dim context = EndBlock(Nothing)
                    Return context.ProcessStatementTerminator(lambdaContext)

                Case SyntaxKind.ColonToken
                    ' A colon only represents the end of the single-line Else
                    ' if there are no statements before the colon.
                    If _statements.Count > 0 Then
                        Parser.ConsumeColonInSingleLineExpression()
                        Return Me
                    End If
                    GoTo EndBlock

                Case SyntaxKind.ElseKeyword
                    ' Terminated by Else from containing block.
                    Parser.ConsumedStatementTerminator(allowLeadingMultilineTrivia:=False)
                    Return ProcessElseAsStatementTerminator()

                Case Else
                    ' Terminated if we've already seen at least one statement.
                    If _statements.Count > 0 Then
                        Return ProcessOtherAsStatementTerminator()
                    End If
                    ' Terminated if the next token can follow a statement.
                    If Parser.CanFollowStatementButIsNotSelectFollowingExpression(token) Then
                        Return ProcessOtherAsStatementTerminator()
                    End If
                    Parser.ConsumedStatementTerminator(allowLeadingMultilineTrivia:=False)
                    Return Me

            End Select
        End Function

        Friend Overrides Function ProcessElseAsStatementTerminator() As BlockContext
            Dim context = EndBlock(Nothing)
            Return context.ProcessElseAsStatementTerminator()
        End Function

        Friend Overrides Function ProcessOtherAsStatementTerminator() As BlockContext
            Dim context = EndBlock(Nothing)
            Return context.ProcessOtherAsStatementTerminator()
        End Function

    End Class

End Namespace
