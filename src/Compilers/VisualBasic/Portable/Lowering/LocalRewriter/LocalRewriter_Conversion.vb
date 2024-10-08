' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitConversion(node As BoundConversion) As BoundNode

            If Not _inExpressionLambda AndAlso Conversions.IsIdentityConversion(node.ConversionKind) Then

                Dim result = DirectCast(Visit(node.Operand), BoundExpression)

                If node.ExplicitCastInCode AndAlso IsFloatingPointExpressionOfUnknownPrecision(result) Then
                    ' To force a value of a floating point type to the exact precision of its type, an explicit cast can be used.
                    ' It means that explicit casts to CDbl() or CSng() should be preserved on the node.
                    ' If original conversion has become something else with unknown precision, add an explicit identity cast.
                    result = node.Update(
                        result,
                        ConversionKind.Identity,
                        checked:=False,
                        explicitCastInCode:=True,
                        constantValueOpt:=node.ConstantValueOpt,
                        extendedInfoOpt:=node.ExtendedInfoOpt,
                        type:=node.Type)
                End If

                Return result
            End If

            If node.Operand.Kind = BoundKind.UserDefinedConversion Then
                If _inExpressionLambda Then
                    Return node.Update(DirectCast(Visit(node.Operand), BoundExpression),
                                       node.ConversionKind,
                                       node.Checked,
                                       node.ExplicitCastInCode,
                                       node.ConstantValueOpt,
                                       node.ExtendedInfoOpt,
                                       node.Type)
                End If

                If (node.ConversionKind And ConversionKind.Nullable) <> 0 Then
                    Return RewriteNullableUserDefinedConversion(DirectCast(node.Operand, BoundUserDefinedConversion))
                End If
                Return Visit(DirectCast(node.Operand, BoundUserDefinedConversion).UnderlyingExpression)
            End If

            ' not all nullable conversions have Nullable flag
            ' For example   Nothing --> Boolean?  has conversionkind = WideningNothingLiteral
            If (node.Type IsNot Nothing AndAlso node.Type.IsNullableType OrElse
                node.Operand.Type IsNot Nothing AndAlso node.Operand.Type.IsNullableType) AndAlso
               Not _inExpressionLambda Then

                Return RewriteNullableConversion(node)
            End If

            ' Rewrite Anonymous Delegate conversion into a delegate creation
            If (node.ConversionKind And ConversionKind.AnonymousDelegate) <> 0 Then
                Return RewriteAnonymousDelegateConversion(node)
            End If

            ' Handle other conversions.
            Debug.Assert(TryCast(node.ExtendedInfoOpt, BoundRelaxationLambda)?.ReceiverPlaceholderOpt Is Nothing)

            ' Optimization for object comparisons that are operands of a conversion to boolean.
            ' Must be done before the object comparison is visited.
            If Not node.HasErrors AndAlso node.Type.IsBooleanType() AndAlso node.Operand.Type.IsObjectType() Then
                Dim operand As BoundNode = node.Operand

                ' Skip parens.
                While operand.Kind = BoundKind.Parenthesized
                    operand = DirectCast(operand, BoundParenthesized).Expression
                End While

                If operand.Kind = BoundKind.BinaryOperator Then

                    Dim binary = DirectCast(operand, BoundBinaryOperator)

                    Select Case binary.OperatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Equals,
                             BinaryOperatorKind.NotEquals,
                             BinaryOperatorKind.LessThan,
                             BinaryOperatorKind.LessThanOrEqual,
                             BinaryOperatorKind.GreaterThan,
                             BinaryOperatorKind.GreaterThanOrEqual
                            ' Attempt to optimize the coercion.
                            ' The result of the comparison is known to be boolean, so force it to be.
                            ' Rewrite of the operator will do the right thing.
                            Debug.Assert(binary.Type.IsObjectType())
                            Return Visit(binary.Update(binary.OperatorKind,
                                                       binary.Left,
                                                       binary.Right,
                                                       binary.Checked,
                                                       binary.ConstantValueOpt,
                                                       node.Type))
                    End Select
                End If
            End If

            ' Set "inExpressionLambda" if we're converting lambda to expression tree.
            Dim returnValue As BoundNode
            Dim wasInExpressionlambda As Boolean = _inExpressionLambda
            If (node.ConversionKind And (ConversionKind.Lambda Or ConversionKind.ConvertedToExpressionTree)) = (ConversionKind.Lambda Or ConversionKind.ConvertedToExpressionTree) Then
                _inExpressionLambda = True
            End If

            If node.ExtendedInfoOpt IsNot Nothing AndAlso node.ExtendedInfoOpt.Kind = BoundKind.RelaxationLambda Then
                returnValue = RewriteLambdaRelaxationConversion(node)

            ElseIf (node.ConversionKind And ConversionKind.InterpolatedString) = ConversionKind.InterpolatedString Then
                returnValue = RewriteInterpolatedStringConversion(node)

            ElseIf (node.ConversionKind And (ConversionKind.Tuple Or ConversionKind.Nullable)) = ConversionKind.Tuple Then
                returnValue = RewriteTupleConversion(node)

            Else
                returnValue = MyBase.VisitConversion(node)
                If returnValue.Kind = BoundKind.Conversion Then
                    returnValue = TransformRewrittenConversion(DirectCast(returnValue, BoundConversion))
                End If
            End If

            _inExpressionLambda = wasInExpressionlambda
            Return returnValue
        End Function

        Private Shared Function IsFloatingPointExpressionOfUnknownPrecision(rewrittenNode As BoundExpression) As Boolean
            If rewrittenNode Is Nothing Then
                Return False
            End If

            ' Note: no special handling for node having a constant value because it cannot reach here

            Dim specialType = rewrittenNode.Type.SpecialType
            If specialType <> SpecialType.System_Double AndAlso specialType <> SpecialType.System_Single Then
                Return False
            End If

            Select Case rewrittenNode.Kind
                ' ECMA-335   I.12.1.3 Handling of floating-point data types.
                '    ... the value might be retained in the internal representation
                '   for future use, if it is reloaded from the storage location without having been modified ...
                '
                ' Unfortunately, the above means that precision is not guaranteed even when loading from storage.
                '
                ' Case BoundKind.FieldAccess
                ' Case BoundKind.ArrayAccess
                '    Return True

                Case BoundKind.Sequence
                    Dim sequence = DirectCast(rewrittenNode, BoundSequence)
                    Return IsFloatingPointExpressionOfUnknownPrecision(sequence.ValueOpt)

                Case BoundKind.Conversion
                    ' lowered conversions have definite precision unless they are implicit identity casts
                    Dim conversion = DirectCast(rewrittenNode, BoundConversion)
                    Return conversion.ConversionKind = ConversionKind.Identity AndAlso Not conversion.ExplicitCastInCode
            End Select

            ' it is a float/double expression and we have no idea ...
            Return True
        End Function

        Private Function RewriteTupleConversion(node As BoundConversion) As BoundExpression
            Dim syntax = node.Syntax
            Dim rewrittenOperand = VisitExpression(node.Operand)
            Dim rewrittenType = DirectCast(VisitType(node.Type), NamedTypeSymbol)

            Return MakeTupleConversion(syntax, rewrittenOperand, rewrittenType, DirectCast(node.ExtendedInfoOpt, BoundConvertedTupleElements))
        End Function

        Private Function MakeTupleConversion(syntax As SyntaxNode, rewrittenOperand As BoundExpression, destinationType As TypeSymbol, convertedElements As BoundConvertedTupleElements) As BoundExpression
            If destinationType.IsSameTypeIgnoringAll(rewrittenOperand.Type) Then
                'binder keeps some tuple conversions just for the purpose of semantic model
                'otherwise they are as good as identity conversions

                Return rewrittenOperand
            End If

            Dim destElementTypes = destinationType.GetElementTypesOfTupleOrCompatible()
            Dim numElements = destElementTypes.Length

            Dim srcType As TypeSymbol = rewrittenOperand.Type
            Dim tupleTypeSymbol As TupleTypeSymbol

            If srcType.IsTupleType Then
                tupleTypeSymbol = DirectCast(srcType, TupleTypeSymbol)
            Else
                ' The following codepath should be very uncommon (if reachable at all)
                ' we should generally not see tuple compatible types in bound trees and 
                ' see actual tuple types instead.
                Debug.Assert(srcType.IsTupleCompatible())

                ' PERF: if allocations here become nuisance, consider caching the TupleTypeSymbol
                '       in the type symbols that can actually be tuple compatible
                tupleTypeSymbol = TupleTypeSymbol.Create(DirectCast(srcType, NamedTypeSymbol))
            End If

            Dim srcElementFields = tupleTypeSymbol.TupleElements

            Dim fieldAccessorsBuilder = ArrayBuilder(Of BoundExpression).GetInstance(numElements)
            Dim assignmentToTemp As BoundExpression = Nothing
            Dim tupleTemp As SynthesizedLocal = Nothing
            Dim savedTuple As BoundExpression = CaptureOperand(rewrittenOperand, tupleTemp, assignmentToTemp)

            Dim factory As New SyntheticBoundNodeFactory(_topMethod, _currentMethodOrLambda, syntax, _compilationState, _diagnostics)

            For i As Integer = 0 To numElements - 1
                Dim field = srcElementFields(i)

                Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = field.CalculateUseSiteInfo()

                ReportUseSite(rewrittenOperand, useSiteInfo, _diagnostics)

                Dim fieldAccess = MakeTupleFieldAccess(syntax, field, savedTuple, constantValueOpt:=Nothing, isLValue:=False)

                ' lower the conversion
                AddPlaceholderReplacement(convertedElements.ElementPlaceholders(i), fieldAccess)
                fieldAccessorsBuilder.Add(VisitExpression(convertedElements.ConvertedElements(i)))
                RemovePlaceholderReplacement(convertedElements.ElementPlaceholders(i))
            Next

            Dim result = MakeTupleCreationExpression(syntax, DirectCast(destinationType, NamedTypeSymbol), fieldAccessorsBuilder.ToImmutableAndFree())
            Return factory.Sequence(tupleTemp, assignmentToTemp, result)
        End Function

        Private Function RewriteLambdaRelaxationConversion(node As BoundConversion) As BoundNode
            Dim returnValue As BoundNode
            Dim relaxationLambda As BoundLambda = DirectCast(node.ExtendedInfoOpt, BoundRelaxationLambda).Lambda

            If _inExpressionLambda AndAlso
                 NoParameterRelaxation(node.Operand, relaxationLambda.LambdaSymbol) Then

                ' COMPAT: skip relaxation in this case. ET can drop the return value of the inner lambda.
                returnValue = MyBase.VisitConversion(
                    node.Update(node.Operand,
                                      node.ConversionKind, node.Checked, node.ExplicitCastInCode,
                                      node.ConstantValueOpt,
                                      extendedInfoOpt:=Nothing, type:=node.Type))

                returnValue = TransformRewrittenConversion(DirectCast(returnValue, BoundConversion))
            Else
                returnValue = node.Update(VisitExpressionNode(relaxationLambda),
                                      node.ConversionKind, node.Checked, node.ExplicitCastInCode,
                                      node.ConstantValueOpt,
                                      extendedInfoOpt:=Nothing, type:=node.Type)
            End If

            Return returnValue
        End Function

        Private Function RewriteLambdaRelaxationConversion(node As BoundDirectCast) As BoundNode
            Dim returnValue As BoundNode

            If _inExpressionLambda AndAlso
                 NoParameterRelaxation(node.Operand, node.RelaxationLambdaOpt.LambdaSymbol) Then

                ' COMPAT: skip relaxation in this case. ET can drop the return value of the inner lambda.
                returnValue = MyBase.VisitDirectCast(
                    node.Update(node.Operand,
                                      node.ConversionKind, node.SuppressVirtualCalls,
                                      node.ConstantValueOpt,
                                      relaxationLambdaOpt:=Nothing, type:=node.Type))

            Else
                returnValue = node.Update(VisitExpressionNode(node.RelaxationLambdaOpt),
                                      node.ConversionKind, node.SuppressVirtualCalls,
                                      node.ConstantValueOpt,
                                      relaxationLambdaOpt:=Nothing, type:=node.Type)
            End If

            Return returnValue
        End Function

        Private Function RewriteLambdaRelaxationConversion(node As BoundTryCast) As BoundNode
            Dim returnValue As BoundNode

            If _inExpressionLambda AndAlso
                 NoParameterRelaxation(node.Operand, node.RelaxationLambdaOpt.LambdaSymbol) Then

                ' COMPAT: skip relaxation in this case. ET can drop the return value of the inner lambda.
                returnValue = MyBase.VisitTryCast(
                    node.Update(node.Operand,
                                      node.ConversionKind,
                                      node.ConstantValueOpt,
                                      relaxationLambdaOpt:=Nothing, type:=node.Type))

            Else
                returnValue = node.Update(VisitExpressionNode(node.RelaxationLambdaOpt),
                                      node.ConversionKind,
                                      node.ConstantValueOpt,
                                      relaxationLambdaOpt:=Nothing, type:=node.Type)
            End If

            Return returnValue
        End Function

        Private Shared Function NoParameterRelaxation(from As BoundExpression, toLambda As LambdaSymbol) As Boolean
            Dim fromLambda As LambdaSymbol = TryCast(from, BoundLambda)?.LambdaSymbol

            ' are we are relaxing for the purpose of dropping return?
            Return fromLambda IsNot Nothing AndAlso
                Not fromLambda.IsSub AndAlso
                toLambda.IsSub AndAlso
                MethodSignatureComparer.HaveSameParameterTypes(fromLambda.Parameters, Nothing, toLambda.Parameters, Nothing, considerByRef:=True, considerCustomModifiers:=False, considerTupleNames:=False)

        End Function

        ' Rewrite Anonymous Delegate conversion into a delegate creation
        Private Function RewriteAnonymousDelegateConversion(node As BoundConversion) As BoundNode
            Debug.Assert(Not Conversions.IsIdentityConversion(node.ConversionKind))
            Debug.Assert(node.Operand.Type.IsDelegateType() AndAlso
                         DirectCast(node.Operand.Type, NamedTypeSymbol).IsAnonymousType AndAlso
                         node.Type.IsDelegateType() AndAlso
                         node.Type.SpecialType <> SpecialType.System_MulticastDelegate)

            Dim F As New SyntheticBoundNodeFactory(Me._topMethod, Me._currentMethodOrLambda, node.Syntax, Me._compilationState, Me._diagnostics)
            If (node.Operand.IsDefaultValueConstant) Then
                Return F.Null(node.Type)
            Else
                Dim lambdaOpt As BoundLambda
                Dim receiverPlaceholderOpt As BoundRValuePlaceholder

                If node.ExtendedInfoOpt IsNot Nothing Then
                    Dim relaxationLambda = DirectCast(node.ExtendedInfoOpt, BoundRelaxationLambda)
                    lambdaOpt = relaxationLambda.Lambda
                    receiverPlaceholderOpt = relaxationLambda.ReceiverPlaceholderOpt
                Else
                    lambdaOpt = Nothing
                    receiverPlaceholderOpt = Nothing
                End If

                If (Not Me._inExpressionLambda AndAlso CouldPossiblyBeNothing(F, node.Operand)) Then
                    Dim savedOriginalValue = F.SynthesizedLocal(node.Operand.Type)
                    Dim checkIfNothing = F.ReferenceIsNothing(F.Local(savedOriginalValue, False))
                    Dim conversionIfNothing = F.Null(node.Type)
                    Dim convertedValue = New BoundDelegateCreationExpression(node.Syntax, F.Local(savedOriginalValue, False),
                                                                                DirectCast(node.Operand.Type, NamedTypeSymbol).DelegateInvokeMethod,
                                                                                lambdaOpt,
                                                                                receiverPlaceholderOpt,
                                                                                methodGroupOpt:=Nothing,
                                                                                type:=node.Type)
                    Dim conditionalResult As BoundExpression = F.TernaryConditionalExpression(condition:=checkIfNothing, ifTrue:=conversionIfNothing, ifFalse:=convertedValue)
                    Return F.Sequence(savedOriginalValue,
                                      F.AssignmentExpression(F.Local(savedOriginalValue, True), VisitExpression(node.Operand)),
                                      VisitExpression(conditionalResult))
                Else
                    Dim convertedValue = New BoundDelegateCreationExpression(node.Syntax, node.Operand,
                                                                            DirectCast(node.Operand.Type, NamedTypeSymbol).DelegateInvokeMethod,
                                                                            lambdaOpt,
                                                                            receiverPlaceholderOpt,
                                                                            methodGroupOpt:=Nothing,
                                                                            type:=node.Type)
                    Return VisitExpression(convertedValue)
                End If
            End If
        End Function

        Private Function CouldPossiblyBeNothing(F As SyntheticBoundNodeFactory, node As BoundExpression) As Boolean
            Select Case node.Kind
                Case BoundKind.TernaryConditionalExpression
                    Dim t = DirectCast(node, BoundTernaryConditionalExpression)
                    Return CouldPossiblyBeNothing(F, t.WhenTrue) OrElse CouldPossiblyBeNothing(F, t.WhenFalse)
                Case BoundKind.Conversion
                    Dim t = DirectCast(node, BoundConversion)
                    Return CouldPossiblyBeNothing(F, t.Operand)
                Case BoundKind.Lambda
                    Return False
                Case BoundKind.Call
                    Dim t = DirectCast(node, BoundCall)
                    Return Not (t.Method = F.SpecialMember(SpecialMember.System_Delegate__CreateDelegate, True) OrElse
                                t.Method = F.SpecialMember(SpecialMember.System_Delegate__CreateDelegate4, True) OrElse
                                t.Method = F.WellKnownMember(Of MethodSymbol)(WellKnownMember.System_Reflection_MethodInfo__CreateDelegate, True))
                Case Else
                    Return True
            End Select
        End Function

        Private Function RewriteNullableConversion(node As BoundConversion) As BoundExpression
            Debug.Assert(Not _inExpressionLambda)

            Dim rewrittenOperand = DirectCast(Me.Visit(node.Operand), BoundExpression)

            If Conversions.IsIdentityConversion(node.ConversionKind) Then
                Debug.Assert(rewrittenOperand.Type.IsSameTypeIgnoringAll(node.Type))
                Return rewrittenOperand
            End If

            Return RewriteNullableConversion(node, rewrittenOperand)
        End Function

        Private Function RewriteNullableConversion(node As BoundConversion,
                                                   rewrittenOperand As BoundExpression) As BoundExpression
            Dim resultType = node.Type
            Dim operandType = rewrittenOperand.Type

            Debug.Assert(resultType.IsNullableType OrElse
                         (operandType IsNot Nothing AndAlso operandType.IsNullableType),
                         "operand or operator must be nullable")

            ' Nothing --> T? ==> new T?
            If rewrittenOperand.ConstantValueOpt Is ConstantValue.Nothing Then
                Return NullableNull(rewrittenOperand.Syntax, resultType)
            End If

            ' Conversions between reference types and nullables do not need further rewriting.
            ' Lifting will be done as part of box/unbox operation.
            If operandType.IsReferenceType OrElse resultType.IsReferenceType Then

                If resultType.IsStringType Then
                    ' conversion to string is an intrinsic conversion and can be lifted.
                    ' T? --> string   ==>   T.Value -- string
                    ' note that nullable null does not convert to string, i.e. this conversion
                    ' is not null-propagating

                    rewrittenOperand = NullableValue(rewrittenOperand)

                ElseIf operandType.IsStringType Then
                    ' CType(string, T?) ---> new T?(CType(string, T))
                    Dim innerTargetType = resultType.GetNullableUnderlyingType
                    Dim useSiteInfo = GetNewCompoundUseSiteInfo()
                    Dim convKind = Conversions.ClassifyConversion(rewrittenOperand.Type, innerTargetType, useSiteInfo).Key
                    Debug.Assert(Conversions.ConversionExists(convKind))
                    _diagnostics.Add(node, useSiteInfo)
                    Return WrapInNullable(
                                    TransformRewrittenConversion(
                                        node.Update(rewrittenOperand,
                                                    convKind,
                                                    node.Checked,
                                                    node.ExplicitCastInCode,
                                                    node.ConstantValueOpt,
                                                    node.ExtendedInfoOpt,
                                                    resultType.GetNullableUnderlyingType)),
                                    resultType)

                ElseIf operandType.IsNullableType Then
                    If HasNoValue(rewrittenOperand) Then
                        ' DirectCast(Nothing, operatorType)
                        Return New BoundDirectCast(node.Syntax,
                                                   MakeNullLiteral(rewrittenOperand.Syntax, resultType),
                                                   ConversionKind.WideningNothingLiteral,
                                                   resultType)
                    End If

                    If HasValue(rewrittenOperand) Then
                        ' DirectCast(operand.GetValueOrDefault, operatorType)
                        Dim unwrappedOperand = NullableValueOrDefault(rewrittenOperand)
                        Dim useSiteInfo = GetNewCompoundUseSiteInfo()
                        Dim convKind = Conversions.ClassifyDirectCastConversion(unwrappedOperand.Type, resultType, useSiteInfo)
                        Debug.Assert(Conversions.ConversionExists(convKind))
                        _diagnostics.Add(node, useSiteInfo)
                        Return New BoundDirectCast(node.Syntax,
                                                   unwrappedOperand,
                                                   convKind,
                                                   resultType)
                    End If
                End If

                Return TransformRewrittenConversion(
                            node.Update(rewrittenOperand,
                                        node.ConversionKind And (Not ConversionKind.Nullable),
                                        node.Checked,
                                        node.ExplicitCastInCode,
                                        node.ConstantValueOpt,
                                        node.ExtendedInfoOpt,
                                        resultType))
            End If

            Debug.Assert(Not resultType.IsSameTypeIgnoringAll(operandType), "converting to same type")
            Dim result As BoundExpression = rewrittenOperand

            ' unwrap operand if needed and propagate HasValue if needed.
            ' If need to propagate HasValue, may also need to hoist the operand value into a temp

            Dim operandHasValue As BoundExpression = Nothing

            Dim temps As ArrayBuilder(Of LocalSymbol) = Nothing
            Dim inits As ArrayBuilder(Of BoundExpression) = Nothing

            If operandType.IsNullableType Then
                If resultType.IsNullableType Then
                    If HasValue(rewrittenOperand) Then
                        ' just get the value
                        result = NullableValueOrDefault(rewrittenOperand)

                    ElseIf HasNoValue(rewrittenOperand) Then

                        ' converting null
                        Return NullableNull(result.Syntax, resultType)
                    Else
                        Dim whenNotNull As BoundExpression = Nothing
                        Dim whenNull As BoundExpression = Nothing
                        If IsConditionalAccess(rewrittenOperand, whenNotNull, whenNull) Then
                            If HasValue(whenNotNull) AndAlso HasNoValue(whenNull) Then
                                Return UpdateConditionalAccess(rewrittenOperand,
                                                               FinishRewriteNullableConversion(node, resultType, NullableValueOrDefault(whenNotNull), Nothing, Nothing, Nothing),
                                                               NullableNull(result.Syntax, resultType))
                            End If
                        End If

                        ' uncaptured locals are safe here because we are dealing with a single operand
                        result = ProcessNullableOperand(rewrittenOperand, operandHasValue, temps, inits, doNotCaptureLocals:=True)
                    End If
                Else
                    ' no propagation.
                    result = NullableValue(rewrittenOperand)
                End If
            End If

            Return FinishRewriteNullableConversion(node, resultType, result, operandHasValue, temps, inits)
        End Function

        Private Function GetNewCompoundUseSiteInfo() As CompoundUseSiteInfo(Of AssemblySymbol)
            Return New CompoundUseSiteInfo(Of AssemblySymbol)(_diagnostics, Me.Compilation.Assembly)
        End Function

        Private Function FinishRewriteNullableConversion(
            node As BoundConversion,
            resultType As TypeSymbol,
            operand As BoundExpression,
            operandHasValue As BoundExpression,
            temps As ArrayBuilder(Of LocalSymbol),
            inits As ArrayBuilder(Of BoundExpression)
        ) As BoundExpression
            Debug.Assert(resultType Is node.Type)

            Dim unwrappedResultType = resultType.GetNullableUnderlyingTypeOrSelf

            ' apply unlifted conversion
            If Not operand.Type.IsSameTypeIgnoringAll(unwrappedResultType) Then
                Dim useSiteInfo = GetNewCompoundUseSiteInfo()
                Dim convKind = Conversions.ClassifyConversion(operand.Type, unwrappedResultType, useSiteInfo).Key
                Debug.Assert(Conversions.ConversionExists(convKind))
                Debug.Assert((convKind And ConversionKind.Tuple) = (node.ConversionKind And ConversionKind.Tuple))

                ' Check for potential constant folding
                Dim integerOverflow As Boolean = False
                Dim constantResult = Conversions.TryFoldConstantConversion(operand, unwrappedResultType, integerOverflow)

                Debug.Assert(constantResult Is Nothing OrElse Not constantResult.IsBad)

                If constantResult IsNot Nothing AndAlso Not constantResult.IsBad Then
                    ' Overflow should have been detected at classification time during binding.
                    Debug.Assert(Not integerOverflow OrElse Not node.Checked)
                    operand = RewriteConstant(New BoundLiteral(node.Syntax, constantResult, unwrappedResultType), constantResult)

                Else
                    _diagnostics.Add(node, useSiteInfo)

                    If (convKind And ConversionKind.Tuple) <> 0 Then
                        operand = MakeTupleConversion(node.Syntax, operand, unwrappedResultType, DirectCast(node.ExtendedInfoOpt, BoundConvertedTupleElements))

                    Else
                        operand = TransformRewrittenConversion(New BoundConversion(node.Syntax,
                                                    operand,
                                                    convKind,
                                                    node.Checked,
                                                    node.ExplicitCastInCode,
                                                    node.ConstantValueOpt,
                                                    node.ExtendedInfoOpt,
                                                    unwrappedResultType))
                    End If
                End If
            End If

            ' wrap if needed
            If resultType.IsNullableType Then
                operand = WrapInNullable(operand, resultType)

                ' propagate null from the operand
                If operandHasValue IsNot Nothing Then
                    operand = MakeTernaryConditionalExpression(node.Syntax,
                                                            operandHasValue,
                                                            operand,
                                                            NullableNull(operand.Syntax, resultType))

                    ' if used temps, arrange a sequence for temps and inits.
                    If temps IsNot Nothing Then
                        operand = New BoundSequence(operand.Syntax,
                                                   temps.ToImmutableAndFree,
                                                   inits.ToImmutableAndFree,
                                                   operand,
                                                   operand.Type)
                    End If
                End If
            End If

            Return operand
        End Function

        Private Function RewriteNullableReferenceConversion(node As BoundConversion,
                                           rewrittenOperand As BoundExpression) As BoundExpression

            Dim resultType = node.Type
            Dim operandType = rewrittenOperand.Type

            If operandType.IsStringType Then
                ' CType(string, T?) ---> new T?(CType(string, T))
                Dim innerTargetType = resultType.GetNullableUnderlyingType
                Dim useSiteInfo = GetNewCompoundUseSiteInfo()
                Dim convKind = Conversions.ClassifyConversion(operandType, innerTargetType, useSiteInfo).Key
                Debug.Assert(Conversions.ConversionExists(convKind))
                _diagnostics.Add(node, useSiteInfo)
                Return WrapInNullable(
                            TransformRewrittenConversion(
                                node.Update(rewrittenOperand,
                                            convKind,
                                            node.Checked,
                                            node.ExplicitCastInCode,
                                            node.ConstantValueOpt,
                                            node.ExtendedInfoOpt,
                                            resultType.GetNullableUnderlyingType)),
                                resultType)
            End If

            If resultType.IsStringType Then
                ' conversion to string is an intrinsic conversion and can be lifted.
                ' T? --> string   ==>   T.Value -- string
                ' note that nullable null does not convert to string null, i.e. this conversion
                ' is not null-propagating

                rewrittenOperand = NullableValue(rewrittenOperand)
                Dim useSiteInfo = GetNewCompoundUseSiteInfo()
                Dim convKind = Conversions.ClassifyDirectCastConversion(rewrittenOperand.Type, resultType, useSiteInfo)
                Debug.Assert(Conversions.ConversionExists(convKind))
                _diagnostics.Add(node, useSiteInfo)
                Return TransformRewrittenConversion(
                            node.Update(rewrittenOperand,
                                        node.ConversionKind And (Not ConversionKind.Nullable),
                                        node.Checked,
                                        node.ExplicitCastInCode,
                                        node.ConstantValueOpt,
                                        node.ExtendedInfoOpt,
                                        resultType))
            End If

            If operandType.IsNullableType Then
                ' T? --> RefType, this is a boxing conversion (DirectCast)
                If HasNoValue(rewrittenOperand) Then
                    ' DirectCast(Nothing, operatorType)
                    Return New BoundDirectCast(node.Syntax,
                                               MakeNullLiteral(rewrittenOperand.Syntax, resultType),
                                               ConversionKind.WideningNothingLiteral,
                                               resultType)
                End If

                If HasValue(rewrittenOperand) Then
                    ' DirectCast(operand.GetValueOrDefault, operatorType)
                    Dim unwrappedOperand = NullableValueOrDefault(rewrittenOperand)
                    Dim useSiteInfo = GetNewCompoundUseSiteInfo()
                    Dim convKind = Conversions.ClassifyDirectCastConversion(unwrappedOperand.Type, resultType, useSiteInfo)
                    Debug.Assert(Conversions.ConversionExists(convKind))
                    _diagnostics.Add(node, useSiteInfo)
                    Return New BoundDirectCast(node.Syntax,
                                               unwrappedOperand,
                                               convKind,
                                               resultType)
                End If
            End If

            If resultType.IsNullableType Then
                ' RefType --> T? , this is just an unboxing conversion.
                Dim useSiteInfo = GetNewCompoundUseSiteInfo()
                Dim convKind = Conversions.ClassifyDirectCastConversion(rewrittenOperand.Type, resultType, useSiteInfo)
                Debug.Assert(Conversions.ConversionExists(convKind))
                _diagnostics.Add(node, useSiteInfo)
                Return New BoundDirectCast(node.Syntax,
                                           rewrittenOperand,
                                           convKind,
                                           resultType)

            End If

            Throw ExceptionUtilities.Unreachable
        End Function

        Private Function RewriteNullableUserDefinedConversion(node As BoundUserDefinedConversion) As BoundNode
            ' User defined conversions rewrite as:
            '           Sequence(operandCapture, If(operandCapture.HasValue, Conversion(operandCapture), Null)
            '
            ' The structure of the nullable BoundUserDefinedConversion looks like this:
            '    [OPERAND] -> [IN-CONVERSION] -> [CALL] -> [OUT-CONVERSION]
            '
            '   In-conversion also does unwrapping.
            '   Out-conversion also does wrapping.
            '
            ' operand
            ' the thing that we will be converting. Must be nullable. We will be checking HasValue on it and will return null if it does not.
            Dim operand = node.Operand
            Debug.Assert(operand.Type.IsNullableType)

            ' inner conversion 
            Dim inConversion = node.InConversionOpt
            Debug.Assert(inConversion IsNot Nothing, "There is always an inner conversion.")

            ' operator 
            Dim operatorCall As BoundCall = node.Call

            ' outer conversion 
            Dim outConversion As BoundConversion = node.OutConversionOpt
            Debug.Assert(outConversion IsNot Nothing, "There is always an outer conversion.")

            ' result type 
            ' type that we need to return from the conversion. It must be a nullable type.
            Dim resultType As TypeSymbol = outConversion.Type
            Debug.Assert(resultType.IsNullableType, "lifted operator must have nullable type")

            ' === START REWRITE

            Dim rewrittenOperand = Me.VisitExpressionNode(operand)

            ' this is what we return when operand has no value
            Dim whenHasNoValue As BoundExpression = NullableNull(node.Syntax, resultType)

            '== TRIVIAL CASE
            If HasNoValue(rewrittenOperand) Then
                Return whenHasNoValue
            End If

            ' Do we know statically that operand has value?
            Dim operandHasValue As Boolean = HasValue(rewrittenOperand)

            ' This is what we will pass to the operator method if operand has value
            Dim inputToOperatorMethod As BoundExpression

            Dim condition As BoundExpression = Nothing
            Dim temp As SynthesizedLocal = Nothing

            If operandHasValue Then
                ' just pass the operand, no need to capture
                inputToOperatorMethod = rewrittenOperand

            Else
                ' operator input would be captured operand
                Dim tempInit As BoundExpression = Nothing

                ' no need to capture locals since we will not 
                ' evaluate anything between HasValue and ValueOrDefault
                Dim capturedleft As BoundExpression = CaptureNullableIfNeeded(rewrittenOperand,
                                                                              temp,
                                                                              tempInit,
                                                                              doNotCaptureLocals:=True)

                condition = NullableHasValue(If(tempInit, capturedleft))

                ' Note that we will be doing the conversion only when
                ' we know that we have a value, so we will pass to the conversion wrapped NullableValueOrDefault.
                ' so that it could use NullableValueOrDefault instead of Value.
                inputToOperatorMethod = WrapInNullable(NullableValueOrDefault(capturedleft), capturedleft.Type)
            End If

            ' inConversion is always a nullable conversion. We need to rewrite it.
            inputToOperatorMethod = RewriteNullableConversion(inConversion, inputToOperatorMethod)

            ' result of the conversion when operand has value. (replace the arg)
            Dim whenHasValue As BoundExpression = operatorCall.Update(operatorCall.Method,
                                                                      Nothing,
                                                                      operatorCall.ReceiverOpt,
                                                                      ImmutableArray.Create(inputToOperatorMethod),
                                                                      Nothing,
                                                                      operatorCall.ConstantValueOpt,
                                                                      isLValue:=operatorCall.IsLValue,
                                                                      suppressObjectClone:=operatorCall.SuppressObjectClone,
                                                                      type:=operatorCall.Type)

            ' outConversion is a nullable conversion. need to rewrite it.
            whenHasValue = RewriteNullableConversion(outConversion, whenHasValue)

            ' Now we have whenHasValue, whenHasNoValue and condition. The rest is easy.
            If operandHasValue Then
                Return whenHasValue

            Else
                ' == rewrite operand as ternary expression
                Dim result As BoundExpression = MakeTernaryConditionalExpression(node.Syntax, condition, whenHasValue, whenHasNoValue)

                ' if we used a temp, arrange a sequence for it
                If temp IsNot Nothing Then
                    result = New BoundSequence(node.Syntax,
                                               ImmutableArray.Create(Of LocalSymbol)(temp),
                                               ImmutableArray(Of BoundExpression).Empty,
                                               result,
                                               result.Type)
                End If

                Return result
            End If

        End Function

#Region "Post-rewrite conversion"

        Private Function TransformRewrittenConversion(rewrittenConversion As BoundConversion) As BoundExpression
            If rewrittenConversion.HasErrors OrElse _inExpressionLambda Then
                Return rewrittenConversion
            End If

            Dim result As BoundExpression = rewrittenConversion
            Dim underlyingTypeTo = rewrittenConversion.Type.GetEnumUnderlyingTypeOrSelf()
            Dim operand = rewrittenConversion.Operand

            If operand.IsNothingLiteral() Then
                Debug.Assert(rewrittenConversion.ConversionKind = ConversionKind.WideningNothingLiteral OrElse
                             (Conversions.IsIdentityConversion(rewrittenConversion.ConversionKind) AndAlso
                                Not underlyingTypeTo.IsTypeParameter() AndAlso underlyingTypeTo.IsReferenceType) OrElse
                             (rewrittenConversion.ConversionKind And (ConversionKind.Reference Or ConversionKind.Array)) <> 0)

                If underlyingTypeTo.IsTypeParameter() OrElse underlyingTypeTo.IsReferenceType Then
                    result = RewriteAsDirectCast(rewrittenConversion)
                Else
                    Debug.Assert(underlyingTypeTo.IsValueType)
                End If

            ElseIf operand.Kind = BoundKind.Lambda Then
                Return rewrittenConversion
            Else

                Dim underlyingTypeFrom = operand.Type.GetEnumUnderlyingTypeOrSelf()

                If underlyingTypeFrom.IsFloatingType() AndAlso underlyingTypeTo.IsIntegralType() Then
                    result = RewriteFloatingToIntegralConversion(rewrittenConversion, underlyingTypeFrom, underlyingTypeTo)

                ElseIf underlyingTypeFrom.IsDecimalType() AndAlso
                    (underlyingTypeTo.IsBooleanType() OrElse underlyingTypeTo.IsIntegralType() OrElse underlyingTypeTo.IsFloatingType) Then
                    result = RewriteDecimalToNumericOrBooleanConversion(rewrittenConversion, underlyingTypeFrom, underlyingTypeTo)

                ElseIf underlyingTypeTo.IsDecimalType() AndAlso
                    (underlyingTypeFrom.IsBooleanType() OrElse underlyingTypeFrom.IsIntegralType() OrElse underlyingTypeFrom.IsFloatingType) Then
                    result = RewriteNumericOrBooleanToDecimalConversion(rewrittenConversion, underlyingTypeFrom, underlyingTypeTo)

                ElseIf underlyingTypeFrom.IsNullableType OrElse underlyingTypeTo.IsNullableType Then
                    ' conversions between nullable and reference types are not directcasts, they are boxing/unboxing conversions.
                    ' CodeGen will handle this.

                ElseIf underlyingTypeFrom.IsObjectType() AndAlso
                    (underlyingTypeTo.IsTypeParameter() OrElse underlyingTypeTo.IsIntrinsicType()) Then
                    result = RewriteFromObjectConversion(rewrittenConversion, underlyingTypeFrom, underlyingTypeTo)

                ElseIf underlyingTypeFrom.IsTypeParameter() Then
                    result = RewriteAsDirectCast(rewrittenConversion)

                ElseIf underlyingTypeTo.IsTypeParameter() Then
                    result = RewriteAsDirectCast(rewrittenConversion)

                ElseIf underlyingTypeFrom.IsStringType() AndAlso
                     (underlyingTypeTo.IsCharSZArray() OrElse underlyingTypeTo.IsIntrinsicValueType()) Then
                    result = RewriteFromStringConversion(rewrittenConversion, underlyingTypeFrom, underlyingTypeTo)

                ElseIf underlyingTypeTo.IsStringType() AndAlso
                    (underlyingTypeFrom.IsCharSZArray() OrElse underlyingTypeFrom.IsIntrinsicValueType()) Then
                    result = RewriteToStringConversion(rewrittenConversion, underlyingTypeFrom, underlyingTypeTo)

                ElseIf underlyingTypeFrom.IsReferenceType AndAlso underlyingTypeTo.IsCharSZArray() Then
                    result = RewriteReferenceTypeToCharArrayRankOneConversion(rewrittenConversion, underlyingTypeFrom, underlyingTypeTo)

                ElseIf underlyingTypeTo.IsReferenceType Then
                    result = RewriteAsDirectCast(rewrittenConversion)

                ElseIf underlyingTypeFrom.IsReferenceType AndAlso underlyingTypeTo.IsIntrinsicValueType() Then
                    result = RewriteFromObjectConversion(rewrittenConversion, Compilation.GetSpecialType(SpecialType.System_Object), underlyingTypeTo)

                Else
                    Debug.Assert(underlyingTypeTo.IsValueType)
                End If
            End If

            Return result
        End Function

        Private Function RewriteReferenceTypeToCharArrayRankOneConversion(node As BoundConversion, typeFrom As TypeSymbol, typeTo As TypeSymbol) As BoundExpression
            Debug.Assert(typeFrom.IsReferenceType AndAlso typeTo.IsCharSZArray())

            Dim result As BoundExpression = node
            Const member As WellKnownMember = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToCharArrayRankOneObject

            Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then

                Dim operand = node.Operand

                Debug.Assert(memberSymbol.Parameters(0).Type.IsObjectType())

                If Not operand.Type.IsObjectType() Then
                    Dim objectType As TypeSymbol = memberSymbol.Parameters(0).Type
                    Dim useSiteInfo = GetNewCompoundUseSiteInfo()
                    operand = New BoundDirectCast(operand.Syntax,
                                                  operand,
                                                  Conversions.ClassifyDirectCastConversion(operand.Type, objectType, useSiteInfo),
                                                  objectType)
                    _diagnostics.Add(node, useSiteInfo)
                End If

                result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                       ImmutableArray.Create(operand), Nothing, memberSymbol.ReturnType)

                Debug.Assert(memberSymbol.ReturnType.IsSameTypeIgnoringAll(node.Type))
            End If

            Return result
        End Function

        Private Shared Function RewriteAsDirectCast(node As BoundConversion) As BoundExpression
            Debug.Assert(node.Operand.IsNothingLiteral() OrElse
                         (node.ConversionKind And (Not ConversionKind.DelegateRelaxationLevelMask)) =
                            Conversions.ClassifyDirectCastConversion(node.Operand.Type, node.Type, CompoundUseSiteInfo(Of AssemblySymbol).Discarded))

            ' TODO: A chain of widening reference conversions that starts from NOTHING literal can be collapsed to a single node.
            '       Semantics::Convert does this in Dev10.
            '       It looks like we already achieve the same result due to folding of NOTHING conversions.

            Return New BoundDirectCast(node.Syntax, node.Operand, node.ConversionKind, node.Type, Nothing)
        End Function

        Private Function RewriteFromObjectConversion(node As BoundConversion, typeFrom As TypeSymbol, underlyingTypeTo As TypeSymbol) As BoundExpression
            Debug.Assert(typeFrom.IsObjectType())

            Dim result As BoundExpression = node
            Dim member As WellKnownMember = WellKnownMember.Count

            Select Case underlyingTypeTo.SpecialType
                Case SpecialType.System_Boolean : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToBooleanObject
                Case SpecialType.System_SByte : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToSByteObject
                Case SpecialType.System_Byte : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToByteObject
                Case SpecialType.System_Int16 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToShortObject
                Case SpecialType.System_UInt16 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToUShortObject
                Case SpecialType.System_Int32 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToIntegerObject
                Case SpecialType.System_UInt32 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToUIntegerObject
                Case SpecialType.System_Int64 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToLongObject
                Case SpecialType.System_UInt64 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToULongObject
                Case SpecialType.System_Single : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToSingleObject
                Case SpecialType.System_Double : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDoubleObject
                Case SpecialType.System_Decimal : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalObject
                Case SpecialType.System_DateTime : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDateObject
                Case SpecialType.System_Char : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToCharObject
                Case SpecialType.System_String : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringObject

                Case Else
                    If underlyingTypeTo.IsTypeParameter() Then
                        member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToGenericParameter_T_Object
                    End If
            End Select

            If member <> WellKnownMember.Count Then

                Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

                If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then

                    If member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToGenericParameter_T_Object Then
                        memberSymbol = memberSymbol.Construct(underlyingTypeTo)
                    End If

                    Dim operand = node.Operand

                    If Not operand.Type.IsObjectType() Then
                        Debug.Assert(typeFrom.IsObjectType())
                        Debug.Assert(operand.Type.IsReferenceType)
                        Debug.Assert(underlyingTypeTo.IsIntrinsicValueType())

                        Dim useSiteInfo = GetNewCompoundUseSiteInfo()
                        operand = New BoundDirectCast(operand.Syntax,
                                                      operand,
                                                      Conversions.ClassifyDirectCastConversion(operand.Type, typeFrom, useSiteInfo),
                                                      typeFrom)
                        _diagnostics.Add(node, useSiteInfo)
                    End If

                    Debug.Assert(memberSymbol.ReturnType.IsSameTypeIgnoringAll(underlyingTypeTo))
                    Debug.Assert(memberSymbol.Parameters(0).Type Is typeFrom)

                    result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                           ImmutableArray.Create(operand), Nothing, memberSymbol.ReturnType)

                    Dim targetResultType = node.Type

                    If Not targetResultType.IsSameTypeIgnoringAll(memberSymbol.ReturnType) Then
                        ' Must be conversion to an enum
                        Debug.Assert(targetResultType.IsEnumType())

                        Dim conv = ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions
                        Debug.Assert(conv = Conversions.ClassifyConversion(memberSymbol.ReturnType, targetResultType, CompoundUseSiteInfo(Of AssemblySymbol).Discarded).Key)

                        result = New BoundConversion(node.Syntax, DirectCast(result, BoundExpression),
                                                     conv, node.Checked, node.ExplicitCastInCode, targetResultType, Nothing)
                    End If
                End If
            End If

            Return result
        End Function

        Private Function RewriteToStringConversion(node As BoundConversion, underlyingTypeFrom As TypeSymbol, typeTo As TypeSymbol) As BoundExpression
            Debug.Assert(typeTo.IsStringType())

            Dim result As BoundExpression = node
            Dim memberSymbol As MethodSymbol = Nothing

            If underlyingTypeFrom.IsCharSZArray() Then
                Const memberId As SpecialMember = SpecialMember.System_String__CtorSZArrayChar
                memberSymbol = DirectCast(ContainingAssembly.GetSpecialTypeMember(memberId), MethodSymbol)

                If ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                    memberSymbol = Nothing
                End If

            Else
                Dim member As WellKnownMember = WellKnownMember.Count

                ' Note, conversion from Object is handled by RewriteFromObjectConversion.
                Select Case underlyingTypeFrom.SpecialType
                    Case SpecialType.System_Boolean : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringBoolean
                    Case SpecialType.System_SByte,
                         SpecialType.System_Int16,
                         SpecialType.System_Int32 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringInt32

                    Case SpecialType.System_Byte : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringByte

                    Case SpecialType.System_UInt16,
                         SpecialType.System_UInt32 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringUInt32

                    Case SpecialType.System_Int64 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringInt64
                    Case SpecialType.System_UInt64 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringUInt64
                    Case SpecialType.System_Single : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringSingle
                    Case SpecialType.System_Double : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDouble
                    Case SpecialType.System_Decimal : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDecimal
                    Case SpecialType.System_DateTime : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDateTime
                    Case SpecialType.System_Char : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringChar
                End Select

                If member <> WellKnownMember.Count Then

                    memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

                    If ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                        memberSymbol = Nothing
                    End If
                End If
            End If

            If memberSymbol IsNot Nothing Then

                Dim operand = node.Operand
                Dim operandType = operand.Type

                If Not operandType.IsSameTypeIgnoringAll(memberSymbol.Parameters(0).Type) Then
                    Dim conv As ConversionKind

                    If operandType.IsEnumType() Then
                        conv = ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions
                    Else
                        conv = ConversionKind.WideningNumeric
                    End If

                    Debug.Assert(conv = Conversions.ClassifyConversion(operandType, memberSymbol.Parameters(0).Type, CompoundUseSiteInfo(Of AssemblySymbol).Discarded).Key)

                    operand = New BoundConversion(node.Syntax, operand, conv, node.Checked, node.ExplicitCastInCode,
                                                  memberSymbol.Parameters(0).Type, Nothing)
                End If

                If memberSymbol.MethodKind = MethodKind.Constructor Then
                    Debug.Assert(memberSymbol.ContainingType Is typeTo)

                    result = New BoundObjectCreationExpression(
                        node.Syntax,
                        memberSymbol,
                        ImmutableArray.Create(operand),
                        Nothing,
                        typeTo)
                Else
                    Debug.Assert(memberSymbol.ReturnType Is typeTo)
                    result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                           ImmutableArray.Create(operand), Nothing, memberSymbol.ReturnType)
                End If
            End If

            Return result
        End Function

        Private Function RewriteFromStringConversion(node As BoundConversion, typeFrom As TypeSymbol, underlyingTypeTo As TypeSymbol) As BoundExpression
            Debug.Assert(typeFrom.IsStringType())

            Dim result As BoundExpression = node
            Dim member As WellKnownMember = WellKnownMember.Count

            Select Case underlyingTypeTo.SpecialType
                Case SpecialType.System_Boolean : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToBooleanString
                Case SpecialType.System_SByte : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToSByteString
                Case SpecialType.System_Byte : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToByteString
                Case SpecialType.System_Int16 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToShortString
                Case SpecialType.System_UInt16 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToUShortString
                Case SpecialType.System_Int32 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToIntegerString
                Case SpecialType.System_UInt32 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToUIntegerString
                Case SpecialType.System_Int64 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToLongString
                Case SpecialType.System_UInt64 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToULongString
                Case SpecialType.System_Single : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToSingleString
                Case SpecialType.System_Double : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDoubleString
                Case SpecialType.System_Decimal : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalString
                Case SpecialType.System_DateTime : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDateString
                Case SpecialType.System_Char : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToCharString
                Case Else
                    If underlyingTypeTo.IsCharSZArray() Then
                        member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToCharArrayRankOneString
                    End If
            End Select

            If member <> WellKnownMember.Count Then

                Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

                If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                    Dim operand = node.Operand

                    Debug.Assert(memberSymbol.ReturnType.IsSameTypeIgnoringAll(underlyingTypeTo))
                    Debug.Assert(memberSymbol.Parameters(0).Type Is typeFrom)

                    result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                           ImmutableArray.Create(operand), Nothing, memberSymbol.ReturnType)

                    Dim targetResultType = node.Type

                    If Not targetResultType.IsSameTypeIgnoringAll(memberSymbol.ReturnType) Then
                        ' Must be conversion to an enum
                        Debug.Assert(targetResultType.IsEnumType())
                        Dim conv = ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions

                        Debug.Assert(conv = Conversions.ClassifyConversion(memberSymbol.ReturnType, targetResultType, CompoundUseSiteInfo(Of AssemblySymbol).Discarded).Key)

                        result = New BoundConversion(node.Syntax, DirectCast(result, BoundExpression),
                                                     conv, node.Checked, node.ExplicitCastInCode, targetResultType, Nothing)
                    End If
                End If
            End If

            Return result
        End Function

        Private Function RewriteNumericOrBooleanToDecimalConversion(node As BoundConversion, underlyingTypeFrom As TypeSymbol, typeTo As TypeSymbol) As BoundExpression
            Debug.Assert(typeTo.IsDecimalType() AndAlso
                (underlyingTypeFrom.IsBooleanType() OrElse underlyingTypeFrom.IsIntegralType() OrElse underlyingTypeFrom.IsFloatingType))

            Dim result As BoundExpression = node
            Dim memberSymbol As MethodSymbol

            If underlyingTypeFrom.IsBooleanType() Then
                Const memberId As WellKnownMember = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalBoolean
                memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(memberId), MethodSymbol)

                If ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                    memberSymbol = Nothing
                End If
            Else
                Dim member As SpecialMember

                Select Case underlyingTypeFrom.SpecialType
                    Case SpecialType.System_SByte,
                         SpecialType.System_Byte,
                         SpecialType.System_Int16,
                         SpecialType.System_UInt16,
                         SpecialType.System_Int32 : member = SpecialMember.System_Decimal__CtorInt32
                    Case SpecialType.System_UInt32 : member = SpecialMember.System_Decimal__CtorUInt32
                    Case SpecialType.System_Int64 : member = SpecialMember.System_Decimal__CtorInt64
                    Case SpecialType.System_UInt64 : member = SpecialMember.System_Decimal__CtorUInt64
                    Case SpecialType.System_Single : member = SpecialMember.System_Decimal__CtorSingle
                    Case SpecialType.System_Double : member = SpecialMember.System_Decimal__CtorDouble
                    Case Else
                        'cannot get here
                        Return result
                End Select

                memberSymbol = DirectCast(ContainingAssembly.GetSpecialTypeMember(member), MethodSymbol)

                If ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                    memberSymbol = Nothing
                End If
            End If

            ' Call the method.

            If memberSymbol IsNot Nothing Then

                Dim operand = node.Operand
                Dim operandType = operand.Type

                If operandType IsNot memberSymbol.Parameters(0).Type Then
                    Dim conv As ConversionKind

                    If operandType.IsEnumType() Then
                        conv = ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions
                    Else
                        conv = ConversionKind.WideningNumeric
                    End If

                    Debug.Assert(conv = Conversions.ClassifyConversion(operandType, memberSymbol.Parameters(0).Type, CompoundUseSiteInfo(Of AssemblySymbol).Discarded).Key)

                    operand = New BoundConversion(node.Syntax, operand, conv, node.Checked, node.ExplicitCastInCode,
                                                  memberSymbol.Parameters(0).Type, Nothing)
                End If

                If memberSymbol.MethodKind = MethodKind.Constructor Then
                    Debug.Assert(memberSymbol.ContainingType Is typeTo)

                    result = New BoundObjectCreationExpression(
                        node.Syntax,
                        memberSymbol,
                        ImmutableArray.Create(operand),
                        Nothing,
                        typeTo)
                Else
                    Debug.Assert(memberSymbol.ReturnType Is typeTo)
                    result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                           ImmutableArray.Create(operand), Nothing, memberSymbol.ReturnType)
                End If
            End If

            Return result
        End Function

        Private Function RewriteDecimalToNumericOrBooleanConversion(node As BoundConversion, typeFrom As TypeSymbol, underlyingTypeTo As TypeSymbol) As BoundExpression
            Debug.Assert(typeFrom.IsDecimalType() AndAlso
                (underlyingTypeTo.IsBooleanType() OrElse underlyingTypeTo.IsIntegralType() OrElse underlyingTypeTo.IsFloatingType))

            Dim result As BoundExpression = node
            Dim member As WellKnownMember

            Select Case underlyingTypeTo.SpecialType
                Case SpecialType.System_Boolean : member = WellKnownMember.System_Convert__ToBooleanDecimal
                Case SpecialType.System_SByte : member = WellKnownMember.System_Convert__ToSByteDecimal
                Case SpecialType.System_Byte : member = WellKnownMember.System_Convert__ToByteDecimal
                Case SpecialType.System_Int16 : member = WellKnownMember.System_Convert__ToInt16Decimal
                Case SpecialType.System_UInt16 : member = WellKnownMember.System_Convert__ToUInt16Decimal
                Case SpecialType.System_Int32 : member = WellKnownMember.System_Convert__ToInt32Decimal
                Case SpecialType.System_UInt32 : member = WellKnownMember.System_Convert__ToUInt32Decimal
                Case SpecialType.System_Int64 : member = WellKnownMember.System_Convert__ToInt64Decimal
                Case SpecialType.System_UInt64 : member = WellKnownMember.System_Convert__ToUInt64Decimal
                Case SpecialType.System_Single : member = WellKnownMember.System_Convert__ToSingleDecimal
                Case SpecialType.System_Double : member = WellKnownMember.System_Convert__ToDoubleDecimal
                Case Else
                    'cannot get here
                    Return result
            End Select

            Dim memberSymbol As MethodSymbol
            ' Call the method.

            memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                Dim operand = node.Operand

                Debug.Assert(memberSymbol.ReturnType Is underlyingTypeTo)
                Debug.Assert(memberSymbol.Parameters(0).Type Is typeFrom)

                result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                       ImmutableArray.Create(operand), Nothing, memberSymbol.ReturnType)

                Dim targetResultType = node.Type

                If targetResultType IsNot memberSymbol.ReturnType Then
                    ' Must be conversion to an enum
                    Debug.Assert(targetResultType.IsEnumType())
                    Dim conv = ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions

                    Debug.Assert(conv = Conversions.ClassifyConversion(memberSymbol.ReturnType, targetResultType, CompoundUseSiteInfo(Of AssemblySymbol).Discarded).Key)

                    result = New BoundConversion(node.Syntax, DirectCast(result, BoundExpression),
                                                 conv, node.Checked, node.ExplicitCastInCode, targetResultType, Nothing)
                End If
            End If

            Return result
        End Function

        Private Function RewriteFloatingToIntegralConversion(node As BoundConversion, typeFrom As TypeSymbol, underlyingTypeTo As TypeSymbol) As BoundExpression
            Debug.Assert(typeFrom.IsFloatingType() AndAlso underlyingTypeTo.IsIntegralType())
            Debug.Assert(Not _inExpressionLambda)
            Dim result As BoundExpression = node
            Dim operand = node.Operand

            If operand.Kind = BoundKind.Call Then
                Dim callOperand = DirectCast(operand, BoundCall)
                If IsFloatingTruncation(callOperand) Then
                    ' CInt(Fix(number)) and the like can be simplified to just truncate the number to the integral type
                    Return New BoundConversion(node.Syntax, callOperand.Arguments(0), node.ConversionKind, node.Checked, node.ExplicitCastInCode, node.Type)
                ElseIf ReturnsWholeNumberDouble(callOperand) Then
                    ' CInt(Math.Floor(number)) and the like can omit rounding the result of Floor, which is already a whole number
                    Return node
                End If
            End If

            ' Call Math.Round method to enforce VB style rounding.
            Const memberId As WellKnownMember = WellKnownMember.System_Math__RoundDouble
            Dim mathRound As MethodSymbol = DirectCast(Compilation.GetWellKnownTypeMember(memberId), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, memberId, mathRound) Then
                ' If we got here and passed badness check, it should be safe to assume that we have 
                ' a "good" symbol for Double type

                If typeFrom IsNot mathRound.Parameters(0).Type Then
                    ' Converting from Single
                    Debug.Assert(ConversionKind.WideningNumeric = Conversions.ClassifyConversion(typeFrom, mathRound.Parameters(0).Type, CompoundUseSiteInfo(Of AssemblySymbol).Discarded).Key)

                    operand = New BoundConversion(node.Syntax, operand, ConversionKind.WideningNumeric, node.Checked, node.ExplicitCastInCode,
                                                  mathRound.Parameters(0).Type, Nothing)
                End If

                Dim callMathRound = New BoundCall(node.Syntax, mathRound, Nothing, Nothing,
                                                  ImmutableArray.Create(operand), Nothing, mathRound.ReturnType)

                Debug.Assert(node.ConversionKind = Conversions.ClassifyConversion(mathRound.ReturnType, node.Type, CompoundUseSiteInfo(Of AssemblySymbol).Discarded).Key)

                result = New BoundConversion(node.Syntax, callMathRound, node.ConversionKind,
                                             node.Checked, node.ExplicitCastInCode, node.Type, Nothing)
            End If

            Return result
        End Function

        ''' <summary>
        ''' Is this a floating-point operation that results in a whole number, rendering a following rounding operation redundant?
        ''' </summary>
        Private Function ReturnsWholeNumberDouble(node As BoundCall) As Boolean
            Dim methodName As String = node.Method.Name
            If "Ceiling".Equals(methodName) Then
                Return node.Method = Me.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Math__CeilingDouble)
            ElseIf "Floor".Equals(methodName) Then
                Return node.Method = Me.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Math__FloorDouble)
            ElseIf "Round".Equals(methodName) Then
                Return node.Method = Me.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Math__RoundDouble)
            ElseIf "Int".Equals(methodName) Then
                Select Case node.Type.SpecialType
                    Case SpecialType.System_Single
                        Return node.Method = Me.Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Conversion__IntSingle)
                    Case SpecialType.System_Double
                        Return node.Method = Me.Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Conversion__IntDouble)
                    Case Else
                        Return False
                End Select
            End If

            Return False
        End Function

        ''' <summary>
        ''' Is this a floating-point truncation operation that would be redundant if followed by a truncation to an integral type?
        ''' </summary>
        Private Function IsFloatingTruncation(node As BoundCall) As Boolean
            Dim methodName As String = node.Method.Name
            If "Fix".Equals(methodName) Then
                Select Case node.Type.SpecialType
                    Case SpecialType.System_Single
                        Return node.Method = Me.Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Conversion__FixSingle)
                    Case SpecialType.System_Double
                        Return node.Method = Me.Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Conversion__FixDouble)
                    Case Else
                        Return False
                End Select
            ElseIf "Truncate".Equals(methodName) Then
                Return node.Method = Me.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Math__TruncateDouble)
            End If

            Return False
        End Function

#End Region

#Region "DirectCast"

        Public Overrides Function VisitDirectCast(node As BoundDirectCast) As BoundNode
            If Not _inExpressionLambda AndAlso Conversions.IsIdentityConversion(node.ConversionKind) Then
                Return VisitExpressionNode(node.Operand)
            End If

            ' Set "inExpressionLambda" if we're converting lambda to expression tree.
            Dim returnValue As BoundNode
            Dim wasInExpressionlambda As Boolean = _inExpressionLambda
            If (node.ConversionKind And (ConversionKind.Lambda Or ConversionKind.ConvertedToExpressionTree)) = (ConversionKind.Lambda Or ConversionKind.ConvertedToExpressionTree) Then
                _inExpressionLambda = True
            End If

            If node.RelaxationLambdaOpt Is Nothing Then
                returnValue = MyBase.VisitDirectCast(node)
            Else
                returnValue = RewriteLambdaRelaxationConversion(node)
            End If

            _inExpressionLambda = wasInExpressionlambda
            Return returnValue
        End Function

#End Region

#Region "TryCast"

        Public Overrides Function VisitTryCast(node As BoundTryCast) As BoundNode
            If Not _inExpressionLambda AndAlso Conversions.IsIdentityConversion(node.ConversionKind) Then
                Return Visit(node.Operand)
            End If

            ' Set "inExpressionLambda" if we're converting lambda to expression tree.
            Dim returnValue As BoundNode
            Dim wasInExpressionlambda As Boolean = _inExpressionLambda
            If (node.ConversionKind And (ConversionKind.Lambda Or ConversionKind.ConvertedToExpressionTree)) = (ConversionKind.Lambda Or ConversionKind.ConvertedToExpressionTree) Then
                _inExpressionLambda = True
            End If

            If node.RelaxationLambdaOpt Is Nothing Then
                returnValue = Nothing

                If Conversions.IsWideningConversion(node.ConversionKind) AndAlso
                    Not Conversions.IsIdentityConversion(node.ConversionKind) Then

                    Dim operand As BoundExpression = node.Operand
                    If operand.Kind <> BoundKind.Lambda Then
                        Dim typeFrom As TypeSymbol = operand.Type
                        Dim typeTo As TypeSymbol = node.Type

                        If (Not typeTo.IsTypeParameter()) AndAlso typeTo.IsReferenceType AndAlso
                           (Not typeFrom.IsTypeParameter()) AndAlso typeFrom.IsReferenceType Then

                            Debug.Assert(node.ConversionKind = Conversions.ClassifyDirectCastConversion(operand.Type, node.Type, CompoundUseSiteInfo(Of AssemblySymbol).Discarded))
                            returnValue = New BoundDirectCast(node.Syntax, DirectCast(Visit(operand), BoundExpression), node.ConversionKind, typeTo, Nothing)
                        End If
                    End If
                End If

                If returnValue Is Nothing Then
                    returnValue = MyBase.VisitTryCast(node)
                End If

            Else
                returnValue = RewriteLambdaRelaxationConversion(node)
            End If

            _inExpressionLambda = wasInExpressionlambda
            Return returnValue
        End Function

#End Region

    End Class
End Namespace

