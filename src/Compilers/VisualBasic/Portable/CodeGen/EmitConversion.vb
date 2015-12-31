' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports PrimitiveTypeCode = Microsoft.Cci.PrimitiveTypeCode

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGen

    Friend Partial Class CodeGenerator

        Private Shared Function IsSimpleType(type As PrimitiveTypeCode) As Boolean
            Dim result = False

            Select Case type
                Case PrimitiveTypeCode.Boolean,
                     PrimitiveTypeCode.Float32,
                     PrimitiveTypeCode.Float64,
                     PrimitiveTypeCode.Int16,
                     PrimitiveTypeCode.Int32,
                     PrimitiveTypeCode.Int64,
                     PrimitiveTypeCode.Int8,
                     PrimitiveTypeCode.UInt16,
                     PrimitiveTypeCode.UInt32,
                     PrimitiveTypeCode.UInt64,
                     PrimitiveTypeCode.UInt8

                    result = True
            End Select

            Return result
        End Function

        Private Sub EmitConvertIntrinsic(conversion As BoundConversion, underlyingFrom As PrimitiveTypeCode, underlyingTo As PrimitiveTypeCode)

            Debug.Assert(underlyingFrom = conversion.Operand.Type.GetEnumUnderlyingTypeOrSelf().PrimitiveTypeCode)
            Debug.Assert(underlyingTo = conversion.Type.GetEnumUnderlyingTypeOrSelf().PrimitiveTypeCode)
            Debug.Assert((IsSimpleType(underlyingFrom) AndAlso IsSimpleType(underlyingTo)) OrElse (underlyingFrom = PrimitiveTypeCode.Char AndAlso underlyingTo = PrimitiveTypeCode.Int32))

            ' Generate the expression to convert.
            EmitExpression(conversion.Operand, True)

            ' For "identity conversions" from float to float or double to double,
            ' we require the generation of conv.r4 or conv.r8, if the conversion
            ' was explicitly written. The runtime can use these instructions to 
            ' truncate precision, and compiler generates them. 
            If underlyingFrom = underlyingTo AndAlso
               Not conversion.ExplicitCastInCode AndAlso
               underlyingFrom <> PrimitiveTypeCode.Float32 AndAlso
               underlyingFrom <> PrimitiveTypeCode.Float64 Then
                Return
            End If

            ' Handle conversions to Boolean
            If underlyingTo = PrimitiveTypeCode.Boolean Then
                ' Emit Zero
                _builder.EmitConstantValue(ConstantValue.Default(underlyingFrom.GetConstantValueTypeDiscriminator()))

                ' using cgt.un is optimal, but doesn't work in the case of floating point values
                If underlyingFrom.IsFloatingPoint() Then
                    _builder.EmitOpCode(ILOpCode.Ceq)
                    _builder.EmitOpCode(ILOpCode.Ldc_i4_0)
                    _builder.EmitOpCode(ILOpCode.Ceq)
                Else
                    _builder.EmitOpCode(ILOpCode.Cgt_un)
                End If

                Return
            End If

            ' Handle conversions from boolean
            If underlyingFrom = PrimitiveTypeCode.Boolean Then
                ' First, normalize to -1
                _builder.EmitOpCode(ILOpCode.Ldc_i4_0)
                _builder.EmitOpCode(ILOpCode.Cgt_un)
                _builder.EmitOpCode(ILOpCode.Neg)

                If underlyingTo <> PrimitiveTypeCode.Int32 Then
                    ' Convert to the target type, but don't do overflow checking.  This results in unsigned types
                    ' getting their max value.
                    _builder.EmitNumericConversion(PrimitiveTypeCode.Int32, underlyingTo, checked:=False)
                End If

                Return
            End If

            ' Handle conversion between simple numeric types

            If underlyingFrom = PrimitiveTypeCode.Float32 AndAlso underlyingTo.IsIntegral() Then
                ' If converting from an intermediate value, we need to guarantee that
                ' the intermediate value keeps the precision of its type.  The JIT will try to
                ' promote the precision of intermediate values if it can, and this can lead to
                ' incorrect results (VS#241243).
                Select Case conversion.Operand.Kind
                    Case BoundKind.BinaryOperator

                        Select Case (DirectCast(conversion.Operand, BoundBinaryOperator).OperatorKind And BinaryOperatorKind.OpMask)
                            Case BinaryOperatorKind.Add,
                                 BinaryOperatorKind.Subtract,
                                 BinaryOperatorKind.Multiply,
                                 BinaryOperatorKind.Divide,
                                 BinaryOperatorKind.Modulo,
                                 BinaryOperatorKind.Power

                                _builder.EmitOpCode(ILOpCode.Conv_r4)
                        End Select

                    Case BoundKind.UnaryOperator
                        Select Case (DirectCast(conversion.Operand, BoundUnaryOperator).OperatorKind And UnaryOperatorKind.IntrinsicOpMask)
                            Case UnaryOperatorKind.Minus,
                                 UnaryOperatorKind.Plus

                                _builder.EmitOpCode(ILOpCode.Conv_r4)
                        End Select
                End Select

                ' no intermediate value in other cases, so no need for the forced convert
            End If

            EmitConvertSimpleNumeric(conversion, underlyingFrom, underlyingTo, conversion.Checked)
        End Sub

        Private Sub EmitConvertSimpleNumeric(conversion As BoundConversion, typeFrom As PrimitiveTypeCode, typeTo As PrimitiveTypeCode, checked As Boolean)
            Debug.Assert(typeFrom.IsIntegral() OrElse typeFrom.IsFloatingPoint() OrElse typeFrom = PrimitiveTypeCode.Char)
            Debug.Assert(typeTo.IsIntegral() OrElse typeTo.IsFloatingPoint())

            Debug.Assert(Not (typeFrom.IsFloatingPoint() AndAlso typeTo.IsIntegral() AndAlso
                              Not (conversion.Operand.Kind = BoundKind.Call AndAlso
                                   DirectCast(conversion.Operand, BoundCall).Method.Equals(
                                       Me._module.SourceModule.ContainingSourceAssembly.DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Math__RoundDouble)))),
                         "About to ignore VB rules for rounding float numbers.")

            _builder.EmitNumericConversion(typeFrom, typeTo, checked)
        End Sub

        Private Sub EmitConversionExpression(conversion As BoundConversion, used As Boolean)
            If Not used AndAlso Not ConversionHasSideEffects(conversion) Then
                EmitExpression(conversion.Operand, False)
                Return
            End If

            Dim typeTo = conversion.Type

            If conversion.Operand.IsNothingLiteral Then
                Debug.Assert(typeTo.IsValueType OrElse typeTo.IsTypeParameter)

                If used Then
                    'TODO: used
                    EmitLoadDefaultValueOfTypeFromNothingLiteral(typeTo, used:=True, syntaxNode:=conversion.Syntax)
                End If
            Else
                Dim underlyingTo = typeTo.GetEnumUnderlyingTypeOrSelf().PrimitiveTypeCode
                Dim typeFrom = conversion.Operand.Type
                Dim underlyingFrom = typeFrom.GetEnumUnderlyingTypeOrSelf().PrimitiveTypeCode

                If (IsSimpleType(underlyingFrom) AndAlso IsSimpleType(underlyingTo)) OrElse
                   (underlyingFrom = PrimitiveTypeCode.Char AndAlso underlyingTo = PrimitiveTypeCode.Int32) Then ' Allow AscW optimization.
                    EmitConvertIntrinsic(conversion, underlyingFrom, underlyingTo)

                ElseIf typeFrom.IsNullableType Then
                    Debug.Assert(typeTo.IsReferenceType)

                    If (conversion.ConversionKind And ConversionKind.Narrowing) <> 0 Then
                        EmitExpression(conversion.Operand, True)
                        EmitBox(typeFrom, conversion.Operand.Syntax)
                        _builder.EmitOpCode(ILOpCode.Castclass)
                        EmitSymbolToken(typeTo, conversion.Syntax)

                    Else
                        ' boxing itself is CLR-widening, so no need to emit unused boxing
                        EmitExpression(conversion.Operand, used)
                        If used Then
                            EmitBox(typeFrom, conversion.Operand.Syntax)
                        End If

                    End If

                ElseIf typeTo.IsNullableType Then
                    Debug.Assert(typeFrom.IsReferenceType)

                    ' unboxing is CLR-narrowing and may deterministically throw
                    EmitExpression(conversion.Operand, True)
                    EmitUnboxAny(typeTo, conversion.Syntax)

                Else
                    EmitExpression(conversion.Operand, True)

                    If Not Conversions.IsIdentityConversion(conversion.ConversionKind) Then

                        Debug.Assert(Not typeFrom.IsTypeParameter() AndAlso Not typeTo.IsTypeParameter() AndAlso
                                              typeFrom.IsReferenceType AndAlso typeTo.IsValueType)

                        Debug.Assert(typeFrom.SpecialType = SpecialType.System_Object OrElse
                                     typeFrom.SpecialType = SpecialType.System_ValueType OrElse
                                     typeFrom.SpecialType = SpecialType.System_Enum OrElse
                                     typeFrom.IsInterfaceType)

                        ' Conversions from references types to structures should be equivalent to
                        ' conversions from Nothing if the reference is Nothing.  Perform the check
                        ' and do the conversion.

                        Dim unboxLabel = New GeneratedLabelSymbol("unbox")
                        Dim resultLabel = New GeneratedLabelSymbol("result")

                        ' If the Reference is not nothing, branch directly to the Unbox
                        _builder.EmitOpCode(ILOpCode.Dup)
                        _builder.EmitBranch(ILOpCode.Brtrue_s, unboxLabel)

                        ' The reference is nothing, so we need to load the "Nothing" value for the
                        ' struct we're converting to.
                        ' 
                        ' But first Pop off the Null reference cause we don't need it anymore.
                        _builder.EmitOpCode(ILOpCode.Pop)

                        'TODO: used
                        EmitLoadDefaultValueOfTypeFromConstructorCall(conversion.ConstructorOpt, used:=True, syntaxNode:=conversion.Syntax)
                        _builder.EmitBranch(ILOpCode.Br_s, resultLabel)

                        _builder.MarkLabel(unboxLabel)

                        ' Unbox the reference to get the struct from inside the object.
                        _builder.EmitOpCode(ILOpCode.Unbox_any)
                        EmitSymbolToken(typeTo, conversion.Syntax)

                        _builder.MarkLabel(resultLabel)
                    End If
                End If

                EmitPopIfUnused(used)
            End If

        End Sub

        Private Function IsUnboxingDirectCast(conversion As BoundDirectCast) As Boolean
            Dim typeTo As TypeSymbol = conversion.Type
            Dim typeFrom As TypeSymbol = conversion.Operand.Type

            Return Not conversion.Operand.IsNothingLiteral AndAlso
                   Not Conversions.IsIdentityConversion(conversion.ConversionKind) AndAlso
                   Not typeFrom.GetEnumUnderlyingTypeOrSelf().IsSameTypeIgnoringCustomModifiers(typeTo.GetEnumUnderlyingTypeOrSelf()) AndAlso
                   Not typeFrom.IsTypeParameter() AndAlso
                   Not typeFrom.IsValueType AndAlso
                   Not typeTo.IsReferenceType
        End Function

        Private Sub EmitDirectCastExpression(conversion As BoundDirectCast, used As Boolean)
            If Not used AndAlso Not ConversionHasSideEffects(conversion) Then
                EmitExpression(conversion.Operand, False)
                Return
            End If

            'TODO: Dev10 CodeGenerator::GenerateDirectCast does some optimization for
            '      a case when conversion to Boolean is applied to comparison operator.
            '      Do we need to do something special about this case too?

            If conversion.Operand.IsNothingLiteral Then

                If conversion.Type.IsTypeParameter() Then
                    EmitLoadDefaultValueOfTypeParameter(conversion.Type, used, conversion.Syntax)
                    Return
                Else
                    EmitExpression(conversion.Operand, True)

                    If conversion.Type.IsValueType Then
                        ' unbox the return value to get the struct from inside the object
                        EmitUnboxAny(conversion.Type, conversion.Syntax)
                    Else
                        Debug.Assert(conversion.Type.IsReferenceType)
                    End If
                End If
            Else

                EmitExpression(conversion.Operand, True)

                If Not Conversions.IsIdentityConversion(conversion.ConversionKind) Then
                    Dim typeTo = conversion.Type
                    Dim typeFrom = conversion.Operand.Type

                    If typeFrom.GetEnumUnderlyingTypeOrSelf().IsSameTypeIgnoringCustomModifiers(typeTo.GetEnumUnderlyingTypeOrSelf()) Then
                        ' Do nothing, it is the same as identity.
                    ElseIf typeFrom.IsTypeParameter() Then
                        ' For any conversion from a generic parameter to any other type,
                        ' box the operand and then allow the explicit cast from Object to
                        ' the target type. This is a clr requirement.

                        EmitBox(typeFrom, conversion.Operand.Syntax)

                        If typeTo.SpecialType <> SpecialType.System_Object Then
                            If typeTo.IsTypeParameter() Then
                                _builder.EmitOpCode(ILOpCode.Unbox_any)
                                EmitSymbolToken(typeTo, conversion.Syntax)

                                'TODO: is this needed for widening conversions?
                            ElseIf typeTo.IsReferenceType Then
                                _builder.EmitOpCode(ILOpCode.Castclass)
                                EmitSymbolToken(typeTo, conversion.Syntax)

                            Else
                                Debug.Assert(typeTo.IsValueType)
                                ' unbox the return value to get the struct from inside the object
                                EmitUnboxAny(typeTo, conversion.Syntax)
                            End If
                        End If

                    ElseIf typeTo.IsTypeParameter() Then
                        Debug.Assert(Not typeFrom.IsTypeParameter())

                        If typeFrom.IsValueType Then
                            ' For any conversion from a value type to a generic parameter,
                            ' box the operand and then allow the explicit cast from Object to
                            ' the target generic parameter type.
                            EmitBox(typeFrom, conversion.Operand.Syntax)
                        End If

                        _builder.EmitOpCode(ILOpCode.Unbox_any)
                        EmitSymbolToken(typeTo, conversion.Syntax)

                    ElseIf typeFrom.IsValueType Then

                        EmitBox(typeFrom, conversion.Operand.Syntax)

                        If typeTo.IsInterfaceType() Then
                            ' For any conversion from a value type to an implemented interface,
                            ' box the operand and then allow the explicit conversion from Object to
                            ' the interface below.

                            _builder.EmitOpCode(ILOpCode.Castclass)
                            EmitSymbolToken(typeTo, conversion.Syntax)
                        Else
                            Debug.Assert(typeTo.SpecialType = SpecialType.System_Object OrElse
                                         typeTo.SpecialType = SpecialType.System_ValueType OrElse
                                         typeTo.SpecialType = SpecialType.System_Enum)
                        End If

                    ElseIf typeTo.IsReferenceType Then
                        Debug.Assert(typeFrom.IsReferenceType)

                        Dim needExplicitCastClass As Boolean = True

                        If Conversions.IsWideningConversion(conversion.ConversionKind) Then
                            needExplicitCastClass = False

                            ' No need to emit explicit cast, except in the following cases.
                            ' TODO: Do we still need this checks or CLR is smarter now?
                            If typeFrom.IsArrayType() Then
                                Dim fromElementType = DirectCast(typeFrom, ArrayTypeSymbol).ElementType

                                ' Bug VSWhidbey 415020
                                If typeTo.IsArrayType() AndAlso
                                   (fromElementType.IsTypeParameter() OrElse
                                            DirectCast(typeTo, ArrayTypeSymbol).ElementType.IsTypeParameter()) Then

                                    needExplicitCastClass = True

                                ElseIf fromElementType.IsTypeParameter() AndAlso
                                    typeTo.IsInterfaceType() Then

                                    ' Bug VSWhidbey 517458. Special case for IList(Of T), ICollection(Of T) and IEnumerable(Of T).
                                    Dim [interface] = DirectCast(typeTo, NamedTypeSymbol)

                                    If [interface].Arity = 1 AndAlso
                                       Not [interface].TypeArgumentsNoUseSiteDiagnostics(0).IsSameTypeIgnoringCustomModifiers(fromElementType) Then
                                        needExplicitCastClass = True
                                    End If
                                End If
                            End If
                        End If

                        If needExplicitCastClass Then
                            _builder.EmitOpCode(ILOpCode.Castclass)
                            EmitSymbolToken(typeTo, conversion.Syntax)
                        End If

                    Else
                        Debug.Assert(typeTo.IsValueType)
                        Debug.Assert(typeFrom.IsReferenceType)
                        Debug.Assert(typeFrom.SpecialType = SpecialType.System_Object OrElse
                                     typeFrom.SpecialType = SpecialType.System_ValueType OrElse
                                     typeFrom.SpecialType = SpecialType.System_Enum OrElse
                                     typeFrom.IsInterfaceType)

                        Debug.Assert(IsUnboxingDirectCast(conversion))

                        ' unbox the return value to get the struct from inside the object
                        EmitUnboxAny(typeTo, conversion.Syntax)
                    End If
                End If
            End If

            EmitPopIfUnused(used)
        End Sub

        Private Function ConversionHasSideEffects(conversion As BoundConversion) As Boolean
            ' only some intrinsic conversions are side-effect free
            ' the only side-effect of an intrinsic conversion is a throw when we fail to convert.
            ' 
            ' unchecked numeric conv does not throw
            ' implicit ref cast does not throw
            ' ...

            'TODO: compute this (note: returning true is safe - false would just enable optimizations)
            Return True
        End Function

        Private Function ConversionHasSideEffects(conversion As BoundDirectCast) As Boolean
            'TODO: compute this (note: returning true is safe - false would just enable optimizations)
            Return True
        End Function

        Private Function ConversionHasSideEffects(conversion As BoundTryCast) As Boolean
            'TODO: compute this (note: returning true is safe - false would just enable optimizations)
            Return False
        End Function

        Private Sub EmitTryCastExpression(conversion As BoundTryCast, used As Boolean)
            If Not used AndAlso Not ConversionHasSideEffects(conversion) Then
                EmitExpression(conversion.Operand, False)
                Return
            End If

            If conversion.Operand.IsNothingLiteral Then

                If conversion.Type.IsTypeParameter() Then
                    'TODO: used
                    EmitLoadDefaultValueOfTypeParameter(conversion.Type, used:=True, syntaxNode:=conversion.Syntax)
                Else
                    Debug.Assert(conversion.Type.IsReferenceType)
                    EmitExpression(conversion.Operand, True)
                End If
            Else

                EmitExpression(conversion.Operand, True)

                If Not Conversions.IsIdentityConversion(conversion.ConversionKind) Then
                    Dim typeTo = conversion.Type
                    Dim typeFrom = conversion.Operand.Type

                    Debug.Assert(typeFrom IsNot Nothing)
                    Debug.Assert(typeTo IsNot Nothing)

                    If typeFrom.IsReferenceType() OrElse
                        typeFrom.IsTypeParameter() OrElse
                        typeTo.IsTypeParameter() Then

                        If Not IsVerifierReference(typeFrom) Then
                            EmitBox(typeFrom, conversion.Operand.Syntax)
                        End If

                        _builder.EmitOpCode(ILOpCode.Isinst)
                        EmitSymbolToken(typeTo, conversion.Syntax)

                        If Not IsVerifierReference(typeTo) Then
                            _builder.EmitOpCode(ILOpCode.Unbox_any)
                            EmitSymbolToken(typeTo, conversion.Syntax)
                        End If

                    Else
                        Debug.Assert(typeFrom.IsValueType)
                        Debug.Assert(typeTo.IsReferenceType)
                        Debug.Assert(typeTo.SpecialType = SpecialType.System_Object OrElse
                                     typeTo.SpecialType = SpecialType.System_ValueType OrElse
                                     typeTo.SpecialType = SpecialType.System_Enum OrElse
                                     typeTo.IsInterfaceType())

                        EmitBox(typeFrom, conversion.Operand.Syntax)
                    End If
                End If
            End If

            EmitPopIfUnused(used)
        End Sub

    End Class

End Namespace

