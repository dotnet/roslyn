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

        Private Shared Function ShouldCaptureConditionalAccessReceiver(receiver As BoundExpression) As Boolean
            Select Case receiver.Kind
                Case BoundKind.MeReference
                    Return False

                Case BoundKind.Parameter
                    Return DirectCast(receiver, BoundParameter).ParameterSymbol.IsByRef

                Case BoundKind.Local
                    Return DirectCast(receiver, BoundLocal).LocalSymbol.IsByRef

                Case Else
                    Return Not receiver.IsDefaultValue()
            End Select
        End Function

        Public Overrides Function VisitConditionalAccess(node As BoundConditionalAccess) As BoundNode
            Debug.Assert(node.Type IsNot Nothing)

            Dim rewrittenReceiver As BoundExpression = VisitExpressionNode(node.Receiver)
            Dim receiverType As TypeSymbol = rewrittenReceiver.Type

            Dim receiverOrCondition As BoundExpression
            Dim placeholderReplacement As BoundExpression
            Dim newPlaceholderId As Integer = 0
            Dim newPlaceHolder As BoundConditionalAccessReceiverPlaceholder
            Dim captureReceiver As Boolean
            Dim temp As LocalSymbol = Nothing
            Dim assignment As BoundExpression = Nothing
            Dim needWhenNotNullPart As Boolean = True
            Dim needWhenNullPart As Boolean = True

            Dim factory = New SyntheticBoundNodeFactory(_topMethod, _currentMethodOrLambda, node.Syntax, _compilationState, _diagnostics)

            If receiverType.IsNullableType() Then
                ' if( receiver.HasValue, receiver.GetValueOrDefault(). ... -> to Nullable, Nothing) 
                If HasNoValue(rewrittenReceiver) Then
                    ' Nothing
                    receiverOrCondition = Nothing
                    needWhenNotNullPart = False
                    placeholderReplacement = Nothing
                ElseIf HasValue(rewrittenReceiver) Then
                    ' receiver. ... -> to Nullable
                    receiverOrCondition = Nothing
                    needWhenNullPart = False
                    placeholderReplacement = NullableValueOrDefault(rewrittenReceiver)
                Else
                    Dim first As BoundExpression

                    If ShouldCaptureConditionalAccessReceiver(rewrittenReceiver) Then
                        temp = New SynthesizedLocal(Me._currentMethodOrLambda, receiverType, SynthesizedLocalKind.LoweringTemp)

                        assignment = factory.AssignmentExpression(factory.Local(temp, isLValue:=True), rewrittenReceiver.MakeRValue())
                        first = factory.Local(temp, isLValue:=True)
                        placeholderReplacement = factory.Local(temp, isLValue:=True)
                    Else
                        first = rewrittenReceiver
                        placeholderReplacement = rewrittenReceiver
                    End If

                    receiverOrCondition = NullableHasValue(first)
                    placeholderReplacement = NullableValueOrDefault(placeholderReplacement)
                End If

                captureReceiver = False
                newPlaceHolder = Nothing
            Else

                If rewrittenReceiver.IsConstant Then
                    receiverOrCondition = Nothing
                    captureReceiver = False
                    newPlaceHolder = Nothing

                    If rewrittenReceiver.ConstantValueOpt.IsNothing Then
                        ' Nothing
                        placeholderReplacement = Nothing
                        needWhenNotNullPart = False
                    Else
                        ' receiver. ... -> to Nullable
                        placeholderReplacement = rewrittenReceiver.MakeRValue()
                        needWhenNullPart = False
                    End If
                Else
                    ' if( receiver IsNot Nothing, receiver. ... -> to Nullable, Nothing) 
                    receiverOrCondition = rewrittenReceiver

                    ' we need a copy if we deal with nonlocal value (to capture the value)
                    ' Or if we have a ref-constrained T (to do box just once)
                    captureReceiver = (Not receiverType.IsReferenceType AndAlso
                                       Not receiverType.IsValueType AndAlso
                                       Not DirectCast(receiverType, TypeParameterSymbol).HasInterfaceConstraint) OrElse ' This could be a nullable value type, which must be copied in order to not mutate the original value
                                      (receiverType.IsReferenceType AndAlso receiverType.TypeKind = TypeKind.TypeParameter) OrElse
                                      ShouldCaptureConditionalAccessReceiver(rewrittenReceiver)

                    Me._conditionalAccessReceiverPlaceholderId += 1
                    newPlaceholderId = Me._conditionalAccessReceiverPlaceholderId
                    Debug.Assert(newPlaceholderId <> 0)
                    newPlaceHolder = New BoundConditionalAccessReceiverPlaceholder(node.Placeholder.Syntax, newPlaceholderId, captureReceiver, node.Placeholder.Type)
                    placeholderReplacement = newPlaceHolder
                End If
            End If

            Dim whenNotNull As BoundExpression
            Dim accessResultType As TypeSymbol = node.AccessExpression.Type

            If needWhenNotNullPart Then
                AddPlaceholderReplacement(node.Placeholder, placeholderReplacement)
                whenNotNull = VisitExpressionNode(node.AccessExpression)
                RemovePlaceholderReplacement(node.Placeholder)
            Else
                whenNotNull = Nothing ' We should simply produce Nothing as the result, if we need the result.
            End If

            Dim whenNull As BoundExpression

            If node.Type.IsVoidType() Then
                whenNull = Nothing
            Else
                If needWhenNotNullPart AndAlso Not accessResultType.IsNullableType() AndAlso accessResultType.IsValueType Then
                    whenNotNull = WrapInNullable(whenNotNull, node.Type)
                End If

                If needWhenNullPart Then
                    whenNull = If(node.Type.IsNullableType(), NullableNull(node.Syntax, node.Type), factory.Null(node.Type))
                Else
                    whenNull = Nothing
                End If
            End If

            Dim result As BoundExpression

            Debug.Assert(needWhenNotNullPart OrElse needWhenNullPart)

            If needWhenNotNullPart Then
                If needWhenNullPart Then
                    result = New BoundLoweredConditionalAccess(node.Syntax, receiverOrCondition, captureReceiver, newPlaceholderId, whenNotNull, whenNull, node.Type)
                Else
                    Debug.Assert(receiverOrCondition Is Nothing)
                    Debug.Assert(newPlaceHolder Is Nothing)
                    result = whenNotNull
                End If
            ElseIf whenNull IsNot Nothing Then
                Debug.Assert(receiverOrCondition Is Nothing)
                result = whenNull
            Else
                Debug.Assert(receiverOrCondition Is Nothing)
                Debug.Assert(node.Type.IsVoidType())
                result = New BoundSequence(node.Syntax, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray(Of BoundExpression).Empty, Nothing, node.Type)
            End If

            If temp IsNot Nothing Then
                If result.Type.IsVoidType() Then
                    result = New BoundSequence(node.Syntax, ImmutableArray.Create(temp), ImmutableArray.Create(assignment, result), Nothing, result.Type)
                Else
                    result = New BoundSequence(node.Syntax, ImmutableArray.Create(temp), ImmutableArray.Create(assignment), result, result.Type)
                End If
            End If

            Return result
        End Function

        Private Shared Function IsConditionalAccess(operand As BoundExpression, <Out> ByRef whenNotNull As BoundExpression, <Out> ByRef whenNull As BoundExpression) As Boolean
            If operand.Kind = BoundKind.Sequence Then
                Dim sequence = DirectCast(operand, BoundSequence)

                If sequence.ValueOpt Is Nothing Then
                    whenNotNull = Nothing
                    whenNull = Nothing
                    Return False
                End If

                operand = sequence.ValueOpt
            End If

            If operand.Kind = BoundKind.LoweredConditionalAccess Then
                Dim conditional = DirectCast(operand, BoundLoweredConditionalAccess)
                whenNotNull = conditional.WhenNotNull
                whenNull = conditional.WhenNullOpt
                Return True
            End If

            whenNotNull = Nothing
            whenNull = Nothing
            Return False
        End Function

        Private Shared Function UpdateConditionalAccess(operand As BoundExpression, whenNotNull As BoundExpression, whenNull As BoundExpression) As BoundExpression
            Dim sequence As BoundSequence

            If operand.Kind = BoundKind.Sequence Then
                sequence = DirectCast(operand, BoundSequence)
                operand = sequence.ValueOpt
            Else
                sequence = Nothing
            End If

            Dim conditional = DirectCast(operand, BoundLoweredConditionalAccess)

            operand = conditional.Update(conditional.ReceiverOrCondition,
                                         conditional.CaptureReceiver,
                                         conditional.PlaceholderId,
                                         whenNotNull,
                                         whenNull,
                                         whenNotNull.Type)

            If sequence Is Nothing Then
                Return operand
            End If

            Return sequence.Update(sequence.Locals, sequence.SideEffects, operand, operand.Type)
        End Function

    End Class
End Namespace

