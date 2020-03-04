' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' This type is responsible for adding debugging sequence points for the executable code.
    ''' It can be combined with other <see cref="Instrumenter"/>s. Usually, this class should be 
    ''' the root of the chain in order to ensure sound debugging experience for the instrumented code.
    ''' In other words, sequence points are typically applied after all other changes.
    ''' </summary>
    Partial Friend NotInheritable Class DebugInfoInjector
        Inherits CompoundInstrumenter

        ''' <summary>
        ''' A singleton object that performs only one type of instrumentation - addition of debugging sequence points. 
        ''' </summary>
        Public Shared ReadOnly Singleton As New DebugInfoInjector(Instrumenter.NoOp)

        Public Sub New(previous As Instrumenter)
            MyBase.New(previous)
        End Sub

        Private Shared Function MarkStatementWithSequencePoint(original As BoundStatement, rewritten As BoundStatement) As BoundStatement
            Return New BoundSequencePoint(original.Syntax, rewritten)
        End Function

        Public Overrides Function InstrumentExpressionStatement(original As BoundExpressionStatement, rewritten As BoundStatement) As BoundStatement
            Return MarkStatementWithSequencePoint(original, MyBase.InstrumentExpressionStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentStopStatement(original As BoundStopStatement, rewritten As BoundStatement) As BoundStatement
            Return MarkStatementWithSequencePoint(original, MyBase.InstrumentStopStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentEndStatement(original As BoundEndStatement, rewritten As BoundStatement) As BoundStatement
            Return MarkStatementWithSequencePoint(original, MyBase.InstrumentEndStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentContinueStatement(original As BoundContinueStatement, rewritten As BoundStatement) As BoundStatement
            Return MarkStatementWithSequencePoint(original, MyBase.InstrumentContinueStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentExitStatement(original As BoundExitStatement, rewritten As BoundStatement) As BoundStatement
            Return MarkStatementWithSequencePoint(original, MyBase.InstrumentExitStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentGotoStatement(original As BoundGotoStatement, rewritten As BoundStatement) As BoundStatement
            Return MarkStatementWithSequencePoint(original, MyBase.InstrumentGotoStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentLabelStatement(original As BoundLabelStatement, rewritten As BoundStatement) As BoundStatement
            Return MarkStatementWithSequencePoint(original, MyBase.InstrumentLabelStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentRaiseEventStatement(original As BoundRaiseEventStatement, rewritten As BoundStatement) As BoundStatement
            Return MarkStatementWithSequencePoint(original, MyBase.InstrumentRaiseEventStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentReturnStatement(original As BoundReturnStatement, rewritten As BoundStatement) As BoundStatement
            Return MarkStatementWithSequencePoint(original, MyBase.InstrumentReturnStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentThrowStatement(original As BoundThrowStatement, rewritten As BoundStatement) As BoundStatement
            Return MarkStatementWithSequencePoint(original, MyBase.InstrumentThrowStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentOnErrorStatement(original As BoundOnErrorStatement, rewritten As BoundStatement) As BoundStatement
            Return MarkStatementWithSequencePoint(original, MyBase.InstrumentOnErrorStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentResumeStatement(original As BoundResumeStatement, rewritten As BoundStatement) As BoundStatement
            Return MarkStatementWithSequencePoint(original, MyBase.InstrumentResumeStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentAddHandlerStatement(original As BoundAddHandlerStatement, rewritten As BoundStatement) As BoundStatement
            Return MarkStatementWithSequencePoint(original, MyBase.InstrumentAddHandlerStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentRemoveHandlerStatement(original As BoundRemoveHandlerStatement, rewritten As BoundStatement) As BoundStatement
            Return MarkStatementWithSequencePoint(original, MyBase.InstrumentRemoveHandlerStatement(original, rewritten))
        End Function

        Public Overrides Function CreateBlockPrologue(trueOriginal As BoundBlock, original As BoundBlock, ByRef synthesizedLocal As LocalSymbol) As BoundStatement
            Return CreateBlockPrologue(original, MyBase.CreateBlockPrologue(trueOriginal, original, synthesizedLocal))
        End Function

        Public Overrides Function InstrumentTopLevelExpressionInQuery(original As BoundExpression, rewritten As BoundExpression) As BoundExpression
            rewritten = MyBase.InstrumentTopLevelExpressionInQuery(original, rewritten)
            Return New BoundSequencePointExpression(original.Syntax, rewritten, rewritten.Type)
        End Function

        Public Overrides Function InstrumentQueryLambdaBody(original As BoundQueryLambda, rewritten As BoundStatement) As BoundStatement
            rewritten = MyBase.InstrumentQueryLambdaBody(original, rewritten)

            Dim createSequencePoint As SyntaxNode = Nothing
            Dim sequencePointSpan As TextSpan

            Select Case original.LambdaSymbol.SynthesizedKind
                Case SynthesizedLambdaKind.AggregateQueryLambda
                    Dim aggregateClause = DirectCast(original.Syntax.Parent.Parent, AggregateClauseSyntax)

                    If aggregateClause.AggregationVariables.Count = 1 Then
                        ' We are dealing with a simple case of an Aggregate clause - a single aggregate
                        ' function in the Into clause. This lambda is responsible for calculating that
                        ' aggregate function. Actually, it includes all code generated for the entire
                        ' Aggregate clause. We should create sequence point for the entire clause
                        ' rather than sequence points for the top level expressions within the lambda.
                        createSequencePoint = aggregateClause
                        sequencePointSpan = aggregateClause.Span
                    Else
                        ' We should create sequence point that spans from beginning of the Aggregate clause 
                        ' to the beginning of the Into clause because all that code is involved into group calculation.

                        createSequencePoint = aggregateClause
                        If aggregateClause.AdditionalQueryOperators.Count = 0 Then
                            sequencePointSpan = TextSpan.FromBounds(aggregateClause.SpanStart,
                                                                    aggregateClause.Variables.Last.Span.End)
                        Else
                            sequencePointSpan = TextSpan.FromBounds(aggregateClause.SpanStart,
                                                                    aggregateClause.AdditionalQueryOperators.Last.Span.End)
                        End If
                    End If

                Case SynthesizedLambdaKind.LetVariableQueryLambda
                    ' We will apply sequence points to synthesized return statements if they are contained in LetClause
                    Debug.Assert(original.Syntax.Parent.IsKind(SyntaxKind.ExpressionRangeVariable))

                    createSequencePoint = original.Syntax
                    sequencePointSpan = TextSpan.FromBounds(original.Syntax.SpanStart, original.Syntax.Span.End)
            End Select

            If createSequencePoint IsNot Nothing Then
                rewritten = New BoundSequencePointWithSpan(createSequencePoint, rewritten, sequencePointSpan)
            End If

            Return rewritten
        End Function

        Public Overrides Function InstrumentDoLoopEpilogue(original As BoundDoLoopStatement, epilogueOpt As BoundStatement) As BoundStatement
            ' adds EndXXX sequence point to a statement
            ' NOTE: if target statement happens to be a block, then 
            ' sequence point goes inside the block
            ' This ensures that when we stopped on EndXXX, we are still in the block's scope
            ' and can examine locals declared in the block.
            Return New BoundSequencePoint(DirectCast(original.Syntax, DoLoopBlockSyntax).LoopStatement, MyBase.InstrumentDoLoopEpilogue(original, epilogueOpt))
        End Function

        Public Overrides Function CreateSyncLockStatementPrologue(original As BoundSyncLockStatement) As BoundStatement
            ' create a sequence point that contains the whole SyncLock statement as the first reachable sequence point
            ' of the SyncLock statement. 
            Return New BoundSequencePoint(DirectCast(original.Syntax, SyncLockBlockSyntax).SyncLockStatement, MyBase.CreateSyncLockStatementPrologue(original))
        End Function

        Public Overrides Function InstrumentSyncLockObjectCapture(original As BoundSyncLockStatement, rewritten As BoundStatement) As BoundStatement
            Return New BoundSequencePoint(original.LockExpression.Syntax, MyBase.InstrumentSyncLockObjectCapture(original, rewritten))
        End Function

        Public Overrides Function CreateSyncLockExitDueToExceptionEpilogue(original As BoundSyncLockStatement) As BoundStatement
            ' Add a sequence point to highlight the "End SyncLock" syntax in case the body has thrown an exception
            Return New BoundSequencePoint(DirectCast(original.Syntax, SyncLockBlockSyntax).EndSyncLockStatement, MyBase.CreateSyncLockExitDueToExceptionEpilogue(original))
        End Function

        Public Overrides Function CreateSyncLockExitNormallyEpilogue(original As BoundSyncLockStatement) As BoundStatement
            ' Add a sequence point to highlight the "End SyncLock" syntax in case the body has been complete executed and
            ' exited normally
            Return New BoundSequencePoint(DirectCast(original.Syntax, SyncLockBlockSyntax).EndSyncLockStatement, MyBase.CreateSyncLockExitNormallyEpilogue(original))
        End Function

        Public Overrides Function InstrumentWhileEpilogue(original As BoundWhileStatement, epilogueOpt As BoundStatement) As BoundStatement
            Return New BoundSequencePoint(DirectCast(original.Syntax, WhileBlockSyntax).EndWhileStatement, MyBase.InstrumentWhileEpilogue(original, epilogueOpt))
        End Function

        Public Overrides Function InstrumentWhileStatementConditionalGotoStart(original As BoundWhileStatement, ifConditionGotoStart As BoundStatement) As BoundStatement
            Return New BoundSequencePoint(DirectCast(original.Syntax, WhileBlockSyntax).WhileStatement,
                                          MyBase.InstrumentWhileStatementConditionalGotoStart(original, ifConditionGotoStart))
        End Function

        Public Overrides Function InstrumentDoLoopStatementEntryOrConditionalGotoStart(original As BoundDoLoopStatement, ifConditionGotoStartOpt As BoundStatement) As BoundStatement
            Return New BoundSequencePoint(DirectCast(original.Syntax, DoLoopBlockSyntax).DoStatement,
                                          MyBase.InstrumentDoLoopStatementEntryOrConditionalGotoStart(original, ifConditionGotoStartOpt))
        End Function

        Public Overrides Function InstrumentForEachStatementConditionalGotoStart(original As BoundForEachStatement, ifConditionGotoStart As BoundStatement) As BoundStatement
            ' Add hidden sequence point
            Return New BoundSequencePoint(Nothing, MyBase.InstrumentForEachStatementConditionalGotoStart(original, ifConditionGotoStart))
        End Function

        Public Overrides Function InstrumentIfStatementConditionalGoto(original As BoundIfStatement, condGoto As BoundStatement) As BoundStatement
            condGoto = MyBase.InstrumentIfStatementConditionalGoto(original, condGoto)

            Select Case original.Syntax.Kind
                Case SyntaxKind.MultiLineIfBlock
                    Dim asMultiline = DirectCast(original.Syntax, MultiLineIfBlockSyntax)
                    condGoto = New BoundSequencePoint(asMultiline.IfStatement, condGoto)
                Case SyntaxKind.ElseIfBlock
                    Dim asElseIfBlock = DirectCast(original.Syntax, ElseIfBlockSyntax)
                    condGoto = New BoundSequencePoint(asElseIfBlock.ElseIfStatement, condGoto)
                Case SyntaxKind.SingleLineIfStatement
                    Dim asSingleLine = DirectCast(original.Syntax, SingleLineIfStatementSyntax)
                    condGoto = New BoundSequencePointWithSpan(asSingleLine, condGoto, TextSpan.FromBounds(asSingleLine.IfKeyword.SpanStart, asSingleLine.ThenKeyword.EndPosition - 1))
            End Select

            Return condGoto
        End Function

        Public Overrides Function InstrumentIfStatementAfterIfStatement(original As BoundIfStatement, afterIfStatement As BoundStatement) As BoundStatement
            ' Associate afterIf with EndIf
            Return New BoundSequencePoint(DirectCast(original.Syntax, MultiLineIfBlockSyntax).EndIfStatement,
                                          MyBase.InstrumentIfStatementAfterIfStatement(original, afterIfStatement))
        End Function

        Public Overrides Function InstrumentIfStatementConsequenceEpilogue(original As BoundIfStatement, epilogueOpt As BoundStatement) As BoundStatement
            epilogueOpt = MyBase.InstrumentIfStatementConsequenceEpilogue(original, epilogueOpt)

            Dim syntax As VisualBasicSyntaxNode = Nothing
            Select Case original.Syntax.Kind
                Case SyntaxKind.MultiLineIfBlock
                    syntax = DirectCast(original.Syntax, MultiLineIfBlockSyntax).EndIfStatement
                Case SyntaxKind.ElseIfBlock
                    syntax = DirectCast(original.Syntax.Parent, MultiLineIfBlockSyntax).EndIfStatement
                Case SyntaxKind.SingleLineIfStatement
                    ' single line if has no EndIf
                    Return epilogueOpt
            End Select

            Return New BoundSequencePoint(syntax, epilogueOpt)
        End Function

        Public Overrides Function InstrumentIfStatementAlternativeEpilogue(original As BoundIfStatement, epilogueOpt As BoundStatement) As BoundStatement
            Return New BoundSequencePoint(DirectCast(original.AlternativeOpt.Syntax.Parent, MultiLineIfBlockSyntax).EndIfStatement,
                                          MyBase.InstrumentIfStatementAlternativeEpilogue(original, epilogueOpt))
        End Function

        Public Overrides Function CreateIfStatementAlternativePrologue(original As BoundIfStatement) As BoundStatement
            Dim prologue = MyBase.CreateIfStatementAlternativePrologue(original)

            Select Case original.AlternativeOpt.Syntax.Kind
                Case SyntaxKind.ElseBlock
                    prologue = New BoundSequencePoint(DirectCast(original.AlternativeOpt.Syntax, ElseBlockSyntax).ElseStatement, prologue)
                Case SyntaxKind.SingleLineElseClause
                    prologue = New BoundSequencePointWithSpan(original.AlternativeOpt.Syntax, prologue,
                                                              DirectCast(original.AlternativeOpt.Syntax, SingleLineElseClauseSyntax).ElseKeyword.Span)
            End Select

            Return prologue
        End Function

        Public Overrides Function InstrumentDoLoopStatementCondition(original As BoundDoLoopStatement, rewrittenCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Return AddConditionSequencePoint(MyBase.InstrumentDoLoopStatementCondition(original, rewrittenCondition, currentMethodOrLambda), original, currentMethodOrLambda)
        End Function

        Public Overrides Function InstrumentWhileStatementCondition(original As BoundWhileStatement, rewrittenCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Return AddConditionSequencePoint(MyBase.InstrumentWhileStatementCondition(original, rewrittenCondition, currentMethodOrLambda), original, currentMethodOrLambda)
        End Function

        Public Overrides Function InstrumentForEachStatementCondition(original As BoundForEachStatement, rewrittenCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Return AddConditionSequencePoint(MyBase.InstrumentForEachStatementCondition(original, rewrittenCondition, currentMethodOrLambda), original, currentMethodOrLambda)
        End Function

        Public Overrides Function InstrumentObjectForLoopInitCondition(original As BoundForToStatement, rewrittenInitCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Return AddConditionSequencePoint(MyBase.InstrumentObjectForLoopInitCondition(original, rewrittenInitCondition, currentMethodOrLambda), original, currentMethodOrLambda)
        End Function

        Public Overrides Function InstrumentObjectForLoopCondition(original As BoundForToStatement, rewrittenLoopCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Return AddConditionSequencePoint(MyBase.InstrumentObjectForLoopCondition(original, rewrittenLoopCondition, currentMethodOrLambda), original, currentMethodOrLambda)
        End Function

        Public Overrides Function InstrumentIfStatementCondition(original As BoundIfStatement, rewrittenCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Return AddConditionSequencePoint(MyBase.InstrumentIfStatementCondition(original, rewrittenCondition, currentMethodOrLambda), original, currentMethodOrLambda)
        End Function

        Public Overrides Function InstrumentCatchBlockFilter(original As BoundCatchBlock, rewrittenFilter As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            rewrittenFilter = MyBase.InstrumentCatchBlockFilter(original, rewrittenFilter, currentMethodOrLambda)

            ' if we have a filter, we want to stop before the filter expression
            ' and associate the sequence point with whole Catch statement
            rewrittenFilter = New BoundSequencePointExpression(DirectCast(original.Syntax, CatchBlockSyntax).CatchStatement,
                                                               rewrittenFilter,
                                                               rewrittenFilter.Type)

            ' EnC: We need to insert a hidden sequence point to handle function remapping in case 
            ' the containing method is edited while methods invoked in the condition are being executed.
            Return AddConditionSequencePoint(rewrittenFilter, original, currentMethodOrLambda)
        End Function

        Public Overrides Function CreateSelectStatementPrologue(original As BoundSelectStatement) As BoundStatement
            ' Add select case begin sequence point
            Return New BoundSequencePoint(original.ExpressionStatement.Syntax, MyBase.CreateSelectStatementPrologue(original))
        End Function

        Public Overrides Function InstrumentSelectStatementCaseCondition(original As BoundSelectStatement, rewrittenCaseCondition As BoundExpression, currentMethodOrLambda As MethodSymbol, ByRef lazyConditionalBranchLocal As LocalSymbol) As BoundExpression
            Return AddConditionSequencePoint(MyBase.InstrumentSelectStatementCaseCondition(original, rewrittenCaseCondition, currentMethodOrLambda, lazyConditionalBranchLocal),
                                             original, currentMethodOrLambda, lazyConditionalBranchLocal)
        End Function

        Public Overrides Function InstrumentCaseBlockConditionalGoto(original As BoundCaseBlock, condGoto As BoundStatement) As BoundStatement
            Return New BoundSequencePoint(original.CaseStatement.Syntax, MyBase.InstrumentCaseBlockConditionalGoto(original, condGoto))
        End Function

        Public Overrides Function InstrumentCaseElseBlock(original As BoundCaseBlock, rewritten As BoundBlock) As BoundStatement
            Return New BoundSequencePoint(original.CaseStatement.Syntax, MyBase.InstrumentCaseElseBlock(original, rewritten))
        End Function

        Public Overrides Function InstrumentSelectStatementEpilogue(original As BoundSelectStatement, epilogueOpt As BoundStatement) As BoundStatement
            ' Add End Select sequence point
            Return New BoundSequencePoint(DirectCast(original.Syntax, SelectBlockSyntax).EndSelectStatement, MyBase.InstrumentSelectStatementEpilogue(original, epilogueOpt))
        End Function

        Public Overrides Function CreateCatchBlockPrologue(original As BoundCatchBlock) As BoundStatement
            Return New BoundSequencePoint(DirectCast(original.Syntax, CatchBlockSyntax).CatchStatement, MyBase.CreateCatchBlockPrologue(original))
        End Function

        Public Overrides Function CreateFinallyBlockPrologue(original As BoundTryStatement) As BoundStatement
            Return New BoundSequencePoint(DirectCast(original.FinallyBlockOpt.Syntax, FinallyBlockSyntax).FinallyStatement, MyBase.CreateFinallyBlockPrologue(original))
        End Function

        Public Overrides Function CreateTryBlockPrologue(original As BoundTryStatement) As BoundStatement
            Return New BoundSequencePoint(DirectCast(original.Syntax, TryBlockSyntax).TryStatement, MyBase.CreateTryBlockPrologue(original))
        End Function

        Public Overrides Function InstrumentTryStatement(original As BoundTryStatement, rewritten As BoundStatement) As BoundStatement
            ' Add a sequence point for End Try
            ' Note that scope the point is outside of Try/Catch/Finally 
            Return New BoundStatementList(original.Syntax,
                                          ImmutableArray.Create(Of BoundStatement)(
                                                   MyBase.InstrumentTryStatement(original, rewritten),
                                                   New BoundSequencePoint(DirectCast(original.Syntax, TryBlockSyntax).EndTryStatement, Nothing)
                                               )
                                           )
        End Function

        Public Overrides Function InstrumentFieldOrPropertyInitializer(original As BoundFieldOrPropertyInitializer, rewritten As BoundStatement, symbolIndex As Integer, createTemporary As Boolean) As BoundStatement
            rewritten = MyBase.InstrumentFieldOrPropertyInitializer(original, rewritten, symbolIndex, createTemporary)

            If createTemporary Then
                rewritten = MarkInitializerSequencePoint(rewritten, original.Syntax, symbolIndex)
            End If

            Return rewritten
        End Function

        Public Overrides Function InstrumentForEachLoopInitialization(original As BoundForEachStatement, initialization As BoundStatement) As BoundStatement
            ' first sequence point to highlight the for each statement
            Return New BoundSequencePoint(DirectCast(original.Syntax, ForEachBlockSyntax).ForEachStatement,
                                          MyBase.InstrumentForEachLoopInitialization(original, initialization))
        End Function

        Public Overrides Function InstrumentForEachLoopEpilogue(original As BoundForEachStatement, epilogueOpt As BoundStatement) As BoundStatement
            epilogueOpt = MyBase.InstrumentForEachLoopEpilogue(original, epilogueOpt)
            Dim nextStmt = DirectCast(original.Syntax, ForEachBlockSyntax).NextStatement

            If nextStmt IsNot Nothing Then
                epilogueOpt = New BoundSequencePoint(DirectCast(original.Syntax, ForEachBlockSyntax).NextStatement, epilogueOpt)
            End If

            Return epilogueOpt
        End Function

        Public Overrides Function InstrumentForLoopInitialization(original As BoundForToStatement, initialization As BoundStatement) As BoundStatement
            ' first sequence point to highlight the for statement
            Return New BoundSequencePoint(DirectCast(original.Syntax, ForBlockSyntax).ForStatement, MyBase.InstrumentForLoopInitialization(original, initialization))
        End Function

        Public Overrides Function InstrumentForLoopIncrement(original As BoundForToStatement, increment As BoundStatement) As BoundStatement
            increment = MyBase.InstrumentForLoopIncrement(original, increment)
            Dim nextStmt = DirectCast(original.Syntax, ForBlockSyntax).NextStatement

            If nextStmt IsNot Nothing Then
                increment = New BoundSequencePoint(DirectCast(original.Syntax, ForBlockSyntax).NextStatement, increment)
            End If

            Return increment
        End Function

        Public Overrides Function InstrumentLocalInitialization(original As BoundLocalDeclaration, rewritten As BoundStatement) As BoundStatement
            Return MarkInitializerSequencePoint(MyBase.InstrumentLocalInitialization(original, rewritten), original.Syntax)
        End Function

        Public Overrides Function CreateUsingStatementPrologue(original As BoundUsingStatement) As BoundStatement
            ' create a sequence point that contains the whole using statement as the first reachable sequence point
            ' of the using statement. The resource variables are not yet in scope.
            Return New BoundSequencePoint(original.UsingInfo.UsingStatementSyntax.UsingStatement, MyBase.CreateUsingStatementPrologue(original))
        End Function

        Public Overrides Function InstrumentUsingStatementResourceCapture(original As BoundUsingStatement, resourceIndex As Integer, rewritten As BoundStatement) As BoundStatement
            rewritten = MyBase.InstrumentUsingStatementResourceCapture(original, resourceIndex, rewritten)

            If Not original.ResourceList.IsDefault AndAlso original.ResourceList.Length > 1 Then
                ' Case "Using <variable declarations>"  
                Dim localDeclaration = original.ResourceList(resourceIndex)
                Dim syntaxForSequencePoint As SyntaxNode

                If localDeclaration.Kind = BoundKind.LocalDeclaration Then
                    syntaxForSequencePoint = localDeclaration.Syntax.Parent
                Else
                    Debug.Assert(localDeclaration.Kind = BoundKind.AsNewLocalDeclarations)
                    syntaxForSequencePoint = localDeclaration.Syntax
                End If

                rewritten = New BoundSequencePoint(syntaxForSequencePoint, rewritten)
            End If

            Return rewritten
        End Function

        Public Overrides Function CreateUsingStatementDisposePrologue(original As BoundUsingStatement) As BoundStatement
            ' The block should start with a sequence point that points to the "End Using" statement. This is required in order to
            ' highlight the end using when someone step next after the last statement of the original body and in case an exception
            ' was thrown.
            Return New BoundSequencePoint(DirectCast(original.Syntax, UsingBlockSyntax).EndUsingStatement, MyBase.CreateUsingStatementDisposePrologue(original))
        End Function

        Public Overrides Function CreateWithStatementPrologue(original As BoundWithStatement) As BoundStatement
            Return New BoundSequencePoint(DirectCast(original.Syntax, WithBlockSyntax).WithStatement, MyBase.CreateWithStatementPrologue(original))
        End Function

        Public Overrides Function CreateWithStatementEpilogue(original As BoundWithStatement) As BoundStatement
            Return New BoundSequencePoint(DirectCast(original.Syntax, WithBlockSyntax).EndWithStatement, MyBase.CreateWithStatementEpilogue(original))
        End Function
    End Class

End Namespace
