' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Utility class, provides a convenient way of combining various <see cref="Instrumenter"/>s in a chain,
    ''' allowing each of them to apply specific instrumentations in particular order.
    ''' 
    ''' Default implementation of all APIs delegates to the "previous" <see cref="Instrumenter"/> passed as a parameter
    ''' to the constructor of this class. Usually, derived types are going to let the base (this class) to do its work first
    ''' and then operate on the result they get back.
    ''' </summary>
    Friend Class CompoundInstrumenter
        Inherits Instrumenter

        Public Sub New(previous As Instrumenter)
            Debug.Assert(previous IsNot Nothing)
            Me.Previous = previous
        End Sub

        Public ReadOnly Property Previous As Instrumenter

        Public Overrides Function InstrumentExpressionStatement(original As BoundExpressionStatement, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentExpressionStatement(original, rewritten)
        End Function

        Public Overrides Function InstrumentStopStatement(original As BoundStopStatement, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentStopStatement(original, rewritten)
        End Function

        Public Overrides Function InstrumentEndStatement(original As BoundEndStatement, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentEndStatement(original, rewritten)
        End Function

        Public Overrides Function InstrumentContinueStatement(original As BoundContinueStatement, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentContinueStatement(original, rewritten)
        End Function

        Public Overrides Function InstrumentExitStatement(original As BoundExitStatement, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentExitStatement(original, rewritten)
        End Function

        Public Overrides Function InstrumentGotoStatement(original As BoundGotoStatement, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentGotoStatement(original, rewritten)
        End Function

        Public Overrides Function InstrumentLabelStatement(original As BoundLabelStatement, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentLabelStatement(original, rewritten)
        End Function

        Public Overrides Function InstrumentRaiseEventStatement(original As BoundRaiseEventStatement, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentRaiseEventStatement(original, rewritten)
        End Function

        Public Overrides Function InstrumentReturnStatement(original As BoundReturnStatement, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentReturnStatement(original, rewritten)
        End Function

        Public Overrides Function InstrumentThrowStatement(original As BoundThrowStatement, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentThrowStatement(original, rewritten)
        End Function

        Public Overrides Function InstrumentOnErrorStatement(original As BoundOnErrorStatement, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentOnErrorStatement(original, rewritten)
        End Function

        Public Overrides Function InstrumentResumeStatement(original As BoundResumeStatement, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentResumeStatement(original, rewritten)
        End Function

        Public Overrides Function InstrumentAddHandlerStatement(original As BoundAddHandlerStatement, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentAddHandlerStatement(original, rewritten)
        End Function

        Public Overrides Function InstrumentRemoveHandlerStatement(original As BoundRemoveHandlerStatement, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentRemoveHandlerStatement(original, rewritten)
        End Function

        Public Overrides Function CreateBlockPrologue(trueOriginal As BoundBlock, original As BoundBlock, ByRef synthesizedLocal As LocalSymbol) As BoundStatement
            Return Previous.CreateBlockPrologue(trueOriginal, original, synthesizedLocal)
        End Function

        Public Overrides Function InstrumentTopLevelExpressionInQuery(original As BoundExpression, rewritten As BoundExpression) As BoundExpression
            Return Previous.InstrumentTopLevelExpressionInQuery(original, rewritten)
        End Function

        Public Overrides Function InstrumentQueryLambdaBody(original As BoundQueryLambda, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentQueryLambdaBody(original, rewritten)
        End Function

        Public Overrides Function InstrumentDoLoopEpilogue(original As BoundDoLoopStatement, epilogueOpt As BoundStatement) As BoundStatement
            Return Previous.InstrumentDoLoopEpilogue(original, epilogueOpt)
        End Function

        Public Overrides Function CreateSyncLockStatementPrologue(original As BoundSyncLockStatement) As BoundStatement
            Return Previous.CreateSyncLockStatementPrologue(original)
        End Function

        Public Overrides Function InstrumentSyncLockObjectCapture(original As BoundSyncLockStatement, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentSyncLockObjectCapture(original, rewritten)
        End Function

        Public Overrides Function CreateSyncLockExitDueToExceptionEpilogue(original As BoundSyncLockStatement) As BoundStatement
            Return Previous.CreateSyncLockExitDueToExceptionEpilogue(original)
        End Function

        Public Overrides Function CreateSyncLockExitNormallyEpilogue(original As BoundSyncLockStatement) As BoundStatement
            Return Previous.CreateSyncLockExitNormallyEpilogue(original)
        End Function

        Public Overrides Function InstrumentWhileEpilogue(original As BoundWhileStatement, epilogueOpt As BoundStatement) As BoundStatement
            Return Previous.InstrumentWhileEpilogue(original, epilogueOpt)
        End Function

        Public Overrides Function InstrumentWhileStatementConditionalGotoStart(original As BoundWhileStatement, ifConditionGotoStart As BoundStatement) As BoundStatement
            Return Previous.InstrumentWhileStatementConditionalGotoStart(original, ifConditionGotoStart)
        End Function

        Public Overrides Function InstrumentDoLoopStatementEntryOrConditionalGotoStart(original As BoundDoLoopStatement, ifConditionGotoStartOpt As BoundStatement) As BoundStatement
            Return Previous.InstrumentDoLoopStatementEntryOrConditionalGotoStart(original, ifConditionGotoStartOpt)
        End Function

        Public Overrides Function InstrumentForEachStatementConditionalGotoStart(original As BoundForEachStatement, ifConditionGotoStart As BoundStatement) As BoundStatement
            Return Previous.InstrumentForEachStatementConditionalGotoStart(original, ifConditionGotoStart)
        End Function

        Public Overrides Function InstrumentIfStatementConditionalGoto(original As BoundIfStatement, condGoto As BoundStatement) As BoundStatement
            Return Previous.InstrumentIfStatementConditionalGoto(original, condGoto)
        End Function

        Public Overrides Function InstrumentIfStatementAfterIfStatement(original As BoundIfStatement, afterIfStatement As BoundStatement) As BoundStatement
            Return Previous.InstrumentIfStatementAfterIfStatement(original, afterIfStatement)
        End Function

        Public Overrides Function InstrumentIfStatementConsequenceEpilogue(original As BoundIfStatement, epilogueOpt As BoundStatement) As BoundStatement
            Return Previous.InstrumentIfStatementConsequenceEpilogue(original, epilogueOpt)
        End Function

        Public Overrides Function InstrumentIfStatementAlternativeEpilogue(original As BoundIfStatement, epilogueOpt As BoundStatement) As BoundStatement
            Return Previous.InstrumentIfStatementAlternativeEpilogue(original, epilogueOpt)
        End Function

        Public Overrides Function CreateIfStatementAlternativePrologue(original As BoundIfStatement) As BoundStatement
            Return Previous.CreateIfStatementAlternativePrologue(original)
        End Function

        Public Overrides Function InstrumentDoLoopStatementCondition(original As BoundDoLoopStatement, rewrittenCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Return Previous.InstrumentDoLoopStatementCondition(original, rewrittenCondition, currentMethodOrLambda)
        End Function

        Public Overrides Function InstrumentWhileStatementCondition(original As BoundWhileStatement, rewrittenCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Return Previous.InstrumentWhileStatementCondition(original, rewrittenCondition, currentMethodOrLambda)
        End Function

        Public Overrides Function InstrumentForEachStatementCondition(original As BoundForEachStatement, rewrittenCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Return Previous.InstrumentForEachStatementCondition(original, rewrittenCondition, currentMethodOrLambda)
        End Function

        Public Overrides Function InstrumentObjectForLoopInitCondition(original As BoundForToStatement, rewrittenInitCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Return Previous.InstrumentObjectForLoopInitCondition(original, rewrittenInitCondition, currentMethodOrLambda)
        End Function

        Public Overrides Function InstrumentObjectForLoopCondition(original As BoundForToStatement, rewrittenLoopCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Return Previous.InstrumentObjectForLoopCondition(original, rewrittenLoopCondition, currentMethodOrLambda)
        End Function

        Public Overrides Function InstrumentIfStatementCondition(original As BoundIfStatement, rewrittenCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Return Previous.InstrumentIfStatementCondition(original, rewrittenCondition, currentMethodOrLambda)
        End Function

        Public Overrides Function InstrumentCatchBlockFilter(original As BoundCatchBlock, rewrittenFilter As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Return Previous.InstrumentCatchBlockFilter(original, rewrittenFilter, currentMethodOrLambda)
        End Function

        Public Overrides Function CreateCatchBlockPrologue(original As BoundCatchBlock) As BoundStatement
            Return Previous.CreateCatchBlockPrologue(original)
        End Function

        Public Overrides Function CreateFinallyBlockPrologue(original As BoundTryStatement) As BoundStatement
            Return Previous.CreateFinallyBlockPrologue(original)
        End Function

        Public Overrides Function CreateTryBlockPrologue(original As BoundTryStatement) As BoundStatement
            Return Previous.CreateTryBlockPrologue(original)
        End Function

        Public Overrides Function InstrumentTryStatement(original As BoundTryStatement, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentTryStatement(original, rewritten)
        End Function

        Public Overrides Function CreateSelectStatementPrologue(original As BoundSelectStatement) As BoundStatement
            Return Previous.CreateSelectStatementPrologue(original)
        End Function

        Public Overrides Function InstrumentSelectStatementCaseCondition(original As BoundSelectStatement, rewrittenCaseCondition As BoundExpression, currentMethodOrLambda As MethodSymbol, ByRef lazyConditionalBranchLocal As LocalSymbol) As BoundExpression
            Return Previous.InstrumentSelectStatementCaseCondition(original, rewrittenCaseCondition, currentMethodOrLambda, lazyConditionalBranchLocal)
        End Function

        Public Overrides Function InstrumentCaseBlockConditionalGoto(original As BoundCaseBlock, condGoto As BoundStatement) As BoundStatement
            Return Previous.InstrumentCaseBlockConditionalGoto(original, condGoto)
        End Function

        Public Overrides Function InstrumentCaseElseBlock(original As BoundCaseBlock, rewritten As BoundBlock) As BoundStatement
            Return Previous.InstrumentCaseElseBlock(original, rewritten)
        End Function

        Public Overrides Function InstrumentSelectStatementEpilogue(original As BoundSelectStatement, epilogueOpt As BoundStatement) As BoundStatement
            Return Previous.InstrumentSelectStatementEpilogue(original, epilogueOpt)
        End Function

        Public Overrides Function InstrumentFieldOrPropertyInitializer(original As BoundFieldOrPropertyInitializer, rewritten As BoundStatement, symbolIndex As Integer, createTemporary As Boolean) As BoundStatement
            Return Previous.InstrumentFieldOrPropertyInitializer(original, rewritten, symbolIndex, createTemporary)
        End Function

        Public Overrides Function InstrumentForEachLoopInitialization(original As BoundForEachStatement, initialization As BoundStatement) As BoundStatement
            Return Previous.InstrumentForEachLoopInitialization(original, initialization)
        End Function

        Public Overrides Function InstrumentForEachLoopEpilogue(original As BoundForEachStatement, epilogueOpt As BoundStatement) As BoundStatement
            Return Previous.InstrumentForEachLoopEpilogue(original, epilogueOpt)
        End Function

        Public Overrides Function InstrumentForLoopInitialization(original As BoundForToStatement, initialization As BoundStatement) As BoundStatement
            Return Previous.InstrumentForLoopInitialization(original, initialization)
        End Function

        Public Overrides Function InstrumentForLoopIncrement(original As BoundForToStatement, increment As BoundStatement) As BoundStatement
            Return Previous.InstrumentForLoopIncrement(original, increment)
        End Function

        Public Overrides Function InstrumentLocalInitialization(original As BoundLocalDeclaration, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentLocalInitialization(original, rewritten)
        End Function

        Public Overrides Function CreateUsingStatementPrologue(original As BoundUsingStatement) As BoundStatement
            Return Previous.CreateUsingStatementPrologue(original)
        End Function

        Public Overrides Function InstrumentUsingStatementResourceCapture(original As BoundUsingStatement, resourceIndex As Integer, rewritten As BoundStatement) As BoundStatement
            Return Previous.InstrumentUsingStatementResourceCapture(original, resourceIndex, rewritten)
        End Function

        Public Overrides Function CreateUsingStatementDisposePrologue(original As BoundUsingStatement) As BoundStatement
            Return Previous.CreateUsingStatementDisposePrologue(original)
        End Function

        Public Overrides Function CreateWithStatementPrologue(original As BoundWithStatement) As BoundStatement
            Return Previous.CreateWithStatementPrologue(original)
        End Function

        Public Overrides Function CreateWithStatementEpilogue(original As BoundWithStatement) As BoundStatement
            Return Previous.CreateWithStatementEpilogue(original)
        End Function
    End Class

End Namespace
