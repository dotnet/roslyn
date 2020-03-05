' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitIfStatement(node As BoundIfStatement) As BoundNode
            Dim syntax = node.Syntax

            Dim generateUnstructuredExceptionHandlingResumeCode As Boolean = ShouldGenerateUnstructuredExceptionHandlingResumeCode(node)
            Dim unstructuredExceptionHandlingResumeTarget As ImmutableArray(Of BoundStatement) = Nothing

            If generateUnstructuredExceptionHandlingResumeCode Then
                unstructuredExceptionHandlingResumeTarget = RegisterUnstructuredExceptionHandlingResumeTarget(syntax, canThrow:=True)
            End If

            ' == Rewrite Condition
            Dim newCondition = VisitExpressionNode(node.Condition)

            ' == Rewrite Consequence
            Dim newConsequence As BoundStatement = DirectCast(Visit(node.Consequence), BoundStatement)

            ' Update the resume table so exceptions that occur in the [Consequence] don't fall through to 
            ' [AlternativeOpt] upon Resume Next and to make sure that we are in the right scope when we 
            ' Resume Next on [End If].
            Dim finishConsequenceWithResumeTarget As Boolean = (node.AlternativeOpt IsNot Nothing)

            ' need to add a sequence point before the body
            ' make sure SP is outside of the block if consequence is a block
            ' also add SP for End If after the consequence
            ' make sure SP is inside the block if the consequence is a block
            ' we must still have the block in scope when stopped on End If
            Dim instrument As Boolean = Me.Instrument(node)

            If instrument Then
                Dim resumeTarget As BoundStatement = Nothing

                Select Case syntax.Kind
                    Case SyntaxKind.MultiLineIfBlock,
                         SyntaxKind.ElseIfBlock
                        If generateUnstructuredExceptionHandlingResumeCode AndAlso (OptimizationLevelIsDebug OrElse finishConsequenceWithResumeTarget) Then
                            resumeTarget = RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(newConsequence.Syntax)
                        End If

                        finishConsequenceWithResumeTarget = False

                    Case SyntaxKind.SingleLineIfStatement
                        ' single line if has no EndIf

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(syntax.Kind)
                End Select

                newConsequence = Concat(newConsequence, _instrumenterOpt.InstrumentIfStatementConsequenceEpilogue(node, resumeTarget))
            End If

            If generateUnstructuredExceptionHandlingResumeCode AndAlso finishConsequenceWithResumeTarget Then
                newConsequence = Concat(newConsequence, RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(newConsequence.Syntax))
            End If

            ' == Rewrite Else
            Dim newAlternative As BoundStatement = DirectCast(Visit(node.AlternativeOpt), BoundStatement)

            If instrument AndAlso newAlternative IsNot Nothing Then
                If syntax.Kind <> SyntaxKind.SingleLineIfStatement Then
                    Dim asElse = TryCast(node.AlternativeOpt.Syntax, ElseBlockSyntax)
                    If asElse IsNot Nothing Then
                        ' Update the resume table to make sure that we are in the right scope when we Resume Next on [End If].
                        Dim resumeTarget As BoundStatement = Nothing
                        If generateUnstructuredExceptionHandlingResumeCode AndAlso OptimizationLevelIsDebug Then
                            resumeTarget = RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(newAlternative.Syntax)
                        End If

                        newAlternative = Concat(newAlternative,
                                                _instrumenterOpt.InstrumentIfStatementAlternativeEpilogue(node, resumeTarget))
                        newAlternative = PrependWithPrologue(newAlternative, _instrumenterOpt.CreateIfStatementAlternativePrologue(node))
                    End If
                Else
                    Dim asElse = TryCast(node.AlternativeOpt.Syntax, SingleLineElseClauseSyntax)
                    If asElse IsNot Nothing Then
                        newAlternative = PrependWithPrologue(newAlternative, _instrumenterOpt.CreateIfStatementAlternativePrologue(node))
                        ' single line if has no EndIf
                    End If
                End If
            End If

            ' == Rewrite the whole node

            ' EnC: We need to insert a hidden sequence point to handle function remapping in case 
            ' the containing method is edited while methods invoked in the condition are being executed.
            Debug.Assert(newCondition IsNot Nothing)
            If instrument Then
                newCondition = _instrumenterOpt.InstrumentIfStatementCondition(node, newCondition, _currentMethodOrLambda)
            End If

            Dim result As BoundStatement = RewriteIfStatement(
                node.Syntax,
                newCondition,
                newConsequence,
                newAlternative,
                instrumentationTargetOpt:=If(instrument, node, Nothing),
                unstructuredExceptionHandlingResumeTarget:=unstructuredExceptionHandlingResumeTarget)

            Return result
        End Function

        Private Function RewriteIfStatement(
            syntaxNode As SyntaxNode,
            rewrittenCondition As BoundExpression,
            rewrittenConsequence As BoundStatement,
            rewrittenAlternative As BoundStatement,
            instrumentationTargetOpt As BoundStatement,
            Optional unstructuredExceptionHandlingResumeTarget As ImmutableArray(Of BoundStatement) = Nothing
        ) As BoundStatement
            Debug.Assert(unstructuredExceptionHandlingResumeTarget.IsDefaultOrEmpty OrElse instrumentationTargetOpt IsNot Nothing)

            ' Note that ElseIf clauses are transformed into a nested if inside an else at the bound tree generation, so 
            ' a BoundIfStatement does not contain ElseIf clauses.

            Dim afterif = GenerateLabel("afterif")

            Dim afterIfStatement As BoundStatement = New BoundLabelStatement(syntaxNode, afterif)

            If rewrittenAlternative Is Nothing Then
                ' if (condition) 
                '   consequence;  
                '
                ' becomes
                '
                ' GotoIfFalse condition afterif;
                ' consequence;
                ' afterif:

                Dim condGoto As BoundStatement = New BoundConditionalGoto(syntaxNode, rewrittenCondition, False, afterif)

                If Not unstructuredExceptionHandlingResumeTarget.IsDefaultOrEmpty Then
                    condGoto = New BoundStatementList(condGoto.Syntax, unstructuredExceptionHandlingResumeTarget.Add(condGoto))
                End If

                If instrumentationTargetOpt IsNot Nothing Then

                    Select Case instrumentationTargetOpt.Syntax.Kind
                        Case SyntaxKind.MultiLineIfBlock,
                             SyntaxKind.ElseIfBlock,
                             SyntaxKind.SingleLineIfStatement
                            condGoto = _instrumenterOpt.InstrumentIfStatementConditionalGoto(DirectCast(instrumentationTargetOpt, BoundIfStatement), condGoto)
                        Case SyntaxKind.CaseBlock
                            condGoto = _instrumenterOpt.InstrumentCaseBlockConditionalGoto(DirectCast(instrumentationTargetOpt, BoundCaseBlock), condGoto)
                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(instrumentationTargetOpt.Syntax.Kind)
                    End Select

                    If instrumentationTargetOpt.Syntax.Kind = SyntaxKind.MultiLineIfBlock Then
                        ' If it is a multiline If and there is no else, associate afterIf with EndIf
                        afterIfStatement = _instrumenterOpt.InstrumentIfStatementAfterIfStatement(DirectCast(instrumentationTargetOpt, BoundIfStatement), afterIfStatement)
                    Else
                        ' otherwise hide afterif (so that it does not associate with if body).
                        afterIfStatement = SyntheticBoundNodeFactory.HiddenSequencePoint(afterIfStatement)
                    End If
                End If

                Return New BoundStatementList(syntaxNode, ImmutableArray.Create(condGoto, rewrittenConsequence, afterIfStatement))
            Else
                ' if (condition)
                '     consequence;
                ' else 
                '     alternative
                '
                ' becomes
                '
                ' GotoIfFalse condition alt;
                ' consequence
                ' goto afterif;
                ' alt:
                ' alternative;
                ' afterif:

                Dim alt = GenerateLabel("alternative")
                Dim condGoto As BoundStatement = New BoundConditionalGoto(syntaxNode, rewrittenCondition, False, alt)

                If Not unstructuredExceptionHandlingResumeTarget.IsDefaultOrEmpty Then
                    condGoto = New BoundStatementList(condGoto.Syntax, unstructuredExceptionHandlingResumeTarget.Add(condGoto))
                End If

                If instrumentationTargetOpt IsNot Nothing Then
                    Select Case instrumentationTargetOpt.Syntax.Kind
                        Case SyntaxKind.MultiLineIfBlock,
                             SyntaxKind.ElseIfBlock,
                             SyntaxKind.SingleLineIfStatement
                            condGoto = _instrumenterOpt.InstrumentIfStatementConditionalGoto(DirectCast(instrumentationTargetOpt, BoundIfStatement), condGoto)
                        Case SyntaxKind.CaseBlock
                            condGoto = _instrumenterOpt.InstrumentCaseBlockConditionalGoto(DirectCast(instrumentationTargetOpt, BoundCaseBlock), condGoto)
                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(instrumentationTargetOpt.Syntax.Kind)
                    End Select
                End If

                Return New BoundStatementList(syntaxNode, ImmutableArray.Create(Of BoundStatement)(
                                              condGoto,
                                              rewrittenConsequence,
                                              New BoundGotoStatement(syntaxNode, afterif, Nothing),
                                              New BoundLabelStatement(syntaxNode, alt),
                                              rewrittenAlternative,
                                              afterIfStatement))
            End If
        End Function

    End Class
End Namespace
