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
    ''' A base class for components that instrument various portions of executable code.
    ''' It provides a set of APIs that are called by <see cref="LocalRewriter"/> to instrument
    ''' specific portions of the code. These APIs often have two parameters:
    '''     - original bound node produced by the <see cref="Binder"/> for the relevant portion of the code;
    '''     - rewritten bound node created by the <see cref="LocalRewriter"/> for the original node.
    ''' The APIs are expected to return new state of the rewritten node, after they apply appropriate
    ''' modifications, if any.
    ''' 
    ''' The base class provides default implementation for all APIs, which simply returns the rewritten node. 
    ''' </summary>
    Friend Class Instrumenter

        ''' <summary>
        ''' The singleton NoOp instrumenter, can be used to terminate the chain of <see cref="CompoundInstrumenter"/>s.
        ''' </summary>
        Public Shared ReadOnly NoOp As New Instrumenter()

        Public Sub New()
        End Sub

        Private Shared Function InstrumentStatement(original As BoundStatement, rewritten As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.SyntaxTree IsNot Nothing)
            Return rewritten
        End Function

        Public Overridable Function InstrumentExpressionStatement(original As BoundExpressionStatement, rewritten As BoundStatement) As BoundStatement
            Return InstrumentStatement(original, rewritten)
        End Function

        Public Overridable Function InstrumentStopStatement(original As BoundStopStatement, rewritten As BoundStatement) As BoundStatement
            Return InstrumentStatement(original, rewritten)
        End Function

        Public Overridable Function InstrumentEndStatement(original As BoundEndStatement, rewritten As BoundStatement) As BoundStatement
            Return InstrumentStatement(original, rewritten)
        End Function

        Public Overridable Function InstrumentContinueStatement(original As BoundContinueStatement, rewritten As BoundStatement) As BoundStatement
            Return InstrumentStatement(original, rewritten)
        End Function

        Public Overridable Function InstrumentExitStatement(original As BoundExitStatement, rewritten As BoundStatement) As BoundStatement
            Return InstrumentStatement(original, rewritten)
        End Function

        Public Overridable Function InstrumentGotoStatement(original As BoundGotoStatement, rewritten As BoundStatement) As BoundStatement
            Return InstrumentStatement(original, rewritten)
        End Function

        Public Overridable Function InstrumentLabelStatement(original As BoundLabelStatement, rewritten As BoundStatement) As BoundStatement
            Return InstrumentStatement(original, rewritten)
        End Function

        Public Overridable Function InstrumentRaiseEventStatement(original As BoundRaiseEventStatement, rewritten As BoundStatement) As BoundStatement
            Return InstrumentStatement(original, rewritten)
        End Function

        Public Overridable Function InstrumentReturnStatement(original As BoundReturnStatement, rewritten As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated OrElse
                         (original.ExpressionOpt IsNot Nothing AndAlso
                          Not original.ExpressionOpt.WasCompilerGenerated AndAlso
                          original.Syntax Is original.ExpressionOpt.Syntax))
            Debug.Assert(original.SyntaxTree IsNot Nothing)
            Return rewritten
        End Function

        Public Overridable Function InstrumentThrowStatement(original As BoundThrowStatement, rewritten As BoundStatement) As BoundStatement
            Return InstrumentStatement(original, rewritten)
        End Function

        Public Overridable Function InstrumentOnErrorStatement(original As BoundOnErrorStatement, rewritten As BoundStatement) As BoundStatement
            Return InstrumentStatement(original, rewritten)
        End Function

        Public Overridable Function InstrumentResumeStatement(original As BoundResumeStatement, rewritten As BoundStatement) As BoundStatement
            Return InstrumentStatement(original, rewritten)
        End Function

        Public Overridable Function InstrumentAddHandlerStatement(original As BoundAddHandlerStatement, rewritten As BoundStatement) As BoundStatement
            Return InstrumentStatement(original, rewritten)
        End Function

        Public Overridable Function InstrumentRemoveHandlerStatement(original As BoundRemoveHandlerStatement, rewritten As BoundStatement) As BoundStatement
            Return InstrumentStatement(original, rewritten)
        End Function

        ''' <summary>
        ''' Return a node that is associated with an entry of the block. OK to return Nothing.
        ''' </summary>
        Public Overridable Function CreateBlockPrologue(trueOriginal As BoundBlock, original As BoundBlock, ByRef synthesizedLocal As LocalSymbol) As BoundStatement
            synthesizedLocal = Nothing
            Return Nothing
        End Function

        Public Overridable Function InstrumentTopLevelExpressionInQuery(original As BoundExpression, rewritten As BoundExpression) As BoundExpression
            Debug.Assert(Not original.WasCompilerGenerated)
            Return rewritten
        End Function

        Public Overridable Function InstrumentQueryLambdaBody(original As BoundQueryLambda, rewritten As BoundStatement) As BoundStatement
            Debug.Assert(original.LambdaSymbol.SynthesizedKind = SynthesizedLambdaKind.AggregateQueryLambda OrElse
                         original.LambdaSymbol.SynthesizedKind = SynthesizedLambdaKind.LetVariableQueryLambda)
            Return rewritten
        End Function

        ''' <summary>
        ''' Ok to return Nothing when <paramref name="epilogueOpt"/> is Nothing.
        ''' </summary>
        Public Overridable Function InstrumentDoLoopEpilogue(original As BoundDoLoopStatement, epilogueOpt As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(DirectCast(original.Syntax, DoLoopBlockSyntax).LoopStatement IsNot Nothing)
            Return epilogueOpt
        End Function

        ''' <summary>
        ''' Return a prologue. Ok to return Nothing.
        ''' </summary>
        Public Overridable Function CreateSyncLockStatementPrologue(original As BoundSyncLockStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.SyncLockBlock)
            Return Nothing
        End Function

        Public Overridable Function InstrumentSyncLockObjectCapture(original As BoundSyncLockStatement, rewritten As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.SyncLockBlock)
            Return rewritten
        End Function

        ''' <summary>
        ''' Return an epilogue. Ok to return Nothing.
        ''' </summary>
        Public Overridable Function CreateSyncLockExitDueToExceptionEpilogue(original As BoundSyncLockStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.SyncLockBlock)
            Return Nothing
        End Function

        ''' <summary>
        ''' Return an epilogue. Ok to return Nothing.
        ''' </summary>
        Public Overridable Function CreateSyncLockExitNormallyEpilogue(original As BoundSyncLockStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.SyncLockBlock)
            Return Nothing
        End Function

        ''' <summary>
        ''' Ok to return Nothing when <paramref name="epilogueOpt"/> is Nothing.
        ''' </summary>
        Public Overridable Function InstrumentWhileEpilogue(original As BoundWhileStatement, epilogueOpt As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.WhileBlock)
            Return epilogueOpt
        End Function

        Public Overridable Function InstrumentWhileStatementConditionalGotoStart(original As BoundWhileStatement, ifConditionGotoStart As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.WhileBlock)
            Return ifConditionGotoStart
        End Function

        ''' <summary>
        ''' Ok to return Nothing when <paramref name="ifConditionGotoStartOpt"/> is Nothing.
        ''' </summary>
        Public Overridable Function InstrumentDoLoopStatementEntryOrConditionalGotoStart(original As BoundDoLoopStatement, ifConditionGotoStartOpt As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(TypeOf original.Syntax Is DoLoopBlockSyntax)
            Return ifConditionGotoStartOpt
        End Function

        Public Overridable Function InstrumentForEachStatementConditionalGotoStart(original As BoundForEachStatement, ifConditionGotoStart As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.ForEachBlock)
            Return ifConditionGotoStart
        End Function

        Public Overridable Function InstrumentIfStatementConditionalGoto(original As BoundIfStatement, condGoto As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.MultiLineIfBlock OrElse original.Syntax.Kind = SyntaxKind.ElseIfBlock OrElse original.Syntax.Kind = SyntaxKind.SingleLineIfStatement)
            Return condGoto
        End Function

        Public Overridable Function InstrumentIfStatementAfterIfStatement(original As BoundIfStatement, afterIfStatement As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.MultiLineIfBlock)
            Return afterIfStatement
        End Function

        ''' <summary>
        ''' Ok to return Nothing when <paramref name="epilogueOpt"/> is Nothing.
        ''' </summary>
        Public Overridable Function InstrumentIfStatementConsequenceEpilogue(original As BoundIfStatement, epilogueOpt As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.MultiLineIfBlock OrElse original.Syntax.Kind = SyntaxKind.ElseIfBlock OrElse original.Syntax.Kind = SyntaxKind.SingleLineIfStatement)
            Return epilogueOpt
        End Function

        ''' <summary>
        ''' Ok to return Nothing when <paramref name="epilogueOpt"/> is Nothing.
        ''' </summary>
        Public Overridable Function InstrumentIfStatementAlternativeEpilogue(original As BoundIfStatement, epilogueOpt As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.AlternativeOpt.Syntax.Kind = SyntaxKind.ElseBlock)
            Debug.Assert(original.AlternativeOpt.Syntax.Parent.Kind = SyntaxKind.MultiLineIfBlock)
            Return epilogueOpt
        End Function

        ''' <summary>
        ''' Return a node that is associated with an entry of the Else block. Ok to return Nothing.
        ''' </summary>
        Public Overridable Function CreateIfStatementAlternativePrologue(original As BoundIfStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.AlternativeOpt.Syntax.Kind = SyntaxKind.ElseBlock OrElse original.AlternativeOpt.Syntax.Kind = SyntaxKind.SingleLineElseClause)
            Return Nothing
        End Function

        Public Overridable Function InstrumentDoLoopStatementCondition(original As BoundDoLoopStatement, rewrittenCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Debug.Assert(Not original.WasCompilerGenerated)
            Return rewrittenCondition
        End Function

        Public Overridable Function InstrumentWhileStatementCondition(original As BoundWhileStatement, rewrittenCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Debug.Assert(Not original.WasCompilerGenerated)
            Return rewrittenCondition
        End Function

        Public Overridable Function InstrumentForEachStatementCondition(original As BoundForEachStatement, rewrittenCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Debug.Assert(Not original.WasCompilerGenerated)
            Return rewrittenCondition
        End Function

        Public Overridable Function InstrumentObjectForLoopInitCondition(original As BoundForToStatement, rewrittenInitCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Debug.Assert(Not original.WasCompilerGenerated)
            Return rewrittenInitCondition
        End Function

        Public Overridable Function InstrumentObjectForLoopCondition(original As BoundForToStatement, rewrittenLoopCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Debug.Assert(Not original.WasCompilerGenerated)
            Return rewrittenLoopCondition
        End Function

        Public Overridable Function InstrumentIfStatementCondition(original As BoundIfStatement, rewrittenCondition As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Debug.Assert(Not original.WasCompilerGenerated)
            Return rewrittenCondition
        End Function

        Public Overridable Function InstrumentCatchBlockFilter(original As BoundCatchBlock, rewrittenFilter As BoundExpression, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.CatchBlock)
            Debug.Assert(original.ExceptionFilterOpt IsNot Nothing)
            Debug.Assert(original.ExceptionFilterOpt.Syntax.Parent.IsKind(SyntaxKind.CatchFilterClause))
            Return rewrittenFilter
        End Function

        ''' <summary>
        ''' Return a node that is associated with an entry of the block. Ok to return Nothing.
        ''' Note, this method is only called for a catch block without Filter.
        ''' If there is a filter, <see cref="InstrumentCatchBlockFilter"/> is called instead. 
        ''' </summary>
        Public Overridable Function CreateCatchBlockPrologue(original As BoundCatchBlock) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.CatchBlock)
            Debug.Assert(original.ExceptionFilterOpt Is Nothing)
            Return Nothing
        End Function

        ''' <summary>
        ''' Return a node that is associated with an entry of the Finally block. Ok to return Nothing.
        ''' </summary>
        Public Overridable Function CreateFinallyBlockPrologue(original As BoundTryStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.TryBlock)
            Debug.Assert(original.FinallyBlockOpt IsNot Nothing)
            Debug.Assert(original.FinallyBlockOpt.Syntax.Kind = SyntaxKind.FinallyBlock)
            Return Nothing
        End Function

        ''' <summary>
        ''' Return a node that is associated with an entry of the Try block. Ok to return Nothing.
        ''' </summary>
        Public Overridable Function CreateTryBlockPrologue(original As BoundTryStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.TryBlock)
            Return Nothing
        End Function

        Public Overridable Function InstrumentTryStatement(original As BoundTryStatement, rewritten As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.TryBlock)
            Return rewritten
        End Function

        ''' <summary>
        ''' Ok to return Nothing.
        ''' </summary>
        Public Overridable Function CreateSelectStatementPrologue(original As BoundSelectStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Return Nothing
        End Function

        Public Overridable Function InstrumentSelectStatementCaseCondition(original As BoundSelectStatement, rewrittenCaseCondition As BoundExpression, currentMethodOrLambda As MethodSymbol, ByRef lazyConditionalBranchLocal As LocalSymbol) As BoundExpression
            Debug.Assert(Not original.WasCompilerGenerated)
            Return rewrittenCaseCondition
        End Function

        Public Overridable Function InstrumentCaseBlockConditionalGoto(original As BoundCaseBlock, condGoto As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Return condGoto
        End Function

        Public Overridable Function InstrumentCaseElseBlock(original As BoundCaseBlock, rewritten As BoundBlock) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Return rewritten
        End Function

        ''' <summary>
        ''' Ok to return Nothing when <paramref name="epilogueOpt"/> is Nothing.
        ''' If <paramref name="epilogueOpt"/> is not Nothing, add to the end, not to the front.
        ''' </summary>
        Public Overridable Function InstrumentSelectStatementEpilogue(original As BoundSelectStatement, epilogueOpt As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.SelectBlock)
            Return epilogueOpt
        End Function

        Public Overridable Function InstrumentFieldOrPropertyInitializer(original As BoundFieldOrPropertyInitializer, rewritten As BoundStatement, symbolIndex As Integer, createTemporary As Boolean) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.IsKind(SyntaxKind.AsNewClause) OrElse                ' Dim a As New C(); Dim a,b As New C(); Property P As New C()
                         original.Syntax.IsKind(SyntaxKind.ModifiedIdentifier) OrElse         ' Dim a(1) As Integer
                         original.Syntax.IsKind(SyntaxKind.EqualsValue))                      ' Dim a = 1; Property P As Integer = 1
            Return rewritten
        End Function

        Public Overridable Function InstrumentForEachLoopInitialization(original As BoundForEachStatement, initialization As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.ForEachBlock)
            Return initialization
        End Function

        ''' <summary>
        ''' Ok to return Nothing when <paramref name="epilogueOpt"/> is Nothing.
        ''' If <paramref name="epilogueOpt"/> is not Nothing, add to the end, not to the front.
        ''' </summary>
        Public Overridable Function InstrumentForEachLoopEpilogue(original As BoundForEachStatement, epilogueOpt As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.ForEachBlock)
            Return epilogueOpt
        End Function

        Public Overridable Function InstrumentForLoopInitialization(original As BoundForToStatement, initialization As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.ForBlock)
            Return initialization
        End Function

        Public Overridable Function InstrumentForLoopIncrement(original As BoundForToStatement, increment As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.ForBlock)
            Return increment
        End Function

        Public Overridable Function InstrumentLocalInitialization(original As BoundLocalDeclaration, rewritten As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.ModifiedIdentifier)
            Debug.Assert(original.Syntax.Parent.Kind = SyntaxKind.VariableDeclarator)
            Return rewritten
        End Function

        ''' <summary>
        ''' Ok to return Nothing.
        ''' </summary>
        Public Overridable Function CreateUsingStatementPrologue(original As BoundUsingStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.UsingBlock)
            Return Nothing
        End Function

        Public Overridable Function InstrumentUsingStatementResourceCapture(original As BoundUsingStatement, resourceIndex As Integer, rewritten As BoundStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.UsingBlock)
            Return rewritten
        End Function

        ''' <summary>
        ''' Ok to return Nothing.
        ''' </summary>
        Public Overridable Function CreateUsingStatementDisposePrologue(original As BoundUsingStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.UsingBlock)
            Return Nothing
        End Function

        ''' <summary>
        ''' Ok to return Nothing.
        ''' </summary>
        Public Overridable Function CreateWithStatementPrologue(original As BoundWithStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.WithBlock)
            Return Nothing
        End Function

        ''' <summary>
        ''' Ok to return Nothing.
        ''' </summary>
        Public Overridable Function CreateWithStatementEpilogue(original As BoundWithStatement) As BoundStatement
            Debug.Assert(Not original.WasCompilerGenerated)
            Debug.Assert(original.Syntax.Kind = SyntaxKind.WithBlock)
            Return Nothing
        End Function
    End Class

End Namespace
