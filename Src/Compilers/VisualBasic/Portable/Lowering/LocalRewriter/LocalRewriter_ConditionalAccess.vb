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

        Private Function ShouldCaptureConditionalAccessReceiver(receiver As BoundExpression) As Boolean
            Select Case receiver.Kind
                Case BoundKind.MeReference
                    Return False

                Case BoundKind.Parameter
                    Return DirectCast(receiver, BoundParameter).ParameterSymbol.IsByRef

                Case BoundKind.Local
                    Return DirectCast(receiver, BoundLocal).LocalSymbol.IsByRef

                Case Else
                    Return True
            End Select
        End Function

        Private Function CanCaptureReferenceToConditionalAccessReceiver(receiver As BoundExpression) As Boolean

            If receiver IsNot Nothing Then
                Debug.Assert(receiver.Type.IsTypeParameter())
                ' We cannot capture readonly reference to an array element in a ByRef temp and requesting a writable refernce can fail.
                Select Case receiver.Kind
                    Case BoundKind.ArrayAccess
                        Return False
                    Case BoundKind.Sequence
                        Return CanCaptureReferenceToConditionalAccessReceiver(DirectCast(receiver, BoundSequence).ValueOpt)
                End Select
            End If

            Return True
        End Function

        Public Overrides Function VisitConditionalAccess(node As BoundConditionalAccess) As BoundNode
            Debug.Assert(node.Type IsNot Nothing)

            Dim rewrittenReceiver As BoundExpression = VisitExpressionNode(node.Receiver)
            Dim receiverType As TypeSymbol = rewrittenReceiver.Type

            Dim factory = New SyntheticBoundNodeFactory(topMethod, currentMethodOrLambda, node.Syntax, compilationState, diagnostics)
            Dim condition As BoundExpression

            Dim first As BoundExpression
            Dim receiverForAccess As BoundExpression
            Dim structAccess As BoundExpression = Nothing
            Dim notReferenceType As BoundExpression = Nothing
            Dim temp As LocalSymbol = Nothing
            Dim byRefLocal As LocalSymbol = Nothing

            If receiverType.IsNullableType() Then
                ' if( receiver.HasValue, receiver.GetValueOrDefault(). ... -> to Nullable, Nothing) 

                If ShouldCaptureConditionalAccessReceiver(rewrittenReceiver) Then
                    temp = New SynthesizedLocal(Me.currentMethodOrLambda, receiverType, SynthesizedLocalKind.LoweringTemp)
                    first = factory.Sequence(ImmutableArray(Of LocalSymbol).Empty,
                                             ImmutableArray.Create(Of BoundExpression)(
                                                    factory.AssignmentExpression(factory.Local(temp, isLValue:=True),
                                                                                 rewrittenReceiver.MakeRValue())),
                                             factory.Local(temp, isLValue:=True))

                    receiverForAccess = factory.Local(temp, isLValue:=True)
                Else
                    first = rewrittenReceiver
                    receiverForAccess = rewrittenReceiver
                End If

                condition = NullableHasValue(first)
                receiverForAccess = NullableValueOrDefault(receiverForAccess)

            ElseIf receiverType.IsReferenceType Then

                ' if( receiver IsNot Nothing, receiver. ... -> to Nullable, Nothing) 
                If ShouldCaptureConditionalAccessReceiver(rewrittenReceiver) Then
                    temp = New SynthesizedLocal(Me.currentMethodOrLambda, receiverType, SynthesizedLocalKind.LoweringTemp)
                    first = factory.AssignmentExpression(factory.Local(temp, isLValue:=True), rewrittenReceiver.MakeRValue())
                    receiverForAccess = factory.Local(temp, isLValue:=False)
                Else
                    first = rewrittenReceiver.MakeRValue()
                    receiverForAccess = first
                End If

                condition = factory.ReferenceIsNotNothing(first)
                'TODO: Figure out how to suppress calvirt on this receiver

            Else

                Debug.Assert(Not receiverType.IsValueType)
                Debug.Assert(receiverType.IsTypeParameter())

                ' if( receiver IsNot Nothing, receiver. ... -> to Nullable, Nothing) 

                If ShouldCaptureConditionalAccessReceiver(rewrittenReceiver) Then

                    notReferenceType = factory.ReferenceIsNotNothing(factory.DirectCast(factory.DirectCast(factory.Null(),
                                                                                                           receiverType),
                                                                                        factory.SpecialType(SpecialType.System_Object)))

                    If Not Me.currentMethodOrLambda.IsAsync AndAlso Not Me.currentMethodOrLambda.IsIterator AndAlso
                       CanCaptureReferenceToConditionalAccessReceiver(rewrittenReceiver) Then

                        ' We introduce a ByRef local, which will be used to refer to the receiver from within AccessExpression.
                        ' Initially ByRefLocal is set to refer to the rewrittenReceiver location.

                        ' The "receiver IsNot Nothing" check becomes
                        ' Not <receiver's type is refernce type> OrElse { <capture value pointed to by ByRefLocal in a temp>, <store reference to the temp in ByRefLocal>, temp IsNot Nothing }

                        ' Note that after that condition is executed, if it returns true, the ByRefLocal ponts to the captured value of the reference type, which is proven to be Not Nothing,
                        ' and won't change after the null check (we own the local where the value is captured).

                        ' Also, if receiver's type is value type ByRefLocal still points to the original location, which allows access side effects to be observed. 

                        ' The <receiver's type is refernce type> is performed by boxing default value of receiver's type and checking if it is a null reference. This makes Nullable
                        ' type to be treated as a reference type, but it is Ok since it is immutable. The only strange thing is that we won't unwrap the nullable.

                        temp = New SynthesizedLocal(Me.currentMethodOrLambda, receiverType, SynthesizedLocalKind.LoweringTemp)
                        byRefLocal = New SynthesizedLocal(Me.currentMethodOrLambda, receiverType, SynthesizedLocalKind.LoweringTemp, isByRef:=True)

                        Dim capture As BoundExpression = factory.ReferenceAssignment(byRefLocal, rewrittenReceiver).MakeRValue()

                        condition = factory.LogicalOrElse(notReferenceType,
                                                          factory.Sequence(ImmutableArray(Of LocalSymbol).Empty,
                                                                           ImmutableArray.Create(Of BoundExpression)(factory.AssignmentExpression(factory.Local(temp, isLValue:=True),
                                                                                                                                                  factory.Local(byRefLocal, isLValue:=False)),
                                                                                                                     factory.ReferenceAssignment(byRefLocal,
                                                                                                                                                 factory.Local(temp, isLValue:=True)).MakeRValue()),
                                                                           factory.ReferenceIsNotNothing(factory.DirectCast(factory.Local(temp, isLValue:=False),
                                                                                                                            factory.SpecialType(SpecialType.System_Object)))))

                        condition = factory.Sequence(ImmutableArray(Of LocalSymbol).Empty,
                                                     ImmutableArray.Create(capture),
                                                     condition)

                        receiverForAccess = factory.Local(byRefLocal, isLValue:=False)
                    Else
                        ' Async rewriter cannot handle the trick with ByRef local that we are doing above.
                        ' For now we will duplicate access - one access for a value type case, one access for a class case.

                        ' Value type case will use receiver as is.
                        AddPlaceholderReplacement(node.Placeholder, rewrittenReceiver.MakeRValue())
                        structAccess = VisitExpressionNode(node.AccessExpression)
                        RemovePlaceholderReplacement(node.Placeholder)

                        If Not node.Type.IsVoidType() AndAlso Not structAccess.Type.IsNullableType() AndAlso structAccess.Type.IsValueType Then
                            structAccess = WrapInNullable(structAccess, node.Type)
                        End If

                        ' Class case is handled by capturing value in a temp
                        temp = New SynthesizedLocal(Me.currentMethodOrLambda, receiverType, SynthesizedLocalKind.LoweringTemp)
                        condition = factory.ReferenceIsNotNothing(
                                                factory.DirectCast(factory.AssignmentExpression(factory.Local(temp, isLValue:=True), rewrittenReceiver.MakeRValue()),
                                                                   factory.SpecialType(SpecialType.System_Object)))
                        receiverForAccess = factory.Local(temp, isLValue:=False)

                    End If
                Else
                    condition = factory.ReferenceIsNotNothing(factory.DirectCast(rewrittenReceiver.MakeRValue(), factory.SpecialType(SpecialType.System_Object)))
                    receiverForAccess = rewrittenReceiver
                End If

            End If

            AddPlaceholderReplacement(node.Placeholder, receiverForAccess.MakeRValue())
            Dim whenTrue As BoundExpression = VisitExpressionNode(node.AccessExpression)
            RemovePlaceholderReplacement(node.Placeholder)

            Dim whenFalse As BoundExpression

            If node.Type.IsVoidType() Then
                whenFalse = New BoundSequence(node.Syntax, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray(Of BoundExpression).Empty, Nothing, node.Type)

                If Not whenTrue.Type.IsVoidType() Then
                    whenTrue = New BoundSequence(whenTrue.Syntax, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(whenTrue), Nothing, node.Type)
                End If
            Else
                If Not whenTrue.Type.IsNullableType() AndAlso whenTrue.Type.IsValueType Then
                    whenTrue = WrapInNullable(whenTrue, node.Type)
                End If

                whenFalse = If(whenTrue.Type.IsNullableType(), NullableNull(node.Syntax, whenTrue.Type), factory.Null(whenTrue.Type))
            End If


            Dim result As BoundExpression = TransformRewrittenTernaryConditionalExpression(factory.TernaryConditionalExpression(condition, whenTrue, whenFalse))

            If structAccess IsNot Nothing Then
                ' Result now handles class case. let's join it with the struct case
                result = TransformRewrittenTernaryConditionalExpression(factory.TernaryConditionalExpression(notReferenceType, structAccess, result))
            End If

            If temp IsNot Nothing Then
                Dim temporaries As ImmutableArray(Of LocalSymbol)

                If byRefLocal IsNot Nothing Then
                    temporaries = ImmutableArray.Create(temp, byRefLocal)
                Else
                    temporaries = ImmutableArray.Create(temp)
                End If

                If result.Type.IsVoidType() Then
                    result = New BoundSequence(node.Syntax, temporaries, ImmutableArray.Create(result), Nothing, result.Type)
                Else
                    result = New BoundSequence(node.Syntax, temporaries, ImmutableArray(Of BoundExpression).Empty, result, result.Type)
                End If
            End If

            Return result
        End Function

    End Class
End Namespace

