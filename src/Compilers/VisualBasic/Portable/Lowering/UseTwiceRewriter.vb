' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class UseTwiceRewriter

        Public Structure Result
            Public Sub New(first As BoundExpression, second As BoundExpression)
                Me.First = first
                Me.Second = second
            End Sub
            Public ReadOnly First As BoundExpression
            Public ReadOnly Second As BoundExpression
        End Structure

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Given an expression that produces some result and 
        ''' has some observable evaluation side effects, return two expressions:
        '''   1) First - produces the same result with the same observable side effects,
        '''   2) Second - produces the same result, but without observable side effects, whenever possible.
        ''' 
        ''' This is used for compound assignment, method call ByRef parameter copy back, etc.
        ''' </summary>
        Public Shared Function UseTwice(
            containingMember As Symbol,
            value As BoundExpression,
            isForRegularCompoundAssignment As Boolean,
            temporaries As ArrayBuilder(Of SynthesizedLocal)
        ) As Result
            Debug.Assert(value.IsValue())

            Select Case value.Kind
                Case BoundKind.XmlMemberAccess
                    Dim memberAccess = DirectCast(value, BoundXmlMemberAccess)
                    Dim result = UseTwice(containingMember, memberAccess.MemberAccess, isForRegularCompoundAssignment, temporaries)
                    Return New Result(memberAccess.Update(result.First), memberAccess.Update(result.Second))

                Case BoundKind.PropertyAccess
                    Return UseTwicePropertyAccess(containingMember, DirectCast(value, BoundPropertyAccess), isForRegularCompoundAssignment, temporaries)

                Case BoundKind.LateInvocation
                    Return UseTwiceLateInvocation(containingMember, DirectCast(value, BoundLateInvocation), temporaries)

                Case BoundKind.LateMemberAccess
                    Return UseTwiceLateMember(containingMember, DirectCast(value, BoundLateMemberAccess), temporaries)

                Case Else
                    Debug.Assert(value.Kind <> BoundKind.ByRefArgumentWithCopyBack AndAlso
                                 value.Kind <> BoundKind.LateBoundArgumentSupportingAssignmentWithCapture)

                    Dim result = UseTwiceExpression(containingMember, value, temporaries)

                    ' LValue-ness of expressions must be preserved
                    Debug.Assert(result.First.IsLValue = result.Second.IsLValue AndAlso result.Second.IsLValue = value.IsLValue)
                    Return result

            End Select
        End Function

        ' receiver of latebound operation
        Private Shared Function UseTwiceLateBoundReceiver(
            containingMember As Symbol,
            receiverOpt As BoundExpression,
            temporaries As ArrayBuilder(Of SynthesizedLocal)
        ) As Result

            Dim receiver As Result

            If receiverOpt Is Nothing Then
                receiver = New Result(Nothing, Nothing)
            ElseIf receiverOpt.IsLValue AndAlso receiverOpt.Type.IsReferenceType Then
                Dim boundTemp As BoundLocal = Nothing
                Dim capture As BoundAssignmentOperator = CaptureInATemp(containingMember, receiverOpt.MakeRValue(), temporaries, boundTemp)
                boundTemp = boundTemp.Update(boundTemp.LocalSymbol, isLValue:=True, type:=boundTemp.Type)
                receiver = New Result(New BoundSequence(capture.Syntax, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(Of BoundExpression)(capture), boundTemp, boundTemp.Type),
                                      boundTemp)
            ElseIf Not receiverOpt.IsLValue AndAlso Not receiverOpt.Type.IsReferenceType AndAlso Not receiverOpt.Type.IsValueType Then
                Dim boundTemp As BoundLocal = Nothing
                Dim capture As BoundAssignmentOperator = CaptureInATemp(containingMember, receiverOpt.MakeRValue(), temporaries, boundTemp)
                boundTemp = boundTemp.Update(boundTemp.LocalSymbol, isLValue:=True, type:=boundTemp.Type)

                receiver = New Result(New BoundSequence(capture.Syntax, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(Of BoundExpression)(capture), boundTemp, boundTemp.Type),
                                      boundTemp)
            Else
                receiver = UseTwiceExpression(containingMember, receiverOpt, temporaries)
            End If

            ' LValue-ness of a receiver should be preserved because it affects how helper method is called.
            Debug.Assert(receiver.Second Is Nothing OrElse (receiverOpt.IsLValue = receiver.First.IsLValue AndAlso receiverOpt.IsLValue = receiver.Second.IsLValue))
            Return receiver
        End Function

        Private Shared Function UseTwiceExpression(
            containingMember As Symbol,
            value As BoundExpression,
            temporaries As ArrayBuilder(Of SynthesizedLocal)
        ) As Result

            If Not value.IsLValue Then
                Return UseTwiceRValue(containingMember, value, temporaries)
            End If

            Select Case value.Kind
                Case BoundKind.Call
                    Return UseTwiceCall(containingMember, DirectCast(value, BoundCall), temporaries)
                Case BoundKind.ArrayAccess
                    Return UseTwiceArrayAccess(containingMember, DirectCast(value, BoundArrayAccess), temporaries)
                Case BoundKind.FieldAccess
                    Return UseTwiceFieldAccess(containingMember, DirectCast(value, BoundFieldAccess), temporaries)
                Case BoundKind.Local,
                     BoundKind.Parameter,
                     BoundKind.PseudoVariable,
                     BoundKind.WithLValueExpressionPlaceholder
                    Return New Result(value, value)
                Case Else
                    Debug.Assert(False) ' Add tests if this assert fires
                    Return UseTwiceRValue(containingMember, value, temporaries)

            End Select
        End Function

        Private Shared Function CaptureInATemp(
            containingMember As Symbol,
            value As BoundExpression,
            type As TypeSymbol,
            temporaries As ArrayBuilder(Of SynthesizedLocal),
            ByRef referToTemp As BoundLocal
        ) As BoundAssignmentOperator
            Debug.Assert(type IsNot Nothing AndAlso Not type.IsVoidType() AndAlso value.Type Is type)

            Dim temp = New SynthesizedLocal(containingMember, type, SynthesizedLocalKind.LoweringTemp)
            temporaries.Add(temp)

            referToTemp = New BoundLocal(value.Syntax, temp, type)
            referToTemp.SetWasCompilerGenerated()
            Dim capture = (New BoundAssignmentOperator(value.Syntax, referToTemp, value, suppressObjectClone:=True, type:=type)).MakeCompilerGenerated()

            ' Make sure we will not try to write to this local or pass it ByRef.
            referToTemp = referToTemp.MakeRValue()
            Return capture
        End Function

        Private Shared Function CaptureInATemp(
            containingMember As Symbol,
            value As BoundExpression,
            temporaries As ArrayBuilder(Of SynthesizedLocal),
            ByRef referToTemp As BoundLocal
        ) As BoundAssignmentOperator
            Return CaptureInATemp(containingMember, value, value.Type, temporaries, referToTemp)
        End Function

        Private Shared Function UseTwiceRValue(containingMember As Symbol, value As BoundExpression, arg As ArrayBuilder(Of SynthesizedLocal)) As Result
            Dim kind As BoundKind = value.Kind

            If kind = BoundKind.BadVariable OrElse
               kind = BoundKind.MeReference OrElse
               kind = BoundKind.MyBaseReference OrElse
               kind = BoundKind.MyClassReference OrElse
               kind = BoundKind.Literal Then
                Return New Result(value, value)

            ElseIf value.IsValue AndAlso value.Type IsNot Nothing AndAlso Not value.Type.IsVoidType() Then

                Debug.Assert(Not value.IsLValue)
                Debug.Assert(Not value.IsPropertyOrXmlPropertyAccess() OrElse (value.GetAccessKind() = PropertyAccessKind.Get))
                Debug.Assert(Not value.IsLateBound() OrElse (value.GetLateBoundAccessKind() = LateBoundAccessKind.Get))

                Dim constantValue As ConstantValue = value.ConstantValueOpt

                If constantValue IsNot Nothing Then
                    Debug.Assert(value.Kind <> BoundKind.Literal)

                    Dim second = New BoundLiteral(value.Syntax, constantValue, value.Type)
                    second.SetWasCompilerGenerated()
                    Return New Result(value, second)
                End If

                ' TODO: Might need to do some optimization for compiler generated locals.
                '       For example, no reason to recapture a local that is already a capture.
                '       Something to try when we implement WITH statement.
                Dim boundTemp As BoundLocal = Nothing
                Dim first = CaptureInATemp(containingMember, value, arg, boundTemp)

                Debug.Assert(Not first.IsLValue AndAlso Not boundTemp.IsLValue)
                Return New Result(first, boundTemp)
            End If

            Throw ExceptionUtilities.Unreachable
        End Function

        Private Shared Function UseTwiceCall(containingMember As Symbol, node As BoundCall, arg As ArrayBuilder(Of SynthesizedLocal)) As Result
            Debug.Assert(node.IsLValue)
            Return UseTwiceLValue(containingMember, node, arg)
        End Function

        Private Shared Function UseTwiceArrayAccess(containingMember As Symbol, node As BoundArrayAccess, arg As ArrayBuilder(Of SynthesizedLocal)) As Result

            Debug.Assert(node.IsLValue)

#If DONT_USE_BYREF_LOCALS_FOR_USE_TWICE Then
#Else
            If IsInvariantArray(node.Expression.Type) Then
                Return UseTwiceLValue(containingMember, node, arg)
            End If
#End If

            ' Note, as an alternative we could capture reference to the array element in a ByRef temp.
            ' However, without an introduction of an indirect assignment node, IL-gen is unable to distinguish 
            ' when it should assign indirect or should assign a reference. For now, decided to not introduce 
            ' special bound nodes for this purpose. Besides, it is not clear whether ByRef temps will make Async 
            ' easier to implement.
            Dim boundArrayTemp As BoundLocal = Nothing
            Dim storeArray = CaptureInATemp(containingMember, node.Expression, arg, boundArrayTemp)

            Dim n = node.Indices.Length
            Dim indicesFirst(n - 1) As BoundExpression
            Dim indicesSecond(n - 1) As BoundExpression

            For i = 0 To n - 1
                Dim result = UseTwiceRValue(containingMember, node.Indices(i), arg)
                indicesFirst(i) = result.First
                indicesSecond(i) = result.Second
            Next

            Dim second = node.Update(boundArrayTemp, indicesSecond.AsImmutableOrNull(), node.IsLValue, node.Type)
            Dim first = node.Update(storeArray, indicesFirst.AsImmutableOrNull(), node.IsLValue, node.Type)

            Debug.Assert(first.IsLValue AndAlso second.IsLValue)
            Return New Result(first, second)

        End Function

        Private Shared Function IsInvariantArray(type As TypeSymbol) As Boolean
            Dim value = TryCast(type, ArrayTypeSymbol)?.ElementType.IsNotInheritable
            Return value.GetValueOrDefault()
        End Function

        Private Shared Function UseTwiceLValue(containingMember As Symbol, lvalue As BoundExpression, temporaries As ArrayBuilder(Of SynthesizedLocal)) As Result
            Debug.Assert(lvalue.IsLValue)
            Dim temp = New SynthesizedLocal(containingMember, lvalue.Type, SynthesizedLocalKind.LoweringTemp, isByRef:=True)
            Dim first = New BoundReferenceAssignment(lvalue.Syntax,
                                                  New BoundLocal(lvalue.Syntax, temp, temp.Type).MakeCompilerGenerated(),
                                                  lvalue, isLValue:=True, type:=lvalue.Type).MakeCompilerGenerated()
            temporaries.Add(temp)
            Dim second = New BoundLocal(lvalue.Syntax, temp, isLValue:=True, type:=lvalue.Type).MakeCompilerGenerated()
            Return New Result(first, second)
        End Function

        Private Shared Function UseTwiceFieldAccess(containingMember As Symbol, node As BoundFieldAccess, arg As ArrayBuilder(Of SynthesizedLocal)) As Result

            Debug.Assert(node.IsLValue)

            Dim fieldSymbol = node.FieldSymbol

            If fieldSymbol.IsShared AndAlso node.ReceiverOpt IsNot Nothing Then
                ' Get rid of the receiver on second use of the shared field.
                ' It could be an expression that we don't want to visit twice.
                Dim second = node.Update(Nothing, fieldSymbol, node.IsLValue, node.SuppressVirtualCalls, constantsInProgressOpt:=Nothing, node.Type)

                Debug.Assert(second.IsLValue)
                Return New Result(node, second)

            ElseIf node.ReceiverOpt Is Nothing Then
                Return New Result(node, node)

            Else
#If DONT_USE_BYREF_LOCALS_FOR_USE_TWICE Then
                ' Note, as an alternative we could capture reference to the field in a ByRef temp.
                ' However, without an introduction of an indirect assignment node, IL-gen is unable to distinguish 
                ' when it should assign indirect or should assign a reference. For now, decided to not introduce 
                ' special bound nodes for this purpose. Besides, it is not clear whether ByRef temps will make Async 
                ' easier to implement.

                Dim receiver As Result = UseTwiceReceiver(containingMember, node.ReceiverOpt, arg)
                Dim first = node.Update(receiver.First, fieldSymbol, node.IsLValue, suppressVirtualCalls:=False, node.ConstantsInProgressOpt, node.Type)
                Dim second = node.Update(receiver.Second, fieldSymbol, node.IsLValue, suppressVirtualCalls:=False, node.ConstantsInProgressOpt, node.Type)

                Debug.Assert(first.IsLValue AndAlso second.IsLValue)
                Return New Result(first, second)
#Else
                Return UseTwiceLValue(containingMember, node, arg)
#End If
            End If

        End Function

        ' We only want to rewrite property access at the top of the expression, not within
        ' the expression. (For instance, P1 should not be rewritten in M(x.P1.P2).)
        Private Shared Function UseTwicePropertyAccess(containingMember As Symbol, node As BoundPropertyAccess, isForRegularCompoundAssignment As Boolean, arg As ArrayBuilder(Of SynthesizedLocal)) As Result
            Dim propertySymbol = node.PropertySymbol

            ' Visit receiver.
            Dim receiverOpt As BoundExpression = node.ReceiverOpt
            Dim receiver As Result

            If receiverOpt Is Nothing Then
                receiver = New Result(Nothing, Nothing)
            ElseIf node.PropertySymbol.IsShared Then
                receiver = New Result(receiverOpt, Nothing)
            ElseIf receiverOpt.IsLValue AndAlso receiverOpt.Type.IsReferenceType Then
                Dim boundTemp As BoundLocal = Nothing
                receiver = New Result(CaptureInATemp(containingMember, receiverOpt.MakeRValue(), arg, boundTemp), boundTemp)
            ElseIf Not receiverOpt.IsLValue AndAlso Not receiverOpt.Type.IsReferenceType AndAlso Not receiverOpt.Type.IsValueType Then
                Dim boundTemp As BoundLocal = Nothing
                Dim capture As BoundAssignmentOperator = CaptureInATemp(containingMember, receiverOpt.MakeRValue(), arg, boundTemp)
                boundTemp = boundTemp.Update(boundTemp.LocalSymbol, isLValue:=True, type:=boundTemp.Type)

                receiver = New Result(New BoundSequence(capture.Syntax, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(Of BoundExpression)(capture), boundTemp, boundTemp.Type),
                                      boundTemp)
            ElseIf receiverOpt.IsLValue AndAlso
                   CodeGenerator.IsPossibleReferenceTypeReceiverOfConstrainedCall(receiverOpt) AndAlso
                   Not CodeGenerator.ReceiverIsKnownToReferToTempIfReferenceType(receiverOpt) AndAlso
                   Not (isForRegularCompoundAssignment AndAlso CodeGenerator.IsSafeToDereferenceReceiverRefAfterEvaluatingArguments(node.Arguments)) Then

                Debug.Assert(Not receiverOpt.Type.IsReferenceType)

                ' A case where T is actually a class must be handled specially.
                ' Taking a reference to a class instance is fragile because the value behind the 
                ' reference might change while arguments are evaluated. However, the call should be
                ' performed on the instance that is behind reference at the time we push the
                ' reference to the stack. So, for a class we need to emit a reference to a temporary
                ' location, rather than to the original location

                receiver = UseTwiceExpression(containingMember, receiverOpt, arg)

                Dim cloneTemp As BoundLocal = Nothing
                Dim clone As BoundAssignmentOperator = CaptureInATemp(containingMember, receiver.Second.MakeRValue(), arg, cloneTemp)

                Dim complexReceiverFirst = New BoundComplexConditionalAccessReceiver(receiverOpt.Syntax,
                                                                                     receiver.Second,
                                                                                     New BoundSequence(receiverOpt.Syntax, ImmutableArray(Of LocalSymbol).Empty,
                                                                                                       ImmutableArray.Create(Of BoundExpression)(clone),
                                                                                                       cloneTemp.Update(cloneTemp.LocalSymbol, isLValue:=True, cloneTemp.Type),
                                                                                                       cloneTemp.Type).MakeCompilerGenerated(),
                                                                                     receiverOpt.Type).MakeCompilerGenerated()

                Dim complexReceiverSecond = New BoundComplexConditionalAccessReceiver(receiverOpt.Syntax,
                                                                                      receiver.Second,
                                                                                      cloneTemp.Update(cloneTemp.LocalSymbol, isLValue:=True, cloneTemp.Type),
                                                                                      receiverOpt.Type).MakeCompilerGenerated()

                receiver = New Result(New BoundSequence(receiverOpt.Syntax, ImmutableArray(Of LocalSymbol).Empty,
                                                        ImmutableArray.Create(Of BoundExpression)(receiver.First.MakeRValue()),
                                                        complexReceiverFirst, receiverOpt.Type).MakeCompilerGenerated(),
                                      complexReceiverSecond)
            Else
                receiver = UseTwiceExpression(containingMember, receiverOpt, arg)
            End If

            ' Visit args.
            Dim firstArgs As ImmutableArray(Of BoundExpression)
            Dim secondArgs As ImmutableArray(Of BoundExpression)
            If node.Arguments.IsEmpty Then
                firstArgs = ImmutableArray(Of BoundExpression).Empty
                secondArgs = ImmutableArray(Of BoundExpression).Empty
            Else
                Dim nArgs = node.Arguments.Length
                Dim firstArgsArray(nArgs - 1) As BoundExpression
                Dim secondArgsArray(nArgs - 1) As BoundExpression

                For i = 0 To nArgs - 1
                    Dim boundArgument As BoundExpression = node.Arguments(i)
                    If boundArgument.Kind = BoundKind.ArrayCreation AndAlso DirectCast(boundArgument, BoundArrayCreation).IsParamArrayArgument Then
                        ' ParamArray argument
                        UseTwiceParamArrayArgument(containingMember, DirectCast(boundArgument, BoundArrayCreation), arg, firstArgsArray(i), secondArgsArray(i))
                    Else
                        ' Regular argument
                        UseTwiceRegularArgument(containingMember, boundArgument, arg, firstArgsArray(i), secondArgsArray(i))
                    End If
                Next

                firstArgs = firstArgsArray.AsImmutableOrNull()
                secondArgs = secondArgsArray.AsImmutableOrNull()
            End If

            ' Generate PropertyAccess nodes.
            Dim first = node.Update(
                            propertySymbol,
                            node.PropertyGroupOpt,
                            node.AccessKind,
                            isWriteable:=node.IsWriteable,
                            isLValue:=node.IsLValue,
                            receiverOpt:=receiver.First,
                            arguments:=firstArgs,
                            defaultArguments:=node.DefaultArguments,
                            type:=node.Type)

            Dim second = node.Update(
                            propertySymbol,
                            node.PropertyGroupOpt,
                            node.AccessKind,
                            isWriteable:=node.IsWriteable,
                            isLValue:=node.IsLValue,
                            receiverOpt:=receiver.Second,
                            arguments:=secondArgs,
                            defaultArguments:=node.DefaultArguments,
                            type:=node.Type)

            Return New Result(first, second)
        End Function

        Private Shared Function UseTwiceLateInvocation(containingMember As Symbol, node As BoundLateInvocation, arg As ArrayBuilder(Of SynthesizedLocal)) As Result
            ' Visit receiver.
            Dim receiver As Result
            If node.Member.Kind = BoundKind.LateMemberAccess Then
                receiver = UseTwiceLateMember(containingMember, DirectCast(node.Member, BoundLateMemberAccess), arg)
            Else
                receiver = UseTwiceLateBoundReceiver(containingMember, node.Member, arg)
            End If

            ' Visit args.
            Dim firstArgs As ImmutableArray(Of BoundExpression)
            Dim secondArgs As ImmutableArray(Of BoundExpression)
            If node.ArgumentsOpt.IsEmpty Then
                firstArgs = ImmutableArray(Of BoundExpression).Empty
                secondArgs = ImmutableArray(Of BoundExpression).Empty
            Else
                Dim nArgs = node.ArgumentsOpt.Length
                Dim firstArgsArray(nArgs - 1) As BoundExpression
                Dim secondArgsArray(nArgs - 1) As BoundExpression

                For i = 0 To nArgs - 1
                    Dim boundArgument As BoundExpression = node.ArgumentsOpt(i)
                    ' LateBound argument
                    If Not boundArgument.IsSupportingAssignment() Then
                        UseTwiceRegularArgument(containingMember, boundArgument, arg, firstArgsArray(i), secondArgsArray(i))
                    Else
                        Dim temp = New SynthesizedLocal(containingMember, boundArgument.Type, SynthesizedLocalKind.LoweringTemp)
                        arg.Add(temp)

                        firstArgsArray(i) = New BoundLateBoundArgumentSupportingAssignmentWithCapture(boundArgument.Syntax,
                                                                                                      boundArgument,
                                                                                                      temp,
                                                                                                      boundArgument.Type)
                        secondArgsArray(i) = New BoundLocal(boundArgument.Syntax, temp, isLValue:=False, type:=temp.Type)
                    End If
                Next

                firstArgs = firstArgsArray.AsImmutableOrNull()
                secondArgs = secondArgsArray.AsImmutableOrNull()
            End If

            ' Generate nodes.
            Dim first = node.Update(
                            receiver.First,
                            firstArgs,
                            node.ArgumentNamesOpt,
                            node.AccessKind,
                            node.MethodOrPropertyGroupOpt,
                            node.Type)

            Dim second = node.Update(
                            receiver.Second,
                            secondArgs,
                            node.ArgumentNamesOpt,
                            node.AccessKind,
                            node.MethodOrPropertyGroupOpt,
                            node.Type)

            Return New Result(first, second)
        End Function

        Private Shared Function UseTwiceLateMember(containingMember As Symbol, node As BoundLateMemberAccess, arg As ArrayBuilder(Of SynthesizedLocal)) As Result
            ' Visit receiver.
            Dim receiver As Result = UseTwiceLateBoundReceiver(containingMember, node.ReceiverOpt, arg)

            ' Generate nodes.
            Dim first = node.Update(node.NameOpt,
                                    node.ContainerTypeOpt,
                                    receiver.First,
                                    node.TypeArgumentsOpt,
                                    node.AccessKind,
                                    node.Type)

            Dim second = node.Update(node.NameOpt,
                                    node.ContainerTypeOpt,
                                    receiver.Second,
                                    node.TypeArgumentsOpt,
                                    node.AccessKind,
                                    node.Type)

            Return New Result(first, second)
        End Function

        Private Shared Sub UseTwiceRegularArgument(containingMember As Symbol, boundArgument As BoundExpression, arg As ArrayBuilder(Of SynthesizedLocal),
                                                   ByRef first As BoundExpression, ByRef second As BoundExpression)

            Debug.Assert(Not boundArgument.IsLValue)
            Dim result = UseTwiceRValue(containingMember, boundArgument, arg)

            Debug.Assert(Not result.First.IsLValue AndAlso Not result.Second.IsLValue)
            Debug.Assert(result.First.HasErrors = result.Second.HasErrors AndAlso boundArgument.HasErrors = result.First.HasErrors)
            Debug.Assert(result.First.HasErrors OrElse
                         (result.Second.Kind = BoundKind.Literal AndAlso result.First.ConstantValueOpt IsNot Nothing) OrElse
                         (result.Second.Kind = BoundKind.Local AndAlso result.Second.WasCompilerGenerated AndAlso
                          DirectCast(result.Second, BoundLocal).LocalSymbol.IsCompilerGenerated AndAlso
                          result.First.Kind = BoundKind.AssignmentOperator AndAlso result.First.WasCompilerGenerated AndAlso
                          DirectCast(result.First, BoundAssignmentOperator).Left.Kind = BoundKind.Local AndAlso
                          DirectCast(result.Second, BoundLocal).LocalSymbol Is DirectCast(DirectCast(result.First, BoundAssignmentOperator).Left, BoundLocal).LocalSymbol))

            first = result.First
            second = result.Second
        End Sub

        Private Shared Sub UseTwiceParamArrayArgument(containingMember As Symbol, boundArray As BoundArrayCreation, arg As ArrayBuilder(Of SynthesizedLocal),
                                                      ByRef first As BoundExpression, ByRef second As BoundExpression)

            Debug.Assert(Not boundArray.IsLValue)
            Debug.Assert(boundArray.InitializerOpt IsNot Nothing)
            Debug.Assert(boundArray.ArrayLiteralOpt Is Nothing)

            Dim initializer As BoundArrayInitialization = boundArray.InitializerOpt
            Dim initializerSize As Integer = initializer.Initializers.Length

            Dim firstArgsArray(initializerSize - 1) As BoundExpression
            Dim secondArgsArray(initializerSize - 1) As BoundExpression

            ' Process arguments from bound array creation initializer
            For index = 0 To initializerSize - 1
                UseTwiceRegularArgument(containingMember, initializer.Initializers(index), arg, firstArgsArray(index), secondArgsArray(index))
            Next

            ' query will be removed in Production 
            Debug.Assert(boundArray.Bounds.All(Function(expr) expr.Kind = BoundKind.Literal))

            ' Finally, duplicate array creation expression with updated initializers
            first = boundArray.Update(boundArray.IsParamArrayArgument, boundArray.Bounds,
                                      initializer.Update(firstArgsArray.AsImmutableOrNull(), initializer.Type), Nothing, Nothing, boundArray.Type)
            second = boundArray.Update(boundArray.IsParamArrayArgument, boundArray.Bounds,
                                       initializer.Update(secondArgsArray.AsImmutableOrNull(), initializer.Type), Nothing, Nothing, boundArray.Type)
        End Sub

#If DONT_USE_BYREF_LOCALS_FOR_USE_TWICE Then
        Private Shared Function UseTwiceReceiver(containingMember As Symbol, receiverOpt As BoundExpression, arg As ArrayBuilder(Of SynthesizedLocal)) As Result
            If receiverOpt Is Nothing Then
                Return New Result(Nothing, Nothing)
            ElseIf receiverOpt.IsLValue AndAlso receiverOpt.Type.IsReferenceType Then
                Dim boundTemp As BoundLocal = Nothing
                Dim first = CaptureInATemp(containingMember, receiverOpt.MakeRValue(), arg, boundTemp)
                Return New Result(first, boundTemp)
            Else
                Return UseTwiceExpression(containingMember, receiverOpt, arg)
            End If
        End Function
#End If
    End Class

End Namespace
