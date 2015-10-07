' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            Dim rewrittenTryBlock = RewriteTryBlock(node.TryBlock)
            Dim rewrittenCatchBlocks = VisitList(node.CatchBlocks)
            Dim rewrittenFinally = RewriteFinallyBlock(node.FinallyBlockOpt)

            Return RewriteTryStatement(node.Syntax, rewrittenTryBlock, rewrittenCatchBlocks, rewrittenFinally, node.ExitLabelOpt)
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
            syntaxNode As VisualBasicSyntaxNode,
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
                    Return tryBlock
                End If
            End If

            Dim newTry As BoundStatement = New BoundTryStatement(syntaxNode, tryBlock, catchBlocks, finallyBlockOpt, exitLabelOpt)

            For Each [catch] In catchBlocks
                ReportErrorsOnCatchBlockHelpers([catch])
            Next

            ' Add a sequence point for End Try
            ' Note that scope the point is outside of Try/Catch/Finally 
            If Me.GenerateDebugInfo Then
                Dim syntax = TryCast(syntaxNode, TryBlockSyntax)

                If syntax IsNot Nothing Then
                    newTry = New BoundStatementList(syntaxNode,
                                                    ImmutableArray.Create(Of BoundStatement)(
                                                        newTry,
                                                        New BoundSequencePoint(syntax.EndTryStatement, Nothing)
                                                    )
                                                )
                End If
            End If

            Return newTry
        End Function

        Private Function RewriteFinallyBlock(node As BoundBlock) As BoundBlock
            If node Is Nothing Then
                Return node
            End If

            Dim newFinally = DirectCast(Visit(node), BoundBlock)

            If GenerateDebugInfo Then
                Dim syntax = TryCast(node.Syntax, FinallyBlockSyntax)

                If syntax IsNot Nothing Then
                    newFinally = PrependWithSequencePoint(newFinally, syntax.FinallyStatement)
                End If
            End If

            Return newFinally
        End Function

        Private Function RewriteTryBlock(node As BoundBlock) As BoundBlock
            Dim newTry = DirectCast(Visit(node), BoundBlock)

            If GenerateDebugInfo Then
                Dim syntax = TryCast(node.Syntax, TryBlockSyntax)

                If syntax IsNot Nothing Then
                    newTry = PrependWithSequencePoint(newTry, syntax.TryStatement)
                End If
            End If

            Return newTry
        End Function

        Public Overrides Function VisitCatchBlock(node As BoundCatchBlock) As BoundNode
            Dim newExceptionSource = VisitExpressionNode(node.ExceptionSourceOpt)

            Dim newFilter = VisitExpressionNode(node.ExceptionFilterOpt)
            Dim newCatchBody As BoundBlock = DirectCast(Visit(node.Body), BoundBlock)

            If GenerateDebugInfo Then
                Dim syntax = TryCast(node.Syntax, CatchBlockSyntax)

                If syntax IsNot Nothing Then
                    If newFilter IsNot Nothing Then
                        ' if we have a filter, we want to stop before the filter expression
                        ' and associate the sequence point with whole Catch statement
                        newFilter = New BoundSequencePointExpression(syntax.CatchStatement,
                                                                     newFilter,
                                                                     newFilter.Type)
                    Else
                        newCatchBody = PrependWithSequencePoint(newCatchBody, syntax.CatchStatement)
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

            ' EnC: We need to insert a hidden sequence point to handle function remapping in case 
            ' the containing method is edited while methods invoked in the condition are being executed.
            Return node.Update(node.LocalOpt,
                               newExceptionSource,
                               errorLineNumber,
                               If(newFilter IsNot Nothing, AddConditionSequencePoint(newFilter, node), Nothing),
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
