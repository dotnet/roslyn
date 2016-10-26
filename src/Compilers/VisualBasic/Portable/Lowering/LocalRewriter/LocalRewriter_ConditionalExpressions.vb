﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class LocalRewriter

        Public Overrides Function VisitBinaryConditionalExpression(node As BoundBinaryConditionalExpression) As BoundNode
            If Me._inExpressionLambda Then
                ' If we are inside expression lambda we want to keep binary conditional expression
                Return RewriteBinaryConditionalExpressionInExpressionLambda(node)
            End If

            If node.TestExpression.Type IsNot Nothing AndAlso node.TestExpression.Type.IsNullableType Then
                Return RewriteNullableBinaryConditionalExpression(node)
            End If

            Dim convertedTestExpression As BoundExpression = node.ConvertedTestExpression
            If convertedTestExpression Is Nothing Then
                Debug.Assert(node.TestExpressionPlaceholder Is Nothing)
                Return TransformRewrittenBinaryConditionalExpression(MyBase.VisitBinaryConditionalExpression(node))
            End If

            If convertedTestExpression.Kind = BoundKind.Conversion Then
                Dim boundConversion = DirectCast(node.ConvertedTestExpression, BoundConversion)
                Dim conversion = boundConversion.ConversionKind

                If Conversions.IsWideningConversion(conversion) AndAlso Conversions.IsCLRPredefinedConversion(conversion) AndAlso ((conversion And ConversionKind.TypeParameter) = 0) Then
                    Debug.Assert(boundConversion.Operand Is If(node.TestExpressionPlaceholder, node.TestExpression))

                    ' We can ignore conversion in this case
                    Return TransformRewrittenBinaryConditionalExpression(
                                node.Update(VisitExpressionNode(node.TestExpression),
                                            Nothing,
                                            Nothing,
                                            VisitExpressionNode(node.ElseExpression),
                                            node.ConstantValueOpt,
                                            node.Type))
                End If
            End If

            ' Rewrite binary conditional expression using ternary conditional expression

            Dim placeholder As BoundValuePlaceholderBase = node.TestExpressionPlaceholder

            ' Rewrite test expression and create a local to capture the value if needed
            Dim rewrittenTestExpression = VisitExpressionNode(node.TestExpression)
            Dim rewrittenTestExpressionType As TypeSymbol = rewrittenTestExpression.Type

            ' NOTE: we create a temp variable if only it is really needed; 
            '       locals and parameters don't need it
            Dim tempVariableSymbol As SynthesizedLocal = Nothing
            Dim placeholderSubstitute As BoundExpression = Nothing

            Select Case rewrittenTestExpression.Kind
                Case BoundKind.Local,
                     BoundKind.Parameter,
                     BoundKind.Literal
                    placeholderSubstitute = rewrittenTestExpression

                Case Else
                    '  create a temp variable
                    tempVariableSymbol = New SynthesizedLocal(Me._currentMethodOrLambda, rewrittenTestExpressionType, SynthesizedLocalKind.LoweringTemp)
                    '  temp variable reference
                    placeholderSubstitute = New BoundLocal(rewrittenTestExpression.Syntax,
                                                      tempVariableSymbol,
                                                      isLValue:=False,
                                                      type:=rewrittenTestExpressionType)
            End Select

            ' Rewrite test expression conversion 
            If placeholder IsNot Nothing Then
                ' substitute placeholder with temp created or a local/parameter
                AddPlaceholderReplacement(placeholder, placeholderSubstitute)
            End If

            Dim rewrittenConvertedTestExpression = VisitExpressionNode(node.ConvertedTestExpression)

            If placeholder IsNot Nothing Then
                RemovePlaceholderReplacement(placeholder)
            End If

            Dim ternaryConditionalExpression As BoundExpression = placeholderSubstitute

            If tempVariableSymbol IsNot Nothing Then
                '  if there is a temp local assign a value to it
                ternaryConditionalExpression =
                    New BoundAssignmentOperator(rewrittenTestExpression.Syntax,
                                                left:=New BoundLocal(rewrittenTestExpression.Syntax,
                                                                     tempVariableSymbol,
                                                                     isLValue:=True,
                                                                     type:=rewrittenTestExpressionType),
                                                right:=rewrittenTestExpression,
                                                suppressObjectClone:=True,
                                                type:=rewrittenTestExpressionType)
            End If

            ' NOTE: newTestExpression is not actually a boolean expression, but it is of a reference 
            '       type and has 'boolean semantic' (Nothing = False, True otherwise)
            Dim result As BoundExpression =
                TransformRewrittenTernaryConditionalExpression(
                    New BoundTernaryConditionalExpression(node.Syntax,
                                                          condition:=ternaryConditionalExpression,
                                                          whenTrue:=rewrittenConvertedTestExpression,
                                                          whenFalse:=VisitExpressionNode(node.ElseExpression),
                                                          constantValueOpt:=node.ConstantValueOpt,
                                                          type:=node.Type))

            If tempVariableSymbol Is Nothing Then
                Return result
            End If

            Return New BoundSequence(result.Syntax,
                                     locals:=ImmutableArray.Create(Of LocalSymbol)(tempVariableSymbol),
                                     sideEffects:=ImmutableArray(Of BoundExpression).Empty,
                                     valueOpt:=result,
                                     type:=result.Type)
        End Function

        Private Function RewriteBinaryConditionalExpressionInExpressionLambda(node As BoundBinaryConditionalExpression) As BoundExpression
            ' If we are inside expression lambda we want to keep binary conditional expression
            Dim testExpression As BoundExpression = node.TestExpression
            Dim testExpressionType As TypeSymbol = testExpression.Type
            Dim rewrittenTestExpression As BoundExpression = VisitExpression(testExpression)
            Debug.Assert(testExpressionType = rewrittenTestExpression.Type)

            Dim rewrittenWhenTrue As BoundExpression = Nothing

            Dim convertedTestExpression As BoundExpression = node.ConvertedTestExpression
            If convertedTestExpression Is Nothing Then
                If Not testExpressionType.IsNullableOfBoolean Then
                    rewrittenWhenTrue = If(testExpressionType.IsNullableType,
                                           New BoundConversion(rewrittenTestExpression.Syntax,
                                                               rewrittenTestExpression,
                                                               ConversionKind.WideningNullable,
                                                               False,
                                                               False,
                                                               testExpressionType.GetNullableUnderlyingTypeOrSelf),
                                           rewrittenTestExpression)
                End If
            Else
                Debug.Assert(node.TestExpressionPlaceholder Is Nothing OrElse node.TestExpressionPlaceholder.Type = testExpressionType.GetNullableUnderlyingTypeOrSelf)
                rewrittenWhenTrue = VisitExpressionNode(convertedTestExpression,
                                                        node.TestExpressionPlaceholder,
                                                        If(testExpressionType.IsNullableType,
                                                           New BoundConversion(rewrittenTestExpression.Syntax,
                                                               rewrittenTestExpression,
                                                               ConversionKind.WideningNullable,
                                                               False,
                                                               False,
                                                               testExpressionType.GetNullableUnderlyingTypeOrSelf),
                                                           rewrittenTestExpression))
            End If

            Dim rewrittenWhenFalse As BoundExpression = VisitExpression(node.ElseExpression)
            Return node.Update(rewrittenTestExpression, rewrittenWhenTrue, Nothing, rewrittenWhenFalse, node.ConstantValueOpt, node.Type)
        End Function

        Private Shared Function TransformRewrittenBinaryConditionalExpression(node As BoundNode) As BoundNode
            Return If(node.Kind <> BoundKind.BinaryConditionalExpression, node,
                      TransformRewrittenBinaryConditionalExpression(DirectCast(node, BoundBinaryConditionalExpression)))
        End Function

        Private Shared Function TransformRewrittenBinaryConditionalExpression(node As BoundBinaryConditionalExpression) As BoundExpression
            Debug.Assert(node.ConvertedTestExpression Is Nothing)   ' Those should be rewritten by now
            Debug.Assert(node.TestExpressionPlaceholder Is Nothing)

            ' NOTE: C# implementation rewrites the coalesce expression to handle the bug 
            '       related to IF(DirectCast(<class1>, I1), DirectCast(<class2>, I1))
            '       VB handles this case in Emitter

            If node.HasErrors Then
                Return node
            End If

            Dim testExpr = node.TestExpression
            Dim elseExpr = node.ElseExpression

            ' Test expression may only be of a reference or nullable type
            Debug.Assert(testExpr.IsNothingLiteral OrElse testExpr.Type.IsReferenceType OrElse testExpr.Type.IsNullableType)

            ' TODO: Checking type equality of test and else is not strictly needed.
            '       Consider removing this requirement.
            If testExpr.IsConstant AndAlso (testExpr.Type = elseExpr.Type) Then
                '  the only valid IF(...) with the first constant are: IF("abc", <expr>) or IF(Nothing, <expr>)
                If testExpr.ConstantValueOpt.IsNothing Then
                    ' CASE: IF(Nothing, <expr>) 
                    '   Special case: just emit ElseExpression
                    Return node.ElseExpression
                Else
                    ' CASE: IF("abc", <expr>) 
                    '   Dominant type may be different from String, so add conversion
                    Return testExpr
                End If

            End If

            Debug.Assert(testExpr.Type.IsReferenceType)
            Return node
        End Function

        Private Function RewriteNullableBinaryConditionalExpression(node As BoundBinaryConditionalExpression) As BoundNode
            ' == rewrite operands and check for trivial cases
            Dim rewrittenLeft = Me.VisitExpressionNode(node.TestExpression)
            If HasValue(rewrittenLeft) Then
                Return MakeResultFromNonNullLeft(rewrittenLeft, node.ConvertedTestExpression, node.TestExpressionPlaceholder)
            End If

            Dim rewrittenRight = Me.VisitExpressionNode(node.ElseExpression)
            If HasNoValue(rewrittenLeft) Then
                Return rewrittenRight
            End If

            Dim whenNotNull As BoundExpression = Nothing
            Dim whenNull As BoundExpression = Nothing
            If IsConditionalAccess(rewrittenLeft, whenNotNull, whenNull) Then
                If HasNoValue(whenNull) Then
                    If HasValue(whenNotNull) Then
                        Return UpdateConditionalAccess(rewrittenLeft,
                                                       MakeResultFromNonNullLeft(whenNotNull, node.ConvertedTestExpression, node.TestExpressionPlaceholder),
                                                       rewrittenRight)

                    Else
                        Debug.Assert(Not HasNoValue(whenNotNull)) ' Not optimizing for this case

                        ' CONSIDER: We could do inlining when rewrittenRight.IsConstant
                    End If
                End If
            End If

            '=== Rewrite binary conditional expression using ternary conditional expression
            Dim temp As SynthesizedLocal = Nothing
            Dim tempInit As BoundExpression = Nothing

            ' no need to capture locals since we will not 
            ' evaluate anything between HasValue and ValueOrDefault
            Dim capturedleft As BoundExpression = CaptureNullableIfNeeded(rewrittenLeft,
                                                                          temp,
                                                                          tempInit,
                                                                          doNotCaptureLocals:=True)

            Dim condition As BoundExpression = NullableHasValue(If(tempInit, capturedleft))
            Dim whenTrue As BoundExpression

            If node.ConvertedTestExpression Is Nothing Then
                whenTrue = NullableValueOrDefault(capturedleft)
            Else
                If capturedleft.Type.IsSameTypeIgnoringAll(node.ConvertedTestExpression.Type) Then
                    ' Optimization
                    whenTrue = capturedleft
                Else
                    whenTrue = VisitExpressionNode(node.ConvertedTestExpression,
                                               node.TestExpressionPlaceholder,
                                               NullableValueOrDefault(capturedleft))
                End If
            End If

            Dim result As BoundExpression = MakeTernaryConditionalExpression(node.Syntax, condition, whenTrue, rewrittenRight)

            ' if we used a temp, arrange a sequence for it
            If temp IsNot Nothing Then
                result = New BoundSequence(node.Syntax,
                                     ImmutableArray.Create(Of LocalSymbol)(temp),
                                     ImmutableArray(Of BoundExpression).Empty,
                                     result,
                                     result.Type)
            End If

            Return result
        End Function

        Private Function MakeResultFromNonNullLeft(rewrittenLeft As BoundExpression, convertedTestExpression As BoundExpression, testExpressionPlaceholder As BoundRValuePlaceholder) As BoundExpression
            If convertedTestExpression Is Nothing Then
                Return NullableValueOrDefault(rewrittenLeft)
            Else
                If rewrittenLeft.Type.IsSameTypeIgnoringAll(convertedTestExpression.Type) Then
                    ' Optimization
                    Return rewrittenLeft
                End If

                Return VisitExpressionNode(convertedTestExpression,
                                           testExpressionPlaceholder,
                                           NullableValueOrDefault(rewrittenLeft))
            End If
        End Function

        Private Function VisitExpressionNode(node As BoundExpression,
                                    placeholder As BoundValuePlaceholderBase,
                                    placeholderSubstitute As BoundExpression) As BoundExpression

            If placeholder IsNot Nothing Then
                AddPlaceholderReplacement(placeholder, placeholderSubstitute)
            End If

            Dim result = VisitExpressionNode(node)

            If placeholder IsNot Nothing Then
                RemovePlaceholderReplacement(placeholder)
            End If

            Return result
        End Function

        Public Overrides Function VisitTernaryConditionalExpression(node As BoundTernaryConditionalExpression) As BoundNode
            Return TransformRewrittenTernaryConditionalExpression(
                        DirectCast(MyBase.VisitTernaryConditionalExpression(node), BoundTernaryConditionalExpression))
        End Function

        Private Shared Function TransformRewrittenTernaryConditionalExpression(node As BoundTernaryConditionalExpression) As BoundExpression
            If node.Condition.IsConstant AndAlso node.WhenTrue.IsConstant AndAlso node.WhenFalse.IsConstant Then
                ' This optimization be applies if only *all three* operands are constants!!!

                Debug.Assert(node.Condition.ConstantValueOpt.IsBoolean OrElse
                             node.Condition.ConstantValueOpt.IsNothing OrElse
                             node.Condition.ConstantValueOpt.IsString)

                Dim value As Boolean = If(node.Condition.ConstantValueOpt.IsBoolean,
                                          node.Condition.ConstantValueOpt.BooleanValue,
                                          node.Condition.ConstantValueOpt.IsString)

                Return If(value, node.WhenTrue, node.WhenFalse)
            End If
            Return node
        End Function

    End Class
End Namespace
