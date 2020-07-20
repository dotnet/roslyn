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

    Friend Class MethodBlockContext
        Inherits ExecutableStatementContext

        Friend Sub New(contextKind As SyntaxKind, statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(contextKind, statement, prevContext)

            Debug.Assert(SyntaxFacts.IsMethodBlock(contextKind) OrElse
                         contextKind = SyntaxKind.ConstructorBlock OrElse
                         contextKind = SyntaxKind.OperatorBlock OrElse
                         SyntaxFacts.IsAccessorBlock(contextKind) OrElse
                         SyntaxFacts.IsSingleLineLambdaExpression(contextKind) OrElse
                         SyntaxFacts.IsMultiLineLambdaExpression(contextKind))
        End Sub

        Friend Overrides Function ProcessSyntax(node As VisualBasicSyntaxNode) As BlockContext

            If Statements.Count = 0 Then
                Dim trailingTrivia = BeginStatement.LastTriviaIfAny()
                If trailingTrivia IsNot Nothing AndAlso
                    trailingTrivia.Kind = SyntaxKind.ColonTrivia Then
                    node = Parser.ReportSyntaxError(node, ERRID.ERR_MethodBodyNotAtLineStart)
                End If
            End If

            Select Case node.Kind
                ' TODO - This check does not catch error for exit in a block in a method. Is this a syntactic or a
                ' semantic error? TODO - Exit checking for Property done in parser but other Exit error checking is done
                ' in semantics.  All "continue" error checking is done in semantics. Remove this check once exit property
                ' is implemented in semantics.

                Case SyntaxKind.ExitPropertyStatement
                    If BlockKind <> SyntaxKind.GetAccessorBlock AndAlso
                       BlockKind <> SyntaxKind.SetAccessorBlock Then
                        node = Parser.ReportSyntaxError(node, ERRID.ERR_ExitPropNot)
                    End If

            End Select

            Return MyBase.ProcessSyntax(node)
        End Function

        Friend Overrides Function CreateBlockSyntax(endStmt As StatementSyntax) As VisualBasicSyntaxNode
            Dim endBlockStmt As EndBlockStatementSyntax = DirectCast(endStmt, EndBlockStatementSyntax)
            Dim result As VisualBasicSyntaxNode

            Select Case BlockKind
                Case SyntaxKind.SubBlock,
                    SyntaxKind.FunctionBlock
                    Dim beginBlockStmt As MethodStatementSyntax = Nothing
                    GetBeginEndStatements(beginBlockStmt, endBlockStmt)
                    result = SyntaxFactory.MethodBlock(BlockKind, beginBlockStmt, BodyWithWeakChildren(), endBlockStmt)

                Case SyntaxKind.ConstructorBlock
                    Dim beginBlockStmt As SubNewStatementSyntax = Nothing
                    GetBeginEndStatements(beginBlockStmt, endBlockStmt)
                    result = SyntaxFactory.ConstructorBlock(beginBlockStmt, BodyWithWeakChildren(), endBlockStmt)

                Case SyntaxKind.GetAccessorBlock,
                    SyntaxKind.SetAccessorBlock,
                    SyntaxKind.AddHandlerAccessorBlock,
                    SyntaxKind.RemoveHandlerAccessorBlock,
                    SyntaxKind.RaiseEventAccessorBlock
                    Dim beginBlockStmt As AccessorStatementSyntax = Nothing
                    GetBeginEndStatements(beginBlockStmt, endBlockStmt)
                    result = SyntaxFactory.AccessorBlock(BlockKind, beginBlockStmt, BodyWithWeakChildren(), endBlockStmt)

                Case SyntaxKind.OperatorBlock
                    Dim beginBlockStmt As OperatorStatementSyntax = Nothing
                    GetBeginEndStatements(beginBlockStmt, endBlockStmt)
                    result = SyntaxFactory.OperatorBlock(beginBlockStmt, BodyWithWeakChildren(), endBlockStmt)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(BlockKind)
            End Select

            FreeStatements()

            Return result
        End Function

        Friend Overrides Function TryLinkSyntax(node As VisualBasicSyntaxNode, ByRef newContext As BlockContext) As LinkResult

            ' Reparse a method block if the Async modifier is added or removed.
            '
            ' This does not handle method headers, which are also parsed differently depending on the async
            ' context. However, incremental parsing is currently not performed inside method headers, so if
            ' the async modifier is added or removed the entire method header is crumbled. If we ever start
            ' reusing subcomponents of method headers during incremental parsing there will need to be similar
            ' checks there. (The incremental parser tests AsyncToSyncMethodDecl and SyncToAsyncMethodDecl will
            ' catch regressions in incremental async method header parsing.)
            '
            ' Reparse a method block if the Iterator modifier is added or removed.
            If Not node.MatchesFactoryContext(Me) Then
                Return LinkResult.NotUsed
            End If

            Return MyBase.TryLinkSyntax(node, newContext)

        End Function
    End Class
End Namespace
