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
                                         DirectCast(node.Syntax, WhileBlockSyntax).WhileStatement,
                                         DirectCast(node.Syntax, WhileBlockSyntax).EndWhileStatement,
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
            statementBeginSyntax As VisualBasicSyntaxNode,
            statementEndSyntax As VisualBasicSyntaxNode,
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

            If GenerateDebugInfo Then
                If statementEndSyntax IsNot Nothing Then
                    rewrittenBody = InsertBlockEpilogue(rewrittenBody, afterBodyResumeTargetOpt, statementEndSyntax)
                    afterBodyResumeTargetOpt = Nothing
                End If
            End If

            If afterBodyResumeTargetOpt IsNot Nothing Then
                rewrittenBody = Concat(rewrittenBody, afterBodyResumeTargetOpt)
            End If

            ' EnC: We need to insert a hidden sequence point to handle function remapping in case 
            ' the containing method is edited while methods invoked in the condition are being executed.
            Dim ifConditionGotoStart As BoundStatement = New BoundConditionalGoto(
                statementSyntax,
                AddConditionSequencePoint(rewrittenCondition, statement),
                loopIfTrue,
                startLabel)

            If Not conditionResumeTargetOpt.IsDefaultOrEmpty Then
                ifConditionGotoStart = New BoundStatementList(ifConditionGotoStart.Syntax, conditionResumeTargetOpt.Add(ifConditionGotoStart))
            End If

            If Me.GenerateDebugInfo Then
                ' will be hidden or not, depending on statementBeginSyntax being nothing (for each) or not (real while loop)
                ifConditionGotoStart = New BoundSequencePoint(statementBeginSyntax, ifConditionGotoStart)
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

            If GenerateDebugInfo Then
                gotoContinue = New BoundSequencePoint(Nothing, gotoContinue)
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
