﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class LocalRewriter
        Public Overrides Function VisitNullableIsTrueOperator(node As BoundNullableIsTrueOperator) As BoundNode
            Debug.Assert(node.Operand.Type.IsNullableOfBoolean())

            Dim optimizableForConditionalBranch As Boolean = False
            Dim operand = VisitExpression(AdjustIfOptimizableForConditionalBranch(node.Operand, optimizableForConditionalBranch))

            If optimizableForConditionalBranch AndAlso HasValue(operand) Then
                Return NullableValueOrDefault(operand)
            End If

            If _inExpressionLambda Then
                Return node.Update(operand, node.Type)
            End If

            If HasNoValue(operand) Then
                Return New BoundLiteral(node.Syntax, ConstantValue.False, node.Type)
            End If

            Dim whenNotNull As BoundExpression = Nothing
            Dim whenNull As BoundExpression = Nothing
            If IsConditionalAccess(operand, whenNotNull, whenNull) Then
                If HasNoValue(whenNull) Then
                    Debug.Assert(Not HasNoValue(whenNotNull))
                    Return UpdateConditionalAccess(operand,
                                              NullableValueOrDefault(whenNotNull),
                                              New BoundLiteral(node.Syntax, ConstantValue.False, node.Type))
                End If
            End If

            Return NullableValueOrDefault(operand)
        End Function

        Private Shared Function AdjustIfOptimizableForConditionalBranch(operand As BoundExpression, <Out> ByRef optimizableForConditionalBranch As Boolean) As BoundExpression
            optimizableForConditionalBranch = False

            Dim current As BoundExpression = operand
            Do
                Select Case current.Kind
                    Case BoundKind.BinaryOperator
                        Dim binary = DirectCast(current, BoundBinaryOperator)
                        If (binary.OperatorKind And BinaryOperatorKind.IsOperandOfConditionalBranch) <> 0 Then
                            Select Case (binary.OperatorKind And BinaryOperatorKind.OpMask)
                                Case BinaryOperatorKind.AndAlso, BinaryOperatorKind.OrElse
                                    Debug.Assert((binary.OperatorKind And BinaryOperatorKind.Lifted) <> 0)
                                    optimizableForConditionalBranch = True
                                    Return binary.Update(binary.OperatorKind Or BinaryOperatorKind.OptimizableForConditionalBranch,
                                                         binary.Left, binary.Right, binary.Checked, binary.ConstantValueOpt, binary.Type)
                            End Select
                        End If

                        Return operand

                    Case BoundKind.Parenthesized
                        current = DirectCast(current, BoundParenthesized).Expression
                    Case Else
                        Return operand
                End Select
            Loop

        End Function

        Public Overrides Function VisitUserDefinedUnaryOperator(node As BoundUserDefinedUnaryOperator) As BoundNode
            If _inExpressionLambda Then
                Return node.Update(node.OperatorKind, VisitExpression(node.UnderlyingExpression), node.Type)
            End If

            If (node.OperatorKind And UnaryOperatorKind.Lifted) <> 0 Then
                Return RewriteLiftedUserDefinedUnaryOperator(node)
            End If

            Return Visit(node.UnderlyingExpression)
        End Function

        Public Overrides Function VisitUnaryOperator(node As BoundUnaryOperator) As BoundNode
            If (node.OperatorKind And UnaryOperatorKind.Lifted) = 0 OrElse _inExpressionLambda Then
                Dim result As BoundNode = MyBase.VisitUnaryOperator(node)
                If result.Kind = BoundKind.UnaryOperator Then

                    result = RewriteUnaryOperator(DirectCast(result, BoundUnaryOperator))
                End If
                Return result

            Else
                Return RewriteLiftedUnaryOperator(node)
            End If
        End Function

        Private Function RewriteUnaryOperator(node As BoundUnaryOperator) As BoundExpression
            Dim result As BoundExpression = node
            Dim kind As UnaryOperatorKind = node.OperatorKind

            If Not node.HasErrors AndAlso ((kind And UnaryOperatorKind.Lifted) = 0) AndAlso (kind <> UnaryOperatorKind.Error) AndAlso Not _inExpressionLambda Then
                Dim opType As TypeSymbol = node.Type

                If opType.IsObjectType() Then
                    result = RewriteObjectUnaryOperator(node)
                ElseIf opType.IsDecimalType() Then
                    result = RewriteDecimalUnaryOperator(node)
                End If
            End If

            Return result
        End Function

        Private Function RewriteObjectUnaryOperator(node As BoundUnaryOperator) As BoundExpression
            Debug.Assert(node.Operand.Type.IsObjectType() AndAlso node.Type.IsObjectType())

            Dim result As BoundExpression = node
            Dim opKind = (node.OperatorKind And UnaryOperatorKind.IntrinsicOpMask)

            Dim member As WellKnownMember

            If opKind = UnaryOperatorKind.Plus Then
                member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__PlusObjectObject
            ElseIf opKind = UnaryOperatorKind.Minus Then
                member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__NegateObjectObject
            Else
                Debug.Assert(opKind = UnaryOperatorKind.Not)
                member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__NotObjectObject
            End If

            ' Call member(operand)
            Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                       ImmutableArray.Create(node.Operand), Nothing, memberSymbol.ReturnType)
            End If

            Return result
        End Function

        Private Function RewriteDecimalUnaryOperator(node As BoundUnaryOperator) As BoundExpression
            Debug.Assert(node.Operand.Type.IsDecimalType() AndAlso node.Type.IsDecimalType())

            Dim result As BoundExpression = node
            Dim opKind = (node.OperatorKind And UnaryOperatorKind.IntrinsicOpMask)

            If opKind = UnaryOperatorKind.Plus Then
                result = node.Operand
            Else
                Debug.Assert(opKind = UnaryOperatorKind.Minus)

                ' Call Decimal.Negate(operand)

                Const memberId As SpecialMember = SpecialMember.System_Decimal__NegateDecimal
                Dim memberSymbol = DirectCast(ContainingAssembly.GetSpecialTypeMember(memberId), MethodSymbol)

                If Not ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                    result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                           ImmutableArray.Create(node.Operand), Nothing, memberSymbol.ReturnType)
                End If
            End If

            Return result
        End Function

        Private Function RewriteLiftedUnaryOperator(node As BoundUnaryOperator) As BoundNode
            Dim operand As BoundExpression = VisitExpressionNode(node.Operand)

            ' null stays null
            If HasNoValue(operand) Then
                ' return new R?()
                Return NullableNull(operand, node.Type)
            End If

            ' we know that operand is not null, just wrap the result of unlifted op
            If HasValue(operand) Then
                ' return new R?(UnliftedOp(operand))
                Dim unliftedOp = ApplyUnliftedUnaryOp(node, NullableValueOrDefault(operand))

                Return WrapInNullable(unliftedOp, node.Type)
            End If

            ' NOTE: in some cases (like if operand is a local) we do not need the temp
            '
            ' return {.temp
            '           temp = operand
            '           iif (temp.HasValue, 
            '               new R?(UnliftedOp(operand)), 
            '               new R?)
            '        }
            Dim temp As SynthesizedLocal = Nothing
            Dim tempInit As BoundExpression = Nothing

            ' No need to capture locals in Unary since we will not eval another operand
            Dim capturedOperand = CaptureNullableIfNeeded(operand, temp, tempInit, doNotCaptureLocals:=True)

            Dim unliftedOpOnCaptured = ApplyUnliftedUnaryOp(node, NullableValueOrDefault(capturedOperand))
            Dim value As BoundExpression = MakeTernaryConditionalExpression(node.Syntax,
                                                              NullableHasValue(capturedOperand),
                                                              WrapInNullable(unliftedOpOnCaptured, node.Type),
                                                              capturedOperand)

            ' if we used a temp, arrange a sequence for it and its initialization
            If temp IsNot Nothing Then
                value = New BoundSequence(node.Syntax,
                                     ImmutableArray.Create(Of LocalSymbol)(temp),
                                     ImmutableArray.Create(tempInit),
                                     value,
                                     value.Type)
            End If

            Return value
        End Function

        Private Function RewriteLiftedUserDefinedUnaryOperator(node As BoundUserDefinedUnaryOperator) As BoundExpression
            '
            ' Lifted user defined operator has structure as the following:
            '            
            '                    |          
            '             [implicit wrap]
            '                    |
            '                  CALL
            '                    |
            '           [implicit unwrap]
            '                    |                   |
            '                OPERAND
            '
            ' Implicit unwrapping conversion if present is always O? -> O 
            ' It is encoded as a disparity between CALL argument type and parameter type of the call symbol.
            '
            ' Implicit wrapping conversion of the result, if present, is always T -> T?
            '
            ' The rewrite is:
            '   If (OPERAND.HasValue, CALL(OPERAND), Null)
            '
            ' Note that the result of the operator is nullable type. 

            Dim operand = Me.VisitExpressionNode(node.Operand)
            Dim operatorCall = node.Call

            Dim resultType = operatorCall.Type

            Debug.Assert(resultType.IsNullableType())
            Dim whenHasNoValue = NullableNull(node.Syntax, resultType)

            Debug.Assert(operand.Type.IsNullableType, "operand must be nullable")

            Dim operandHasNoValue As Boolean = HasNoValue(operand)

            ' TRIVIAL CASE
            If operandHasNoValue Then
                Return whenHasNoValue
            End If

            Dim temps As ArrayBuilder(Of LocalSymbol) = Nothing
            Dim inits As ArrayBuilder(Of BoundExpression) = Nothing

            ' PREPARE OPERAND
            Dim operandHasValue As Boolean = HasValue(operand)
            Dim callInput As BoundExpression
            Dim operandHasValueExpression As BoundExpression = Nothing

            If operandHasValue Then
                callInput = NullableValueOrDefault(operand)
            Else
                callInput = ProcessNullableOperand(operand, operandHasValueExpression, temps, inits, doNotCaptureLocals:=True)
            End If

            Debug.Assert(callInput.Type.IsSameTypeIgnoringAll(operatorCall.Method.Parameters(0).Type),
                         "operator must take unwrapped value of the operand")

            Dim whenHasValue As BoundExpression = operatorCall.Update(operatorCall.Method,
                                                                       Nothing,
                                                                       operatorCall.ReceiverOpt,
                                                                       ImmutableArray.Create(Of BoundExpression)(callInput),
                                                                       Nothing,
                                                                       operatorCall.ConstantValueOpt,
                                                                       isLValue:=operatorCall.IsLValue,
                                                                       suppressObjectClone:=operatorCall.SuppressObjectClone,
                                                                       type:=operatorCall.Method.ReturnType)

            If Not whenHasValue.Type.IsSameTypeIgnoringAll(resultType) Then
                whenHasValue = WrapInNullable(whenHasValue, resultType)
            End If

            Debug.Assert(whenHasValue.Type.IsSameTypeIgnoringAll(resultType), "result type must be same as resultType")

            ' RESULT

            If operandHasValue Then
                Debug.Assert(temps Is Nothing AndAlso inits Is Nothing)
                Return whenHasValue

            Else
                Dim condition As BoundExpression = operandHasValueExpression
                Dim result As BoundExpression = MakeTernaryConditionalExpression(node.Syntax,
                                                               condition,
                                                               whenHasValue,
                                                               whenHasNoValue)

                ' if we used a temp, arrange a sequence for it
                If temps IsNot Nothing Then
                    result = New BoundSequence(node.Syntax,
                                         temps.ToImmutableAndFree,
                                         inits.ToImmutableAndFree,
                                         result,
                                         result.Type)
                End If

                Return result
            End If

        End Function

        Private Function ApplyUnliftedUnaryOp(originalOperator As BoundUnaryOperator, operandValue As BoundExpression) As BoundExpression
            Debug.Assert(Not operandValue.Type.IsNullableType)

            'return UnliftedOP(operandValue)
            Dim unliftedOpKind = originalOperator.OperatorKind And (Not UnaryOperatorKind.Lifted)

            Return RewriteUnaryOperator(
                        New BoundUnaryOperator(originalOperator.Syntax,
                                               unliftedOpKind,
                                               operandValue,
                                               originalOperator.Checked,
                                               originalOperator.Type.GetNullableUnderlyingType))
        End Function
    End Class

End Namespace
