' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class LocalRewriter

        Public Overrides Function VisitUserDefinedBinaryOperator(node As BoundUserDefinedBinaryOperator) As BoundNode
            If _inExpressionLambda Then
                Return node.Update(node.OperatorKind, DirectCast(Visit(node.UnderlyingExpression), BoundExpression), node.Checked, node.Type)
            End If

            If (node.OperatorKind And BinaryOperatorKind.Lifted) <> 0 Then
                Return RewriteLiftedUserDefinedBinaryOperator(node)
            End If

            Return Visit(node.UnderlyingExpression)
        End Function

        Public Overrides Function VisitUserDefinedShortCircuitingOperator(node As BoundUserDefinedShortCircuitingOperator) As BoundNode
            If _inExpressionLambda Then
                ' If we are inside expression lambda we need to rewrite it into just a bitwise operator call
                Dim placeholder As BoundRValuePlaceholder = node.LeftOperandPlaceholder
                Dim leftOperand As BoundExpression = node.LeftOperand

                If placeholder IsNot Nothing Then
                    Debug.Assert(leftOperand IsNot Nothing)
                    AddPlaceholderReplacement(placeholder, VisitExpression(leftOperand))
                End If

                Dim rewritten = DirectCast(VisitExpression(node.BitwiseOperator), BoundUserDefinedBinaryOperator)

                If placeholder IsNot Nothing Then
                    RemovePlaceholderReplacement(placeholder)
                End If

                ' NOTE: Everything but 'rewritten' will be discarded by ExpressionLambdaRewriter
                '       we keep the node only to make sure we can replace 'And' with 'AndAlso'
                '       and 'Or' with 'OrElse' when generating proper factory calls.
                Return node.Update(node.LeftOperand, node.LeftOperandPlaceholder, node.LeftTest, rewritten, node.Type)
            End If

            ' The rewrite that should happen here is:
            '     If(LeftTest(temp = left), temp, BitwiseOperator)

            Dim temp As New SynthesizedLocal(_currentMethodOrLambda, node.LeftOperand.Type, SynthesizedLocalKind.LoweringTemp)

            Dim tempAccess As New BoundLocal(node.Syntax, temp, True, temp.Type)

            AddPlaceholderReplacement(node.LeftOperandPlaceholder,
                                          New BoundAssignmentOperator(node.Syntax,
                                                                      tempAccess,
                                                                      VisitExpressionNode(node.LeftOperand),
                                                                      True, temp.Type))

            Dim rewrittenTest = VisitExpressionNode(node.LeftTest)

            tempAccess = tempAccess.MakeRValue()

            UpdatePlaceholderReplacement(node.LeftOperandPlaceholder, tempAccess)

            Dim rewrittenBitwise = VisitExpressionNode(node.BitwiseOperator)

            RemovePlaceholderReplacement(node.LeftOperandPlaceholder)

            Return New BoundSequence(node.Syntax,
                                     ImmutableArray.Create(Of LocalSymbol)(temp),
                                     ImmutableArray(Of BoundExpression).Empty,
                                     MakeTernaryConditionalExpression(node.Syntax,
                                                                    rewrittenTest,
                                                                    tempAccess,
                                                                    rewrittenBitwise),
                                     temp.Type)
        End Function

        Public Overrides Function VisitBinaryOperator(node As BoundBinaryOperator) As BoundNode
            ' Do not blow the stack due to a deep recursion on the left. 

            Dim optimizeForConditionalBranch As Boolean = (node.OperatorKind And BinaryOperatorKind.OptimizableForConditionalBranch) <> 0
            Dim optimizeChildForConditionalBranch As Boolean = optimizeForConditionalBranch

            Dim child As BoundExpression = GetLeftOperand(node, optimizeChildForConditionalBranch)

            If child.Kind <> BoundKind.BinaryOperator Then
                Return RewriteBinaryOperatorSimple(node, optimizeForConditionalBranch)
            End If

            Dim stack = ArrayBuilder(Of (Binary As BoundBinaryOperator, OptimizeForConditionalBranch As Boolean)).GetInstance()
            stack.Push((node, optimizeForConditionalBranch))

            Dim binary As BoundBinaryOperator = DirectCast(child, BoundBinaryOperator)

            Do
                If optimizeChildForConditionalBranch Then
                    Select Case (binary.OperatorKind And BinaryOperatorKind.OpMask)
                        Case BinaryOperatorKind.AndAlso, BinaryOperatorKind.OrElse
                            Exit Select
                        Case Else
                            optimizeChildForConditionalBranch = False
                    End Select
                End If

                stack.Push((binary, optimizeChildForConditionalBranch))

                child = GetLeftOperand(binary, optimizeChildForConditionalBranch)

                If child.Kind <> BoundKind.BinaryOperator Then
                    Exit Do
                End If

                binary = DirectCast(child, BoundBinaryOperator)
            Loop

            Dim left = VisitExpressionNode(child)

            Do
                Dim tuple As (Binary As BoundBinaryOperator, OptimizeForConditionalBranch As Boolean) = stack.Pop()
                binary = tuple.Binary

                Dim right = VisitExpression(GetRightOperand(binary, tuple.OptimizeForConditionalBranch))

                If (binary.OperatorKind And BinaryOperatorKind.Lifted) <> 0 Then
                    left = FinishRewriteOfLiftedIntrinsicBinaryOperator(binary, left, right, tuple.OptimizeForConditionalBranch)
                Else
                    left = TransformRewrittenBinaryOperator(binary.Update(binary.OperatorKind, left, right, binary.Checked, binary.ConstantValueOpt, Me.VisitType(binary.Type)))
                End If
            Loop While binary IsNot node

            Debug.Assert(stack.Count = 0)
            stack.Free()

            Return left
        End Function

        Private Shared Function GetLeftOperand(binary As BoundBinaryOperator, ByRef optimizeForConditionalBranch As Boolean) As BoundExpression
            If optimizeForConditionalBranch AndAlso (binary.OperatorKind And BinaryOperatorKind.OpMask) <> BinaryOperatorKind.OrElse Then
                Debug.Assert((binary.OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.AndAlso)
                ' If left operand is evaluated to Null, three-valued Boolean logic dictates that the right operand of AndAlso
                ' should still be evaluated. So, we cannot simply snap the left operand to Boolean.
                optimizeForConditionalBranch = False
            End If

            Return binary.Left.GetMostEnclosedParenthesizedExpression()
        End Function

        Private Shared Function GetRightOperand(binary As BoundBinaryOperator, adjustIfOptimizableForConditionalBranch As Boolean) As BoundExpression
            If adjustIfOptimizableForConditionalBranch Then
                Return LocalRewriter.AdjustIfOptimizableForConditionalBranch(binary.Right, Nothing)
            Else
                Return binary.Right
            End If
        End Function

        Private Function RewriteBinaryOperatorSimple(node As BoundBinaryOperator, optimizeForConditionalBranch As Boolean) As BoundNode
            If (node.OperatorKind And BinaryOperatorKind.Lifted) <> 0 Then
                Return RewriteLiftedIntrinsicBinaryOperatorSimple(node, optimizeForConditionalBranch)
            End If

            Return TransformRewrittenBinaryOperator(DirectCast(MyBase.VisitBinaryOperator(node), BoundBinaryOperator))
        End Function

        Private Function ReplaceMyGroupCollectionPropertyGetWithUnderlyingField(operand As BoundExpression) As BoundExpression

            If operand.HasErrors Then
                Return operand
            End If

            ' See Semantics::AlterForMyGroup
            ' "goo.Form1 IS something" is translated to "goo.m_Form1 IS something" when
            ' Form1 is a property generated by MyGroupCollection
            ' Otherwise 'goo.Form1 IS Nothing" would be always false because 'goo.Form1'
            ' property call creates an instance on the fly.

            Select Case operand.Kind
                Case BoundKind.DirectCast
                    ' Dig through possible DirectCast conversions
                    Dim cast = DirectCast(operand, BoundDirectCast)
                    Return cast.Update(ReplaceMyGroupCollectionPropertyGetWithUnderlyingField(cast.Operand),
                                       cast.ConversionKind, cast.SuppressVirtualCalls, cast.ConstantValueOpt, cast.RelaxationLambdaOpt, cast.Type)

                Case BoundKind.Conversion
                    ' Dig through possible conversion. For example, in context of an expression tree it is not changed to DirectCast conversion.
                    Dim cast = DirectCast(operand, BoundConversion)
                    Return cast.Update(ReplaceMyGroupCollectionPropertyGetWithUnderlyingField(cast.Operand),
                                       cast.ConversionKind, cast.Checked, cast.ExplicitCastInCode, cast.ConstantValueOpt,
                                       cast.ExtendedInfoOpt,
                                       cast.Type)

                Case BoundKind.Call
                    Dim boundCall = DirectCast(operand, BoundCall)

                    If boundCall.Method.MethodKind = MethodKind.PropertyGet AndAlso
                       boundCall.Method.AssociatedSymbol IsNot Nothing AndAlso
                       boundCall.Method.AssociatedSymbol.IsMyGroupCollectionProperty Then
                        Return New BoundFieldAccess(boundCall.Syntax,
                                                    boundCall.ReceiverOpt,
                                                    DirectCast(boundCall.Method.AssociatedSymbol, PropertySymbol).AssociatedField,
                                                    isLValue:=False,
                                                    type:=boundCall.Type)
                    End If

                Case BoundKind.PropertyAccess
                    ' Can get here when we are inside a lambda converted to an expression tree.
                    Debug.Assert(_inExpressionLambda)
                    Dim propertyAccess = DirectCast(operand, BoundPropertyAccess)

                    If propertyAccess.AccessKind = PropertyAccessKind.Get AndAlso
                       propertyAccess.PropertySymbol.IsMyGroupCollectionProperty Then
                        Return New BoundFieldAccess(propertyAccess.Syntax,
                                                    propertyAccess.ReceiverOpt,
                                                    propertyAccess.PropertySymbol.AssociatedField,
                                                    isLValue:=False,
                                                    type:=propertyAccess.Type)
                    End If

                Case Else
                    Debug.Assert(operand.Kind <> BoundKind.Parenthesized) ' Must have been removed by now.
            End Select

            Return operand
        End Function

        Private Function TransformRewrittenBinaryOperator(node As BoundBinaryOperator) As BoundExpression
            Dim opKind = node.OperatorKind

            Debug.Assert((opKind And BinaryOperatorKind.Lifted) = 0)

            Select Case (opKind And BinaryOperatorKind.OpMask)
                Case BinaryOperatorKind.Is, BinaryOperatorKind.IsNot
                    node = node.Update(node.OperatorKind,
                                       ReplaceMyGroupCollectionPropertyGetWithUnderlyingField(node.Left),
                                       ReplaceMyGroupCollectionPropertyGetWithUnderlyingField(node.Right),
                                       node.Checked,
                                       node.ConstantValueOpt,
                                       node.Type)

                    If (node.Left.Type IsNot Nothing AndAlso node.Left.Type.IsNullableType) OrElse
                       (node.Right.Type IsNot Nothing AndAlso node.Right.Type.IsNullableType) Then

                        Return RewriteNullableIsOrIsNotOperator(node)
                    End If

                Case BinaryOperatorKind.Concatenate  ' Concat needs to be done before expr trees, so in LocalRewriter instead of VBSemanticsRewriter
                    If node.Type.IsObjectType() Then
                        Return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConcatenateObjectObjectObject)
                    Else
                        Return RewriteConcatenateOperator(node)
                    End If

                Case BinaryOperatorKind.Like
                    If node.Left.Type.IsObjectType() Then
                        Return RewriteLikeOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_LikeOperator__LikeObjectObjectObjectCompareMethod)
                    Else
                        Return RewriteLikeOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_LikeOperator__LikeStringStringStringCompareMethod)
                    End If

                Case BinaryOperatorKind.Equals
                    Dim leftType = node.Left.Type
                    ' NOTE: For some reason Dev11 seems to still ignore inside the expression tree the fact that the target 
                    '       type of the binary operator is Boolean and used Object op Object => Object helpers even in this case 
                    '       despite what is said in comments in RuntimeMembers CodeGenerator::GetHelperForObjRelOp
                    ' TODO: Recheck

                    If node.Type.IsObjectType() OrElse Me._inExpressionLambda AndAlso leftType.IsObjectType() Then
                        Return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectEqualObjectObjectBoolean)
                    ElseIf node.Type.IsBooleanType() Then

                        If leftType.IsObjectType() Then
                            Return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectEqualObjectObjectBoolean)
                        ElseIf leftType.IsStringType() Then
                            Return RewriteStringComparisonOperator(node)
                        ElseIf leftType.IsDecimalType() Then
                            Return RewriteDecimalComparisonOperator(node)
                        ElseIf leftType.IsDateTimeType() Then
                            Return RewriteDateComparisonOperator(node)
                        End If
                    End If

                Case BinaryOperatorKind.NotEquals
                    Dim leftType = node.Left.Type
                    ' NOTE: See comment above

                    If node.Type.IsObjectType() OrElse Me._inExpressionLambda AndAlso leftType.IsObjectType() Then
                        Return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectNotEqualObjectObjectBoolean)
                    ElseIf node.Type.IsBooleanType() Then

                        If leftType.IsObjectType() Then
                            Return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectNotEqualObjectObjectBoolean)
                        ElseIf leftType.IsStringType() Then
                            Return RewriteStringComparisonOperator(node)
                        ElseIf leftType.IsDecimalType() Then
                            Return RewriteDecimalComparisonOperator(node)
                        ElseIf leftType.IsDateTimeType() Then
                            Return RewriteDateComparisonOperator(node)
                        End If
                    End If

                Case BinaryOperatorKind.LessThanOrEqual
                    Dim leftType = node.Left.Type
                    ' NOTE: See comment above

                    If node.Type.IsObjectType() OrElse Me._inExpressionLambda AndAlso leftType.IsObjectType() Then
                        Return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectLessEqualObjectObjectBoolean)
                    ElseIf node.Type.IsBooleanType() Then

                        If leftType.IsObjectType() Then
                            Return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectLessEqualObjectObjectBoolean)
                        ElseIf leftType.IsStringType() Then
                            Return RewriteStringComparisonOperator(node)
                        ElseIf leftType.IsDecimalType() Then
                            Return RewriteDecimalComparisonOperator(node)
                        ElseIf leftType.IsDateTimeType() Then
                            Return RewriteDateComparisonOperator(node)
                        End If
                    End If

                Case BinaryOperatorKind.GreaterThanOrEqual
                    Dim leftType = node.Left.Type
                    ' NOTE: See comment above

                    If node.Type.IsObjectType() OrElse Me._inExpressionLambda AndAlso leftType.IsObjectType() Then
                        Return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectGreaterEqualObjectObjectBoolean)
                    ElseIf node.Type.IsBooleanType() Then

                        If leftType.IsObjectType() Then
                            Return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectGreaterEqualObjectObjectBoolean)
                        ElseIf leftType.IsStringType() Then
                            Return RewriteStringComparisonOperator(node)
                        ElseIf leftType.IsDecimalType() Then
                            Return RewriteDecimalComparisonOperator(node)
                        ElseIf leftType.IsDateTimeType() Then
                            Return RewriteDateComparisonOperator(node)
                        End If
                    End If

                Case BinaryOperatorKind.LessThan
                    Dim leftType = node.Left.Type
                    ' NOTE: See comment above

                    If node.Type.IsObjectType() OrElse Me._inExpressionLambda AndAlso leftType.IsObjectType() Then
                        Return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectLessObjectObjectBoolean)
                    ElseIf node.Type.IsBooleanType() Then

                        If leftType.IsObjectType() Then
                            Return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectLessObjectObjectBoolean)
                        ElseIf leftType.IsStringType() Then
                            Return RewriteStringComparisonOperator(node)
                        ElseIf leftType.IsDecimalType() Then
                            Return RewriteDecimalComparisonOperator(node)
                        ElseIf leftType.IsDateTimeType() Then
                            Return RewriteDateComparisonOperator(node)
                        End If
                    End If

                Case BinaryOperatorKind.GreaterThan
                    Dim leftType = node.Left.Type
                    ' NOTE: See comment above

                    If node.Type.IsObjectType() OrElse Me._inExpressionLambda AndAlso leftType.IsObjectType() Then
                        Return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectGreaterObjectObjectBoolean)
                    ElseIf node.Type.IsBooleanType() Then

                        If leftType.IsObjectType() Then
                            Return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectGreaterObjectObjectBoolean)
                        ElseIf leftType.IsStringType() Then
                            Return RewriteStringComparisonOperator(node)
                        ElseIf leftType.IsDecimalType() Then
                            Return RewriteDecimalComparisonOperator(node)
                        ElseIf leftType.IsDateTimeType() Then
                            Return RewriteDateComparisonOperator(node)
                        End If
                    End If

                Case BinaryOperatorKind.Add
                    If node.Type.IsObjectType() AndAlso Not _inExpressionLambda Then
                        Return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__AddObjectObjectObject)
                    ElseIf node.Type.IsDecimalType() Then
                        Return RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__AddDecimalDecimal)
                    End If

                Case BinaryOperatorKind.Subtract
                    If node.Type.IsObjectType() AndAlso Not _inExpressionLambda Then
                        Return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__SubtractObjectObjectObject)
                    ElseIf node.Type.IsDecimalType() Then
                        Return RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__SubtractDecimalDecimal)
                    End If

                Case BinaryOperatorKind.Multiply
                    If node.Type.IsObjectType() AndAlso Not _inExpressionLambda Then
                        Return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__MultiplyObjectObjectObject)
                    ElseIf node.Type.IsDecimalType() Then
                        Return RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__MultiplyDecimalDecimal)
                    End If

                Case BinaryOperatorKind.Modulo
                    If node.Type.IsObjectType() AndAlso Not _inExpressionLambda Then
                        Return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ModObjectObjectObject)
                    ElseIf node.Type.IsDecimalType() Then
                        Return RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__RemainderDecimalDecimal)
                    End If

                Case BinaryOperatorKind.Divide
                    If node.Type.IsObjectType() AndAlso Not _inExpressionLambda Then
                        Return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__DivideObjectObjectObject)
                    ElseIf node.Type.IsDecimalType() Then
                        Return RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__DivideDecimalDecimal)
                    End If

                Case BinaryOperatorKind.IntegerDivide
                    If node.Type.IsObjectType() AndAlso Not _inExpressionLambda Then
                        Return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__IntDivideObjectObjectObject)
                    End If

                Case BinaryOperatorKind.Power
                    If node.Type.IsObjectType() AndAlso Not _inExpressionLambda Then
                        Return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ExponentObjectObjectObject)
                    Else
                        Return RewritePowOperator(node)
                    End If

                Case BinaryOperatorKind.LeftShift
                    If node.Type.IsObjectType() AndAlso Not _inExpressionLambda Then
                        Return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__LeftShiftObjectObjectObject)
                    End If

                Case BinaryOperatorKind.RightShift
                    If node.Type.IsObjectType() AndAlso Not _inExpressionLambda Then
                        Return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__RightShiftObjectObjectObject)
                    End If

                Case BinaryOperatorKind.OrElse, BinaryOperatorKind.AndAlso
                    If node.Type.IsObjectType() AndAlso Not _inExpressionLambda Then
                        Return RewriteObjectShortCircuitOperator(node)
                    End If

                Case BinaryOperatorKind.Xor
                    If node.Type.IsObjectType() AndAlso Not _inExpressionLambda Then
                        Return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__XorObjectObjectObject)
                    End If

                Case BinaryOperatorKind.Or
                    If node.Type.IsObjectType() AndAlso Not _inExpressionLambda Then
                        Return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__OrObjectObjectObject)
                    End If

                Case BinaryOperatorKind.And
                    If node.Type.IsObjectType() AndAlso Not _inExpressionLambda Then
                        Return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__AndObjectObjectObject)
                    End If

            End Select

            Return node
        End Function

        Private Function RewriteDateComparisonOperator(node As BoundBinaryOperator) As BoundExpression
            If _inExpressionLambda Then
                Return node
            End If

            Debug.Assert(node.Left.Type.IsDateTimeType())
            Debug.Assert(node.Right.Type.IsDateTimeType())
            Debug.Assert(node.Type.IsBooleanType())

            Dim result As BoundExpression = node
            Dim left As BoundExpression = node.Left
            Dim right As BoundExpression = node.Right

            If left.Type.IsDateTimeType() AndAlso right.Type.IsDateTimeType() Then

                ' Rewrite as follows:
                ' DateTime.Compare(left, right) [Operator] 0

                Const memberId As SpecialMember = SpecialMember.System_DateTime__CompareDateTimeDateTime
                Dim memberSymbol = DirectCast(ContainingAssembly.GetSpecialTypeMember(memberId), MethodSymbol)

                If Not ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                    Debug.Assert(memberSymbol.ReturnType.SpecialType = SpecialType.System_Int32)

                    Dim compare = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                                ImmutableArray.Create(left, right), Nothing, memberSymbol.ReturnType)

                    result = New BoundBinaryOperator(node.Syntax,
                                                     node.OperatorKind And BinaryOperatorKind.OpMask,
                                                     compare,
                                                     New BoundLiteral(node.Syntax, ConstantValue.Create(0I), memberSymbol.ReturnType),
                                                     False,
                                                     node.Type)
                End If
            End If

            Return result
        End Function

        Private Function RewriteDecimalComparisonOperator(node As BoundBinaryOperator) As BoundExpression
            If _inExpressionLambda Then
                Return node
            End If

            Debug.Assert(node.Left.Type.IsDecimalType())
            Debug.Assert(node.Right.Type.IsDecimalType())
            Debug.Assert(node.Type.IsBooleanType())

            Dim result As BoundExpression = node
            Dim left As BoundExpression = node.Left
            Dim right As BoundExpression = node.Right

            If left.Type.IsDecimalType() AndAlso right.Type.IsDecimalType() Then

                ' Rewrite as follows:
                ' Decimal.Compare(left, right) [Operator] 0

                Const memberId As SpecialMember = SpecialMember.System_Decimal__CompareDecimalDecimal
                Dim memberSymbol = DirectCast(ContainingAssembly.GetSpecialTypeMember(memberId), MethodSymbol)

                If Not ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                    Debug.Assert(memberSymbol.ReturnType.SpecialType = SpecialType.System_Int32)

                    Dim compare = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                                ImmutableArray.Create(left, right), Nothing, memberSymbol.ReturnType)

                    result = New BoundBinaryOperator(node.Syntax,
                                                     node.OperatorKind And BinaryOperatorKind.OpMask,
                                                     compare,
                                                     New BoundLiteral(node.Syntax, ConstantValue.Create(0I), memberSymbol.ReturnType),
                                                     False,
                                                     node.Type)
                End If
            End If

            Return result
        End Function

        Private Function RewriteObjectShortCircuitOperator(node As BoundBinaryOperator) As BoundExpression
            Debug.Assert(node.Type.IsObjectType())

            Dim result As BoundExpression = node
            Dim rewrittenLeft As BoundExpression = node.Left
            Dim rewrittenRight As BoundExpression = node.Right

            If rewrittenLeft.Type.IsObjectType() AndAlso rewrittenRight.Type.IsObjectType() Then

                ' This operator translates into:
                '       DirectCast(ToBoolean(left) OrElse/AndAlso ToBoolean(right), Object)
                ' Result is boxed Boolean.

                ' Dev10 uses complex routine in IL gen that emits the calls and also avoids boxing+calling the helper
                ' for result of nested OrElse/AndAlso. I will try to achieve the same effect by digging into DirectCast node
                ' on each side. Since, expressions are rewritten bottom-up, we don't need to look deeper than one level.
                ' Note, we may unwrap unnecessary DirectCast node that wasn't created by this function for nested OrElse/AndAlso, 
                ' but this should not have any negative or observable side effect.

                Dim left As BoundExpression = rewrittenLeft
                Dim right As BoundExpression = rewrittenRight

                If left.Kind = BoundKind.DirectCast Then
                    Dim cast = DirectCast(left, BoundDirectCast)
                    If cast.Operand.Type.IsBooleanType() Then
                        ' Just get rid of DirectCast node.
                        left = cast.Operand
                    End If
                End If

                If right.Kind = BoundKind.DirectCast Then
                    Dim cast = DirectCast(right, BoundDirectCast)
                    If cast.Operand.Type.IsBooleanType() Then
                        ' Just get rid of DirectCast node.
                        right = cast.Operand
                    End If
                End If

                If left Is rewrittenLeft OrElse right Is rewrittenRight Then
                    ' Need to call ToBoolean
                    Const memberId As WellKnownMember = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToBooleanObject
                    Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(memberId), MethodSymbol)

                    If Not ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then

                        Debug.Assert(memberSymbol.ReturnType.IsBooleanType())
                        Debug.Assert(memberSymbol.Parameters(0).Type.IsObjectType())

                        If left Is rewrittenLeft Then
                            left = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                                 ImmutableArray.Create(left), Nothing, memberSymbol.ReturnType)
                        End If

                        If right Is rewrittenRight Then
                            right = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                                  ImmutableArray.Create(right), Nothing, memberSymbol.ReturnType)
                        End If
                    End If
                End If

                If left IsNot rewrittenLeft AndAlso right IsNot rewrittenRight Then
                    ' left and right are successfully rewritten
                    Debug.Assert(left.Type.IsBooleanType() AndAlso right.Type.IsBooleanType())

                    Dim op = New BoundBinaryOperator(node.Syntax, node.OperatorKind And BinaryOperatorKind.OpMask, left, right, False, left.Type)

                    ' Box result of the operator
                    result = New BoundDirectCast(node.Syntax, op, ConversionKind.WideningValue, node.Type, Nothing)
                End If

            Else
                Throw ExceptionUtilities.Unreachable
            End If

            Return result
        End Function

        Private Function RewritePowOperator(node As BoundBinaryOperator) As BoundExpression
            If _inExpressionLambda Then
                Return node
            End If

            Dim result As BoundExpression = node
            Dim left As BoundExpression = node.Left
            Dim right As BoundExpression = node.Right

            If node.Type.IsDoubleType() AndAlso left.Type.IsDoubleType() AndAlso right.Type.IsDoubleType() Then

                ' Rewrite as follows:
                ' Math.Pow(left, right)

                Const memberId As WellKnownMember = WellKnownMember.System_Math__PowDoubleDouble
                Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(memberId), MethodSymbol)

                If Not ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                    Debug.Assert(memberSymbol.ReturnType.IsDoubleType())

                    result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                           ImmutableArray.Create(left, right), Nothing, memberSymbol.ReturnType)
                End If
            End If

            Return result
        End Function

        Private Function RewriteDecimalBinaryOperator(node As BoundBinaryOperator, member As SpecialMember) As BoundExpression
            If _inExpressionLambda Then
                Return node
            End If

            Debug.Assert(node.Left.Type.IsDecimalType())
            Debug.Assert(node.Right.Type.IsDecimalType())
            Debug.Assert(node.Type.IsDecimalType())

            Dim result As BoundExpression = node
            Dim left As BoundExpression = node.Left
            Dim right As BoundExpression = node.Right

            If left.Type.IsDecimalType() AndAlso right.Type.IsDecimalType() Then

                ' Call Decimal.member(left, right)
                Dim memberSymbol = DirectCast(ContainingAssembly.GetSpecialTypeMember(member), MethodSymbol)

                If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                    Debug.Assert(memberSymbol.ReturnType.IsDecimalType())
                    result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                           ImmutableArray.Create(left, right), Nothing, memberSymbol.ReturnType)
                End If
            End If

            Return result
        End Function

        Private Function RewriteStringComparisonOperator(node As BoundBinaryOperator) As BoundExpression
            Debug.Assert(node.Left.Type.IsStringType())
            Debug.Assert(node.Right.Type.IsStringType())
            Debug.Assert(node.Type.IsBooleanType())

            Dim result As BoundExpression = node
            Dim left As BoundExpression = node.Left
            Dim right As BoundExpression = node.Right
            Dim compareText As Boolean = (node.OperatorKind And BinaryOperatorKind.CompareText) <> 0

            ' Rewrite as follows:
            ' Operators.CompareString(left, right, compareText)  [Operator] 0

            ' Prefer embedded version of the member if present
            Dim embeddedOperatorsType As NamedTypeSymbol = Compilation.GetWellKnownType(WellKnownType.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators)
            Dim compareStringMember As WellKnownMember =
                If(embeddedOperatorsType.IsErrorType AndAlso TypeOf embeddedOperatorsType Is MissingMetadataTypeSymbol,
                   WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareStringStringStringBoolean,
                   WellKnownMember.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators__CompareStringStringStringBoolean)
            Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(compareStringMember), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, compareStringMember, memberSymbol) Then
                Debug.Assert(memberSymbol.ReturnType.SpecialType = SpecialType.System_Int32)
                Debug.Assert(memberSymbol.Parameters(2).Type.IsBooleanType())

                Dim compare = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                       ImmutableArray.Create(left,
                                                             right,
                                                             New BoundLiteral(node.Syntax, ConstantValue.Create(compareText), memberSymbol.Parameters(2).Type)),
                                        Nothing,
                                        memberSymbol.ReturnType)

                result = New BoundBinaryOperator(node.Syntax, (node.OperatorKind And BinaryOperatorKind.OpMask),
                                                 compare, New BoundLiteral(node.Syntax, ConstantValue.Create(0I), memberSymbol.ReturnType),
                                                 False, node.Type)
            End If

            Return result
        End Function

        Private Function RewriteObjectComparisonOperator(node As BoundBinaryOperator, member As WellKnownMember) As BoundExpression
            Debug.Assert(node.Left.Type.IsObjectType())
            Debug.Assert(node.Right.Type.IsObjectType())
            Debug.Assert(node.Type.IsObjectType() OrElse node.Type.IsBooleanType())

            Dim result As BoundExpression = node
            Dim left As BoundExpression = node.Left
            Dim right As BoundExpression = node.Right
            Dim compareText As Boolean = (node.OperatorKind And BinaryOperatorKind.CompareText) <> 0

            ' Call member(left, right, compareText)
            Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                Debug.Assert(memberSymbol.ReturnType Is node.Type OrElse Me._inExpressionLambda AndAlso memberSymbol.ReturnType.IsObjectType)
                Debug.Assert(memberSymbol.Parameters(2).Type.IsBooleanType())

                result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                       ImmutableArray.Create(left,
                                                             right,
                                                             New BoundLiteral(node.Syntax, ConstantValue.Create(compareText), memberSymbol.Parameters(2).Type)),
                                        Nothing,
                                        memberSymbol.ReturnType,
                                        suppressObjectClone:=True)

                If Me._inExpressionLambda AndAlso memberSymbol.ReturnType.IsObjectType AndAlso node.Type.IsBooleanType Then
                    result = New BoundConversion(node.Syntax, DirectCast(result, BoundExpression), ConversionKind.NarrowingBoolean, node.Checked, False, node.Type)
                End If
            End If

            Return result
        End Function

        Private Function RewriteLikeOperator(node As BoundBinaryOperator, member As WellKnownMember) As BoundExpression
            Dim left As BoundExpression = node.Left
            Dim right As BoundExpression = node.Right
            Debug.Assert((node.Type.IsObjectType() AndAlso left.Type.IsObjectType() AndAlso right.Type.IsObjectType()) OrElse
                         (node.Type.IsBooleanType() AndAlso left.Type.IsStringType() AndAlso right.Type.IsStringType()))

            Dim result As BoundExpression = node
            Dim compareText As Boolean = (node.OperatorKind And BinaryOperatorKind.CompareText) <> 0

            ' Call member(left, right, if (compareText, 1, 0))
            Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                Debug.Assert(memberSymbol.ReturnType Is node.Type)
                Debug.Assert(memberSymbol.Parameters(2).Type.IsEnumType())
                Debug.Assert(memberSymbol.Parameters(2).Type.GetEnumUnderlyingTypeOrSelf().SpecialType = SpecialType.System_Int32)

                result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                       ImmutableArray.Create(left,
                                                             right,
                                                             New BoundLiteral(node.Syntax, ConstantValue.Create(If(compareText, 1I, 0I)), memberSymbol.Parameters(2).Type)),
                                        Nothing,
                                        memberSymbol.ReturnType,
                                        suppressObjectClone:=True)
            End If

            Return result
        End Function

        Private Function RewriteObjectBinaryOperator(node As BoundBinaryOperator, member As WellKnownMember) As BoundExpression
            Dim left As BoundExpression = node.Left
            Dim right As BoundExpression = node.Right
            Debug.Assert(left.Type.IsObjectType())
            Debug.Assert(right.Type.IsObjectType())
            Debug.Assert(node.Type.IsObjectType())

            Dim result As BoundExpression = node

            ' Call member(left, right)
            Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                       ImmutableArray.Create(left, right), Nothing, memberSymbol.ReturnType, suppressObjectClone:=True)
            End If

            Return result
        End Function

        Private Function RewriteLiftedIntrinsicBinaryOperatorSimple(node As BoundBinaryOperator, optimizeForConditionalBranch As Boolean) As BoundNode
            Dim left As BoundExpression = VisitExpressionNode(node.Left)
            Dim right As BoundExpression = VisitExpressionNode(GetRightOperand(node, optimizeForConditionalBranch))

            Return FinishRewriteOfLiftedIntrinsicBinaryOperator(node, left, right, optimizeForConditionalBranch)
        End Function

        Private Function FinishRewriteOfLiftedIntrinsicBinaryOperator(node As BoundBinaryOperator, left As BoundExpression, right As BoundExpression, optimizeForConditionalBranch As Boolean) As BoundExpression
            Debug.Assert((node.OperatorKind And BinaryOperatorKind.Lifted) <> 0)
            Debug.Assert(Not optimizeForConditionalBranch OrElse
                         (node.OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.OrElse OrElse
                         (node.OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.AndAlso)

            Dim leftHasValue = HasValue(left)
            Dim rightHasValue = HasValue(right)

            Dim leftHasNoValue = HasNoValue(left)
            Dim rightHasNoValue = HasNoValue(right)

            ' The goal of optimization is to eliminate the need to deal with instances of Nullable(Of Boolean) type as early as possible,
            ' and, as a result, simplify evaluation of built-in OrElse/AndAlso operators by eliminating the need to use three-valued Boolean logic.
            ' The optimization is possible because when an entire Boolean Expression is evaluated to Null, that has the same effect as if result
            ' of evaluation was False. However, we do want to preserve the original order of evaluation, according to language rules. 
            If optimizeForConditionalBranch AndAlso node.Type.IsNullableOfBoolean() AndAlso left.Type.IsNullableOfBoolean() AndAlso right.Type.IsNullableOfBoolean() AndAlso
               (leftHasValue OrElse Not Me._inExpressionLambda OrElse (node.OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.OrElse) Then

                Return RewriteAndOptimizeLiftedIntrinsicLogicalShortCircuitingOperator(node, left, right, leftHasNoValue, leftHasValue, rightHasNoValue, rightHasValue)
            End If

            If Me._inExpressionLambda Then
                Return node.Update(node.OperatorKind, left, right, node.Checked, node.ConstantValueOpt, node.Type)
            End If

            ' Check for trivial (no nulls, two nulls) Cases

            '== TWO NULLS
            If leftHasNoValue And rightHasNoValue Then
                ' return new R?()
                Return NullableNull(left, node.Type)
            End If

            '== NO NULLS
            If leftHasValue And rightHasValue Then
                ' return new R?(UnliftedOp(left, right))
                Dim unliftedOp = ApplyUnliftedBinaryOp(node,
                                                      NullableValueOrDefault(left),
                                                      NullableValueOrDefault(right))

                Return WrapInNullable(unliftedOp, node.Type)
            End If

            ' non-trivial cases rewrite differently when operands are boolean
            If node.Left.Type.IsNullableOfBoolean Then
                Select Case (node.OperatorKind And BinaryOperatorKind.OpMask)
                    'boolean context makes no difference for Xor.
                    Case BinaryOperatorKind.And,
                        BinaryOperatorKind.Or,
                        BinaryOperatorKind.AndAlso,
                        BinaryOperatorKind.OrElse

                        Return RewriteLiftedBooleanBinaryOperator(node, left, right, leftHasNoValue, rightHasNoValue, leftHasValue, rightHasValue)
                End Select
            End If

            '== ONE NULL
            ' result is null. No need to do the Op, even if checked.
            If leftHasNoValue Or rightHasNoValue Then
                'Reducing to {[left | right] ; Null }
                Dim notNullOperand = If(leftHasNoValue, right, left)
                Dim nullOperand = NullableNull(If(leftHasNoValue, left, right), node.Type)

                Return MakeSequence(notNullOperand, nullOperand)
            End If

            ' At this point both operands are not known to be nulls, both may need to be evaluated
            ' We may also statically know if one is definitely not a null
            ' we cannot know, though, if whole operator yields null or not.

            If rightHasValue Then
                Dim whenNotNull As BoundExpression = Nothing
                Dim whenNull As BoundExpression = Nothing
                If IsConditionalAccess(left, whenNotNull, whenNull) Then
                    Dim rightValue = NullableValueOrDefault(right)

                    If (rightValue.IsConstant OrElse rightValue.Kind = BoundKind.Local OrElse rightValue.Kind = BoundKind.Parameter) AndAlso
                       HasValue(whenNotNull) AndAlso HasNoValue(whenNull) Then

                        Return UpdateConditionalAccess(left,
                                                       WrapInNullable(ApplyUnliftedBinaryOp(node,
                                                                                            NullableValueOrDefault(whenNotNull),
                                                                                            rightValue),
                                                                      node.Type),
                                                       NullableNull(whenNull, node.Type))
                    End If
                End If
            End If

            Dim temps As ArrayBuilder(Of LocalSymbol) = Nothing
            Dim inits As ArrayBuilder(Of BoundExpression) = Nothing

            Dim leftHasValueExpr As BoundExpression = Nothing
            Dim rightHasValueExpr As BoundExpression = Nothing

            Dim processedLeft = ProcessNullableOperand(left,
                                                       leftHasValueExpr,
                                                       temps,
                                                       inits,
                                                       RightCantChangeLeftLocal(left, right),
                                                       leftHasValue)

            ' left is done when right is running, so right cannot change if it is a local
            Dim processedRight = ProcessNullableOperand(right,
                                                        rightHasValueExpr,
                                                        temps,
                                                        inits,
                                                        doNotCaptureLocals:=True,
                                                        operandHasValue:=rightHasValue)

            Dim value As BoundExpression = Nothing

            Dim operatorHasValue As BoundExpression = MakeBooleanBinaryExpression(node.Syntax,
                                                             BinaryOperatorKind.And,
                                                             leftHasValueExpr,
                                                             rightHasValueExpr)

            Dim unliftedOpOnCaptured = ApplyUnliftedBinaryOp(node,
                                                            processedLeft,
                                                            processedRight)

            value = MakeTernaryConditionalExpression(node.Syntax,
                                              operatorHasValue,
                                              WrapInNullable(unliftedOpOnCaptured, node.Type),
                                              NullableNull(node.Syntax, node.Type))

            ' if we used temps, arrange a sequence for them.
            If temps IsNot Nothing Then
                value = New BoundSequence(node.Syntax,
                                     temps.ToImmutableAndFree,
                                     inits.ToImmutableAndFree,
                                     value,
                                     value.Type)
            End If

            Return value
        End Function

        ''' <summary>
        ''' The goal of optimization is to eliminate the need to deal with instances of Nullable(Of Boolean) type as early as possible,
        ''' and, as a result, simplify evaluation of built-in OrElse/AndAlso operators by eliminating the need to use three-valued Boolean logic.
        ''' The optimization is possible because when an entire Boolean Expression is evaluated to Null, that has the same effect as if result
        ''' of evaluation was False. However, we do want to preserve the original order of evaluation, according to language rules. 
        ''' This method returns an expression that still has Nullable(Of Boolean) type, but that expression is much simpler and can be 
        ''' further simplified by the consumer.
        ''' </summary>
        Private Function RewriteAndOptimizeLiftedIntrinsicLogicalShortCircuitingOperator(node As BoundBinaryOperator,
                                                                                         left As BoundExpression, right As BoundExpression,
                                                                                         leftHasNoValue As Boolean, leftHasValue As Boolean,
                                                                                         rightHasNoValue As Boolean, rightHasValue As Boolean) As BoundExpression

            Debug.Assert(leftHasValue OrElse Not Me._inExpressionLambda OrElse (node.OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.OrElse)
            Dim booleanResult As BoundExpression = Nothing

            If Not Me._inExpressionLambda Then
                If leftHasNoValue And rightHasNoValue Then
                    ' return new R?(), the consumer will take care of optimizing it out if possible. 
                    Return NullableNull(left, node.Type)
                End If

                If (node.OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.OrElse Then
                    If leftHasNoValue Then
                        ' There is nothing to evaluate on the left, the result is True only if Right is True
                        Return right
                    ElseIf rightHasNoValue Then
                        ' There is nothing to evaluate on the right, the result is True only if Left is True
                        Return left
                    End If
                Else
                    Debug.Assert((node.OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.AndAlso)

                    If leftHasNoValue Then
                        ' We can return False in this case. There is nothing to evaluate on the left, but we still need to evaluate the right
                        booleanResult = EvaluateOperandAndReturnFalse(node, right, rightHasValue)
                    ElseIf rightHasNoValue Then
                        ' We can return False in this case. There is nothing to evaluate on the right, but we still need to evaluate the left
                        booleanResult = EvaluateOperandAndReturnFalse(node, left, leftHasValue)
                    ElseIf Not leftHasValue Then
                        ' We cannot tell whether Left is Null or not.
                        ' For [x AndAlso y] we can produce Boolean result as follows:
                        '
                        ' tempX = x
                        ' (Not tempX.HasValue OrElse tempX.GetValueOrDefault()) AndAlso
                        ' (y.GetValueOrDefault() AndAlso tempX.HasValue)
                        '
                        Dim leftTemp As SynthesizedLocal = Nothing
                        Dim leftInit As BoundExpression = Nothing

                        ' Right may be a method that takes Left byref - " local AndAlso TakesArgByref(local) "
                        ' So in general we must capture Left even if it is a local.
                        Dim capturedLeft = CaptureNullableIfNeeded(left, leftTemp, leftInit, RightCantChangeLeftLocal(left, right))

                        booleanResult = MakeBooleanBinaryExpression(node.Syntax,
                                            BinaryOperatorKind.AndAlso,
                                            MakeBooleanBinaryExpression(node.Syntax,
                                                BinaryOperatorKind.OrElse,
                                                New BoundUnaryOperator(node.Syntax,
                                                                       UnaryOperatorKind.Not,
                                                                       NullableHasValue(capturedLeft),
                                                                       False,
                                                                       node.Type.GetNullableUnderlyingType()),
                                                NullableValueOrDefault(capturedLeft)),
                                            MakeBooleanBinaryExpression(node.Syntax,
                                                BinaryOperatorKind.AndAlso,
                                                NullableValueOrDefault(right),
                                                NullableHasValue(capturedLeft)))

                        ' if we used temp, put it in a sequence
                        Debug.Assert((leftTemp Is Nothing) = (leftInit Is Nothing))
                        If leftTemp IsNot Nothing Then
                            booleanResult = New BoundSequence(node.Syntax,
                                                              ImmutableArray.Create(Of LocalSymbol)(leftTemp),
                                                              ImmutableArray.Create(Of BoundExpression)(leftInit),
                                                              booleanResult,
                                                              booleanResult.Type)
                        End If
                    End If
                End If
            End If

            If booleanResult Is Nothing Then
                ' UnliftedOp(left.GetValueOrDefault(), right.GetValueOrDefault()))
                ' For AndAlso, this optimization is valid only when we know that left has value
                Debug.Assert(leftHasValue OrElse (node.OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.OrElse)

                booleanResult = ApplyUnliftedBinaryOp(node, NullableValueOrDefaultWithOperandHasValue(left, leftHasValue), NullableValueOrDefaultWithOperandHasValue(right, rightHasValue))
            End If

            ' return new R?(booleanResult), the consumer will take care of optimizing out the creation of this Nullable(Of Boolean) instance, if possible.
            Return WrapInNullable(booleanResult, node.Type)
        End Function

        Private Function EvaluateOperandAndReturnFalse(node As BoundBinaryOperator, operand As BoundExpression, operandHasValue As Boolean) As BoundExpression
            Debug.Assert(node.Type.IsNullableOfBoolean())
            Debug.Assert(operand.Type.IsNullableOfBoolean())

            Dim result = New BoundLiteral(node.Syntax, ConstantValue.False, node.Type.GetNullableUnderlyingType())
            Return New BoundSequence(node.Syntax, ImmutableArray(Of LocalSymbol).Empty,
                                     ImmutableArray.Create(If(operandHasValue, NullableValueOrDefault(operand), operand)),
                                     result, result.Type)
        End Function

        Private Function NullableValueOrDefaultWithOperandHasValue(operand As BoundExpression, operandHasValue As Boolean) As BoundExpression
            Debug.Assert(operand.Type.IsNullableOfBoolean())

            If Not Me._inExpressionLambda OrElse operandHasValue Then
                Return NullableValueOrDefault(operand)
            Else
                ' In expression tree this will be shown as Coalesce, which is preferred over a GetValueOrDefault call  
                Return New BoundNullableIsTrueOperator(operand.Syntax, operand, operand.Type.GetNullableUnderlyingType())
            End If
        End Function

        Private Function RewriteLiftedBooleanBinaryOperator(node As BoundBinaryOperator,
                                                            left As BoundExpression,
                                                            right As BoundExpression,
                                                            leftHasNoValue As Boolean,
                                                            rightHasNoValue As Boolean,
                                                            leftHasValue As Boolean,
                                                            rightHasValue As Boolean) As BoundExpression

            Debug.Assert(left.Type.IsNullableOfBoolean AndAlso right.Type.IsNullableOfBoolean AndAlso node.Type.IsNullableOfBoolean)
            Debug.Assert(Not (leftHasNoValue And rightHasNoValue))
            Debug.Assert(Not (leftHasValue And rightHasValue))

            Dim nullableOfBoolean = node.Type
            Dim booleanType = nullableOfBoolean.GetNullableUnderlyingType

            Dim op = node.OperatorKind And BinaryOperatorKind.OpMask
            Dim isOr As Boolean = (op = BinaryOperatorKind.OrElse) OrElse
                                    (op = BinaryOperatorKind.Or)

            '== ONE NULL
            If leftHasNoValue Or rightHasNoValue Then
                Dim notNullOperand As BoundExpression
                Dim nullOperand As BoundExpression
                Dim operandHasValue As Boolean

                If rightHasNoValue Then
                    notNullOperand = left
                    nullOperand = right
                    operandHasValue = leftHasValue
                Else
                    notNullOperand = right
                    nullOperand = left
                    operandHasValue = rightHasValue
                End If

                If operandHasValue Then
                    ' reduce "Operand [And | AndAlso] NULL" ---> "If(Operand, NULL, False)".
                    ' reduce "Operand [Or | OrElse]   NULL" ---> "If(Operand, True, NULL)".

                    Dim syntax = notNullOperand.Syntax
                    Dim condition = NullableValueOrDefault(notNullOperand)

                    Return MakeTernaryConditionalExpression(node.Syntax,
                            condition,
                            If(isOr,
                                NullableTrue(syntax, nullableOfBoolean),
                                NullableNull(nullOperand, nullableOfBoolean)),
                            If(isOr,
                                NullableNull(nullOperand, nullableOfBoolean),
                                NullableFalse(syntax, nullableOfBoolean)))
                Else

                    ' Dev10 uses AndAlso, but since operands are captured and hasValue is basically an access to a local 
                    ' so it makes sense to use And and avoid branching.
                    ' reduce "Operand [And | AndAlso] NULL" --> "If(Operand.HasValue And Not Operand.GetValueOrDefault, Operand, NULL)".
                    ' reduce "Operand [Or | OrElse]   NULL" --> "If(Operand.GetValueOrDefault,                          Operand, NULL)".
                    Dim temp As SynthesizedLocal = Nothing
                    Dim tempInit As BoundExpression = Nothing

                    ' we have only one operand to evaluate, do not capture locals.
                    Dim capturedOperand = CaptureNullableIfNeeded(notNullOperand, temp, tempInit, doNotCaptureLocals:=True)

                    Dim syntax = notNullOperand.Syntax
                    Dim capturedOperandValue = NullableValueOrDefault(capturedOperand)

                    Dim condition = If(isOr,
                                       NullableValueOrDefault(capturedOperand),
                                       MakeBooleanBinaryExpression(syntax, BinaryOperatorKind.And,
                                                            NullableHasValue(capturedOperand),
                                                            New BoundUnaryOperator(syntax, UnaryOperatorKind.Not,
                                                                NullableValueOrDefault(capturedOperand),
                                                                False,
                                                                booleanType)))

                    Dim result As BoundExpression =
                        MakeTernaryConditionalExpression(node.Syntax,
                            condition,
                            capturedOperand,
                            NullableNull(nullOperand, nullableOfBoolean))

                    ' if we used a temp, arrange a sequence for it and its initialization
                    If temp IsNot Nothing Then
                        result = New BoundSequence(node.Syntax,
                                             ImmutableArray.Create(Of LocalSymbol)(temp),
                                             ImmutableArray.Create(tempInit),
                                             result,
                                             result.Type)
                    End If

                    Return result
                End If
            End If

            '== GENERAL CASE

            ' x And y is rewritten into:
            '
            ' tempX = x
            ' [tempY = y] ' if not short-circuiting
            ' If (tempX.HasValue AndAlso Not tempX.GetValueOrDefault(),
            '   False?,                             ' result based on the left operand
            '   If ((tempY = y).HasValue,           ' if short-circuiting, otherwise just "y.HasValue"
            '       If (tempY.GetValueOrDefault(),  ' innermost If
            '           tempX,
            '           False?),
            '       Null?)
            '
            ' Other operators rewrite using the same template, but constants and conditions are slightly different.
            ' Additionally we observe if HasValue is known statically or capturing may not be needed
            ' so some of the Ifs may be folded

            Dim IsShortCircuited = (op = BinaryOperatorKind.AndAlso Or op = BinaryOperatorKind.OrElse)

            Dim leftTemp As SynthesizedLocal = Nothing
            Dim leftInit As BoundExpression = Nothing
            Dim capturedLeft As BoundExpression = left

            ' Capture left operand if we do not know whether it has value (so that we could call HasValue and ValueOrDefault).
            If Not leftHasValue Then
                ' Right may be a method that takes Left byref - " local And TakesArgByref(local) "
                ' So in general we must capture Left even if it is a local.
                capturedLeft = CaptureNullableIfNeeded(left, leftTemp, leftInit, RightCantChangeLeftLocal(left, right))
            End If

            Dim rightTemp As SynthesizedLocal = Nothing
            Dim rightInit As BoundExpression = Nothing
            Dim capturedRight As BoundExpression = right

            ' Capture right operand if we do not know whether it has value and we are not short-circuiting
            ' on optimized left operand (in which case right operand is used only once).
            ' When we are short circuiting, the right operand will be captured at the point where it is
            ' evaluated (notice "tempY = y" in the template).
            If Not rightHasValue AndAlso Not (leftHasValue AndAlso IsShortCircuited) Then
                ' when evaluating Right, left is already evaluated so we leave right local as-is.
                ' nothing can change it.
                capturedRight = CaptureNullableIfNeeded(capturedRight, rightTemp, rightInit, doNotCaptureLocals:=True)
            End If

            Dim OperandValue As BoundExpression = If(leftHasValue, capturedRight, capturedLeft)
            Dim ConstValue As BoundExpression = NullableOfBooleanValue(node.Syntax, isOr, nullableOfBoolean)

            ' innermost If
            Dim value As BoundExpression = MakeTernaryConditionalExpression(node.Syntax,
                If(leftHasValue,
                   NullableValueOrDefault(capturedLeft),
                   NullableValueOrDefault(capturedRight)),
                If(isOr,
                   ConstValue,
                   OperandValue),
                If(isOr,
                   OperandValue,
                   ConstValue))

            If Not leftHasValue Then
                If Not rightHasValue Then
                    ' second nested if - when need to look at Right
                    '
                    ' If (right.HasValue, 
                    '       nestedIf,
                    '       Null)
                    '
                    ' note that we use init of the Right as a target of HasValue when short-circuiting 
                    ' it will run only if we do not get result after looking at left
                    '
                    ' NOTE: when not short circuiting we use captured right and evaluate init 
                    ' unconditionally before all the ifs.

                    Dim conditionOperand As BoundExpression
                    If IsShortCircuited Then
                        ' use init if we have one.
                        ' if Right is trivial local or const, may not have init, just use capturedRight
                        conditionOperand = If(rightInit, capturedRight)

                        ' make sure init can no longer be used
                        rightInit = Nothing
                    Else
                        conditionOperand = capturedRight
                    End If

                    value = MakeTernaryConditionalExpression(node.Syntax,
                        NullableHasValue(conditionOperand),
                        value,
                        NullableNull(node.Syntax, nullableOfBoolean))
                End If

                If Not rightHasValue OrElse IsShortCircuited Then
                    Dim capturedLeftValue As BoundExpression = NullableValueOrDefault(capturedLeft)

                    Dim leftCapturedOrInit As BoundExpression

                    If rightInit IsNot Nothing OrElse leftInit Is Nothing Then
                        leftCapturedOrInit = capturedLeft
                    Else
                        leftCapturedOrInit = leftInit
                        leftInit = Nothing
                    End If

                    ' Outermost If (do we know result after looking at first operand ?):
                    '
                    ' Or -
                    ' If (left.HasValue AndAlso left.Value, ...

                    ' And -
                    ' If (left.HasValue AndAlso Not left.Value, ...
                    '
                    'TODO: when not initializing right temp, can use And. (fewer branches)
                    value = MakeTernaryConditionalExpression(node.Syntax,
                        MakeBooleanBinaryExpression(node.Syntax,
                            BinaryOperatorKind.AndAlso,
                            NullableHasValue(leftCapturedOrInit),
                            If(isOr,
                               capturedLeftValue,
                               New BoundUnaryOperator(node.Syntax,
                                                      UnaryOperatorKind.Not,
                                                      capturedLeftValue,
                                                      False,
                                                      booleanType))),
                        NullableOfBooleanValue(node.Syntax, isOr, nullableOfBoolean),
                        value)
                End If
            End If

            ' if we used temps, and did not embed inits, put them in a sequence
            If leftTemp IsNot Nothing OrElse rightTemp IsNot Nothing Then
                Dim temps = ArrayBuilder(Of LocalSymbol).GetInstance
                Dim inits = ArrayBuilder(Of BoundExpression).GetInstance

                If leftTemp IsNot Nothing Then
                    temps.Add(leftTemp)
                    If leftInit IsNot Nothing Then
                        inits.Add(leftInit)
                    End If
                End If

                If rightTemp IsNot Nothing Then
                    temps.Add(rightTemp)
                    If rightInit IsNot Nothing Then
                        inits.Add(rightInit)
                    End If
                End If

                value = New BoundSequence(node.Syntax,
                                         temps.ToImmutableAndFree,
                                         inits.ToImmutableAndFree,
                                         value,
                                         value.Type)
            End If

            Return value
        End Function

        Private Function RewriteNullableIsOrIsNotOperator(node As BoundBinaryOperator) As BoundExpression
            Dim left As BoundExpression = node.Left
            Dim right As BoundExpression = node.Right

            Debug.Assert(left.IsNothingLiteral OrElse right.IsNothingLiteral)
            Debug.Assert(node.OperatorKind = BinaryOperatorKind.Is OrElse node.OperatorKind = BinaryOperatorKind.IsNot)

            If _inExpressionLambda Then
                Return node
            End If

            Return RewriteNullableIsOrIsNotOperator((node.OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.Is, If(left.IsNothingLiteral, right, left), node.Type)
        End Function

        Private Function RewriteNullableIsOrIsNotOperator(isIs As Boolean, operand As BoundExpression, resultType As TypeSymbol) As BoundExpression
            Debug.Assert(resultType.IsBooleanType())
            Debug.Assert(operand.Type.IsNullableType)

            If HasNoValue(operand) Then
                Return New BoundLiteral(operand.Syntax,
                                        If(isIs,
                                            ConstantValue.True,
                                            ConstantValue.False),
                                        resultType)

            ElseIf HasValue(operand) Then
                Return MakeSequence(operand, New BoundLiteral(operand.Syntax,
                                    If(isIs,
                                        ConstantValue.False,
                                        ConstantValue.True),
                                    resultType))
            Else

                Dim whenNotNull As BoundExpression = Nothing
                Dim whenNull As BoundExpression = Nothing
                If IsConditionalAccess(operand, whenNotNull, whenNull) Then
                    If HasNoValue(whenNull) Then
                        Return UpdateConditionalAccess(operand,
                                                       RewriteNullableIsOrIsNotOperator(isIs, whenNotNull, resultType),
                                                       RewriteNullableIsOrIsNotOperator(isIs, whenNull, resultType))
                    End If
                End If

                Dim result As BoundExpression

                result = NullableHasValue(operand)
                If isIs Then
                    result = New BoundUnaryOperator(result.Syntax,
                                                    UnaryOperatorKind.Not,
                                                    result,
                                                    False,
                                                    resultType)
                End If

                Return result
            End If
        End Function

        Private Function RewriteLiftedUserDefinedBinaryOperator(node As BoundUserDefinedBinaryOperator) As BoundNode
            '
            ' Lifted user defined operator has structure as the following:
            '            
            '                    |          
            '             [implicit wrap]
            '                    |
            '                  CALL
            '                   /\
            '  [implicit unwrap]   [implicit unwrap]
            '         |                   |
            '       LEFT                RIGHT
            '
            ' Implicit left/right unwrapping conversions if present are always L? -> L and R? -> R
            ' They are encoded as a disparity between CALL argument types and parameter types of the call symbol.
            '
            ' Implicit wrapping conversion of the result, if present, is always T -> T?
            '
            ' The rewrite is:
            '   If (LEFT.HasValue And RIGHT.HasValue, CALL(LEFT, RIGHT), Null)
            '
            ' Note that the result of the operator is nullable type. 

            Dim left = Me.VisitExpressionNode(node.Left)
            Dim right = Me.VisitExpressionNode(node.Right)
            Dim operatorCall = node.Call

            Dim resultType = operatorCall.Type

            Debug.Assert(resultType.IsNullableType())
            Dim whenHasNoValue = NullableNull(node.Syntax, resultType)

            Debug.Assert(left.Type.IsNullableType() AndAlso right.Type.IsNullableType(), "left and right must be nullable")

            Dim leftHasNoValue As Boolean = HasNoValue(left)
            Dim rightHasNoValue As Boolean = HasNoValue(right)

            ' TWO NULLS
            If (leftHasNoValue And rightHasNoValue) Then
                Return whenHasNoValue
            End If

            ' ONE NULL
            If (leftHasNoValue Or rightHasNoValue) Then
                Return MakeSequence(If(leftHasNoValue, right, left), whenHasNoValue)
            End If

            Dim temps As ArrayBuilder(Of LocalSymbol) = Nothing
            Dim inits As ArrayBuilder(Of BoundExpression) = Nothing

            ' PREPARE OPERANDS
            Dim leftHasValue As Boolean = HasValue(left)
            Dim rightHasValue As Boolean = HasValue(right)

            Dim leftCallInput As BoundExpression
            Dim rightCallInput As BoundExpression
            Dim condition As BoundExpression = Nothing

            If leftHasValue Then
                leftCallInput = NullableValueOrDefault(left)

                If rightHasValue Then
                    rightCallInput = NullableValueOrDefault(right)
                Else
                    leftCallInput = CaptureNullableIfNeeded(leftCallInput, temps, inits, doNotCaptureLocals:=True)
                    rightCallInput = ProcessNullableOperand(right, condition, temps, inits, doNotCaptureLocals:=True)
                End If
            ElseIf rightHasValue Then
                leftCallInput = ProcessNullableOperand(left, condition, temps, inits, doNotCaptureLocals:=True)
                rightCallInput = NullableValueOrDefault(right)
                rightCallInput = CaptureNullableIfNeeded(rightCallInput, temps, inits, doNotCaptureLocals:=True)
            Else
                Dim leftHasValueExpression As BoundExpression = Nothing
                Dim rightHasValueExpression As BoundExpression = Nothing

                leftCallInput = ProcessNullableOperand(left, leftHasValueExpression, temps, inits, doNotCaptureLocals:=True)
                rightCallInput = ProcessNullableOperand(right, rightHasValueExpression, temps, inits, doNotCaptureLocals:=True)

                condition = MakeBooleanBinaryExpression(node.Syntax, BinaryOperatorKind.And, leftHasValueExpression, rightHasValueExpression)
            End If

            Debug.Assert(leftCallInput.Type.IsSameTypeIgnoringAll(operatorCall.Method.Parameters(0).Type),
                         "operator must take either unwrapped values or not-nullable left directly")
            Debug.Assert(rightCallInput.Type.IsSameTypeIgnoringAll(operatorCall.Method.Parameters(1).Type),
                         "operator must take either unwrapped values or not-nullable right directly")

            Dim whenHasValue As BoundExpression = operatorCall.Update(operatorCall.Method,
                                                                       Nothing,
                                                                       operatorCall.ReceiverOpt,
                                                                       ImmutableArray.Create(Of BoundExpression)(leftCallInput, rightCallInput),
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

            If leftHasValue And rightHasValue Then
                Debug.Assert(temps Is Nothing AndAlso inits Is Nothing AndAlso condition Is Nothing)
                Return whenHasValue

            Else
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

        Private Function ApplyUnliftedBinaryOp(originalOperator As BoundBinaryOperator,
                                               left As BoundExpression,
                                               right As BoundExpression) As BoundExpression

            Debug.Assert(Not left.Type.IsNullableType)
            Debug.Assert(Not right.Type.IsNullableType)

            'return UnliftedOP(left, right)
            Dim unliftedOpKind = originalOperator.OperatorKind And (Not BinaryOperatorKind.Lifted)

            Return MakeBinaryExpression(originalOperator.Syntax,
                                        unliftedOpKind,
                                        left,
                                        right,
                                        originalOperator.Checked,
                                        originalOperator.Type.GetNullableUnderlyingType)
        End Function
    End Class

End Namespace
