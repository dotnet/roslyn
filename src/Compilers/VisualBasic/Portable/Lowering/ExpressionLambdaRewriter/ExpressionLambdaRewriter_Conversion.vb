' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class ExpressionLambdaRewriter

        Private Function VisitConversion(node As BoundConversion) As BoundExpression
            If Conversions.IsIdentityConversion(node.ConversionKind) AndAlso Not node.Type.IsFloatingType() Then
                Return Me.VisitInternal(node.Operand)
            End If

            Debug.Assert(node.ExtendedInfoOpt Is Nothing)
            Return ConvertExpression(node.Operand, node.ConversionKind, node.Operand.Type, node.Type, node.Checked, node.ExplicitCastInCode, ConversionSemantics.[Default])
        End Function

        Private Function VisitDirectCast(node As BoundDirectCast) As BoundExpression
            If Conversions.IsIdentityConversion(node.ConversionKind) Then
                Return Me.VisitInternal(node.Operand)
            End If

            Debug.Assert(node.RelaxationLambdaOpt Is Nothing)
            Return ConvertExpression(node.Operand, node.ConversionKind, node.Operand.Type, node.Type, False, True, ConversionSemantics.DirectCast)
        End Function

        Private Function VisitTryCast(node As BoundTryCast) As BoundExpression
            If Conversions.IsIdentityConversion(node.ConversionKind) Then
                Return Me.VisitInternal(node.Operand)
            End If

            Debug.Assert(node.RelaxationLambdaOpt Is Nothing)
            Return ConvertExpression(node.Operand, node.ConversionKind, node.Operand.Type, node.Type, False, True, ConversionSemantics.TryCast)
        End Function

        Private Enum ConversionSemantics
            [Default]
            [DirectCast]
            [TryCast]
        End Enum

        Private Function ConvertExpression(operand As BoundExpression, conversion As ConversionKind, typeFrom As TypeSymbol, typeTo As TypeSymbol,
                                           isChecked As Boolean, explicitCastInCode As Boolean, semantics As ConversionSemantics) As BoundExpression

            ' Although the code here is similar to VBSemanticRewriter.RewriteConversion, the actual helpers used are sometimes
            ' different in expression trees than in the generated code. The appears to be in order to get better 
            ' compatibility with various LINQ providers. So, we have to select the helpers with expression-tree specific code.

            Dim toIsNullable As Boolean = typeTo.IsNullableType
            Dim notNullableTo As TypeSymbol = typeTo.GetNullableUnderlyingTypeOrSelf

            If operand.IsNothingLiteral() Then
                ' Conversion from 'Nothing' literal.

                Debug.Assert(conversion = ConversionKind.WideningNothingLiteral OrElse
                             (Conversions.IsIdentityConversion(conversion) AndAlso
                                Not notNullableTo.IsTypeParameter() AndAlso notNullableTo.IsReferenceType) OrElse
                             (conversion And (ConversionKind.Reference Or ConversionKind.Array)) <> 0)

                If notNullableTo.IsTypeParameter() Then
                    If toIsNullable Then
                        Return Convert(VisitInternal(Me._factory.Null(Me.ObjectType)), typeTo, False)
                    Else
                        Return [Default](typeTo)
                    End If

                ElseIf notNullableTo.IsReferenceType Then
                    Return CreateLiteralExpression(operand, typeTo)  ' null constant.

                Else
                    ' Find the parameterless constructor to be used in conversion of Nothing to a value type
                    If toIsNullable Then
                        Return CreateLiteralExpression(operand, typeTo)  ' null constant.
                    Else
                        Return InitWithParameterlessValueTypeConstructor(typeTo)
                    End If
                End If
            End If

            If operand.Kind = BoundKind.Lambda Then
                Return ConvertLambda(DirectCast(operand, BoundLambda), typeTo)
            End If

            If (conversion And ConversionKind.UserDefined) <> 0 Then
                Debug.Assert(semantics = ConversionSemantics.Default)
                Dim userDefinedConversion = DirectCast(operand, BoundUserDefinedConversion)

                Return CreateUserDefinedConversion(userDefinedConversion, typeTo, (conversion And ConversionKind.Nullable) <> 0, isChecked)

            ElseIf typeTo.IsInterfaceType AndAlso typeFrom.IsValueType Then
                ' When converting from value type to interface type, we convert to System.Object first
                Dim objectValue As BoundExpression = CreateBuiltInConversion(typeFrom, Me.ObjectType, Visit(operand), isChecked, explicitCastInCode, semantics)
                Return CreateBuiltInConversion(Me.ObjectType, typeTo, objectValue, isChecked, explicitCastInCode, semantics)

            Else
                Return CreateBuiltInConversion(typeFrom, typeTo, Visit(operand), isChecked, explicitCastInCode, semantics)
            End If
        End Function

        Private Function ConvertLambda(node As BoundLambda, type As TypeSymbol) As BoundExpression
            If type.IsExpressionTree(Me._binder) Then
                type = type.ExpressionTargetDelegate(Me._factory.Compilation)
                Dim result = VisitLambdaInternal(node, DirectCast(type, NamedTypeSymbol))
                Return ConvertRuntimeHelperToExpressionTree("Quote", result)
            Else
                Return VisitLambdaInternal(node, DirectCast(type, NamedTypeSymbol))
            End If
        End Function

        ''' <summary>
        ''' Rewrites a built-in conversion. Doesn't handle user-defined conversions or Nothing literals.
        ''' </summary>
        Private Function CreateBuiltInConversion(typeFrom As TypeSymbol,
                                                 typeTo As TypeSymbol,
                                                 rewrittenOperand As BoundExpression,
                                                 isChecked As Boolean,
                                                 isExplicit As Boolean,
                                                 semantics As ConversionSemantics,
                                                 Optional specialConversionForNullable As Boolean = False) As BoundExpression

            Dim fromIsNullable As Boolean = typeFrom.IsNullableType
            Dim toIsNullable As Boolean = typeTo.IsNullableType

            Dim notNullableTo As TypeSymbol = typeTo.GetNullableUnderlyingTypeOrSelf()
            Dim underlyingTo As TypeSymbol = notNullableTo.GetEnumUnderlyingTypeOrSelf()

            Dim notNullableFrom As TypeSymbol = typeFrom.GetNullableUnderlyingTypeOrSelf()
            Dim underlyingFrom As TypeSymbol = notNullableFrom.GetEnumUnderlyingTypeOrSelf()

            If fromIsNullable OrElse toIsNullable Then

                If fromIsNullable Then
                    If Not toIsNullable Then
                        If typeTo.IsObjectType Then
                            ' Fall through

                        ElseIf TypeSymbol.Equals(notNullableFrom, notNullableTo, TypeCompareKind.ConsiderEverything) Then
                            Debug.Assert(semantics <> ConversionSemantics.TryCast)

                            ' X? -> X
                            If specialConversionForNullable Then
                                Return ConvertNullableToUnderlying(rewrittenOperand, typeFrom, isChecked)
                            Else
                                Return Convert(rewrittenOperand, typeTo, isChecked AndAlso IsIntegralType(notNullableTo))
                            End If

                        Else
                            Debug.Assert(semantics <> ConversionSemantics.TryCast)

                            Dim interimType As TypeSymbol = notNullableFrom
                            Debug.Assert(Not TypeSymbol.Equals(interimType, typeTo, TypeCompareKind.ConsiderEverything))
                            rewrittenOperand = CreateBuiltInConversion(typeFrom, interimType, rewrittenOperand, isChecked, isExplicit,
                                                                       ConversionSemantics.[Default], specialConversionForNullable:=True)
                            Return CreateBuiltInConversion(interimType, typeTo, rewrittenOperand, isChecked, isExplicit,
                                                           ConversionSemantics.[Default], specialConversionForNullable:=True)
                        End If
                    End If
                End If

                If toIsNullable Then
                    If Not fromIsNullable Then
                        If typeFrom.IsObjectType Then
                            ' Fall through

                        ElseIf TypeSymbol.Equals(notNullableFrom, notNullableTo, TypeCompareKind.ConsiderEverything) Then
                            Debug.Assert(semantics <> ConversionSemantics.TryCast)

                            ' X -> X?
                            If specialConversionForNullable Then
                                Return ConvertUnderlyingToNullable(rewrittenOperand, typeTo, isChecked)
                            Else
                                Return Convert(rewrittenOperand, typeTo, isChecked AndAlso IsIntegralType(notNullableTo))
                            End If

                        Else
                            Debug.Assert(semantics <> ConversionSemantics.TryCast)

                            Dim interimType As TypeSymbol = notNullableTo
                            Debug.Assert(Not TypeSymbol.Equals(interimType, typeTo, TypeCompareKind.ConsiderEverything))
                            rewrittenOperand = CreateBuiltInConversion(typeFrom, interimType, rewrittenOperand, isChecked, isExplicit,
                                                                       ConversionSemantics.[Default], specialConversionForNullable:=True)
                            Return CreateBuiltInConversion(interimType, typeTo, rewrittenOperand, isChecked, isExplicit,
                                                           ConversionSemantics.[Default], specialConversionForNullable:=True)
                        End If
                    End If
                End If

                ' NOTE: All other cases fall through to the regular conversion
            End If

            ' Check if we have a special conversion that uses a helper.
            Dim specialHelper As MethodSymbol = Nothing
            ' NOTE: in Dev11 TryCast and DirectCast do not seem to use helper methods 
            If semantics = ConversionSemantics.Default Then
                specialHelper = GetConversionHelperMethod(underlyingFrom.SpecialType, underlyingTo.SpecialType)
            End If

            If specialHelper IsNot Nothing Then
                Dim helperOperandType As TypeSymbol = specialHelper.Parameters(0).Type
                If fromIsNullable Then
                    helperOperandType = Me._factory.NullableOf(helperOperandType)
                End If

                Dim helperReturnType As TypeSymbol = specialHelper.ReturnType
                If toIsNullable Then
                    helperReturnType = Me._factory.NullableOf(helperReturnType)
                End If

                Dim underlyingOperand = ConvertIfNeeded(rewrittenOperand, typeFrom, helperOperandType,
                                                        isChecked AndAlso IsIntegralType(helperOperandType))
                Dim convertedWithHelper = Convert(underlyingOperand, helperReturnType, specialHelper,
                                                  isChecked AndAlso IsIntegralType(helperReturnType))
                Return ConvertIfNeeded(convertedWithHelper, helperReturnType, typeTo, isChecked AndAlso IsIntegralType(underlyingTo))

            ElseIf underlyingFrom.IsObjectType() AndAlso underlyingTo.IsTypeParameter() Then
                Select Case semantics
                    Case ConversionSemantics.DirectCast
                        Return Convert(rewrittenOperand, typeTo, False)

                    Case ConversionSemantics.TryCast
                        Return CreateTypeAs(rewrittenOperand, typeTo)

                    Case Else
                        Dim helper As MethodSymbol = Me._factory.WellKnownMember(Of MethodSymbol)(WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToGenericParameter_T_Object)
                        Return [Call](_factory.Null(), helper.Construct(typeTo), rewrittenOperand)
                End Select

            ElseIf underlyingFrom.IsTypeParameter() Then
                If semantics = ConversionSemantics.TryCast Then
                    Return CreateTypeAs(If(typeTo.SpecialType = SpecialType.System_Object,
                                           rewrittenOperand, Convert(rewrittenOperand, Me.ObjectType, False)),
                                        typeTo)
                Else
                    ' Converting from type parameter to something besides object is done as double conversion; first to object, then to final type (if not object).
                    Dim objectConversion = Convert(rewrittenOperand, Me.ObjectType, False)
                    Return ConvertIfNeeded(objectConversion, _factory.SpecialType(SpecialType.System_Object), typeTo, False)
                End If

            ElseIf underlyingTo.IsStringType() AndAlso underlyingFrom.IsCharSZArray() Then
                Return [New](SpecialMember.System_String__CtorSZArrayChar, rewrittenOperand)

            ElseIf underlyingFrom.IsReferenceType AndAlso underlyingTo.IsCharSZArray() Then
                Dim helper As Symbol
                Dim argumentType As TypeSymbol
                If underlyingFrom.IsStringType() Then
                    helper = _factory.WellKnownMember(Of Symbol)(WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToCharArrayRankOneString)
                    argumentType = _factory.SpecialType(SpecialType.System_String)
                Else
                    helper = _factory.WellKnownMember(Of Symbol)(WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToCharArrayRankOneObject)
                    argumentType = _factory.SpecialType(SpecialType.System_Object)
                End If

                Return Convert(ConvertIfNeeded(rewrittenOperand, typeFrom, argumentType, False),
                               typeTo,
                               DirectCast(helper, MethodSymbol),
                               False)

            ElseIf underlyingFrom.IsBooleanType() AndAlso underlyingTo.IsNumericType() Then
                ' Done via a convert(negate(convert(bool_expr)))

                Dim typeBeforeNegation As TypeSymbol = GetSignedVersionOfNumericType(underlyingTo)
                Dim typeBeforeNegationIsByte As Boolean = typeBeforeNegation.SpecialType = SpecialType.System_SByte

                If typeBeforeNegationIsByte Then
                    typeBeforeNegation = _factory.SpecialType(SpecialType.System_Int32)
                End If

                If isChecked AndAlso (typeBeforeNegation IsNot underlyingTo) Then
                    isChecked = False
                End If

                If fromIsNullable AndAlso Not (typeBeforeNegation IsNot underlyingTo) Then
                    typeBeforeNegation = Me._factory.NullableOf(typeBeforeNegation)
                End If

                Dim converted = Convert(rewrittenOperand, typeBeforeNegation, isChecked AndAlso IsIntegralType(typeBeforeNegation))
                Dim negated = Negate(converted)
                Return ConvertIfNeeded(negated, typeBeforeNegation, typeTo, isChecked AndAlso IsIntegralType(typeBeforeNegation))

            Else
                ' Nothing particularly special about this conversion.

                If isExplicit AndAlso underlyingTo.IsFloatingType() Then ' Explicit Single/Double conversion are maintained even if identity conversions.
                    Debug.Assert(semantics <> ConversionSemantics.TryCast)
                    Return Convert(rewrittenOperand, typeTo, isChecked AndAlso IsIntegralType(underlyingTo))

                ElseIf semantics = ConversionSemantics.TryCast Then
                    Return CreateTypeAsIfNeeded(rewrittenOperand, typeFrom, typeTo)

                Else
                    Return ConvertIfNeeded(rewrittenOperand, typeFrom, typeTo, isChecked AndAlso IsIntegralType(underlyingTo))
                End If
            End If
        End Function

        ' Get the signed version of a type, if its an unsigned numeric type;
        ' otherwise return the type.
        Private Function GetSignedVersionOfNumericType(type As TypeSymbol) As TypeSymbol
            Select Case type.SpecialType
                Case SpecialType.System_Byte
                    Return Me._factory.SpecialType(SpecialType.System_SByte)
                Case SpecialType.System_UInt16
                    Return Me._factory.SpecialType(SpecialType.System_Int16)
                Case SpecialType.System_UInt32
                    Return Me._factory.SpecialType(SpecialType.System_Int32)
                Case SpecialType.System_UInt64
                    Return Me._factory.SpecialType(SpecialType.System_Int64)
                Case Else
                    Return type
            End Select
        End Function

        Private Function ConvertUnderlyingToNullable(operand As BoundExpression, nullableType As TypeSymbol, isChecked As Boolean) As BoundExpression
            If isChecked AndAlso Not IsIntegralType(nullableType) Then
                isChecked = False
            End If

            Dim helper As MethodSymbol = DirectCast(Me._factory.SpecialMember(
                        SpecialMember.System_Nullable_T__op_Implicit_FromT), MethodSymbol)

            If helper IsNot Nothing Then
                Dim substitutedNullableType = DirectCast(nullableType, SubstitutedNamedType)
                Return Convert(operand, nullableType, DirectCast(substitutedNullableType.GetMemberForDefinition(helper), MethodSymbol), isChecked)
            End If

            ' Error must be reported already
            Return Convert(operand, nullableType, isChecked)
        End Function

        Private Function ConvertNullableToUnderlying(operand As BoundExpression, nullableType As TypeSymbol, isChecked As Boolean) As BoundExpression
            Dim underlyingType As TypeSymbol = nullableType.GetNullableUnderlyingType

            If isChecked AndAlso Not IsIntegralType(underlyingType) Then
                isChecked = False
            End If

            Dim helper As MethodSymbol = DirectCast(Me._factory.SpecialMember(
                        SpecialMember.System_Nullable_T__op_Explicit_ToT), MethodSymbol)

            If helper IsNot Nothing Then
                Dim substitutedNullableType = DirectCast(nullableType, SubstitutedNamedType)
                Return Convert(operand, underlyingType, DirectCast(substitutedNullableType.GetMemberForDefinition(helper), MethodSymbol), isChecked)
            End If

            ' Error must be reported already
            Return Convert(operand, underlyingType, isChecked)
        End Function

        ' Handle a bound user-defined conversion.
        Private Function CreateUserDefinedConversion(node As BoundUserDefinedConversion, resultType As TypeSymbol, isLifted As Boolean, isChecked As Boolean) As BoundExpression
            ' A user-defined conversion consists of a method call, wrapped by possibly two user-defined conversion.

            Dim methodCall As BoundCall = node.Call
            Dim method As MethodSymbol = methodCall.Method
            Dim methodCallType As TypeSymbol = methodCall.Type
            Debug.Assert(method.ParameterCount = 1)

            ' The argument to be passed to the conversion
            Dim argument As BoundExpression = Nothing

            ' Handle lifted conversions
            If isLifted Then
                ' If the types of the original argument and conversion result
                ' are both nullable or not, Dev11 just uses one conversion
                Dim originalArgument As BoundExpression = node.Operand
                If originalArgument.Type.IsNullableType = resultType.IsNullableType Then
                    Return Convert(Visit(originalArgument), resultType, method, isChecked AndAlso IsIntegralType(resultType))
                End If

                ' Otherwise just follow non-lifted scenario
            End If

            ' Rewrite the operand (which might be the inner conversion or might not) to the user-defined operator method.
            Dim rewrittenCallOperand As BoundExpression = Visit(methodCall.Arguments(0))

            ' Create the Convert node with the method.
            Dim userDefinedConversion As BoundExpression = Convert(rewrittenCallOperand, methodCallType, method, isChecked AndAlso IsIntegralType(methodCallType))

            ' If there's an outer conversion, create that.
            Dim outerConversion As BoundConversion = node.OutConversionOpt
            If outerConversion IsNot Nothing Then
                Debug.Assert(outerConversion.Type.IsSameTypeIgnoringAll(resultType))
                Return CreateBuiltInConversion(methodCallType, resultType, userDefinedConversion,
                                               outerConversion.Checked, outerConversion.ExplicitCastInCode, ConversionSemantics.[Default])
            Else
                Return userDefinedConversion
            End If
        End Function

        Private Function CreateTypeAs(expr As BoundExpression, type As TypeSymbol) As BoundExpression
            Return ConvertRuntimeHelperToExpressionTree("TypeAs", expr, _factory.[Typeof](type))
        End Function

        Private Function CreateTypeAsIfNeeded(operand As BoundExpression, oldType As TypeSymbol, newType As TypeSymbol) As BoundExpression
            Return If((TypeSymbol.Equals(oldType, newType, TypeCompareKind.ConsiderEverything)), operand, CreateTypeAs(operand, newType))
        End Function

        ' Emit a Convert node to a specific type with no helper method.
        Private Function Convert(expr As BoundExpression, type As TypeSymbol, isChecked As Boolean) As BoundExpression
            Return ConvertRuntimeHelperToExpressionTree(If(isChecked, "ConvertChecked", "Convert"), expr, _factory.[Typeof](type))
        End Function

        ' Emit a Convert node to a specific type with a helper method.
        Private Function Convert(expr As BoundExpression, type As TypeSymbol, helper As MethodSymbol, isChecked As Boolean) As BoundExpression
            Return ConvertRuntimeHelperToExpressionTree(If(isChecked, "ConvertChecked", "Convert"), expr, _factory.[Typeof](type), _factory.MethodInfo(helper))
        End Function

        ' Emit a convert node if types are different.
        Private Function ConvertIfNeeded(operand As BoundExpression, oldType As TypeSymbol, newType As TypeSymbol, isChecked As Boolean) As BoundExpression
            Return If((TypeSymbol.Equals(oldType, newType, TypeCompareKind.ConsiderEverything)), operand, Convert(operand, newType, isChecked))
        End Function

        ''' <summary>
        ''' Get the conversion helper for converting between special types in an expression tree. 
        ''' These are often different than the ones used in regular code.
        ''' </summary>
        Private Function GetConversionHelperMethod(stFrom As SpecialType, stTo As SpecialType) As MethodSymbol
            Dim wellKnownHelper = CType(-1, WellKnownMember)
            Dim specialHelper = CType(-1, SpecialMember)

            Select Case stTo
                Case SpecialType.System_Boolean
                    Select Case stFrom
                        Case SpecialType.System_SByte : wellKnownHelper = WellKnownMember.System_Convert__ToBooleanInt32
                        Case SpecialType.System_Byte : wellKnownHelper = WellKnownMember.System_Convert__ToBooleanInt32
                        Case SpecialType.System_Int16 : wellKnownHelper = WellKnownMember.System_Convert__ToBooleanInt32
                        Case SpecialType.System_UInt16 : wellKnownHelper = WellKnownMember.System_Convert__ToBooleanInt32
                        Case SpecialType.System_Int32 : wellKnownHelper = WellKnownMember.System_Convert__ToBooleanInt32
                        Case SpecialType.System_UInt32 : wellKnownHelper = WellKnownMember.System_Convert__ToBooleanUInt32
                        Case SpecialType.System_Int64 : wellKnownHelper = WellKnownMember.System_Convert__ToBooleanInt64
                        Case SpecialType.System_UInt64 : wellKnownHelper = WellKnownMember.System_Convert__ToBooleanUInt64
                        Case SpecialType.System_Single : wellKnownHelper = WellKnownMember.System_Convert__ToBooleanSingle
                        Case SpecialType.System_Double : wellKnownHelper = WellKnownMember.System_Convert__ToBooleanDouble
                        Case SpecialType.System_Decimal : wellKnownHelper = WellKnownMember.System_Convert__ToBooleanDecimal
                        Case SpecialType.System_String : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToBooleanString
                        Case SpecialType.System_Object : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToBooleanObject
                    End Select

                Case SpecialType.System_Byte
                    Select Case stFrom
                        Case SpecialType.System_Double : wellKnownHelper = WellKnownMember.System_Convert__ToByteDouble
                        Case SpecialType.System_Single : wellKnownHelper = WellKnownMember.System_Convert__ToByteSingle
                        Case SpecialType.System_Decimal : specialHelper = SpecialMember.System_Decimal__op_Explicit_ToByte
                        Case SpecialType.System_String : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToByteString
                        Case SpecialType.System_Object : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToByteObject
                    End Select

                Case SpecialType.System_SByte
                    Select Case stFrom
                        Case SpecialType.System_Double : wellKnownHelper = WellKnownMember.System_Convert__ToSByteDouble
                        Case SpecialType.System_Single : wellKnownHelper = WellKnownMember.System_Convert__ToSByteSingle
                        Case SpecialType.System_Decimal : specialHelper = SpecialMember.System_Decimal__op_Explicit_ToSByte
                        Case SpecialType.System_String : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToSByteString
                        Case SpecialType.System_Object : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToSByteObject
                    End Select

                Case SpecialType.System_Int16
                    Select Case stFrom
                        Case SpecialType.System_Double : wellKnownHelper = WellKnownMember.System_Convert__ToInt16Double
                        Case SpecialType.System_Single : wellKnownHelper = WellKnownMember.System_Convert__ToInt16Single
                        Case SpecialType.System_Decimal : specialHelper = SpecialMember.System_Decimal__op_Explicit_ToInt16
                        Case SpecialType.System_String : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToShortString
                        Case SpecialType.System_Object : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToShortObject
                    End Select

                Case SpecialType.System_UInt16
                    Select Case stFrom
                        Case SpecialType.System_Double : wellKnownHelper = WellKnownMember.System_Convert__ToUInt16Double
                        Case SpecialType.System_Single : wellKnownHelper = WellKnownMember.System_Convert__ToUInt16Single
                        Case SpecialType.System_Decimal : specialHelper = SpecialMember.System_Decimal__op_Explicit_ToUInt16
                        Case SpecialType.System_String : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToUShortString
                        Case SpecialType.System_Object : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToUShortObject
                    End Select

                Case SpecialType.System_Int32
                    Select Case stFrom
                        Case SpecialType.System_Double : wellKnownHelper = WellKnownMember.System_Convert__ToInt32Double
                        Case SpecialType.System_Single : wellKnownHelper = WellKnownMember.System_Convert__ToInt32Single
                        Case SpecialType.System_Decimal : specialHelper = SpecialMember.System_Decimal__op_Explicit_ToInt32
                        Case SpecialType.System_String : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToIntegerString
                        Case SpecialType.System_Object : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToIntegerObject
                    End Select

                Case SpecialType.System_UInt32
                    Select Case stFrom
                        Case SpecialType.System_Double : wellKnownHelper = WellKnownMember.System_Convert__ToUInt32Double
                        Case SpecialType.System_Single : wellKnownHelper = WellKnownMember.System_Convert__ToUInt32Single
                        Case SpecialType.System_Decimal : specialHelper = SpecialMember.System_Decimal__op_Explicit_ToUInt32
                        Case SpecialType.System_String : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToUIntegerString
                        Case SpecialType.System_Object : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToUIntegerObject
                    End Select

                Case SpecialType.System_Int64
                    Select Case stFrom
                        Case SpecialType.System_Double : wellKnownHelper = WellKnownMember.System_Convert__ToInt64Double
                        Case SpecialType.System_Single : wellKnownHelper = WellKnownMember.System_Convert__ToInt64Single
                        Case SpecialType.System_Decimal : specialHelper = SpecialMember.System_Decimal__op_Explicit_ToInt64
                        Case SpecialType.System_String : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToLongString
                        Case SpecialType.System_Object : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToLongObject
                    End Select

                Case SpecialType.System_UInt64
                    Select Case stFrom
                        Case SpecialType.System_Double : wellKnownHelper = WellKnownMember.System_Convert__ToUInt64Double
                        Case SpecialType.System_Single : wellKnownHelper = WellKnownMember.System_Convert__ToUInt64Single
                        Case SpecialType.System_Decimal : specialHelper = SpecialMember.System_Decimal__op_Explicit_ToUInt64
                        Case SpecialType.System_String : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToULongString
                        Case SpecialType.System_Object : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToULongObject
                    End Select

                Case SpecialType.System_Decimal
                    Select Case stFrom
                        Case SpecialType.System_SByte : specialHelper = SpecialMember.System_Decimal__op_Implicit_FromInt32
                        Case SpecialType.System_Byte : specialHelper = SpecialMember.System_Decimal__op_Implicit_FromInt32
                        Case SpecialType.System_Int16 : specialHelper = SpecialMember.System_Decimal__op_Implicit_FromInt32
                        Case SpecialType.System_UInt16 : specialHelper = SpecialMember.System_Decimal__op_Implicit_FromInt32
                        Case SpecialType.System_Int32 : specialHelper = SpecialMember.System_Decimal__op_Implicit_FromInt32
                        Case SpecialType.System_UInt32 : specialHelper = SpecialMember.System_Decimal__op_Implicit_FromUInt32
                        Case SpecialType.System_Int64 : specialHelper = SpecialMember.System_Decimal__op_Implicit_FromInt64
                        Case SpecialType.System_UInt64 : specialHelper = SpecialMember.System_Decimal__op_Implicit_FromUInt64
                        Case SpecialType.System_Single : specialHelper = SpecialMember.System_Decimal__op_Explicit_FromSingle
                        Case SpecialType.System_Double : specialHelper = SpecialMember.System_Decimal__op_Explicit_FromDouble
                        Case SpecialType.System_Boolean : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalBoolean
                        Case SpecialType.System_String : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalString
                        Case SpecialType.System_Object : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalObject
                    End Select

                Case SpecialType.System_Single
                    Select Case stFrom
                        Case SpecialType.System_Decimal : specialHelper = SpecialMember.System_Decimal__op_Explicit_ToSingle
                        Case SpecialType.System_String : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToSingleString
                        Case SpecialType.System_Object : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToSingleObject
                    End Select

                Case SpecialType.System_Double
                    Select Case stFrom
                        Case SpecialType.System_Decimal : specialHelper = SpecialMember.System_Decimal__op_Explicit_ToDouble
                        Case SpecialType.System_String : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDoubleString
                        Case SpecialType.System_Object : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDoubleObject
                    End Select

                Case SpecialType.System_Char
                    Select Case stFrom
                        Case SpecialType.System_String : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToCharString
                        Case SpecialType.System_Object : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToCharObject
                    End Select

                Case SpecialType.System_String
                    Select Case stFrom
                        Case SpecialType.System_Boolean : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringBoolean
                        Case SpecialType.System_SByte,
                             SpecialType.System_Int16,
                             SpecialType.System_Int32 : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringInt32

                        Case SpecialType.System_Byte : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringByte

                        Case SpecialType.System_UInt16,
                             SpecialType.System_UInt32 : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringUInt32

                        Case SpecialType.System_Int64 : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringInt64
                        Case SpecialType.System_UInt64 : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringUInt64
                        Case SpecialType.System_Single : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringSingle
                        Case SpecialType.System_Double : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDouble
                        Case SpecialType.System_Decimal : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDecimal
                        Case SpecialType.System_DateTime : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDateTime
                        Case SpecialType.System_Char : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringChar
                        Case SpecialType.System_Object : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringObject
                    End Select

                Case SpecialType.System_DateTime
                    Select Case stFrom
                        Case SpecialType.System_String : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDateString
                        Case SpecialType.System_Object : wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDateObject

                    End Select
            End Select

            If wellKnownHelper >= 0 Then
                Return Me._factory.WellKnownMember(Of MethodSymbol)(wellKnownHelper)
            ElseIf specialHelper >= 0 Then
                Return DirectCast(_factory.SpecialMember(specialHelper), MethodSymbol)
            Else
                Return Nothing
            End If
        End Function

    End Class
End Namespace
