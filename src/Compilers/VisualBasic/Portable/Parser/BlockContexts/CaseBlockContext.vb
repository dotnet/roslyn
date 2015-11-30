﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------
' Contains the definition of the BlockContext
'-----------------------------------------------------------------------------

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class CaseBlockContext
        Inherits ExecutableStatementContext

        Friend Sub New(contextKind As SyntaxKind, statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(contextKind, statement, prevContext)

            Debug.Assert((contextKind = SyntaxKind.CaseBlock AndAlso statement.Kind = SyntaxKind.CaseStatement) OrElse
                              (contextKind = SyntaxKind.CaseElseBlock AndAlso statement.Kind = SyntaxKind.CaseElseStatement))
        End Sub

        Friend Overrides Function ProcessSyntax(node As VisualBasicSyntaxNode) As BlockContext

            Select Case node.Kind
                Case SyntaxKind.CaseStatement, SyntaxKind.CaseElseStatement
                    'TODO - In Dev11 this error is reported on the case keyword and not the whole statement
                    If BlockKind = SyntaxKind.CaseElseBlock Then
                        node = Parser.ReportSyntaxError(node, ERRID.ERR_CaseAfterCaseElse)
                    End If
                    Dim context = PrevBlock.ProcessSyntax(CreateBlockSyntax(Nothing))
                    Debug.Assert(context Is PrevBlock)
                    Return context.ProcessSyntax(node)
            End Select

            Return MyBase.ProcessSyntax(node)
        End Function

        Friend Overrides Function TryLinkSyntax(node As VisualBasicSyntaxNode, ByRef newContext As BlockContext) As LinkResult
            newContext = Nothing
            Select Case node.Kind

                Case _
                    SyntaxKind.CaseStatement,
                    SyntaxKind.CaseElseStatement

                    Return UseSyntax(node, newContext)

                Case Else
                    Return MyBase.TryLinkSyntax(node, newContext)
            End Select
        End Function

        Friend Overrides Function CreateBlockSyntax(endStmt As StatementSyntax) As VisualBasicSyntaxNode
            Debug.Assert(endStmt Is Nothing)
            Debug.Assert(BeginStatement IsNot Nothing)

            Dim result As VisualBasicSyntaxNode
            If BlockKind = SyntaxKind.CaseBlock Then
                result = SyntaxFactory.CaseBlock(DirectCast(BeginStatement, CaseStatementSyntax), Body())
            Else
                result = SyntaxFactory.CaseElseBlock(DirectCast(BeginStatement, CaseStatementSyntax), Body())
            End If

            FreeStatements()

            Return result
        End Function

        Friend Overrides Function EndBlock(endStmt As StatementSyntax) As BlockContext
            Dim blockSyntax = CreateBlockSyntax(Nothing)
            Dim context = PrevBlock.ProcessSyntax(blockSyntax)
            Debug.Assert(context Is PrevBlock)
            Return PrevBlock.EndBlock(endStmt)
        End Function

    End Class

End Namespace
