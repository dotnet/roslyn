' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            Return RewriteWhileStatement(node.Syntax,
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
            syntaxNode As VBSyntaxNode,
            statementBeginSyntax As VBSyntaxNode,
            statementEndSyntax As VBSyntaxNode,
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

            If GenerateDebugInfo Then
                If statementEndSyntax IsNot Nothing Then
                    rewrittenBody = InsertBlockEpilogue(rewrittenBody, afterBodyResumeTargetOpt, statementEndSyntax)
                    afterBodyResumeTargetOpt = Nothing
                End If
            End If

            If afterBodyResumeTargetOpt IsNot Nothing Then
                rewrittenBody = Concat(rewrittenBody, afterBodyResumeTargetOpt)
            End If

            Dim ifConditionGotoStart As BoundStatement = New BoundConditionalGoto(
                                                                 syntaxNode,
                                                                 rewrittenCondition,
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
            Dim gotoContinue As BoundStatement = New BoundGotoStatement(syntaxNode, continueLabel, Nothing)

            If loopResumeLabelOpt IsNot Nothing Then
                gotoContinue = Concat(loopResumeLabelOpt, gotoContinue)
            End If

            If GenerateDebugInfo Then
                gotoContinue = New BoundSequencePoint(Nothing, gotoContinue)
            End If

            Return New BoundStatementList(syntaxNode, ImmutableArray.Create(Of BoundStatement)(
                    gotoContinue,
                    New BoundLabelStatement(syntaxNode, startLabel),
                    rewrittenBody,
                    New BoundLabelStatement(syntaxNode, continueLabel),
                    ifConditionGotoStart,
                    New BoundLabelStatement(syntaxNode, exitLabel)
                ))

        End Function
    End Class
End Namespace
