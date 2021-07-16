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

    Friend Class NamespaceBlockContext
        Inherits DeclarationContext

        Friend Sub New(statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(SyntaxKind.NamespaceBlock, statement, prevContext)
        End Sub

        Friend Sub New(kind As SyntaxKind, statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(kind, statement, prevContext)

            Debug.Assert(kind = SyntaxKind.CompilationUnit)
        End Sub

        Friend Overrides Function ProcessSyntax(node As VisualBasicSyntaxNode) As BlockContext

            Dim kind As SyntaxKind = node.Kind

            Select Case kind

                Case SyntaxKind.NamespaceStatement
                    Return New NamespaceBlockContext(DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.ModuleStatement
                    Return New TypeBlockContext(SyntaxKind.ModuleBlock, DirectCast(node, StatementSyntax), Me)

                Case _
                    SyntaxKind.NamespaceBlock,
                    SyntaxKind.ModuleBlock
                    ' Handle blocks created by this context.
                    Add(node)
                    Return Me
            End Select

            Return MyBase.ProcessSyntax(node)
        End Function

        Friend Overrides Function TryLinkSyntax(node As VisualBasicSyntaxNode, ByRef newContext As BlockContext) As LinkResult
            newContext = Nothing
            Select Case node.Kind

                Case _
                    SyntaxKind.NamespaceStatement,
                    SyntaxKind.ModuleStatement,
                    SyntaxKind.SubStatement,
                    SyntaxKind.SubNewStatement,
                    SyntaxKind.FunctionStatement,
                    SyntaxKind.OperatorStatement,
                    SyntaxKind.PropertyStatement,
                    SyntaxKind.EventStatement,
                    SyntaxKind.FieldDeclaration

                    Return UseSyntax(node, newContext)

                Case SyntaxKind.ModuleBlock

                    Return UseSyntax(node, newContext, DirectCast(node, TypeBlockSyntax).EndBlockStatement.IsMissing)

                Case SyntaxKind.NamespaceBlock

                    Return UseSyntax(node, newContext, DirectCast(node, NamespaceBlockSyntax).EndNamespaceStatement.IsMissing)

                Case _
                    SyntaxKind.SubBlock,
                    SyntaxKind.ConstructorBlock,
                    SyntaxKind.FunctionBlock,
                    SyntaxKind.OperatorBlock,
                    SyntaxKind.PropertyBlock,
                    SyntaxKind.EventBlock
                    ' These must be crumbled to correctly handle the error case
                    newContext = Me
                    Return LinkResult.Crumble

                Case Else
                    Return MyBase.TryLinkSyntax(node, newContext)
            End Select
        End Function

        Friend Overrides Function CreateBlockSyntax(endStmt As StatementSyntax) As VisualBasicSyntaxNode

            Debug.Assert(BeginStatement IsNot Nothing)
            Dim beginBlockStmt = DirectCast(BeginStatement, NamespaceStatementSyntax)
            Dim endBlockStmt As EndBlockStatementSyntax = DirectCast(endStmt, EndBlockStatementSyntax)
            GetBeginEndStatements(beginBlockStmt, endBlockStmt)

            Dim result = SyntaxFactory.NamespaceBlock(beginBlockStmt, Body(), endBlockStmt)

            FreeStatements()

            Return result
        End Function

    End Class

End Namespace
