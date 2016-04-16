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
                                         syntax.DoStatement,
                                         syntax.LoopStatement,
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

            If GenerateDebugInfo Then
                If syntax.LoopStatement IsNot Nothing Then
                    rewrittenBody = InsertEndBlockSequencePoint(rewrittenBody, syntax.LoopStatement)
                End If
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
            Dim ifConditionGotoStart As BoundStatement = New BoundConditionalGoto(
                syntax.DoStatement,
                AddConditionSequencePoint(rewrittenBottomCondition, node),
                jumpIfTrue:=Not node.ConditionIsUntil,
                label:=startLabel)

            If Not conditionResumeTarget.IsDefaultOrEmpty Then
                ifConditionGotoStart = New BoundStatementList(ifConditionGotoStart.Syntax, conditionResumeTarget.Add(ifConditionGotoStart))
            End If

            If GenerateDebugInfo Then
                Return New BoundStatementList(node.Syntax, ImmutableArray.Create(
                        start,
                        New BoundSequencePoint(syntax.DoStatement, Nothing),
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

            Dim loopResumeLabel As BoundLabelStatement = Nothing

            If generateUnstructuredExceptionHandlingResumeCode Then
                loopResumeLabel = RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(syntax)
            End If

            If GenerateDebugInfo Then
                If syntax.LoopStatement IsNot Nothing Then
                    rewrittenBody = InsertBlockEpilogue(rewrittenBody, loopResumeLabel, syntax.LoopStatement)
                    loopResumeLabel = Nothing
                End If
            End If

            If loopResumeLabel IsNot Nothing Then
                rewrittenBody = Concat(rewrittenBody, loopResumeLabel)
            End If

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

            If GenerateDebugInfo Then
                Return New BoundStatementList(syntax, ImmutableArray.Create(
                        start,
                        New BoundSequencePoint(syntax.DoStatement, Nothing),
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
