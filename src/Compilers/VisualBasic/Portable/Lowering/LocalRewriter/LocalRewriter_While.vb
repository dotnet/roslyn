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
        Public Overrides Function VisitWhileStatement(node As BoundWhileStatement) As BoundNode

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

            Return RewriteWhileStatement(node,
                                         VisitExpressionNode(node.Condition),
                                         rewrittenBody,
                                         node.ContinueLabel,
                                         node.ExitLabel,
                                         True,
                                         loopResumeLabel,
                                         conditionResumeTarget,
                                         afterBodyResumeLabel)
        End Function

        Protected Function RewriteWhileStatement(
            statement As BoundStatement,
            rewrittenCondition As BoundExpression,
            rewrittenBody As BoundStatement,
            continueLabel As LabelSymbol,
            exitLabel As LabelSymbol,
            Optional loopIfTrue As Boolean = True,
            Optional loopResumeLabelOpt As BoundLabelStatement = Nothing,
            Optional conditionResumeTargetOpt As ImmutableArray(Of BoundStatement) = Nothing,
            Optional afterBodyResumeTargetOpt As BoundStatement = Nothing
        ) As BoundNode
            Dim startLabel = GenerateLabel("start")
            Dim statementSyntax = statement.Syntax

            Dim instrument As Boolean = Me.Instrument(statement)

            If instrument Then
                Select Case statement.Kind
                    Case BoundKind.WhileStatement
                        afterBodyResumeTargetOpt = _instrumenterOpt.InstrumentWhileEpilogue(DirectCast(statement, BoundWhileStatement), afterBodyResumeTargetOpt)
                    Case BoundKind.DoLoopStatement
                        afterBodyResumeTargetOpt = _instrumenterOpt.InstrumentDoLoopEpilogue(DirectCast(statement, BoundDoLoopStatement), afterBodyResumeTargetOpt)
                    Case BoundKind.ForEachStatement
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(statement.Kind)
                End Select
            End If

            rewrittenBody = Concat(rewrittenBody, afterBodyResumeTargetOpt)

            ' EnC: We need to insert a hidden sequence point to handle function remapping in case 
            ' the containing method is edited while methods invoked in the condition are being executed.
            If rewrittenCondition IsNot Nothing AndAlso instrument Then
                Select Case statement.Kind
                    Case BoundKind.WhileStatement
                        rewrittenCondition = _instrumenterOpt.InstrumentWhileStatementCondition(DirectCast(statement, BoundWhileStatement), rewrittenCondition, _currentMethodOrLambda)
                    Case BoundKind.DoLoopStatement
                        rewrittenCondition = _instrumenterOpt.InstrumentDoLoopStatementCondition(DirectCast(statement, BoundDoLoopStatement), rewrittenCondition, _currentMethodOrLambda)
                    Case BoundKind.ForEachStatement
                        rewrittenCondition = _instrumenterOpt.InstrumentForEachStatementCondition(DirectCast(statement, BoundForEachStatement), rewrittenCondition, _currentMethodOrLambda)
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(statement.Kind)
                End Select
            End If

            Dim ifConditionGotoStart As BoundStatement = New BoundConditionalGoto(
                statementSyntax,
                rewrittenCondition,
                loopIfTrue,
                startLabel)

            If Not conditionResumeTargetOpt.IsDefaultOrEmpty Then
                ifConditionGotoStart = New BoundStatementList(ifConditionGotoStart.Syntax, conditionResumeTargetOpt.Add(ifConditionGotoStart))
            End If

            If instrument Then
                Select Case statement.Kind
                    Case BoundKind.WhileStatement
                        ifConditionGotoStart = _instrumenterOpt.InstrumentWhileStatementConditionalGotoStart(DirectCast(statement, BoundWhileStatement), ifConditionGotoStart)
                    Case BoundKind.DoLoopStatement
                        ifConditionGotoStart = _instrumenterOpt.InstrumentDoLoopStatementEntryOrConditionalGotoStart(DirectCast(statement, BoundDoLoopStatement), ifConditionGotoStart)
                    Case BoundKind.ForEachStatement
                        ifConditionGotoStart = _instrumenterOpt.InstrumentForEachStatementConditionalGotoStart(DirectCast(statement, BoundForEachStatement), ifConditionGotoStart)
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(statement.Kind)
                End Select
            End If

            ' While condition
            '    body
            ' End While
            '
            ' becomes
            '
            ' goto continue;
            ' start:
            ' body
            ' continue:
            ' {GotoIfTrue condition start}
            ' exit:

            'mark the initial jump as hidden.
            'We do not want to associate it with statement before.
            'This jump may be a target of another jump (for example if loops are nested) and that will make 
            'impression of the previous statement being re-executed
            Dim gotoContinue As BoundStatement = New BoundGotoStatement(statementSyntax, continueLabel, Nothing)

            If loopResumeLabelOpt IsNot Nothing Then
                gotoContinue = Concat(loopResumeLabelOpt, gotoContinue)
            End If

            If instrument Then
                gotoContinue = SyntheticBoundNodeFactory.HiddenSequencePoint(gotoContinue)
            End If

            Return New BoundStatementList(statementSyntax, ImmutableArray.Create(Of BoundStatement)(
                    gotoContinue,
                    New BoundLabelStatement(statementSyntax, startLabel),
                    rewrittenBody,
                    New BoundLabelStatement(statementSyntax, continueLabel),
                    ifConditionGotoStart,
                    New BoundLabelStatement(statementSyntax, exitLabel)
                ))

        End Function
    End Class
End Namespace
