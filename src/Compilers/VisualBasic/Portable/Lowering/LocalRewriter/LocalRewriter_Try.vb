' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitTryStatement(node As BoundTryStatement) As BoundNode
            Debug.Assert(_unstructuredExceptionHandling.Context Is Nothing)

            Dim rewrittenTryBlock = RewriteTryBlock(node)
            Dim rewrittenCatchBlocks = VisitList(node.CatchBlocks)
            Dim rewrittenFinally = RewriteFinallyBlock(node)

            Dim rewritten As BoundStatement = RewriteTryStatement(node.Syntax, rewrittenTryBlock, rewrittenCatchBlocks, rewrittenFinally, node.ExitLabelOpt)

            If Me.Instrument(node) Then
                Dim syntax = TryCast(node.Syntax, TryBlockSyntax)

                If syntax IsNot Nothing Then
                    rewritten = _instrumenterOpt.InstrumentTryStatement(node, rewritten)
                End If
            End If

            Return rewritten
        End Function

        ''' <summary>
        ''' Is there any code to execute in the given statement that could have side-effects,
        ''' such as throwing an exception? This implementation is conservative, in the sense
        ''' that it may return true when the statement actually may have no side effects.
        ''' </summary>
        Private Shared Function HasSideEffects(statement As BoundStatement) As Boolean
            If statement Is Nothing Then
                Return False
            End If

            Select Case statement.Kind
                Case BoundKind.NoOpStatement
                    Return False
                Case BoundKind.Block
                    Dim block = DirectCast(statement, BoundBlock)
                    For Each s In block.Statements
                        If HasSideEffects(s) Then
                            Return True
                        End If
                    Next
                    Return False
                Case BoundKind.SequencePoint
                    Dim sequence = DirectCast(statement, BoundSequencePoint)
                    Return HasSideEffects(sequence.StatementOpt)
                Case BoundKind.SequencePointWithSpan
                    Dim sequence = DirectCast(statement, BoundSequencePointWithSpan)
                    Return HasSideEffects(sequence.StatementOpt)
                Case Else
                    Return True
            End Select
        End Function

        Public Function RewriteTryStatement(
            syntaxNode As SyntaxNode,
            tryBlock As BoundBlock,
            catchBlocks As ImmutableArray(Of BoundCatchBlock),
            finallyBlockOpt As BoundBlock,
            exitLabelOpt As LabelSymbol
        ) As BoundStatement

            If Not Me.OptimizationLevelIsDebug Then
                ' When optimizing and the try block has no side effects, we can discard the catch blocks.
                If Not HasSideEffects(tryBlock) Then
                    catchBlocks = ImmutableArray(Of BoundCatchBlock).Empty
                End If

                ' A finally block with no side effects can be omitted.
                If Not HasSideEffects(finallyBlockOpt) Then
                    finallyBlockOpt = Nothing
                End If

                If catchBlocks.IsDefaultOrEmpty AndAlso finallyBlockOpt Is Nothing Then
                    If exitLabelOpt Is Nothing Then
                        Return tryBlock
                    Else
                        ' Ensure implicit label statement is materialized
                        Return New BoundStatementList(syntaxNode,
                                                      ImmutableArray.Create(Of BoundStatement)(tryBlock,
                                                      New BoundLabelStatement(syntaxNode, exitLabelOpt)))
                    End If
                End If
            End If

            Dim newTry As BoundStatement = New BoundTryStatement(syntaxNode, tryBlock, catchBlocks, finallyBlockOpt, exitLabelOpt)

            For Each [catch] In catchBlocks
                ReportErrorsOnCatchBlockHelpers([catch])
            Next

            Return newTry
        End Function

        Private Function RewriteFinallyBlock(tryStatement As BoundTryStatement) As BoundBlock
            Dim node As BoundBlock = tryStatement.FinallyBlockOpt

            If node Is Nothing Then
                Return node
            End If

            Dim newFinally = DirectCast(Visit(node), BoundBlock)

            If Instrument(tryStatement) Then
                Dim syntax = TryCast(node.Syntax, FinallyBlockSyntax)

                If syntax IsNot Nothing Then
                    newFinally = PrependWithPrologue(newFinally, _instrumenterOpt.CreateFinallyBlockPrologue(tryStatement))
                End If
            End If

            Return newFinally
        End Function

        Private Function RewriteTryBlock(tryStatement As BoundTryStatement) As BoundBlock
            Dim node As BoundBlock = tryStatement.TryBlock
            Dim newTry = DirectCast(Visit(node), BoundBlock)

            If Instrument(tryStatement) Then
                Dim syntax = TryCast(node.Syntax, TryBlockSyntax)

                If syntax IsNot Nothing Then
                    newTry = PrependWithPrologue(newTry, _instrumenterOpt.CreateTryBlockPrologue(tryStatement))
                End If
            End If

            Return newTry
        End Function

        Public Overrides Function VisitCatchBlock(node As BoundCatchBlock) As BoundNode
            Dim newExceptionSource = VisitExpressionNode(node.ExceptionSourceOpt)

            Dim newFilter = VisitExpressionNode(node.ExceptionFilterOpt)
            Dim newCatchBody As BoundBlock = DirectCast(Visit(node.Body), BoundBlock)

            If Instrument(node) Then
                Dim syntax = TryCast(node.Syntax, CatchBlockSyntax)

                If syntax IsNot Nothing Then
                    If newFilter IsNot Nothing Then
                        ' if we have a filter, we want to stop before the filter expression
                        ' and associate the sequence point with whole Catch statement
                        ' EnC: We need to insert a hidden sequence point to handle function remapping in case 
                        ' the containing method is edited while methods invoked in the condition are being executed.
                        newFilter = _instrumenterOpt.InstrumentCatchBlockFilter(node, newFilter, _currentMethodOrLambda)
                    Else
                        newCatchBody = PrependWithPrologue(newCatchBody, _instrumenterOpt.CreateCatchBlockPrologue(node))
                    End If
                End If
            End If

            Dim errorLineNumber As BoundExpression = Nothing

            If node.ErrorLineNumberOpt IsNot Nothing Then
                Debug.Assert(_currentLineTemporary Is Nothing)
                Debug.Assert((Me._flags And RewritingFlags.AllowCatchWithErrorLineNumberReference) <> 0)

                errorLineNumber = VisitExpressionNode(node.ErrorLineNumberOpt)

            ElseIf _currentLineTemporary IsNot Nothing AndAlso _currentMethodOrLambda Is _topMethod Then
                errorLineNumber = New BoundLocal(node.Syntax, _currentLineTemporary, isLValue:=False, type:=_currentLineTemporary.Type)
            End If

            Return node.Update(node.LocalOpt,
                               newExceptionSource,
                               errorLineNumber,
                               newFilter,
                               newCatchBody,
                               node.IsSynthesizedAsyncCatchAll)
        End Function

        Private Sub ReportErrorsOnCatchBlockHelpers(node As BoundCatchBlock)
            ' when starting/finishing any code associated with an exception handler (including exception filters)
            ' we need to call SetProjectError/ClearProjectError

            ' NOTE: we do not inject the helper calls via a rewrite. 
            ' SetProjectError is called with implicit argument on the stack and cannot be expressed in the tree.
            ' ClearProjectError could be added as a rewrite, but for similarity with SetProjectError we will do it in IL gen too.
            ' we will however check for the presence of the helpers and complain here if we cannot find them.

            ' TODO: when building VB runtime, this check is unnecessary as we should not emit the helpers.
            Dim setProjectError As WellKnownMember = If(node.ErrorLineNumberOpt Is Nothing,
                                                        WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__SetProjectError,
                                                        WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__SetProjectError_Int32)

            Dim setProjectErrorMethod As MethodSymbol = DirectCast(Compilation.GetWellKnownTypeMember(setProjectError), MethodSymbol)
            ReportMissingOrBadRuntimeHelper(node, setProjectError, setProjectErrorMethod)

            If node.ExceptionFilterOpt Is Nothing OrElse node.ExceptionFilterOpt.Kind <> BoundKind.UnstructuredExceptionHandlingCatchFilter Then
                Const clearProjectError As WellKnownMember = WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__ClearProjectError
                Dim clearProjectErrorMethod = DirectCast(Compilation.GetWellKnownTypeMember(clearProjectError), MethodSymbol)
                ReportMissingOrBadRuntimeHelper(node, clearProjectError, clearProjectErrorMethod)
            End If

        End Sub
    End Class
End Namespace
