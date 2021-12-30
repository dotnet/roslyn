' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
'-----------------------------------------------------------------------------
' Contains the definition of the BlockContext
'-----------------------------------------------------------------------------

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class LambdaContext
        Inherits MethodBlockContext

        Friend Sub New(statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(If(statement.Kind = SyntaxKind.FunctionLambdaHeader, SyntaxKind.MultiLineFunctionLambdaExpression, SyntaxKind.MultiLineSubLambdaExpression), statement, prevContext)

            Debug.Assert(statement.Kind = SyntaxKind.FunctionLambdaHeader OrElse statement.Kind = SyntaxKind.SubLambdaHeader)
            Debug.Assert(SyntaxFacts.IsMultiLineLambdaExpression(BlockKind))
        End Sub

        Friend Overrides ReadOnly Property IsLambda As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides Function CreateBlockSyntax(endStmt As StatementSyntax) As VisualBasicSyntaxNode

            Dim header As LambdaHeaderSyntax = Nothing
            Dim body = Me.Body()

            Dim endBlockStmt As EndBlockStatementSyntax = DirectCast(endStmt, EndBlockStatementSyntax)
            GetBeginEndStatements(header, endBlockStmt)

            Debug.Assert(BeginStatement IsNot Nothing)
            Dim lambdaExpr = SyntaxFactory.MultiLineLambdaExpression(BlockKind, header, body, endBlockStmt)

            FreeStatements()

            Return lambdaExpr
        End Function

        Friend Overrides Function EndBlock(endStmt As StatementSyntax) As BlockContext

            'Don't create the lambda block and don't pass it to the previous context.  The previous context is not the
            ' right place to store it because the lambda goes into an expression and not the surrounding statement block.

            Return PrevBlock
        End Function

        Friend Overrides ReadOnly Property IsSingleLine As Boolean
            Get
                Return False
            End Get
        End Property

    End Class

End Namespace
