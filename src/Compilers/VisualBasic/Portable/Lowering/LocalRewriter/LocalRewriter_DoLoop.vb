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
        Public Overrides Function VisitDoLoopStatement(node As BoundDoLoopStatement) As BoundNode
            Debug.Assert(node IsNot Nothing)

            If node.ConditionOpt IsNot Nothing Then
                If node.ConditionIsTop Then
                    Return VisitTopConditionLoop(node)
                Else
                    Return VisitBottomConditionLoop(node)
                End If
            End If

            Return VisitInfiniteLoop(node)
        End Function

        Private Function VisitTopConditionLoop(node As BoundDoLoopStatement) As BoundNode
            Debug.Assert(node.ConditionOpt IsNot Nothing AndAlso node.ConditionIsTop)

            Dim generateUnstructuredExceptionHandlingResumeCode As Boolean = ShouldGenerateUnstructuredExceptionHandlingResumeCode(node)

            Dim loopResumeLabel As BoundLabelStatement = Nothing
            Dim conditionResumeTarget As ImmutableArray(Of BoundStatement) = Nothing

            If generateUnstructuredExceptionHandlingResumeCode Then
                loopResumeLabel = RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(node.Syntax)
                conditionResumeTarget = RegisterUnstructuredExceptionHandlingResumeTarget(node.Syntax, canThrow:=True)
            End If

            Dim rewrittenBody = DirectCast(Visit(node.Body), BoundStatement)

            Dim afterBodyResumeLabel As BoundLabelStatement = Nothing

            If generateUnstructuredExceptionHandlingResumeCode Then
                afterBodyResumeLabel = RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(node.Syntax)
            End If

            Dim syntax = DirectCast(node.Syntax, DoLoopBlockSyntax)

            Return RewriteWhileStatement(node,
                                         VisitExpressionNode(node.ConditionOpt),
                                         rewrittenBody,
                                         node.ContinueLabel,
                                         node.ExitLabel,
                                         Not node.ConditionIsUntil,
                                         loopResumeLabel,
                                         conditionResumeTarget,
                                         afterBodyResumeLabel)
        End Function

        Private Function VisitBottomConditionLoop(node As BoundDoLoopStatement) As BoundNode
            Debug.Assert(node.ConditionOpt IsNot Nothing AndAlso Not node.ConditionIsTop)

            Dim syntax = DirectCast(node.Syntax, DoLoopBlockSyntax)

            Dim generateUnstructuredExceptionHandlingResumeCode As Boolean = ShouldGenerateUnstructuredExceptionHandlingResumeCode(node)

            Dim doResumeLabel As BoundLabelStatement = Nothing

            If generateUnstructuredExceptionHandlingResumeCode Then
                doResumeLabel = RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(syntax.DoStatement)
            End If

            Dim startLabel = GenerateLabel("start")
            Dim start As BoundStatement = New BoundLabelStatement(syntax.DoStatement, startLabel)

            If doResumeLabel IsNot Nothing Then
                start = Concat(doResumeLabel, start)
            End If

            Dim rewrittenBody = DirectCast(Visit(node.Body), BoundStatement)

            Dim instrument = Me.Instrument(node)
            If instrument AndAlso syntax.LoopStatement IsNot Nothing Then
                rewrittenBody = Concat(rewrittenBody, _instrumenterOpt.InstrumentDoLoopEpilogue(node, Nothing))
            End If

            Dim conditionResumeTarget As ImmutableArray(Of BoundStatement) = Nothing

            If generateUnstructuredExceptionHandlingResumeCode Then
                conditionResumeTarget = RegisterUnstructuredExceptionHandlingResumeTarget(node.Syntax, canThrow:=True)
            End If

            Dim rewrittenBottomCondition = VisitExpressionNode(node.ConditionOpt)

            ' Do 
            '    body
            ' Loop [While|Until condition]
            '
            ' becomes
            '
            ' start:
            ' body
            ' continue:
            ' {GotoIfTrue|False condition start}
            ' exit:

            ' EnC: We need to insert a hidden sequence point to handle function remapping in case 
            ' the containing method is edited while methods invoked in the condition are being executed.
            If rewrittenBottomCondition IsNot Nothing AndAlso instrument Then
                rewrittenBottomCondition = _instrumenterOpt.InstrumentDoLoopStatementCondition(node, rewrittenBottomCondition, _currentMethodOrLambda)
            End If

            Dim ifConditionGotoStart As BoundStatement = New BoundConditionalGoto(
                syntax.DoStatement,
                rewrittenBottomCondition,
                jumpIfTrue:=Not node.ConditionIsUntil,
                label:=startLabel)

            If Not conditionResumeTarget.IsDefaultOrEmpty Then
                ifConditionGotoStart = New BoundStatementList(ifConditionGotoStart.Syntax, conditionResumeTarget.Add(ifConditionGotoStart))
            End If

            If instrument Then
                Return New BoundStatementList(node.Syntax, ImmutableArray.Create(
                        start,
                        _instrumenterOpt.InstrumentDoLoopStatementEntryOrConditionalGotoStart(node, Nothing),
                        rewrittenBody,
                        New BoundLabelStatement(syntax.DoStatement, node.ContinueLabel),
                        ifConditionGotoStart,
                        New BoundLabelStatement(syntax.DoStatement, node.ExitLabel)
                    ))
            End If

            Return New BoundStatementList(node.Syntax, ImmutableArray.Create(
                    start,
                    rewrittenBody,
                    New BoundLabelStatement(node.Syntax, node.ContinueLabel),
                    ifConditionGotoStart,
                    New BoundLabelStatement(node.Syntax, node.ExitLabel)
                ))

        End Function

        Private Function VisitInfiniteLoop(node As BoundDoLoopStatement) As BoundNode
            Debug.Assert(node.ConditionOpt Is Nothing)

            Dim syntax = DirectCast(node.Syntax, DoLoopBlockSyntax)

            Dim generateUnstructuredExceptionHandlingResumeCode As Boolean = ShouldGenerateUnstructuredExceptionHandlingResumeCode(node)

            Dim doResumeLabel As BoundLabelStatement = Nothing

            If generateUnstructuredExceptionHandlingResumeCode Then
                doResumeLabel = RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(syntax.DoStatement)
            End If

            Dim startLabel = GenerateLabel("start")
            Dim start As BoundStatement = New BoundLabelStatement(syntax.DoStatement, startLabel)

            If doResumeLabel IsNot Nothing Then
                start = Concat(doResumeLabel, start)
            End If

            Dim rewrittenBody = DirectCast(Visit(node.Body), BoundStatement)

            Dim loopResumeLabel As BoundStatement = Nothing

            If generateUnstructuredExceptionHandlingResumeCode Then
                loopResumeLabel = RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(syntax)
            End If

            Dim instrument = Me.Instrument(node)
            If instrument AndAlso syntax.LoopStatement IsNot Nothing Then
                loopResumeLabel = _instrumenterOpt.InstrumentDoLoopEpilogue(node, loopResumeLabel)
            End If

            rewrittenBody = Concat(rewrittenBody, loopResumeLabel)

            ' Do
            '    body
            ' Loop
            '
            ' becomes
            '
            ' start:
            ' body
            ' continue:
            ' {Goto start}
            ' exit:

            If instrument Then
                Return New BoundStatementList(syntax, ImmutableArray.Create(
                        start,
                        _instrumenterOpt.InstrumentDoLoopStatementEntryOrConditionalGotoStart(node, Nothing),
                        rewrittenBody,
                        New BoundLabelStatement(syntax.DoStatement, node.ContinueLabel),
                        New BoundGotoStatement(syntax.DoStatement, startLabel, Nothing),
                        New BoundLabelStatement(syntax.DoStatement, node.ExitLabel)
                    ))
            End If

            Return New BoundStatementList(syntax, ImmutableArray.Create(
                start,
                rewrittenBody,
                New BoundLabelStatement(node.Syntax, node.ContinueLabel),
                New BoundGotoStatement(node.Syntax, startLabel, Nothing),
                New BoundLabelStatement(node.Syntax, node.ExitLabel)
            ))

        End Function

    End Class
End Namespace
