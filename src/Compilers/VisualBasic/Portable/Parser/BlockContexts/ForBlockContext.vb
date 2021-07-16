' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains the definition of the BlockContext
'-----------------------------------------------------------------------------
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class ForBlockContext
        Inherits ExecutableStatementContext

        Private Shared ReadOnly s_emptyNextStatement As NextStatementSyntax

        Shared Sub New()
            s_emptyNextStatement = InternalSyntaxFactory.NextStatement(InternalSyntaxFactory.MissingKeyword(SyntaxKind.NextKeyword), Nothing)
        End Sub

        Friend Sub New(statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(If(statement.Kind = SyntaxKind.ForStatement, SyntaxKind.ForBlock, SyntaxKind.ForEachBlock), statement, prevContext)

            Debug.Assert(statement.Kind = SyntaxKind.ForStatement OrElse statement.Kind = SyntaxKind.ForEachStatement)
        End Sub

        Friend Overrides Function CreateBlockSyntax(endStmt As StatementSyntax) As VisualBasicSyntaxNode

            Dim beginStmt As StatementSyntax = Nothing
            Dim nextStmt = DirectCast(endStmt, NextStatementSyntax)
            GetBeginEndStatements(beginStmt, nextStmt)

            Debug.Assert(BeginStatement IsNot Nothing)

            If endStmt Is s_emptyNextStatement Then
                ' This means that this block was closed by a next statement with variables. i.e. next i,j
                ' In that case, don't create a missing next just set the next statement to nothing.
                nextStmt = Nothing
            End If

            Dim result As VisualBasicSyntaxNode
            If BlockKind = SyntaxKind.ForBlock Then
                result = SyntaxFactory.ForBlock(DirectCast(beginStmt, ForStatementSyntax), Body(), nextStmt)
            Else
                result = SyntaxFactory.ForEachBlock(DirectCast(beginStmt, ForEachStatementSyntax), Body(), nextStmt)
            End If

            FreeStatements()

            Return result
        End Function

        Friend Overrides Function EndBlock(endStmt As StatementSyntax) As BlockContext

            Dim context As BlockContext = Me

            ' End this For block 
            Dim blockSyntax = context.CreateBlockSyntax(endStmt)
            context = context.PrevBlock
            context = context.ProcessSyntax(blockSyntax)

            If endStmt IsNot Nothing Then
                Dim nextStmt As NextStatementSyntax = DirectCast(endStmt, NextStatementSyntax)

                For i = 2 To nextStmt.ControlVariables.Count
                    If context Is Nothing Then
                        Exit For
                    End If

                    If context.BlockKind <> SyntaxKind.ForBlock AndAlso context.BlockKind <> SyntaxKind.ForEachBlock Then
                        Exit For
                    End If

                    blockSyntax = context.CreateBlockSyntax(s_emptyNextStatement)
                    context = context.PrevBlock
                    context = context.ProcessSyntax(blockSyntax)
                Next
            End If

            Return context
        End Function

    End Class

End Namespace
