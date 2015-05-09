' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitIfStatement(node As BoundIfStatement) As BoundNode
            Dim syntax = node.Syntax

            Dim conditionSyntax As VisualBasicSyntaxNode = Nothing

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
            If GenerateDebugInfo Then
                Select Case syntax.Kind
                    Case SyntaxKind.MultiLineIfBlock
                        Dim asMultiline = DirectCast(syntax, MultiLineIfBlockSyntax)
                        conditionSyntax = asMultiline.IfStatement
                        newConsequence = InsertBlockEpilogue(newConsequence,
                                                             If(generateUnstructuredExceptionHandlingResumeCode AndAlso (OptimizationLevelIsDebug OrElse finishConsequenceWithResumeTarget),
                                                                RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(newConsequence.Syntax),
                                                                Nothing),
                                                             asMultiline.EndIfStatement)
                        finishConsequenceWithResumeTarget = False

                    Case SyntaxKind.ElseIfBlock
                        Dim asElseIfBlock = DirectCast(syntax, ElseIfBlockSyntax)
                        conditionSyntax = asElseIfBlock.ElseIfStatement
                        newConsequence = InsertBlockEpilogue(newConsequence,
                                                             If(generateUnstructuredExceptionHandlingResumeCode AndAlso (OptimizationLevelIsDebug OrElse finishConsequenceWithResumeTarget),
                                                                RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(newConsequence.Syntax),
                                                                Nothing),
                                                             DirectCast(syntax.Parent, MultiLineIfBlockSyntax).EndIfStatement)
                        finishConsequenceWithResumeTarget = False

                    Case SyntaxKind.SingleLineIfStatement
                        Dim asSingleLine = DirectCast(syntax, SingleLineIfStatementSyntax)
                        conditionSyntax = asSingleLine
                        ' single line if has no EndIf
                End Select
            End If

            If generateUnstructuredExceptionHandlingResumeCode AndAlso finishConsequenceWithResumeTarget Then
                newConsequence = Concat(newConsequence, RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(newConsequence.Syntax))
            End If

            ' == Rewrite Else
            Dim newAlternative As BoundStatement = DirectCast(Visit(node.AlternativeOpt), BoundStatement)

            If GenerateDebugInfo AndAlso newAlternative IsNot Nothing Then
                If syntax.Kind <> SyntaxKind.SingleLineIfStatement Then
                    Dim asElse = TryCast(node.AlternativeOpt.Syntax, ElseBlockSyntax)
                    If asElse IsNot Nothing Then
                        ' Update the resume table to make sure that we are in the right scope when we Resume Next on [End If].
                        newAlternative = InsertBlockEpilogue(newAlternative,
                                                             If(generateUnstructuredExceptionHandlingResumeCode AndAlso OptimizationLevelIsDebug,
                                                                RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(newAlternative.Syntax),
                                                                Nothing),
                                                             DirectCast(asElse.Parent, MultiLineIfBlockSyntax).EndIfStatement)
                        newAlternative = PrependWithSequencePoint(newAlternative, asElse.ElseStatement)
                    End If
                Else
                    Dim asElse = TryCast(node.AlternativeOpt.Syntax, SingleLineElseClauseSyntax)
                    If asElse IsNot Nothing Then
                        newAlternative = PrependWithSequencePoint(newAlternative, asElse, asElse.ElseKeyword.Span)
                        ' single line if has no EndIf
                    End If
                End If
            End If

            ' == Rewrite the whole node

            ' EnC: We need to insert a hidden sequence point to handle function remapping in case 
            ' the containing method is edited while methods invoked in the condition are being executed.
            Dim result As BoundStatement = RewriteIfStatement(
                node.Syntax,
                conditionSyntax,
                AddConditionSequencePoint(newCondition, node),
                newConsequence,
                newAlternative,
                generateDebugInfo:=True,
                unstructuredExceptionHandlingResumeTarget:=unstructuredExceptionHandlingResumeTarget)

            Return result
        End Function

        Private Function RewriteIfStatement(
            syntaxNode As VisualBasicSyntaxNode,
            conditionSyntax As VisualBasicSyntaxNode,
            rewrittenCondition As BoundExpression,
            rewrittenConsequence As BoundStatement,
            rewrittenAlternative As BoundStatement,
            generateDebugInfo As Boolean,
            Optional unstructuredExceptionHandlingResumeTarget As ImmutableArray(Of BoundStatement) = Nothing
        ) As BoundStatement
            Debug.Assert(unstructuredExceptionHandlingResumeTarget.IsDefaultOrEmpty OrElse generateDebugInfo)

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

                If Me.GenerateDebugInfo AndAlso generateDebugInfo Then

                    Select Case syntaxNode.Kind
                        Case SyntaxKind.MultiLineIfBlock
                            Dim asMultiline = DirectCast(syntaxNode, MultiLineIfBlockSyntax)

                            condGoto = New BoundSequencePoint(conditionSyntax, condGoto)

                            ' If it is a multiline If and there is no else, associate afterIf with EndIf
                            afterIfStatement = New BoundSequencePoint(asMultiline.EndIfStatement, afterIfStatement)
                        Case SyntaxKind.SingleLineIfStatement
                            Dim asSingleLine = DirectCast(syntaxNode, SingleLineIfStatementSyntax)

                            condGoto = New BoundSequencePointWithSpan(conditionSyntax, condGoto, TextSpan.FromBounds(asSingleLine.IfKeyword.SpanStart, asSingleLine.ThenKeyword.EndPosition - 1))

                            ' otherwise hide afterif (so that it does not associate with if body).
                            afterIfStatement = New BoundSequencePoint(Nothing, afterIfStatement)
                        Case Else
                            condGoto = New BoundSequencePoint(conditionSyntax, condGoto)

                            ' otherwise hide afterif (so that it does not associate with if body).
                            afterIfStatement = New BoundSequencePoint(Nothing, afterIfStatement)
                    End Select
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

                If Me.GenerateDebugInfo AndAlso generateDebugInfo Then
                    If syntaxNode.Kind = SyntaxKind.SingleLineIfStatement Then
                        Dim asSingleLine = DirectCast(syntaxNode, SingleLineIfStatementSyntax)

                        condGoto = New BoundSequencePointWithSpan(conditionSyntax, condGoto, TextSpan.FromBounds(asSingleLine.IfKeyword.SpanStart, asSingleLine.ThenKeyword.EndPosition - 1))
                    Else

                        condGoto = New BoundSequencePoint(conditionSyntax, condGoto)
                    End If
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
