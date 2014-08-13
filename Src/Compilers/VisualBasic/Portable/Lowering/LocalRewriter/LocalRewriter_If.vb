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
                        conditionSyntax = asMultiline.IfPart.Begin
                        newConsequence = InsertBlockEpilogue(newConsequence,
                                                             If(generateUnstructuredExceptionHandlingResumeCode,
                                                                RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(newConsequence.Syntax),
                                                                Nothing),
                                                             asMultiline.End)
                        finishConsequenceWithResumeTarget = False

                    Case SyntaxKind.ElseIfPart
                        Dim asIfPart = DirectCast(syntax, IfPartSyntax)
                        conditionSyntax = asIfPart.Begin
                        newConsequence = InsertBlockEpilogue(newConsequence,
                                                             If(generateUnstructuredExceptionHandlingResumeCode,
                                                                RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(newConsequence.Syntax),
                                                                Nothing),
                                                             DirectCast(syntax.Parent, MultiLineIfBlockSyntax).End)
                        finishConsequenceWithResumeTarget = False

                    Case SyntaxKind.SingleLineIfStatement
                        Dim asSingleLine = DirectCast(syntax, SingleLineIfStatementSyntax)
                        conditionSyntax = asSingleLine.IfPart.Begin
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
                    Dim asElse = TryCast(node.AlternativeOpt.Syntax, ElsePartSyntax)
                    If asElse IsNot Nothing Then
                        ' Update the resume table to make sure that we are in the right scope when we Resume Next on [End If].
                        newAlternative = InsertBlockEpilogue(newAlternative,
                                                             If(generateUnstructuredExceptionHandlingResumeCode,
                                                                RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(newAlternative.Syntax),
                                                                Nothing),
                                                             DirectCast(asElse.Parent, MultiLineIfBlockSyntax).End)
                        newAlternative = PrependWithSequencePoint(newAlternative, asElse.Begin)
                    End If
                Else
                    Dim asElse = TryCast(node.AlternativeOpt.Syntax, SingleLineElsePartSyntax)
                    If asElse IsNot Nothing Then
                        newAlternative = PrependWithSequencePoint(newAlternative, asElse.Begin)
                        ' single line if has no EndIf
                    End If
                End If
            End If

            ' == Rewrite the whole node
            Dim result As BoundStatement = RewriteIfStatement(node.Syntax, conditionSyntax, newCondition, newConsequence, newAlternative,
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
                    condGoto = New BoundSequencePoint(conditionSyntax, condGoto)

                    Dim asMultiline = TryCast(syntaxNode, MultiLineIfBlockSyntax)
                    If asMultiline IsNot Nothing Then
                        ' if it is a multiline If and there is no else, associate afterIf with EndIf
                        afterIfStatement = New BoundSequencePoint(asMultiline.End, afterIfStatement)
                    Else
                        ' otherwise hide afterif (so that it does not associate with if body.
                        afterIfStatement = New BoundSequencePoint(Nothing, afterIfStatement)
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

                If Me.GenerateDebugInfo AndAlso generateDebugInfo Then
                    condGoto = New BoundSequencePoint(conditionSyntax, condGoto)
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
