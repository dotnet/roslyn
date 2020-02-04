' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
'-----------------------------------------------------------------------------
' Contains the definition of the BlockContext
'-----------------------------------------------------------------------------

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class FinallyPartContext
        Inherits ExecutableStatementContext

        Friend Sub New(statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(SyntaxKind.FinallyBlock, statement, prevContext)

            Debug.Assert(statement.Kind = SyntaxKind.FinallyStatement)
        End Sub

        Friend Overrides Function ProcessSyntax(node As VisualBasicSyntaxNode) As BlockContext

            Debug.Assert(node IsNot Nothing)
            Select Case node.Kind
                Case SyntaxKind.CatchStatement
                    'TODO - Davidsch
                    ' In Dev10 this is reported on the keyword not the statement
                    Add(Parser.ReportSyntaxError(node, ERRID.ERR_CatchAfterFinally))
                    Return Me

                Case SyntaxKind.FinallyStatement
                    'TODO - Davidsch
                    ' In Dev10 this is reported on the keyword not the statement
                    Add(Parser.ReportSyntaxError(node, ERRID.ERR_FinallyAfterFinally))
                    Return Me
            End Select

            Return MyBase.ProcessSyntax(node)
        End Function

        Friend Overrides Function TryLinkSyntax(node As VisualBasicSyntaxNode, ByRef newContext As BlockContext) As LinkResult
            newContext = Nothing
            Select Case node.Kind

                Case _
                    SyntaxKind.CatchStatement,
                    SyntaxKind.FinallyStatement
                    Return UseSyntax(node, newContext)

                Case Else
                    Return MyBase.TryLinkSyntax(node, newContext)
            End Select
        End Function

        Friend Overrides Function CreateBlockSyntax(statement As StatementSyntax) As VisualBasicSyntaxNode
            Debug.Assert(statement Is Nothing)
            Debug.Assert(BeginStatement IsNot Nothing)

            Dim result = SyntaxFactory.FinallyBlock(DirectCast(BeginStatement, FinallyStatementSyntax), Body())

            FreeStatements()

            Return result
        End Function

        Friend Overrides Function EndBlock(statement As StatementSyntax) As BlockContext
            Dim context = PrevBlock.ProcessSyntax(CreateBlockSyntax(Nothing))
            Debug.Assert(context Is PrevBlock)
            Return context.EndBlock(statement)
        End Function

    End Class

End Namespace
