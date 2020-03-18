' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend MustInherit Class SingleLineIfOrElseBlockContext
        Inherits ExecutableStatementContext

        Protected Sub New(kind As SyntaxKind, statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(kind, statement, prevContext)
        End Sub

        Friend Overrides ReadOnly Property IsSingleLine As Boolean
            Get
                Return True
            End Get
        End Property

        Protected ReadOnly Property TreatOtherAsStatementTerminator As Boolean
            Get
                ' Is this line-If, or immediately enclosing line-If(s), 
                ' a body of a single-line statement lambda. Then the token 
                ' terminates the lambda
                Dim parentContext As BlockContext = PrevBlock

                Do
                    Select Case parentContext.BlockKind
                        Case SyntaxKind.SingleLineElseClause,
                             SyntaxKind.SingleLineIfStatement
                            parentContext = parentContext.PrevBlock

                        Case SyntaxKind.SingleLineSubLambdaExpression
                            Return True

                        Case Else
                            Return False
                    End Select
                Loop
            End Get
        End Property

        Protected Function ProcessOtherAsStatementTerminator() As BlockContext
            Dim context = EndBlock(Nothing)

            Do
                Select Case context.BlockKind
                    Case SyntaxKind.SingleLineElseClause,
                         SyntaxKind.SingleLineIfStatement
                        context = context.EndBlock(Nothing)

                    Case SyntaxKind.SingleLineSubLambdaExpression
                        ' This will force termination of the single line lambda
                        Return context.PrevBlock

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(context.BlockKind)
                End Select
            Loop
        End Function

    End Class

End Namespace
