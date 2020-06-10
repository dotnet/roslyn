' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains the definition of the BlockContext
'-----------------------------------------------------------------------------
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class SingleLineLambdaContext
        Inherits MethodBlockContext

        Friend Sub New(statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(If(statement.Kind = SyntaxKind.FunctionLambdaHeader, SyntaxKind.SingleLineFunctionLambdaExpression, SyntaxKind.SingleLineSubLambdaExpression), statement, prevContext)

            Debug.Assert(statement.Kind = SyntaxKind.FunctionLambdaHeader OrElse statement.Kind = SyntaxKind.SubLambdaHeader)
            Debug.Assert(SyntaxFacts.IsSingleLineLambdaExpression(BlockKind))
        End Sub

        Friend Overrides ReadOnly Property IsLambda As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides Function CreateBlockSyntax(endStmt As StatementSyntax) As VisualBasicSyntaxNode
            Dim statements = Body()
            Dim statement As VisualBasicSyntaxNode
            Dim reportRequiresSingleStatement As Boolean

            If statements.Count = 0 Then
                statement = InternalSyntaxFactory.EmptyStatement(InternalSyntaxFactory.MissingEmptyToken)
                reportRequiresSingleStatement = True
            Else
                Debug.Assert(statements.Count = 1)
                statement = DirectCast(statements(0), StatementSyntax)
                reportRequiresSingleStatement = Not statement.ContainsDiagnostics() AndAlso Not IsSingleStatement(statement)
            End If

            ' Single line sub ignores the endStmt which may be nothing or the statement that was just added to the sub.
            Dim header = DirectCast(BeginStatement, LambdaHeaderSyntax)
            Dim lambdaExpr = SyntaxFactory.SingleLineLambdaExpression(BlockKind, header, statement)

            If reportRequiresSingleStatement Then
                lambdaExpr = Parser.ReportSyntaxError(lambdaExpr, ERRID.ERR_SubRequiresSingleStatement)
            ElseIf header.Kind = SyntaxKind.FunctionLambdaHeader AndAlso header.Modifiers.Any(SyntaxKind.IteratorKeyword) Then
                lambdaExpr = Parser.ReportSyntaxError(lambdaExpr, ERRID.ERR_BadIteratorExpressionLambda)
            End If

            FreeStatements()

            Return lambdaExpr
        End Function

        Friend Overrides Function EndBlock(endStmt As StatementSyntax) As BlockContext

            'Don't create the lambda block and don't pass it to the previous context.  The previous context is not the
            ' right place to store it because the lambda goes into an expression and not the surrounding statement block.

            Return PrevBlock
        End Function

        Friend Overrides Function ResyncAndProcessStatementTerminator(statement As StatementSyntax, lambdaContext As BlockContext) As BlockContext
            Return ProcessStatementTerminator(lambdaContext)
        End Function

        Friend Overrides Function ProcessStatementTerminator(lambdaContext As BlockContext) As BlockContext
            Dim token = Parser.CurrentToken
            Select Case token.Kind
                Case SyntaxKind.StatementTerminatorToken, SyntaxKind.EndOfFileToken
                    ' A single-line lambda is terminated at the end of the line.

                Case SyntaxKind.ColonToken
                    ' A single-line sub with multiple statements. Report ERR_SubRequiresSingleStatement
                    ' on the first statement and end the sub. If there are no statements, we'll report the
                    ' error, on the entire block, in CreateBlockSyntax instead.
                    If _statements.Count > 0 Then
                        _statements(0) = Parser.ReportSyntaxError(_statements(0), ERRID.ERR_SubRequiresSingleStatement)
                    End If
                    Return EndLambda()

            End Select

            Return PrevBlock
        End Function

        Friend Overrides ReadOnly Property IsSingleLine As Boolean
            Get
                Return True
            End Get
        End Property

        Private Shared Function IsSingleStatement(statement As VisualBasicSyntaxNode) As Boolean
            Select Case statement.Kind
                Case SyntaxKind.EmptyStatement,
                    SyntaxKind.MultiLineIfBlock,
                    SyntaxKind.SimpleDoLoopBlock,
                    SyntaxKind.DoWhileLoopBlock,
                    SyntaxKind.DoUntilLoopBlock,
                    SyntaxKind.DoLoopWhileBlock,
                    SyntaxKind.DoLoopUntilBlock,
                    SyntaxKind.ForBlock,
                    SyntaxKind.ForEachBlock,
                    SyntaxKind.SelectBlock,
                    SyntaxKind.WhileBlock,
                    SyntaxKind.WithBlock,
                    SyntaxKind.SyncLockBlock,
                    SyntaxKind.UsingBlock,
                    SyntaxKind.TryBlock
                    Return False
                Case Else
                    Return True
            End Select
        End Function

    End Class

End Namespace

