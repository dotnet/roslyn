' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class OverloadResolution

        ''' <summary>
        ''' A map from Operator name to number of parameters and kind.
        ''' </summary>
        Private Shared ReadOnly s_operatorNames As Dictionary(Of String, OperatorInfo)

        ''' <summary>
        ''' Operator kind and expected number of parameters.
        ''' </summary>
        Friend Structure OperatorInfo
            Private ReadOnly _Id As Integer

            Public Sub New(op As UnaryOperatorKind)
                _Id = (1 Or (op << 2))
            End Sub

            Public Sub New(op As BinaryOperatorKind)
                _Id = (2 Or (op << 2))
            End Sub

            Public ReadOnly Property ParamCount As Integer
                Get
                    Return (_Id And 3)
                End Get
            End Property

            Public ReadOnly Property IsBinary As Boolean
                Get
                    Return ParamCount = 2
                End Get
            End Property

            Public ReadOnly Property IsUnary As Boolean
                Get
                    Return ParamCount = 1
                End Get
            End Property

            Public ReadOnly Property UnaryOperatorKind As UnaryOperatorKind
                Get
                    If Not IsUnary Then
                        Return UnaryOperatorKind.Error
                    End If

                    Return CType(_Id >> 2, UnaryOperatorKind)
                End Get
            End Property

            Public ReadOnly Property BinaryOperatorKind As BinaryOperatorKind
                Get
                    If Not IsBinary Then
                        Return BinaryOperatorKind.Error
                    End If

                    Return CType(_Id >> 2, BinaryOperatorKind)
                End Get
            End Property
        End Structure

        Friend Shared Function GetOperatorInfo(name As String) As OperatorInfo
            Dim result As OperatorInfo = Nothing

            If name.Length > 3 AndAlso IdentifierComparison.Equals("op_", name.Substring(0, 3)) AndAlso s_operatorNames.TryGetValue(name, result) Then
                Return result
            End If

            Return Nothing
        End Function

        Shared Sub New()
            Dim operators As New Dictionary(Of String, OperatorInfo)(IdentifierComparison.Comparer)

            operators.Add(WellKnownMemberNames.OnesComplementOperatorName, New OperatorInfo(UnaryOperatorKind.Not))
            operators.Add(WellKnownMemberNames.TrueOperatorName, New OperatorInfo(UnaryOperatorKind.IsTrue))
            operators.Add(WellKnownMemberNames.FalseOperatorName, New OperatorInfo(UnaryOperatorKind.IsFalse))
            operators.Add(WellKnownMemberNames.UnaryPlusOperatorName, New OperatorInfo(UnaryOperatorKind.Plus))
            operators.Add(WellKnownMemberNames.AdditionOperatorName, New OperatorInfo(BinaryOperatorKind.Add))
            operators.Add(WellKnownMemberNames.UnaryNegationOperatorName, New OperatorInfo(UnaryOperatorKind.Minus))
            operators.Add(WellKnownMemberNames.SubtractionOperatorName, New OperatorInfo(BinaryOperatorKind.Subtract))
            operators.Add(WellKnownMemberNames.MultiplyOperatorName, New OperatorInfo(BinaryOperatorKind.Multiply))
            operators.Add(WellKnownMemberNames.DivisionOperatorName, New OperatorInfo(BinaryOperatorKind.Divide))
            operators.Add(WellKnownMemberNames.IntegerDivisionOperatorName, New OperatorInfo(BinaryOperatorKind.IntegerDivide))
            operators.Add(WellKnownMemberNames.ModulusOperatorName, New OperatorInfo(BinaryOperatorKind.Modulo))
            operators.Add(WellKnownMemberNames.ExponentOperatorName, New OperatorInfo(BinaryOperatorKind.Power))
            operators.Add(WellKnownMemberNames.EqualityOperatorName, New OperatorInfo(BinaryOperatorKind.Equals))
            operators.Add(WellKnownMemberNames.InequalityOperatorName, New OperatorInfo(BinaryOperatorKind.NotEquals))
            operators.Add(WellKnownMemberNames.LessThanOperatorName, New OperatorInfo(BinaryOperatorKind.LessThan))
            operators.Add(WellKnownMemberNames.GreaterThanOperatorName, New OperatorInfo(BinaryOperatorKind.GreaterThan))
            operators.Add(WellKnownMemberNames.LessThanOrEqualOperatorName, New OperatorInfo(BinaryOperatorKind.LessThanOrEqual))
            operators.Add(WellKnownMemberNames.GreaterThanOrEqualOperatorName, New OperatorInfo(BinaryOperatorKind.GreaterThanOrEqual))
            operators.Add(WellKnownMemberNames.LikeOperatorName, New OperatorInfo(BinaryOperatorKind.Like))
            operators.Add(WellKnownMemberNames.ConcatenateOperatorName, New OperatorInfo(BinaryOperatorKind.Concatenate))
            operators.Add(WellKnownMemberNames.BitwiseAndOperatorName, New OperatorInfo(BinaryOperatorKind.And))
            operators.Add(WellKnownMemberNames.BitwiseOrOperatorName, New OperatorInfo(BinaryOperatorKind.Or))
            operators.Add(WellKnownMemberNames.ExclusiveOrOperatorName, New OperatorInfo(BinaryOperatorKind.Xor))
            operators.Add(WellKnownMemberNames.LeftShiftOperatorName, New OperatorInfo(BinaryOperatorKind.LeftShift))
            operators.Add(WellKnownMemberNames.RightShiftOperatorName, New OperatorInfo(BinaryOperatorKind.RightShift))
            operators.Add(WellKnownMemberNames.ImplicitConversionName, New OperatorInfo(UnaryOperatorKind.Implicit))
            operators.Add(WellKnownMemberNames.ExplicitConversionName, New OperatorInfo(UnaryOperatorKind.Explicit))

            ' These cannot be declared in source, but can be imported.
            operators.Add(WellKnownMemberNames.LogicalNotOperatorName, New OperatorInfo(UnaryOperatorKind.Not))
            operators.Add(WellKnownMemberNames.LogicalAndOperatorName, New OperatorInfo(BinaryOperatorKind.And))
            operators.Add(WellKnownMemberNames.LogicalOrOperatorName, New OperatorInfo(BinaryOperatorKind.Or))
            operators.Add(WellKnownMemberNames.UnsignedLeftShiftOperatorName, New OperatorInfo(BinaryOperatorKind.LeftShift))
            operators.Add(WellKnownMemberNames.UnsignedRightShiftOperatorName, New OperatorInfo(BinaryOperatorKind.RightShift))

            s_operatorNames = operators
        End Sub

        Friend Shared Function GetOperatorTokenKind(name As String) As SyntaxKind
            Dim opInfo As OperatorInfo = GetOperatorInfo(name)
            Return GetOperatorTokenKind(opInfo)
        End Function

        Friend Shared Function GetOperatorTokenKind(opInfo As OperatorInfo) As SyntaxKind
            If opInfo.IsUnary Then
                Return GetOperatorTokenKind(opInfo.UnaryOperatorKind)
            Else
                Return GetOperatorTokenKind(opInfo.BinaryOperatorKind)
            End If
        End Function

        Friend Shared Function GetOperatorTokenKind(op As UnaryOperatorKind) As SyntaxKind
            Select Case op
                Case UnaryOperatorKind.IsFalse
                    Return SyntaxKind.IsFalseKeyword
                Case UnaryOperatorKind.IsTrue
                    Return SyntaxKind.IsTrueKeyword
                Case UnaryOperatorKind.Minus
                    Return SyntaxKind.MinusToken
                Case UnaryOperatorKind.Not
                    Return SyntaxKind.NotKeyword
                Case UnaryOperatorKind.Plus
                    Return SyntaxKind.PlusToken
                Case UnaryOperatorKind.Implicit, UnaryOperatorKind.Explicit
                    Return SyntaxKind.CTypeKeyword
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(op)
            End Select
        End Function

        Friend Shared Function GetOperatorTokenKind(op As BinaryOperatorKind) As SyntaxKind
            Select Case op
                Case BinaryOperatorKind.Add
                    Return SyntaxKind.PlusToken
                Case BinaryOperatorKind.Subtract
                    Return SyntaxKind.MinusToken
                Case BinaryOperatorKind.Multiply
                    Return SyntaxKind.AsteriskToken
                Case BinaryOperatorKind.Divide
                    Return SyntaxKind.SlashToken
                Case BinaryOperatorKind.IntegerDivide
                    Return SyntaxKind.BackslashToken
                Case BinaryOperatorKind.Modulo
                    Return SyntaxKind.ModKeyword
                Case BinaryOperatorKind.Power
                    Return SyntaxKind.CaretToken
                Case BinaryOperatorKind.Equals
                    Return SyntaxKind.EqualsToken
                Case BinaryOperatorKind.NotEquals
                    Return SyntaxKind.LessThanGreaterThanToken
                Case BinaryOperatorKind.LessThan
                    Return SyntaxKind.LessThanToken
                Case BinaryOperatorKind.GreaterThan
                    Return SyntaxKind.GreaterThanToken
                Case BinaryOperatorKind.LessThanOrEqual
                    Return SyntaxKind.LessThanEqualsToken
                Case BinaryOperatorKind.GreaterThanOrEqual
                    Return SyntaxKind.GreaterThanEqualsToken
                Case BinaryOperatorKind.Like
                    Return SyntaxKind.LikeKeyword
                Case BinaryOperatorKind.Concatenate
                    Return SyntaxKind.AmpersandToken
                Case BinaryOperatorKind.And
                    Return SyntaxKind.AndKeyword
                Case BinaryOperatorKind.Or
                    Return SyntaxKind.OrKeyword
                Case BinaryOperatorKind.Xor
                    Return SyntaxKind.XorKeyword
                Case BinaryOperatorKind.LeftShift
                    Return SyntaxKind.LessThanLessThanToken
                Case BinaryOperatorKind.RightShift
                    Return SyntaxKind.GreaterThanGreaterThanToken
                Case BinaryOperatorKind.AndAlso
                    Return SyntaxKind.AndAlsoKeyword
                Case BinaryOperatorKind.OrElse
                    Return SyntaxKind.OrElseKeyword
                Case BinaryOperatorKind.Is
                    Return SyntaxKind.IsKeyword
                Case BinaryOperatorKind.IsNot
                    Return SyntaxKind.IsNotKeyword

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(op)
            End Select
        End Function

        Friend Shared Function TryGetOperatorName(op As BinaryOperatorKind) As String

            Select Case (op And BinaryOperatorKind.OpMask)
                Case BinaryOperatorKind.Add
                    Return WellKnownMemberNames.AdditionOperatorName
                Case BinaryOperatorKind.Concatenate
                    Return WellKnownMemberNames.ConcatenateOperatorName
                Case BinaryOperatorKind.Like
                    Return WellKnownMemberNames.LikeOperatorName
                Case BinaryOperatorKind.Equals
                    Return WellKnownMemberNames.EqualityOperatorName
                Case BinaryOperatorKind.NotEquals
                    Return WellKnownMemberNames.InequalityOperatorName
                Case BinaryOperatorKind.LessThanOrEqual
                    Return WellKnownMemberNames.LessThanOrEqualOperatorName
                Case BinaryOperatorKind.GreaterThanOrEqual
                    Return WellKnownMemberNames.GreaterThanOrEqualOperatorName
                Case BinaryOperatorKind.LessThan
                    Return WellKnownMemberNames.LessThanOperatorName
                Case BinaryOperatorKind.GreaterThan
                    Return WellKnownMemberNames.GreaterThanOperatorName
                Case BinaryOperatorKind.Subtract
                    Return WellKnownMemberNames.SubtractionOperatorName
                Case BinaryOperatorKind.Multiply
                    Return WellKnownMemberNames.MultiplyOperatorName
                Case BinaryOperatorKind.Power
                    Return WellKnownMemberNames.ExponentOperatorName
                Case BinaryOperatorKind.Divide
                    Return WellKnownMemberNames.DivisionOperatorName
                Case BinaryOperatorKind.Modulo
                    Return WellKnownMemberNames.ModulusOperatorName
                Case BinaryOperatorKind.IntegerDivide
                    Return WellKnownMemberNames.IntegerDivisionOperatorName
                Case BinaryOperatorKind.LeftShift
                    Return WellKnownMemberNames.LeftShiftOperatorName
                Case BinaryOperatorKind.RightShift
                    Return WellKnownMemberNames.RightShiftOperatorName
                Case BinaryOperatorKind.Xor
                    Return WellKnownMemberNames.ExclusiveOrOperatorName
                Case BinaryOperatorKind.Or
                    Return WellKnownMemberNames.BitwiseOrOperatorName
                Case BinaryOperatorKind.And
                    Return WellKnownMemberNames.BitwiseAndOperatorName

                Case Else
                    Return Nothing
                    'Case BinaryOperatorKind.OrElse
                    'Case BinaryOperatorKind.AndAlso
                    'Case BinaryOperatorKind.Is
                    'Case BinaryOperatorKind.IsNot
            End Select
        End Function

        Friend Shared Function TryGetOperatorName(op As UnaryOperatorKind) As String

            Select Case (op And UnaryOperatorKind.OpMask)
                Case UnaryOperatorKind.Plus
                    Return WellKnownMemberNames.UnaryPlusOperatorName
                Case UnaryOperatorKind.Minus
                    Return WellKnownMemberNames.UnaryNegationOperatorName
                Case UnaryOperatorKind.Not
                    Return WellKnownMemberNames.OnesComplementOperatorName
                Case UnaryOperatorKind.Implicit
                    Return WellKnownMemberNames.ImplicitConversionName
                Case UnaryOperatorKind.Explicit
                    Return WellKnownMemberNames.ExplicitConversionName
                Case UnaryOperatorKind.IsTrue
                    Return WellKnownMemberNames.TrueOperatorName
                Case UnaryOperatorKind.IsFalse
                    Return WellKnownMemberNames.FalseOperatorName

                Case Else
                    Return Nothing
            End Select
        End Function

        Friend Shared Function ValidateOverloadedOperator(
            method As MethodSymbol,
            opInfo As OperatorInfo,
            Optional diagnosticsOpt As DiagnosticBag = Nothing
        ) As Boolean
            Debug.Assert(method.IsMethodKindBasedOnSyntax OrElse diagnosticsOpt Is Nothing)
            Debug.Assert(opInfo.ParamCount <> 0)

            If method.ParameterCount <> opInfo.ParamCount Then
                Return False
            End If

            Dim result As Boolean = True
            Dim containingType As NamedTypeSymbol = method.ContainingType
            Dim targetsContainingType As Boolean = False
            Dim targetMismatchError As ERRID
            Dim isConversion As Boolean = False

            If opInfo.IsUnary Then
                Select Case opInfo.UnaryOperatorKind
                    Case UnaryOperatorKind.Implicit, UnaryOperatorKind.Explicit
                        isConversion = True
                        targetMismatchError = ERRID.ERR_ConvParamMustBeContainingType1
                        If OverloadedOperatorTargetsContainingType(containingType, method.ReturnType) Then
                            targetsContainingType = True
                        End If
                    Case Else
                        targetMismatchError = ERRID.ERR_UnaryParamMustBeContainingType1

                        If Not method.ReturnType.IsBooleanType() Then
                            Select Case opInfo.UnaryOperatorKind
                                Case UnaryOperatorKind.IsTrue
                                    If diagnosticsOpt IsNot Nothing Then
                                        diagnosticsOpt.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OperatorRequiresBoolReturnType1, SyntaxFacts.GetText(SyntaxKind.IsTrueKeyword)), method.Locations(0))
                                        result = False
                                    Else
                                        Return False
                                    End If
                                Case UnaryOperatorKind.IsFalse
                                    If diagnosticsOpt IsNot Nothing Then
                                        diagnosticsOpt.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OperatorRequiresBoolReturnType1, SyntaxFacts.GetText(SyntaxKind.IsFalseKeyword)), method.Locations(0))
                                        result = False
                                    Else
                                        Return False
                                    End If
                            End Select
                        End If

                End Select

            Else
                Debug.Assert(opInfo.IsBinary)
                targetMismatchError = ERRID.ERR_BinaryParamMustBeContainingType1

                Select Case opInfo.BinaryOperatorKind
                    Case BinaryOperatorKind.LeftShift, BinaryOperatorKind.RightShift
                        If method.Parameters(1).Type.GetNullableUnderlyingTypeOrSelf().SpecialType <> Microsoft.CodeAnalysis.SpecialType.System_Int32 Then
                            If diagnosticsOpt IsNot Nothing Then
                                diagnosticsOpt.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OperatorRequiresIntegerParameter1,
                                                                       SyntaxFacts.GetText(If(opInfo.BinaryOperatorKind = BinaryOperatorKind.LeftShift,
                                                                                              SyntaxKind.LessThanLessThanToken,
                                                                                              SyntaxKind.GreaterThanGreaterThanToken))), method.Locations(0))
                                result = False
                            Else
                                Return False
                            End If
                        End If
                End Select
            End If

            For Each param In method.Parameters
                If OverloadedOperatorTargetsContainingType(containingType, param.Type) Then
                    targetsContainingType = True
                End If

                If param.IsByRef Then
                    If diagnosticsOpt IsNot Nothing Then
                        ' Diagnostic has been reported when we were interpreting parameter's modifiers.
                        Debug.Assert(Not method.IsMethodKindBasedOnSyntax)
                        result = False
                    Else
                        Return False
                    End If
                End If
            Next

            If Not targetsContainingType Then
                If diagnosticsOpt IsNot Nothing Then
                    diagnosticsOpt.Add(ErrorFactory.ErrorInfo(targetMismatchError, method.ContainingSymbol), method.Locations(0))
                    result = False
                Else
                    Return False
                End If
            ElseIf isConversion Then
                ' Conversion operators specific checks
                Dim sourceType As TypeSymbol = method.Parameters(0).Type
                Dim targetType As TypeSymbol = method.ReturnType

                If sourceType.IsObjectType() Then
                    If diagnosticsOpt IsNot Nothing Then
                        diagnosticsOpt.Add(ErrorFactory.ErrorInfo(ERRID.ERR_ConversionFromObject), method.Locations(0))
                        result = False
                    Else
                        Return False
                    End If
                ElseIf targetType.IsObjectType() Then
                    If diagnosticsOpt IsNot Nothing Then
                        diagnosticsOpt.Add(ErrorFactory.ErrorInfo(ERRID.ERR_ConversionToObject), method.Locations(0))
                        result = False
                    Else
                        Return False
                    End If
                ElseIf sourceType.IsInterfaceType() Then
                    If diagnosticsOpt IsNot Nothing Then
                        diagnosticsOpt.Add(ErrorFactory.ErrorInfo(ERRID.ERR_ConversionFromInterfaceType), method.Locations(0))
                        result = False
                    Else
                        Return False
                    End If
                ElseIf targetType.IsInterfaceType() Then
                    If diagnosticsOpt IsNot Nothing Then
                        diagnosticsOpt.Add(ErrorFactory.ErrorInfo(ERRID.ERR_ConversionToInterfaceType), method.Locations(0))
                        result = False
                    Else
                        Return False
                    End If
                ElseIf If(containingType.SpecialType = SpecialType.System_Nullable_T,
                          sourceType Is targetType,
                          sourceType.GetNullableUnderlyingTypeOrSelf() Is targetType.GetNullableUnderlyingTypeOrSelf()) Then
                    If diagnosticsOpt IsNot Nothing Then
                        diagnosticsOpt.Add(ErrorFactory.ErrorInfo(ERRID.ERR_ConversionToSameType), method.Locations(0))
                        result = False
                    Else
                        Return False
                    End If
                ElseIf (sourceType.Kind = SymbolKind.NamedType OrElse sourceType.Kind = SymbolKind.TypeParameter) AndAlso
                       (targetType.Kind = SymbolKind.NamedType OrElse targetType.Kind = SymbolKind.TypeParameter) Then
                    If Conversions.HasWideningDirectCastConversionButNotEnumTypeConversion(targetType, sourceType, Nothing) Then
                        If diagnosticsOpt IsNot Nothing Then
                            diagnosticsOpt.Add(ErrorFactory.ErrorInfo(If(targetType Is method.ContainingSymbol,
                                                                      ERRID.ERR_ConversionFromBaseType,
                                                                      ERRID.ERR_ConversionToDerivedType)), method.Locations(0))
                            result = False
                        Else
                            Return False
                        End If
                    ElseIf Conversions.HasWideningDirectCastConversionButNotEnumTypeConversion(sourceType, targetType, Nothing) Then
                        If diagnosticsOpt IsNot Nothing Then
                            diagnosticsOpt.Add(ErrorFactory.ErrorInfo(If(targetType Is method.ContainingSymbol,
                                                                      ERRID.ERR_ConversionFromDerivedType,
                                                                      ERRID.ERR_ConversionToBaseType)), method.Locations(0))
                            result = False
                        Else
                            Return False
                        End If
                    End If
                End If
            End If

            Return result
        End Function

        Private Shared Function OverloadedOperatorTargetsContainingType(containingType As NamedTypeSymbol, typeFromSignature As TypeSymbol) As Boolean
            If containingType.SpecialType = SpecialType.System_Nullable_T Then
                Return typeFromSignature Is containingType
            Else
                Return TypeSymbol.Equals(typeFromSignature.GetNullableUnderlyingTypeOrSelf().GetTupleUnderlyingTypeOrSelf(), containingType.GetTupleUnderlyingTypeOrSelf(), TypeCompareKind.ConsiderEverything)
            End If
        End Function

        Public Shared Function MapUnaryOperatorKind(opCode As SyntaxKind) As UnaryOperatorKind
            Dim result As UnaryOperatorKind

            Select Case opCode
                Case SyntaxKind.UnaryPlusExpression
                    result = UnaryOperatorKind.Plus
                Case SyntaxKind.UnaryMinusExpression
                    result = UnaryOperatorKind.Minus
                Case SyntaxKind.NotExpression
                    result = UnaryOperatorKind.Not
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opCode)
            End Select

            Return result
        End Function

        ''' <summary>
        ''' Returns UnaryOperatorKind.Error in case of error, otherwise adjusted operator kind.
        ''' </summary>
        Public Shared Function ResolveUnaryOperator(
            opCode As UnaryOperatorKind,
            operand As BoundExpression,
            binder As Binder,
            <Out()> ByRef intrinsicOperatorType As SpecialType,
            <Out()> ByRef userDefinedOperator As OverloadResolutionResult,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As UnaryOperatorKind
            Debug.Assert((opCode And UnaryOperatorKind.IntrinsicOpMask) = opCode AndAlso opCode <> UnaryOperatorKind.Error)

            opCode = (opCode And UnaryOperatorKind.IntrinsicOpMask)
            intrinsicOperatorType = SpecialType.None
            userDefinedOperator = Nothing

            Dim operandType = operand.Type

            ' First, dig through Nullable
            Dim nullableUnderlying = operandType.GetNullableUnderlyingTypeOrSelf()
            Dim operandIsNullable = (operandType IsNot nullableUnderlying)

            ' Now dig through Enum
            Dim enumUnderlying = nullableUnderlying.GetEnumUnderlyingTypeOrSelf()
            Dim operandIsEnum = (enumUnderlying IsNot nullableUnderlying)

            ' Filter out unexpected underlying types for Nullable and enum types
            If (operandIsEnum OrElse operandIsNullable) AndAlso
                (enumUnderlying.IsStringType() OrElse enumUnderlying.IsObjectType()) Then
                Return UnaryOperatorKind.Error
            End If

            Dim sourceType = enumUnderlying

            If sourceType.SpecialType <> SpecialType.System_Object AndAlso
               Not sourceType.IsIntrinsicType() Then

                If operandType.CanContainUserDefinedOperators(useSiteDiagnostics) Then
                    userDefinedOperator = ResolveUserDefinedUnaryOperator(operand, opCode, binder, includeEliminatedCandidates:=False, useSiteDiagnostics:=useSiteDiagnostics)

                    If Not userDefinedOperator.BestResult.HasValue AndAlso userDefinedOperator.Candidates.Length = 0 Then
                        userDefinedOperator = ResolveUserDefinedUnaryOperator(operand, opCode, binder, includeEliminatedCandidates:=True, useSiteDiagnostics:=useSiteDiagnostics)

                        If userDefinedOperator.Candidates.Length = 0 Then
                            Return UnaryOperatorKind.Error
                        End If
                    End If

                    Return UnaryOperatorKind.UserDefined
                End If

                Return UnaryOperatorKind.Error
            End If

            Dim result As UnaryOperatorKind = UnaryOperatorKind.Error

            If operandIsEnum AndAlso opCode = UnaryOperatorKind.Not AndAlso sourceType.IsIntegralType() Then
                '§11.17 Logical Operators
                'The enumerated type operators do the bitwise operation on the underlying type of 
                'the enumerated type, but the return value is the enumerated type.
                result = UnaryOperatorKind.Not
            Else
                intrinsicOperatorType = ResolveNotLiftedIntrinsicUnaryOperator(opCode, sourceType.SpecialType)

                If intrinsicOperatorType <> SpecialType.None Then
                    result = opCode
                End If
            End If

            If result <> UnaryOperatorKind.Error AndAlso operandIsNullable Then
                result = result Or UnaryOperatorKind.Lifted
            End If

            Return result
        End Function

        ''' <summary>
        ''' Returns result type of the operator or SpecialType.None if operator is not supported.
        ''' </summary>
        Friend Shared Function ResolveNotLiftedIntrinsicUnaryOperator(
            opCode As UnaryOperatorKind,
            operandSpecialType As SpecialType
        ) As SpecialType

            Dim intrinsicOperatorType As SpecialType

            Select Case opCode
                Case UnaryOperatorKind.Not

                    '§11.17 Logical Operators
                    'Bo	SB	By	Sh	US	In	UI	Lo	UL	De	Si	Do	Da	Ch	St	Ob
                    'Bo	SB	By	Sh	US	In	UI	Lo	UL	Lo	Lo	Lo	Err	Err	Lo	Ob

                    Select Case operandSpecialType
                        Case SpecialType.System_Boolean,
                             SpecialType.System_SByte,
                             SpecialType.System_Byte,
                             SpecialType.System_Int16,
                             SpecialType.System_UInt16,
                             SpecialType.System_Int32,
                             SpecialType.System_UInt32,
                             SpecialType.System_Int64,
                             SpecialType.System_UInt64,
                             SpecialType.System_Object

                            intrinsicOperatorType = operandSpecialType

                        Case SpecialType.System_Decimal,
                             SpecialType.System_Single,
                             SpecialType.System_Double,
                             SpecialType.System_String

                            intrinsicOperatorType = SpecialType.System_Int64

                        Case Else
                            intrinsicOperatorType = SpecialType.None
                    End Select

                Case UnaryOperatorKind.Plus

                    '§11.13.1 Unary Plus Operator
                    'Bo	SB	By	Sh	US	In	UI	Lo	UL	De	Si	Do	Da	Ch	St	Ob
                    'Sh	SB	By	Sh	US	In	UI	Lo	UL	De	Si	Do	Err	Err	Do	Ob

                    Select Case operandSpecialType
                        Case SpecialType.System_Boolean
                            intrinsicOperatorType = SpecialType.System_Int16

                        Case SpecialType.System_SByte,
                             SpecialType.System_Byte,
                             SpecialType.System_Int16,
                             SpecialType.System_UInt16,
                             SpecialType.System_Int32,
                             SpecialType.System_UInt32,
                             SpecialType.System_Int64,
                             SpecialType.System_UInt64,
                             SpecialType.System_Decimal,
                             SpecialType.System_Single,
                             SpecialType.System_Double,
                             SpecialType.System_Object

                            intrinsicOperatorType = operandSpecialType

                        Case SpecialType.System_String
                            intrinsicOperatorType = SpecialType.System_Double

                        Case Else
                            intrinsicOperatorType = SpecialType.None
                    End Select

                Case UnaryOperatorKind.Minus

                    '§11.13.2 Unary Minus Operator
                    'Bo	SB	By	Sh	US	In	UI	Lo	UL	De	Si	Do	Da	Ch	St	Ob
                    'Sh	SB	Sh	Sh	In	In	Lo	Lo	De	De	Si	Do	Err	Err	Do	Ob

                    Select Case operandSpecialType
                        Case SpecialType.System_Boolean,
                             SpecialType.System_Byte

                            intrinsicOperatorType = SpecialType.System_Int16

                        Case SpecialType.System_SByte,
                             SpecialType.System_Int16,
                             SpecialType.System_Int32,
                             SpecialType.System_Int64,
                             SpecialType.System_Decimal,
                             SpecialType.System_Single,
                             SpecialType.System_Double,
                             SpecialType.System_Object

                            intrinsicOperatorType = operandSpecialType

                        Case SpecialType.System_UInt16
                            intrinsicOperatorType = SpecialType.System_Int32

                        Case SpecialType.System_UInt32
                            intrinsicOperatorType = SpecialType.System_Int64

                        Case SpecialType.System_UInt64
                            intrinsicOperatorType = SpecialType.System_Decimal

                        Case SpecialType.System_String
                            intrinsicOperatorType = SpecialType.System_Double

                        Case Else
                            intrinsicOperatorType = SpecialType.None
                    End Select

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opCode)
            End Select

            Return intrinsicOperatorType
        End Function

        ''' <summary>
        ''' Attempts to fold unary operator applied to a constant expression. 
        ''' 
        ''' Returns Nothing if operator cannot be folded.
        ''' 
        ''' If folding failed due to non-integer overflow, ConstantValue.Bad is returned. Consumer 
        ''' is responsible for reporting appropriate diagnostics.
        ''' 
        ''' If integer overflow occurs, integerOverflow is set to True and ConstantValue for overflowed result is returned. 
        ''' Consumer is responsible for reporting appropriate diagnostics and potentially discarding the result.
        ''' </summary>
        Public Shared Function TryFoldConstantUnaryOperator(
            op As UnaryOperatorKind,
            operand As BoundExpression,
            resultType As TypeSymbol,
            ByRef integerOverflow As Boolean
        ) As ConstantValue

            Debug.Assert(operand IsNot Nothing)
            Debug.Assert(resultType IsNot Nothing)

            integerOverflow = False

            Dim operandValue As ConstantValue = operand.ConstantValueOpt

            If operandValue Is Nothing OrElse operandValue.IsBad OrElse resultType.IsErrorType() Then
                ' Not a constant
                Return Nothing
            End If

            Dim operandType = operand.Type
            Dim result As ConstantValue = Nothing
            Dim underlyingResultType = resultType.GetEnumUnderlyingTypeOrSelf()

            If operandType.AllowsCompileTimeOperations() AndAlso
               underlyingResultType.AllowsCompileTimeOperations() Then

                Debug.Assert(underlyingResultType.IsValidForConstantValue(operandValue))

                ' Attempt folding
                If underlyingResultType.IsIntegralType() Then
                    Dim value As Int64 = GetConstantValueAsInt64(operandValue)

                    Select Case (op And UnaryOperatorKind.IntrinsicOpMask)
                        Case UnaryOperatorKind.Plus
                            ' Nothing to do
                        Case UnaryOperatorKind.Minus
                            Debug.Assert(Not underlyingResultType.IsUnsignedIntegralType())
                            If value = Int64.MinValue Then
                                integerOverflow = True
                            End If

                            value = -value

                        Case UnaryOperatorKind.Not
                            value = Not value

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(op)
                    End Select

                    Dim detectedIntegerOverflow = integerOverflow
                    Dim discriminator = underlyingResultType.GetConstantValueTypeDiscriminator()

                    result = GetConstantValue(discriminator, NarrowIntegralResult(value, discriminator, discriminator, integerOverflow))

                    integerOverflow = (op And UnaryOperatorKind.IntrinsicOpMask) = UnaryOperatorKind.Minus AndAlso (integerOverflow OrElse detectedIntegerOverflow)

                ElseIf underlyingResultType.IsFloatingType Then
                    Dim value As Double = If(underlyingResultType.IsSingleType(), operandValue.SingleValue, operandValue.DoubleValue)

                    Select Case (op And UnaryOperatorKind.IntrinsicOpMask)
                        Case UnaryOperatorKind.Plus
                            ' Nothing to do
                        Case UnaryOperatorKind.Minus
                            value = -value

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(op)
                    End Select

                    Dim overflow As Boolean = False
                    value = NarrowFloatingResult(value, underlyingResultType.GetConstantValueTypeDiscriminator(), overflow)

                    ' We have decided to ignore overflows in compile-time evaluation
                    ' of floating expressions.

                    If underlyingResultType.IsSingleType() Then
                        result = ConstantValue.Create(CType(value, Single))
                    Else
                        result = ConstantValue.Create(value)
                    End If

                ElseIf underlyingResultType.IsDecimalType() Then
                    Dim value As Decimal = operandValue.DecimalValue

                    Select Case (op And UnaryOperatorKind.IntrinsicOpMask)
                        Case UnaryOperatorKind.Plus
                            ' Nothing to do
                        Case UnaryOperatorKind.Minus
                            value = Decimal.Negate(value)

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(op)
                    End Select

                    result = ConstantValue.Create(value)

                ElseIf underlyingResultType.IsBooleanType() Then

                    Debug.Assert((op And UnaryOperatorKind.IntrinsicOpMask) = UnaryOperatorKind.Not)
                    result = ConstantValue.Create(Not operandValue.BooleanValue)
                End If

            End If

            Return result
        End Function

        Public Shared Function MapBinaryOperatorKind(opCode As SyntaxKind) As BinaryOperatorKind
            Dim result As BinaryOperatorKind

            Select Case opCode
                Case SyntaxKind.AddExpression : result = BinaryOperatorKind.Add
                Case SyntaxKind.ConcatenateExpression : result = BinaryOperatorKind.Concatenate
                Case SyntaxKind.LikeExpression : result = BinaryOperatorKind.Like
                Case SyntaxKind.EqualsExpression : result = BinaryOperatorKind.Equals
                Case SyntaxKind.NotEqualsExpression : result = BinaryOperatorKind.NotEquals
                Case SyntaxKind.LessThanOrEqualExpression : result = BinaryOperatorKind.LessThanOrEqual
                Case SyntaxKind.GreaterThanOrEqualExpression : result = BinaryOperatorKind.GreaterThanOrEqual
                Case SyntaxKind.LessThanExpression : result = BinaryOperatorKind.LessThan
                Case SyntaxKind.GreaterThanExpression : result = BinaryOperatorKind.GreaterThan
                Case SyntaxKind.SubtractExpression : result = BinaryOperatorKind.Subtract
                Case SyntaxKind.MultiplyExpression : result = BinaryOperatorKind.Multiply
                Case SyntaxKind.ExponentiateExpression : result = BinaryOperatorKind.Power
                Case SyntaxKind.DivideExpression : result = BinaryOperatorKind.Divide
                Case SyntaxKind.ModuloExpression : result = BinaryOperatorKind.Modulo
                Case SyntaxKind.IntegerDivideExpression : result = BinaryOperatorKind.IntegerDivide
                Case SyntaxKind.LeftShiftExpression : result = BinaryOperatorKind.LeftShift
                Case SyntaxKind.RightShiftExpression : result = BinaryOperatorKind.RightShift
                Case SyntaxKind.ExclusiveOrExpression : result = BinaryOperatorKind.Xor
                Case SyntaxKind.OrExpression : result = BinaryOperatorKind.Or
                Case SyntaxKind.OrElseExpression : result = BinaryOperatorKind.OrElse
                Case SyntaxKind.AndExpression : result = BinaryOperatorKind.And
                Case SyntaxKind.AndAlsoExpression : result = BinaryOperatorKind.AndAlso
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opCode)
            End Select

            Return result
        End Function

        ''' <summary>
        ''' Returns UnaryOperatorKind.Error in case of error, otherwise adjusted operator kind.
        ''' 
        ''' For comparison operators, the operator type computed here is not
        ''' the result type of the comparison (which is typically boolean),
        ''' but is the type to which the operands are to be converted. For
        ''' other operators, the type computed here is both the result type
        ''' and the common operand type.
        ''' </summary>
        Public Shared Function ResolveBinaryOperator(
            opCode As BinaryOperatorKind,
            left As BoundExpression,
            right As BoundExpression,
            binder As Binder,
            considerUserDefinedOrLateBound As Boolean,
            <Out()> ByRef intrinsicOperatorType As SpecialType,
            <Out()> ByRef userDefinedOperator As OverloadResolutionResult,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As BinaryOperatorKind

            Debug.Assert((opCode And BinaryOperatorKind.OpMask) = opCode AndAlso opCode <> BinaryOperatorKind.Error)

            opCode = (opCode And BinaryOperatorKind.OpMask)
            intrinsicOperatorType = SpecialType.None
            userDefinedOperator = Nothing

            Dim leftType As TypeSymbol = left.Type
            Dim rightType As TypeSymbol = right.Type

            ' First, dig through Nullable
            Dim leftNullableUnderlying = leftType.GetNullableUnderlyingTypeOrSelf()
            Dim leftIsNullable = (leftType IsNot leftNullableUnderlying)
            Dim rightNullableUnderlying = rightType.GetNullableUnderlyingTypeOrSelf()
            Dim rightIsNullable = (rightType IsNot rightNullableUnderlying)

            ' Now dig through Enum
            Dim leftEnumUnderlying = leftNullableUnderlying.GetEnumUnderlyingTypeOrSelf()
            Dim leftIsEnum = (leftEnumUnderlying IsNot leftNullableUnderlying)
            Dim rightEnumUnderlying = rightNullableUnderlying.GetEnumUnderlyingTypeOrSelf()
            Dim rightIsEnum = (rightEnumUnderlying IsNot rightNullableUnderlying)

            ' Filter out unexpected underlying types for Nullable and enum types
            If ((leftIsEnum OrElse leftIsNullable) AndAlso
                (leftEnumUnderlying.IsStringType() OrElse leftEnumUnderlying.IsObjectType() OrElse leftEnumUnderlying.IsCharSZArray())) OrElse
               ((rightIsEnum OrElse rightIsNullable) AndAlso
                (rightEnumUnderlying.IsStringType() OrElse rightEnumUnderlying.IsObjectType() OrElse rightEnumUnderlying.IsCharSZArray())) Then
                Return BinaryOperatorKind.Error
            End If

            If UseUserDefinedBinaryOperators(opCode, leftType, rightType) Then

                If considerUserDefinedOrLateBound Then
                    If leftType.CanContainUserDefinedOperators(useSiteDiagnostics) OrElse rightType.CanContainUserDefinedOperators(useSiteDiagnostics) OrElse
                       (opCode = BinaryOperatorKind.Subtract AndAlso
                        leftType.GetNullableUnderlyingTypeOrSelf().IsDateTimeType() AndAlso
                        rightType.GetNullableUnderlyingTypeOrSelf().IsDateTimeType()) Then ' Let (Date - Date) use operator overloading.

                        userDefinedOperator = ResolveUserDefinedBinaryOperator(left, right, opCode, binder, includeEliminatedCandidates:=False, useSiteDiagnostics:=useSiteDiagnostics)

                        If userDefinedOperator.ResolutionIsLateBound Then
                            intrinsicOperatorType = SpecialType.System_Object
                            Return opCode
                        ElseIf Not userDefinedOperator.BestResult.HasValue AndAlso userDefinedOperator.Candidates.Length = 0 Then
                            userDefinedOperator = ResolveUserDefinedBinaryOperator(left, right, opCode, binder, includeEliminatedCandidates:=True, useSiteDiagnostics:=Nothing)

                            If userDefinedOperator.Candidates.Length = 0 Then
                                Return BinaryOperatorKind.Error
                            End If
                        End If

                        Return BinaryOperatorKind.UserDefined
                    Else
                        ' An operator with an Object operand and a Type Parameter operand
                        ' is latebound if the Type Parameter has no class constraint.
                        Dim latebound As Boolean = False
                        If leftType.IsObjectType() Then
                            If rightType.IsTypeParameter() AndAlso DirectCast(rightType, TypeParameterSymbol).GetNonInterfaceConstraint(useSiteDiagnostics) Is Nothing Then
                                latebound = True
                            End If
                        ElseIf rightType.IsObjectType() Then
                            If leftType.IsTypeParameter() AndAlso DirectCast(leftType, TypeParameterSymbol).GetNonInterfaceConstraint(useSiteDiagnostics) Is Nothing Then
                                latebound = True
                            End If
                        End If

                        If latebound Then
                            intrinsicOperatorType = SpecialType.System_Object
                            Return opCode
                        End If
                    End If
                End If

                Return BinaryOperatorKind.Error
            End If

            Dim result As BinaryOperatorKind = BinaryOperatorKind.Error

            If leftIsEnum AndAlso rightIsEnum AndAlso
               (opCode = BinaryOperatorKind.Xor OrElse opCode = BinaryOperatorKind.And OrElse opCode = BinaryOperatorKind.Or) AndAlso
               leftNullableUnderlying.IsSameTypeIgnoringAll(rightNullableUnderlying) Then
                '§11.17 Logical Operators
                'The enumerated type operators do the bitwise operation on the underlying 
                'type of the enumerated type, but the return value is the enumerated type.
                result = opCode

                If leftIsNullable OrElse rightIsNullable Then
                    result = result Or BinaryOperatorKind.Lifted
                End If
            Else
                Dim leftSpecialType = leftEnumUnderlying.SpecialType
                Dim rightSpecialType = rightEnumUnderlying.SpecialType

                ' Operands of type 1-dimensional array of Char are treated as if they
                ' were of type String.
                If leftSpecialType = SpecialType.None AndAlso leftEnumUnderlying.IsCharSZArray() Then
                    leftSpecialType = SpecialType.System_String
                End If

                If rightSpecialType = SpecialType.None AndAlso rightEnumUnderlying.IsCharSZArray() Then
                    rightSpecialType = SpecialType.System_String
                End If

                intrinsicOperatorType = ResolveNotLiftedIntrinsicBinaryOperator(opCode, leftSpecialType, rightSpecialType)

                If intrinsicOperatorType <> SpecialType.None Then
                    result = opCode
                End If

                ' Like and '&' don't have lifted form.
                ' Disallow nullable lifting if result is not a value type
                If result <> BinaryOperatorKind.Error AndAlso
                   (leftIsNullable OrElse rightIsNullable) AndAlso
                   intrinsicOperatorType <> SpecialType.None AndAlso
                   intrinsicOperatorType <> SpecialType.System_String AndAlso
                   intrinsicOperatorType <> SpecialType.System_Object AndAlso
                    opCode <> BinaryOperatorKind.Concatenate AndAlso opCode <> BinaryOperatorKind.Like Then
                    result = result Or BinaryOperatorKind.Lifted
                End If
            End If

            Return result
        End Function

        Public Shared Function UseUserDefinedBinaryOperators(opCode As BinaryOperatorKind, leftType As TypeSymbol, rightType As TypeSymbol) As Boolean
            Dim leftEnumUnderlying = leftType.GetNullableUnderlyingTypeOrSelf().GetEnumUnderlyingTypeOrSelf()
            Dim rightEnumUnderlying = rightType.GetNullableUnderlyingTypeOrSelf().GetEnumUnderlyingTypeOrSelf()

            ' Operands of type 1-dimensional array of Char are treated as if they
            ' were of type String.
            If (leftEnumUnderlying.SpecialType <> SpecialType.System_Object AndAlso
                   Not leftEnumUnderlying.IsIntrinsicType() AndAlso
                   Not leftEnumUnderlying.IsCharSZArray()) OrElse
               (rightEnumUnderlying.SpecialType <> SpecialType.System_Object AndAlso
                   Not rightEnumUnderlying.IsIntrinsicType() AndAlso
                   Not rightEnumUnderlying.IsCharSZArray()) OrElse
               (leftEnumUnderlying.IsDateTimeType() AndAlso rightEnumUnderlying.IsDateTimeType() AndAlso
                   opCode = BinaryOperatorKind.Subtract) Then ' Let (Date - Date) use operator overloading.
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Attempts to fold binary operator applied to constant expressions. 
        ''' 
        ''' Returns Nothing if operator cannot be folded.
        ''' 
        ''' If folding failed due to non-integer overflow or divide by zero, 
        ''' ConstantValue.Bad is returned. Consumer is responsible for reporting appropriate diagnostics.
        ''' 
        ''' If divide by zero occurs, divideByZero is set to True.
        ''' 
        ''' If integer overflow occurs, integerOverflow is set to True and ConstantValue for overflowed result is returned. 
        ''' Consumer is responsible for reporting appropriate diagnostics and potentially discarding the result.
        ''' </summary>
        Public Shared Function TryFoldConstantBinaryOperator(
            operatorKind As BinaryOperatorKind,
            left As BoundExpression,
            right As BoundExpression,
            resultType As TypeSymbol,
            ByRef integerOverflow As Boolean,
            ByRef divideByZero As Boolean,
            ByRef lengthOutOfLimit As Boolean
        ) As ConstantValue

            Debug.Assert(left IsNot Nothing)
            Debug.Assert(right IsNot Nothing)
            Debug.Assert(resultType IsNot Nothing)

            integerOverflow = False
            divideByZero = False
            lengthOutOfLimit = False

            Dim leftConstantValue As ConstantValue = left.ConstantValueOpt
            Dim rightConstantValue As ConstantValue = right.ConstantValueOpt

            If leftConstantValue Is Nothing OrElse leftConstantValue.IsBad OrElse
               rightConstantValue Is Nothing OrElse rightConstantValue.IsBad OrElse
               resultType.IsErrorType() Then
                ' Not a constant
                Return Nothing
            End If

            Dim leftType = left.Type
            Dim rightType = right.Type
            Dim op As BinaryOperatorKind = (operatorKind And BinaryOperatorKind.OpMask)

            Dim result As ConstantValue = Nothing

            If op <> BinaryOperatorKind.Like AndAlso
                (operatorKind And BinaryOperatorKind.CompareText) = 0 AndAlso
                leftType.AllowsCompileTimeOperations() AndAlso
                rightType.AllowsCompileTimeOperations() AndAlso
                resultType.AllowsCompileTimeOperations() Then

                Debug.Assert(leftType.IsSameTypeIgnoringAll(rightType) OrElse
                             op = BinaryOperatorKind.LeftShift OrElse op = BinaryOperatorKind.RightShift)

                Dim leftUnderlying = leftType.GetEnumUnderlyingTypeOrSelf()
                Dim resultUnderlying = resultType.GetEnumUnderlyingTypeOrSelf()

                If leftUnderlying.IsIntegralType() OrElse leftUnderlying.IsCharType() OrElse leftUnderlying.IsDateTimeType() Then
                    result = FoldIntegralCharOrDateTimeBinaryOperator(
                                                op,
                                                leftConstantValue,
                                                rightConstantValue,
                                                leftUnderlying,
                                                resultUnderlying,
                                                integerOverflow,
                                                divideByZero)

                ElseIf leftUnderlying.IsFloatingType() Then
                    result = FoldFloatingBinaryOperator(
                                                op,
                                                leftConstantValue,
                                                rightConstantValue,
                                                leftUnderlying,
                                                resultUnderlying)

                ElseIf leftUnderlying.IsDecimalType() Then
                    result = FoldDecimalBinaryOperator(
                                                op,
                                                leftConstantValue,
                                                rightConstantValue,
                                                resultUnderlying,
                                                divideByZero)

                ElseIf leftUnderlying.IsStringType() Then
                    ' During normal compilation we never fold string comparison with 
                    ' Option Compare Text in effect. However, it looks like Dev10 goes through
                    ' this code path in EE and folds comparison regardless of Option Compare.
                    ' I am not sure if we need this in Roslyn, will ignore Option Compare Text for now.
                    Debug.Assert((operatorKind And BinaryOperatorKind.CompareText) = 0)
                    result = FoldStringBinaryOperator(
                                                op,
                                                leftConstantValue,
                                                rightConstantValue)

                    If result.IsBad Then
                        lengthOutOfLimit = True
                    End If

                ElseIf leftUnderlying.IsBooleanType() Then
                    result = FoldBooleanBinaryOperator(
                                                op,
                                                leftConstantValue,
                                                rightConstantValue)
                End If

                Debug.Assert(result IsNot Nothing)
            End If

            Return result
        End Function

        Private Shared Function FoldIntegralCharOrDateTimeBinaryOperator(
            op As BinaryOperatorKind,
            left As ConstantValue,
            right As ConstantValue,
            operandType As TypeSymbol,
            resultType As TypeSymbol,
            ByRef integerOverflow As Boolean,
            ByRef divideByZero As Boolean
        ) As ConstantValue
            Debug.Assert(operandType.IsIntegralType() OrElse operandType.IsCharType() OrElse operandType.IsDateTimeType())
            Debug.Assert((op And BinaryOperatorKind.OpMask) = op)
            Debug.Assert(Not integerOverflow)
            Debug.Assert(Not divideByZero)

            Dim result As ConstantValue

            Dim leftValue As Long = GetConstantValueAsInt64(left)
            Dim rightValue As Long = GetConstantValueAsInt64(right)

            If resultType.IsBooleanType() Then
                Dim resultValue As Boolean

                Select Case op
                    Case BinaryOperatorKind.Equals
                        resultValue = If(operandType.IsUnsignedIntegralType(),
                                         UncheckedCULng(leftValue) = UncheckedCULng(rightValue),
                                         leftValue = rightValue)

                    Case BinaryOperatorKind.NotEquals
                        resultValue = If(operandType.IsUnsignedIntegralType(),
                                         UncheckedCULng(leftValue) <> UncheckedCULng(rightValue),
                                         leftValue <> rightValue)

                    Case BinaryOperatorKind.LessThanOrEqual
                        resultValue = If(operandType.IsUnsignedIntegralType(),
                                         UncheckedCULng(leftValue) <= UncheckedCULng(rightValue),
                                         leftValue <= rightValue)

                    Case BinaryOperatorKind.GreaterThanOrEqual
                        resultValue = If(operandType.IsUnsignedIntegralType(),
                                         UncheckedCULng(leftValue) >= UncheckedCULng(rightValue),
                                         leftValue >= rightValue)

                    Case BinaryOperatorKind.LessThan
                        resultValue = If(operandType.IsUnsignedIntegralType(),
                                         UncheckedCULng(leftValue) < UncheckedCULng(rightValue),
                                         leftValue < rightValue)

                    Case BinaryOperatorKind.GreaterThan
                        resultValue = If(operandType.IsUnsignedIntegralType(),
                                         UncheckedCULng(leftValue) > UncheckedCULng(rightValue),
                                         leftValue > rightValue)
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(op)
                End Select

                result = ConstantValue.Create(resultValue)

            Else
                ' Compute the result in 64-bit arithmetic, and determine if the
                ' operation overflows the result type.
                Dim resultValue As Long
                Dim leftDiscriminator = operandType.GetConstantValueTypeDiscriminator()
                Dim resultDiscriminator = resultType.GetConstantValueTypeDiscriminator()

                Select Case op
                    Case BinaryOperatorKind.Add
                        resultValue = NarrowIntegralResult(leftValue + rightValue, leftDiscriminator, resultDiscriminator, integerOverflow)

                        If Not resultType.IsUnsignedIntegralType() Then
                            If (rightValue > 0 AndAlso resultValue < leftValue) OrElse
                               (rightValue < 0 AndAlso resultValue > leftValue) Then
                                integerOverflow = True
                            End If

                        ElseIf UncheckedCULng(resultValue) < UncheckedCULng(leftValue) Then
                            integerOverflow = True
                        End If

                    Case BinaryOperatorKind.Subtract

                        resultValue = NarrowIntegralResult(leftValue - rightValue, leftDiscriminator, resultDiscriminator, integerOverflow)

                        If Not resultType.IsUnsignedIntegralType() Then
                            If (rightValue > 0 AndAlso resultValue > leftValue) OrElse
                               (rightValue < 0 AndAlso resultValue < leftValue) Then
                                integerOverflow = True
                            End If

                        ElseIf UncheckedCULng(resultValue) > UncheckedCULng(leftValue) Then
                            integerOverflow = True
                        End If

                    Case BinaryOperatorKind.Multiply

                        resultValue = Multiply(leftValue, rightValue, leftDiscriminator, resultDiscriminator, integerOverflow)

                    Case BinaryOperatorKind.IntegerDivide

                        If rightValue = 0 Then
                            divideByZero = True
                        Else
                            resultValue = NarrowIntegralResult(
                                If(resultType.IsUnsignedIntegralType(),
                                    UncheckedCLng(UncheckedCULng(leftValue) \ UncheckedCULng(rightValue)),
                                    UncheckedIntegralDiv(leftValue, rightValue)),
                                leftDiscriminator, resultDiscriminator, integerOverflow)

                            If Not resultType.IsUnsignedIntegralType() AndAlso leftValue = Int64.MinValue AndAlso rightValue = -1 Then
                                integerOverflow = True
                            End If
                        End If

                    Case BinaryOperatorKind.Modulo

                        If rightValue = 0 Then
                            divideByZero = True
                        Else
                            If resultType.IsUnsignedIntegralType() Then
                                resultValue = UncheckedCLng(UncheckedCULng(leftValue) Mod UncheckedCULng(rightValue))

                                ' // 64-bit processors crash on 0, -1 (Bug: dd71694)
                            ElseIf rightValue <> -1L Then
                                resultValue = leftValue Mod rightValue
                            Else
                                resultValue = 0
                            End If
                        End If

                    Case BinaryOperatorKind.Xor

                        resultValue = leftValue Xor rightValue

                    Case BinaryOperatorKind.Or

                        resultValue = leftValue Or rightValue

                    Case BinaryOperatorKind.And

                        resultValue = leftValue And rightValue

                    Case BinaryOperatorKind.LeftShift

                        resultValue = leftValue << (CType(rightValue, Integer) And CodeGen.CodeGenerator.GetShiftSizeMask(operandType))

                        ' // Round-trip the result through a cast.  We do this for two reasons:
                        ' // a) Bits may have shifted off the end and need to be stripped away.
                        ' // b) The sign bit may have changed which requires the result to be sign-extended.

                        Dim overflowTemp As Boolean = False
                        resultValue = NarrowIntegralResult(resultValue, leftDiscriminator, resultDiscriminator, overflowTemp)

                    Case BinaryOperatorKind.RightShift

                        If resultType.IsUnsignedIntegralType() Then
                            resultValue = UncheckedCLng((UncheckedCULng(leftValue) >>
                                                         (CType(rightValue, Integer) And CodeGen.CodeGenerator.GetShiftSizeMask(operandType))))
                        Else
                            resultValue = leftValue >> (CType(rightValue, Integer) And CodeGen.CodeGenerator.GetShiftSizeMask(operandType))
                        End If

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(op)
                End Select

                If divideByZero Then
                    result = ConstantValue.Bad
                Else
                    result = GetConstantValue(resultDiscriminator, resultValue)
                End If
            End If

            Return result
        End Function

        Private Shared Function FoldFloatingBinaryOperator(
            op As BinaryOperatorKind,
            left As ConstantValue,
            right As ConstantValue,
            operandType As TypeSymbol,
            resultType As TypeSymbol
        ) As ConstantValue
            Debug.Assert(operandType.IsFloatingType())
            Debug.Assert((op And BinaryOperatorKind.OpMask) = op)

            Dim result As ConstantValue

            Dim leftValue As Double = If(operandType.IsSingleType, left.SingleValue, left.DoubleValue)
            Dim rightValue As Double = If(operandType.IsSingleType, right.SingleValue, right.DoubleValue)

            If resultType.IsBooleanType() Then
                Dim resultValue As Boolean

                Select Case op

                    Case BinaryOperatorKind.Equals
                        resultValue = (leftValue = rightValue)

                    Case BinaryOperatorKind.NotEquals
                        resultValue = (leftValue <> rightValue)

                    Case BinaryOperatorKind.LessThanOrEqual
                        resultValue = (leftValue <= rightValue)

                    Case BinaryOperatorKind.GreaterThanOrEqual
                        resultValue = (leftValue >= rightValue)

                    Case BinaryOperatorKind.LessThan
                        resultValue = (leftValue < rightValue)

                    Case BinaryOperatorKind.GreaterThan
                        resultValue = (leftValue > rightValue)

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(op)
                End Select

                result = ConstantValue.Create(resultValue)

            Else

                ' Compute the result in 64-bit arithmetic, and determine if the
                ' operation overflows the result type.

                Dim resultValue As Double = 0

                Select Case op

                    Case BinaryOperatorKind.Add
                        resultValue = leftValue + rightValue

                    Case BinaryOperatorKind.Subtract
                        resultValue = leftValue - rightValue

                    Case BinaryOperatorKind.Multiply
                        resultValue = leftValue * rightValue

                    Case BinaryOperatorKind.Power

                        ' VSW#463059: Special case CRT changes to match CLR behavior.
                        If Double.IsInfinity(rightValue) Then
                            If leftValue.Equals(1.0) Then
                                resultValue = leftValue
                                Exit Select
                            End If

                            If leftValue.Equals(-1.0) Then
                                resultValue = Double.NaN
                                Exit Select
                            End If

                        ElseIf (
                            Double.IsNaN(rightValue)
                        ) Then
                            resultValue = Double.NaN
                            Exit Select
                        End If

                        resultValue = Math.Pow(leftValue, rightValue)

                    Case BinaryOperatorKind.Divide

                        ' We have decided not to detect zerodivide in compile-time
                        ' evaluation of floating expressions.
                        resultValue = leftValue / rightValue

                    Case BinaryOperatorKind.Modulo

                        ' We have decided not to detect zerodivide in compile-time
                        ' evaluation of floating expressions.

                        ' Note, that Math.IEEERemainder(leftValue, rightValue) might give different result.
                        ' Dev10 compiler used fmod function here and it behaves differently. It looks like
                        ' ILOpCode.Rem operation, that we use to emit Mod operator, produces result consistent 
                        ' with fmod. 
                        resultValue = leftValue Mod rightValue

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(op)
                End Select

                Dim overflow As Boolean = False
                resultValue = NarrowFloatingResult(resultValue, resultType.GetConstantValueTypeDiscriminator(), overflow)

                ' We have decided not to detect overflow in compile-time
                ' evaluation of floating expressions.

                If resultType.IsSingleType() Then
                    result = ConstantValue.Create(CType(resultValue, Single))
                Else
                    result = ConstantValue.Create(resultValue)
                End If

            End If

            Return result
        End Function

        Private Shared Function FoldDecimalBinaryOperator(
            op As BinaryOperatorKind,
            left As ConstantValue,
            right As ConstantValue,
            resultType As TypeSymbol,
            ByRef divideByZero As Boolean
        ) As ConstantValue
            Debug.Assert((op And BinaryOperatorKind.OpMask) = op)
            Debug.Assert(Not divideByZero)

            Dim result As ConstantValue

            Dim leftValue As Decimal = left.DecimalValue
            Dim rightValue As Decimal = right.DecimalValue

            If resultType.IsBooleanType() Then
                Dim resultValue As Boolean = False
                Dim comparisonResult As Integer = leftValue.CompareTo(rightValue)

                Select Case op

                    Case BinaryOperatorKind.Equals
                        resultValue = (comparisonResult = 0)

                    Case BinaryOperatorKind.NotEquals
                        resultValue = Not (comparisonResult = 0)

                    Case BinaryOperatorKind.LessThanOrEqual
                        resultValue = (comparisonResult <= 0)

                    Case BinaryOperatorKind.GreaterThanOrEqual
                        resultValue = (comparisonResult >= 0)

                    Case BinaryOperatorKind.LessThan
                        resultValue = (comparisonResult < 0)

                    Case BinaryOperatorKind.GreaterThan
                        resultValue = (comparisonResult > 0)

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(op)
                End Select

                result = ConstantValue.Create(resultValue)
            Else
                Dim resultValue As Decimal
                Dim overflow As Boolean = False

                Try
                    Select Case op
                        Case BinaryOperatorKind.Add
                            resultValue = Decimal.Add(leftValue, rightValue)

                        Case BinaryOperatorKind.Subtract
                            resultValue = Decimal.Subtract(leftValue, rightValue)

                        Case BinaryOperatorKind.Multiply
                            resultValue = Decimal.Multiply(leftValue, rightValue)

                        Case BinaryOperatorKind.Divide
                            resultValue = Decimal.Divide(leftValue, rightValue)

                        Case BinaryOperatorKind.Modulo
                            resultValue = Decimal.Remainder(leftValue, rightValue)

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(op)
                    End Select

                Catch ex As OverflowException
                    overflow = True
                Catch ex As DivideByZeroException
                    divideByZero = True
                End Try

                If overflow OrElse divideByZero Then
                    result = ConstantValue.Bad
                Else
                    result = ConstantValue.Create(resultValue)
                End If
            End If

            Return result
        End Function

        ''' <summary>
        ''' Returns ConstantValue.Bad if, and only if, compound string length is out of supported limit.
        ''' </summary>
        Private Shared Function FoldStringBinaryOperator(
            op As BinaryOperatorKind,
            left As ConstantValue,
            right As ConstantValue
        ) As ConstantValue
            Debug.Assert((op And BinaryOperatorKind.OpMask) = op)

            Dim result As ConstantValue

            Select Case op
                Case BinaryOperatorKind.Concatenate

                    Dim leftValue As Rope = If(left.IsNothing, Rope.Empty, left.RopeValue)
                    Dim rightValue As Rope = If(right.IsNothing, Rope.Empty, right.RopeValue)

                    Dim newLength = CLng(leftValue.Length) + CLng(rightValue.Length)

                    If newLength > Integer.MaxValue Then
                        Return ConstantValue.Bad
                    End If

                    Try
                        result = ConstantValue.CreateFromRope(Rope.Concat(leftValue, rightValue))
                    Catch e As System.OutOfMemoryException
                        Return ConstantValue.Bad
                    End Try

                Case BinaryOperatorKind.GreaterThan,
                     BinaryOperatorKind.LessThan,
                     BinaryOperatorKind.GreaterThanOrEqual,
                     BinaryOperatorKind.LessThanOrEqual,
                     BinaryOperatorKind.Equals,
                     BinaryOperatorKind.NotEquals

                    Dim leftValue As String = If(left.IsNothing, String.Empty, left.StringValue)
                    Dim rightValue As String = If(right.IsNothing, String.Empty, right.StringValue)

                    Dim stringComparisonSucceeds As Boolean = False

                    Dim comparisonResult As Integer = String.Compare(leftValue, rightValue, StringComparison.Ordinal)

                    Select Case op
                        Case BinaryOperatorKind.Equals
                            stringComparisonSucceeds = (comparisonResult = 0)

                        Case BinaryOperatorKind.NotEquals
                            stringComparisonSucceeds = (comparisonResult <> 0)

                        Case BinaryOperatorKind.GreaterThan
                            stringComparisonSucceeds = (comparisonResult > 0)

                        Case BinaryOperatorKind.GreaterThanOrEqual
                            stringComparisonSucceeds = (comparisonResult >= 0)

                        Case BinaryOperatorKind.LessThan
                            stringComparisonSucceeds = (comparisonResult < 0)

                        Case BinaryOperatorKind.LessThanOrEqual
                            stringComparisonSucceeds = (comparisonResult <= 0)

                    End Select

                    result = ConstantValue.Create(stringComparisonSucceeds)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(op)
            End Select

            Debug.Assert(Not result.IsBad)
            Return result
        End Function

        Private Shared Function FoldBooleanBinaryOperator(
            op As BinaryOperatorKind,
            left As ConstantValue,
            right As ConstantValue
        ) As ConstantValue
            Debug.Assert((op And BinaryOperatorKind.OpMask) = op)

            Dim result As ConstantValue

            Dim leftValue As Boolean = left.BooleanValue
            Dim rightValue As Boolean = right.BooleanValue

            Dim operationSucceeds As Boolean = False

            Select Case op

                Case BinaryOperatorKind.Equals
                    operationSucceeds = (leftValue = rightValue)

                Case BinaryOperatorKind.NotEquals
                    operationSucceeds = (leftValue <> rightValue)

                Case BinaryOperatorKind.GreaterThan
                    ' Amazingly, False > True.
                    operationSucceeds = (leftValue = False AndAlso rightValue = True)

                Case BinaryOperatorKind.GreaterThanOrEqual
                    operationSucceeds = (leftValue = False OrElse rightValue = True)

                Case BinaryOperatorKind.LessThan
                    operationSucceeds = (leftValue = True AndAlso rightValue = False)

                Case BinaryOperatorKind.LessThanOrEqual
                    operationSucceeds = (leftValue = True OrElse rightValue = False)

                Case BinaryOperatorKind.Xor
                    operationSucceeds = (leftValue Xor rightValue)

                Case BinaryOperatorKind.OrElse,
                     BinaryOperatorKind.Or

                    operationSucceeds = (leftValue OrElse rightValue)

                Case BinaryOperatorKind.AndAlso,
                     BinaryOperatorKind.And
                    operationSucceeds = (leftValue AndAlso rightValue)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(op)
            End Select

            result = ConstantValue.Create(operationSucceeds)

            Return result
        End Function

        ''' <summary>
        ''' Returns result type of the operator or SpecialType.None if operator is not supported.
        ''' </summary>
        Friend Shared Function ResolveNotLiftedIntrinsicBinaryOperator(
            opCode As BinaryOperatorKind,
            left As SpecialType,
            right As SpecialType
        ) As SpecialType

            Dim leftIndex = left.TypeToIndex()
            Dim rightIndex = right.TypeToIndex()

            If Not (leftIndex.HasValue AndAlso rightIndex.HasValue) Then
                Return SpecialType.None
            End If

            Dim tableKind As BinaryOperatorTables.TableKind

            Select Case opCode
                Case BinaryOperatorKind.Add
                    tableKind = BinaryOperatorTables.TableKind.Addition

                Case BinaryOperatorKind.Subtract,
                     BinaryOperatorKind.Multiply,
                     BinaryOperatorKind.Modulo
                    tableKind = BinaryOperatorTables.TableKind.SubtractionMultiplicationModulo

                Case BinaryOperatorKind.Divide
                    tableKind = BinaryOperatorTables.TableKind.Division

                Case BinaryOperatorKind.IntegerDivide
                    tableKind = BinaryOperatorTables.TableKind.IntegerDivision

                Case BinaryOperatorKind.Power
                    tableKind = BinaryOperatorTables.TableKind.Power

                Case BinaryOperatorKind.LeftShift,
                     BinaryOperatorKind.RightShift
                    tableKind = BinaryOperatorTables.TableKind.Shift

                Case BinaryOperatorKind.OrElse,
                     BinaryOperatorKind.AndAlso
                    tableKind = BinaryOperatorTables.TableKind.Logical

                Case BinaryOperatorKind.Concatenate,
                     BinaryOperatorKind.Like
                    tableKind = BinaryOperatorTables.TableKind.ConcatenationLike

                Case BinaryOperatorKind.Equals,
                     BinaryOperatorKind.NotEquals,
                     BinaryOperatorKind.LessThanOrEqual,
                     BinaryOperatorKind.GreaterThanOrEqual,
                     BinaryOperatorKind.LessThan,
                     BinaryOperatorKind.GreaterThan
                    tableKind = BinaryOperatorTables.TableKind.Relational

                Case BinaryOperatorKind.Xor,
                     BinaryOperatorKind.Or,
                     BinaryOperatorKind.And
                    tableKind = BinaryOperatorTables.TableKind.Bitwise

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opCode)
            End Select

            Return CType(BinaryOperatorTables.Table(tableKind, leftIndex.Value, rightIndex.Value), SpecialType)
        End Function

        Private Class BinaryOperatorTables

            Public Enum TableKind
                Addition
                SubtractionMultiplicationModulo
                Division
                Power
                IntegerDivision
                Shift
                Logical
                Bitwise
                Relational
                ConcatenationLike
            End Enum

            ' PERF: Using SByte instead of SpecialType because we want the compiler to use array literal initialization.
            '       The most natural type choice, Enum arrays, are not blittable due to a CLR limitation.
            Public Shared ReadOnly Table(,,) As SByte

            Shared Sub New()

                Const tErr As SByte = SpecialType.None
                Const tObj As SByte = SpecialType.System_Object
                Const tStr As SByte = SpecialType.System_String
                Const tDbl As SByte = SpecialType.System_Double
                Const tSBy As SByte = SpecialType.System_SByte
                Const tShr As SByte = SpecialType.System_Int16
                Const tInt As SByte = SpecialType.System_Int32
                Const tLng As SByte = SpecialType.System_Int64
                Const tDec As SByte = SpecialType.System_Decimal
                Const tSng As SByte = SpecialType.System_Single
                Const tByt As SByte = SpecialType.System_Byte
                Const tUSh As SByte = SpecialType.System_UInt16
                Const tUIn As SByte = SpecialType.System_UInt32
                Const tULn As SByte = SpecialType.System_UInt64
                Const tBoo As SByte = SpecialType.System_Boolean
                Const tChr As SByte = SpecialType.System_Char
                Const tDat As SByte = SpecialType.System_DateTime

                Table =
                {     '  Obj   Str   Bool  Char  SByt  Shrt  Int   Long  Byte  UShr  UInt  ULng  Sngl  Dbl   Dec   Date
                    { ' Addition
                        {tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj}, ' Obj
                        {tObj, tStr, tDbl, tStr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tStr}, ' Str
                        {tObj, tDbl, tShr, tErr, tSBy, tShr, tInt, tLng, tShr, tInt, tLng, tDec, tSng, tDbl, tDec, tErr}, ' Bool
                        {tObj, tStr, tErr, tStr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr}, ' Char
                        {tObj, tDbl, tSBy, tErr, tSBy, tShr, tInt, tLng, tShr, tInt, tLng, tDec, tSng, tDbl, tDec, tErr}, ' SByt
                        {tObj, tDbl, tShr, tErr, tShr, tShr, tInt, tLng, tShr, tInt, tLng, tDec, tSng, tDbl, tDec, tErr}, ' Shrt
                        {tObj, tDbl, tInt, tErr, tInt, tInt, tInt, tLng, tInt, tInt, tLng, tDec, tSng, tDbl, tDec, tErr}, ' Int
                        {tObj, tDbl, tLng, tErr, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tDec, tSng, tDbl, tDec, tErr}, ' Long
                        {tObj, tDbl, tShr, tErr, tShr, tShr, tInt, tLng, tByt, tUSh, tUIn, tULn, tSng, tDbl, tDec, tErr}, ' Byte
                        {tObj, tDbl, tInt, tErr, tInt, tInt, tInt, tLng, tUSh, tUSh, tUIn, tULn, tSng, tDbl, tDec, tErr}, ' UShr
                        {tObj, tDbl, tLng, tErr, tLng, tLng, tLng, tLng, tUIn, tUIn, tUIn, tULn, tSng, tDbl, tDec, tErr}, ' UInt
                        {tObj, tDbl, tDec, tErr, tDec, tDec, tDec, tDec, tULn, tULn, tULn, tULn, tSng, tDbl, tDec, tErr}, ' ULng
                        {tObj, tDbl, tSng, tErr, tSng, tSng, tSng, tSng, tSng, tSng, tSng, tSng, tSng, tDbl, tSng, tErr}, ' Sngl
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' Dbl
                        {tObj, tDbl, tDec, tErr, tDec, tDec, tDec, tDec, tDec, tDec, tDec, tDec, tSng, tDbl, tDec, tErr}, ' Dec
                        {tObj, tStr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tStr}  ' Date
                    },'  Obj   Str   Bool  Char  SByt  Shrt  Int   Long  Byte  UShr  UInt  ULng  Sngl  Dbl   Dec   Date
                    { ' Subtraction, Multiplication, and Modulo
                        {tObj, tObj, tObj, tErr, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tErr}, ' Obj 
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' Str 
                        {tObj, tDbl, tShr, tErr, tSBy, tShr, tInt, tLng, tShr, tInt, tLng, tDec, tSng, tDbl, tDec, tErr}, ' Bool 
                        {tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr}, ' Char 
                        {tObj, tDbl, tSBy, tErr, tSBy, tShr, tInt, tLng, tShr, tInt, tLng, tDec, tSng, tDbl, tDec, tErr}, ' SByt 
                        {tObj, tDbl, tShr, tErr, tShr, tShr, tInt, tLng, tShr, tInt, tLng, tDec, tSng, tDbl, tDec, tErr}, ' Shrt 
                        {tObj, tDbl, tInt, tErr, tInt, tInt, tInt, tLng, tInt, tInt, tLng, tDec, tSng, tDbl, tDec, tErr}, ' Int 
                        {tObj, tDbl, tLng, tErr, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tDec, tSng, tDbl, tDec, tErr}, ' Long 
                        {tObj, tDbl, tShr, tErr, tShr, tShr, tInt, tLng, tByt, tUSh, tUIn, tULn, tSng, tDbl, tDec, tErr}, ' Byte 
                        {tObj, tDbl, tInt, tErr, tInt, tInt, tInt, tLng, tUSh, tUSh, tUIn, tULn, tSng, tDbl, tDec, tErr}, ' UShr 
                        {tObj, tDbl, tLng, tErr, tLng, tLng, tLng, tLng, tUIn, tUIn, tUIn, tULn, tSng, tDbl, tDec, tErr}, ' UInt 
                        {tObj, tDbl, tDec, tErr, tDec, tDec, tDec, tDec, tULn, tULn, tULn, tULn, tSng, tDbl, tDec, tErr}, ' ULng 
                        {tObj, tDbl, tSng, tErr, tSng, tSng, tSng, tSng, tSng, tSng, tSng, tSng, tSng, tDbl, tSng, tErr}, ' Sngl 
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' Dbl 
                        {tObj, tDbl, tDec, tErr, tDec, tDec, tDec, tDec, tDec, tDec, tDec, tDec, tSng, tDbl, tDec, tErr}, ' Dec 
                        {tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr}  ' Date 
                    }, ' Special Note:  Date - Date is actually TimeSpan, but that cannot be encoded in this table.^^^^
                    { ' Division
                        {tObj, tObj, tObj, tErr, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tErr}, ' Obj
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' Str
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tSng, tDbl, tDec, tErr}, ' Bool
                        {tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr}, ' Char
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tSng, tDbl, tDec, tErr}, ' SByt
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tSng, tDbl, tDec, tErr}, ' Shrt
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tSng, tDbl, tDec, tErr}, ' Int
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tSng, tDbl, tDec, tErr}, ' Long
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tSng, tDbl, tDec, tErr}, ' Byte
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tSng, tDbl, tDec, tErr}, ' UShr
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tSng, tDbl, tDec, tErr}, ' UInt
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tSng, tDbl, tDec, tErr}, ' ULng
                        {tObj, tDbl, tSng, tErr, tSng, tSng, tSng, tSng, tSng, tSng, tSng, tSng, tSng, tDbl, tSng, tErr}, ' Sngl
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' Dbl
                        {tObj, tDbl, tDec, tErr, tDec, tDec, tDec, tDec, tDec, tDec, tDec, tDec, tSng, tDbl, tDec, tErr}, ' Dec
                        {tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr}  ' Date
                    },'  Obj   Str   Bool  Char  SByt  Shrt  Int   Long  Byte  UShr  UInt  ULng  Sngl  Dbl   Dec   Date
                    { ' Power
                        {tObj, tObj, tObj, tErr, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tErr}, ' Obj
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' Str
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' Bool
                        {tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr}, ' Char
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' SByt
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' Shrt
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' Int
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' Long
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' Byte
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' UShr
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' UInt
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' ULng
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' Sngl
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' Dbl
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' Dec
                        {tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr}  ' Date
                    },'  Obj   Str   Bool  Char  SByt  Shrt  Int   Long  Byte  UShr  UInt  ULng  Sngl  Dbl   Dec   Date
                    { ' Integer Division
                        {tObj, tObj, tObj, tErr, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tErr}, ' Obj
                        {tObj, tLng, tLng, tErr, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tErr}, ' Str
                        {tObj, tLng, tShr, tErr, tSBy, tShr, tInt, tLng, tShr, tInt, tLng, tLng, tLng, tLng, tLng, tErr}, ' Bool
                        {tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr}, ' Char
                        {tObj, tLng, tSBy, tErr, tSBy, tShr, tInt, tLng, tShr, tInt, tLng, tLng, tLng, tLng, tLng, tErr}, ' SByt
                        {tObj, tLng, tShr, tErr, tShr, tShr, tInt, tLng, tShr, tInt, tLng, tLng, tLng, tLng, tLng, tErr}, ' Shrt
                        {tObj, tLng, tInt, tErr, tInt, tInt, tInt, tLng, tInt, tInt, tLng, tLng, tLng, tLng, tLng, tErr}, ' Int
                        {tObj, tLng, tLng, tErr, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tErr}, ' Long
                        {tObj, tLng, tShr, tErr, tShr, tShr, tInt, tLng, tByt, tUSh, tUIn, tULn, tLng, tLng, tLng, tErr}, ' Byte
                        {tObj, tLng, tInt, tErr, tInt, tInt, tInt, tLng, tUSh, tUSh, tUIn, tULn, tLng, tLng, tLng, tErr}, ' UShr
                        {tObj, tLng, tLng, tErr, tLng, tLng, tLng, tLng, tUIn, tUIn, tUIn, tULn, tLng, tLng, tLng, tErr}, ' UInt
                        {tObj, tLng, tLng, tErr, tLng, tLng, tLng, tLng, tULn, tULn, tULn, tULn, tLng, tLng, tLng, tErr}, ' ULng
                        {tObj, tLng, tLng, tErr, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tErr}, ' Sngl
                        {tObj, tLng, tLng, tErr, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tErr}, ' Dbl
                        {tObj, tLng, tLng, tErr, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tErr}, ' Dec
                        {tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr}  ' Date
                    },'  Obj   Str   Bool  Char  SByt  Shrt  Int   Long  Byte  UShr  UInt  ULng  Sngl  Dbl   Dec   Date
                    {  ' Shift. Note: The right operand serves little purpose in this table, however a table is utilized nonetheless to make the most use of already existing code which analyzes binary operators.
                        {tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj}, ' Obj
                        {tObj, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng}, ' Str
                        {tObj, tShr, tShr, tShr, tShr, tShr, tShr, tShr, tShr, tShr, tShr, tShr, tShr, tShr, tShr, tShr}, ' Bool
                        {tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr}, ' Char
                        {tObj, tSBy, tSBy, tSBy, tSBy, tSBy, tSBy, tSBy, tSBy, tSBy, tSBy, tSBy, tSBy, tSBy, tSBy, tSBy}, ' SByt
                        {tObj, tShr, tShr, tShr, tShr, tShr, tShr, tShr, tShr, tShr, tShr, tShr, tShr, tShr, tShr, tShr}, ' Shrt
                        {tObj, tInt, tInt, tInt, tInt, tInt, tInt, tInt, tInt, tInt, tInt, tInt, tInt, tInt, tInt, tInt}, ' Int
                        {tObj, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng}, ' Long
                        {tObj, tByt, tByt, tByt, tByt, tByt, tByt, tByt, tByt, tByt, tByt, tByt, tByt, tByt, tByt, tByt}, ' Byte
                        {tObj, tUSh, tUSh, tUSh, tUSh, tUSh, tUSh, tUSh, tUSh, tUSh, tUSh, tUSh, tUSh, tUSh, tUSh, tUSh}, ' UShr
                        {tObj, tUIn, tUIn, tUIn, tUIn, tUIn, tUIn, tUIn, tUIn, tUIn, tUIn, tUIn, tUIn, tUIn, tUIn, tUIn}, ' UInt
                        {tObj, tULn, tULn, tULn, tULn, tULn, tULn, tULn, tULn, tULn, tULn, tULn, tULn, tULn, tULn, tULn}, ' ULng
                        {tObj, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng}, ' Sngl
                        {tObj, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng}, ' Dbl
                        {tObj, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng}, ' Dec
                        {tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr}  ' Date
                    },'  Obj   Str   Bool  Char  SByt  Shrt  Int   Long  Byte  UShr  UInt  ULng  Sngl  Dbl   Dec   Date
                    {  ' Logical Operators
                        {tObj, tObj, tObj, tErr, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tErr}, ' Obj
                        {tObj, tBoo, tBoo, tErr, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tErr}, ' Str
                        {tObj, tBoo, tBoo, tErr, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tErr}, ' Bool
                        {tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr}, ' Char
                        {tObj, tBoo, tBoo, tErr, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tErr}, ' SByt
                        {tObj, tBoo, tBoo, tErr, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tErr}, ' Shrt
                        {tObj, tBoo, tBoo, tErr, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tErr}, ' Int
                        {tObj, tBoo, tBoo, tErr, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tErr}, ' Long
                        {tObj, tBoo, tBoo, tErr, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tErr}, ' Byte
                        {tObj, tBoo, tBoo, tErr, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tErr}, ' UShr
                        {tObj, tBoo, tBoo, tErr, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tErr}, ' UInt
                        {tObj, tBoo, tBoo, tErr, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tErr}, ' ULng
                        {tObj, tBoo, tBoo, tErr, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tErr}, ' Sngl
                        {tObj, tBoo, tBoo, tErr, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tErr}, ' Dbl
                        {tObj, tBoo, tBoo, tErr, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tBoo, tErr}, ' Dec
                        {tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr}  ' Date
                    },'  Obj   Str   Bool  Char  SByt  Shrt  Int   Long  Byte  UShr  UInt  ULng  Sngl  Dbl   Dec   Date
                    { ' Bitwise Operators
                        {tObj, tObj, tObj, tErr, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tErr}, ' Obj
                        {tObj, tLng, tBoo, tErr, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tErr}, ' Str
                        {tObj, tBoo, tBoo, tErr, tSBy, tShr, tInt, tLng, tShr, tInt, tLng, tLng, tLng, tLng, tLng, tErr}, ' Bool
                        {tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr}, ' Char
                        {tObj, tLng, tSBy, tErr, tSBy, tShr, tInt, tLng, tShr, tInt, tLng, tLng, tLng, tLng, tLng, tErr}, ' SByt
                        {tObj, tLng, tShr, tErr, tShr, tShr, tInt, tLng, tShr, tInt, tLng, tLng, tLng, tLng, tLng, tErr}, ' Shrt
                        {tObj, tLng, tInt, tErr, tInt, tInt, tInt, tLng, tInt, tInt, tLng, tLng, tLng, tLng, tLng, tErr}, ' Int
                        {tObj, tLng, tLng, tErr, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tErr}, ' Long
                        {tObj, tLng, tShr, tErr, tShr, tShr, tInt, tLng, tByt, tUSh, tUIn, tULn, tLng, tLng, tLng, tErr}, ' Byte
                        {tObj, tLng, tInt, tErr, tInt, tInt, tInt, tLng, tUSh, tUSh, tUIn, tULn, tLng, tLng, tLng, tErr}, ' UShr
                        {tObj, tLng, tLng, tErr, tLng, tLng, tLng, tLng, tUIn, tUIn, tUIn, tULn, tLng, tLng, tLng, tErr}, ' UInt
                        {tObj, tLng, tLng, tErr, tLng, tLng, tLng, tLng, tULn, tULn, tULn, tULn, tLng, tLng, tLng, tErr}, ' ULng
                        {tObj, tLng, tLng, tErr, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tErr}, ' Sngl
                        {tObj, tLng, tLng, tErr, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tErr}, ' Dbl
                        {tObj, tLng, tLng, tErr, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tErr}, ' Dec
                        {tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr}  ' Date
                    },'  Obj   Str   Bool  Char  SByt  Shrt  Int   Long  Byte  UShr  UInt  ULng  Sngl  Dbl   Dec   Date
                    { ' Relational Operators -- This one is a little unusual because it lists the type of the relational operation, even though the result type is always boolean
                        {tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj}, ' Obj
                        {tObj, tStr, tBoo, tStr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDat}, ' Str
                        {tObj, tBoo, tBoo, tErr, tSBy, tShr, tInt, tLng, tShr, tInt, tLng, tDec, tSng, tDbl, tDec, tErr}, ' Bool
                        {tObj, tStr, tErr, tChr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr}, ' Char
                        {tObj, tDbl, tSBy, tErr, tSBy, tShr, tInt, tLng, tShr, tInt, tLng, tDec, tSng, tDbl, tDec, tErr}, ' SByt
                        {tObj, tDbl, tShr, tErr, tShr, tShr, tInt, tLng, tShr, tInt, tLng, tDec, tSng, tDbl, tDec, tErr}, ' Shrt
                        {tObj, tDbl, tInt, tErr, tInt, tInt, tInt, tLng, tInt, tInt, tLng, tDec, tSng, tDbl, tDec, tErr}, ' Int
                        {tObj, tDbl, tLng, tErr, tLng, tLng, tLng, tLng, tLng, tLng, tLng, tDec, tSng, tDbl, tDec, tErr}, ' Long
                        {tObj, tDbl, tShr, tErr, tShr, tShr, tInt, tLng, tByt, tUSh, tUIn, tULn, tSng, tDbl, tDec, tErr}, ' Byte
                        {tObj, tDbl, tInt, tErr, tInt, tInt, tInt, tLng, tUSh, tUSh, tUIn, tULn, tSng, tDbl, tDec, tErr}, ' UShr
                        {tObj, tDbl, tLng, tErr, tLng, tLng, tLng, tLng, tUIn, tUIn, tUIn, tULn, tSng, tDbl, tDec, tErr}, ' UInt
                        {tObj, tDbl, tDec, tErr, tDec, tDec, tDec, tDec, tULn, tULn, tULn, tULn, tSng, tDbl, tDec, tErr}, ' ULng
                        {tObj, tDbl, tSng, tErr, tSng, tSng, tSng, tSng, tSng, tSng, tSng, tSng, tSng, tDbl, tSng, tErr}, ' Sngl
                        {tObj, tDbl, tDbl, tErr, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tDbl, tErr}, ' Dbl
                        {tObj, tDbl, tDec, tErr, tDec, tDec, tDec, tDec, tDec, tDec, tDec, tDec, tSng, tDbl, tDec, tErr}, ' Dec
                        {tObj, tDat, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tErr, tDat}  ' Date
                    },'  Obj   Str   Bool  Char  SByt  Shrt  Int   Long  Byte  UShr  UInt  ULng  Sngl  Dbl   Dec   Date
                    { ' Concatenation and Like
                        {tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj, tObj}, ' Obj 
                        {tObj, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr}, ' Str 
                        {tObj, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr}, ' Bool 
                        {tObj, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr}, ' Char 
                        {tObj, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr}, ' SByt 
                        {tObj, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr}, ' Shrt 
                        {tObj, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr}, ' Int 
                        {tObj, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr}, ' Long 
                        {tObj, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr}, ' Byte 
                        {tObj, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr}, ' UShr 
                        {tObj, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr}, ' UInt 
                        {tObj, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr}, ' ULng 
                        {tObj, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr}, ' Sngl 
                        {tObj, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr}, ' Dbl 
                        {tObj, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr}, ' Date 
                        {tObj, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr, tStr}  ' Dec 
                    }
                }

            End Sub
        End Class

        Public Shared Function ResolveUserDefinedConversion(
            source As TypeSymbol,
            destination As TypeSymbol,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As KeyValuePair(Of ConversionKind, MethodSymbol)
            Debug.Assert(Not source.IsErrorType())
            Debug.Assert(Not destination.IsErrorType())

            Dim result As KeyValuePair(Of ConversionKind, MethodSymbol) = Nothing
            Dim opSet = ArrayBuilder(Of MethodSymbol).GetInstance()

            ' 8.11 User-Defined Conversions
            ' When determining what user-defined conversion to use, the most specific widening conversion will be used;
            ' if no widening conversion is most specific, the most specific narrowing conversion will be used. If there
            ' is no most specific narrowing conversion, then the conversion is undefined and a compile-time error occurs.
            ' When collecting the candidate user-defined conversions for a type T?, the user-defined conversion operators
            ' defined by T are used instead. If the type being converted to is also a nullable value type, then any of
            ' T's user-defined conversions operators that involve only non-nullable value types are lifted. A conversion
            ' operator from T to S is lifted to be a conversion from T? to S? 
            '
            ' !!! Dev10 implementation doesn't match the spec here (the behavior is duplicated):
            ' !!! 1) If there were applicable Widening CType operators applicable according to the
            ' !!!    "Most Specific Widening Conversion" section, Narrowing CType operators are not considered at all.
            ' !!!    Nullable lifting isn't considered too.
            ' !!! 2) With "Most Specific Narrowing Conversion" behavior is slightly different. If there is a conversion
            ' !!!    operator that converts from the most specific source type to the most specific target type, then
            ' !!!    nullable lifting isn't considered.

            CollectUserDefinedConversionOperators(source, destination, opSet, useSiteDiagnostics)

            If opSet.Count = 0 Then
                opSet.Free()
                Return result
            End If

            Dim conversionKinds = ArrayBuilder(Of KeyValuePair(Of ConversionKind, ConversionKind)).GetInstance()
            conversionKinds.ZeroInit(opSet.Count)

            Dim applicable = BitVector.Create(opSet.Count)
            Dim bestMatch As MethodSymbol = Nothing

            If DetermineMostSpecificWideningConversion(source, destination, opSet, conversionKinds, applicable, bestMatch, suppressViabilityChecks:=False, useSiteDiagnostics:=useSiteDiagnostics) Then
                If bestMatch IsNot Nothing Then
                    result = New KeyValuePair(Of ConversionKind, MethodSymbol)(ConversionKind.Widening Or ConversionKind.UserDefined, bestMatch)
                End If

                GoTo Done
            ElseIf opSet.Count = 0 Then
                GoTo Done
            End If

            If DetermineMostSpecificNarrowingConversion(source, destination, opSet, conversionKinds, applicable, bestMatch, suppressViabilityChecks:=False, useSiteDiagnostics:=useSiteDiagnostics) Then
                If bestMatch IsNot Nothing Then
                    result = New KeyValuePair(Of ConversionKind, MethodSymbol)(ConversionKind.Narrowing Or ConversionKind.UserDefined, bestMatch)
                End If

                GoTo Done
            ElseIf opSet.Count = 0 Then
                GoTo Done
            End If

            ' Try nullable lifting.
            If source.IsNullableType() AndAlso destination.IsNullableType() Then

                applicable.Clear()
                conversionKinds.ZeroInit(opSet.Count)

                Dim sourceUnderlying As TypeSymbol = source.GetNullableUnderlyingType()
                Dim destinationUnderlying As TypeSymbol = destination.GetNullableUnderlyingType()

                If Not (sourceUnderlying.IsErrorType() OrElse destinationUnderlying.IsErrorType()) Then
                    ' All candidates applicable to the underlying types should be applicable to the original types, no reason to 
                    ' do viability checks for the second time.

                    If DetermineMostSpecificWideningConversion(sourceUnderlying, destinationUnderlying, opSet, conversionKinds, applicable, bestMatch, suppressViabilityChecks:=True, useSiteDiagnostics:=useSiteDiagnostics) Then
                        If bestMatch IsNot Nothing Then
                            Debug.Assert(Not bestMatch.Parameters(0).Type.IsNullableType())
                            Debug.Assert(Not bestMatch.ReturnType.IsNullableType())
                            result = New KeyValuePair(Of ConversionKind, MethodSymbol)(ConversionKind.Widening Or
                                                                                       ConversionKind.UserDefined Or
                                                                                       ConversionKind.Nullable, bestMatch)
                        End If
                    ElseIf DetermineMostSpecificNarrowingConversion(sourceUnderlying, destinationUnderlying, opSet, conversionKinds, applicable, bestMatch, suppressViabilityChecks:=True, useSiteDiagnostics:=useSiteDiagnostics) Then
                        If bestMatch IsNot Nothing Then
                            Debug.Assert(Not bestMatch.Parameters(0).Type.IsNullableType())
                            Debug.Assert(Not bestMatch.ReturnType.IsNullableType())
                            result = New KeyValuePair(Of ConversionKind, MethodSymbol)(ConversionKind.Narrowing Or
                                                                                       ConversionKind.UserDefined Or
                                                                                       ConversionKind.Nullable, bestMatch)
                        End If
                    End If
                End If
            End If
Done:
            conversionKinds.Free()
            opSet.Free()

            Return result
        End Function

        ''' <summary>
        ''' Returns True if resolution of user defined conversions is complete, i.e. there were operators
        ''' applicable for the "Most Specific Widening Conversion" purposes. 
        ''' This, however, doesn't mean that resolution is successful.
        ''' </summary>
        Private Shared Function DetermineMostSpecificWideningConversion(
            source As TypeSymbol,
            destination As TypeSymbol,
            opSet As ArrayBuilder(Of MethodSymbol),
            conversionKinds As ArrayBuilder(Of KeyValuePair(Of ConversionKind, ConversionKind)),
            <[In]()> ByRef applicable As BitVector,
            <Out()> ByRef bestMatch As MethodSymbol,
            suppressViabilityChecks As Boolean,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As Boolean
#If DEBUG Then
            Debug.Assert(bestMatch Is Nothing)
            For i As Integer = 0 To opSet.Count - 1
                Debug.Assert(Not applicable(i))
            Next
#End If

            bestMatch = Nothing

            Dim bestMatchDegreeOfGenericity As Integer = -1 ' (-1) - unknown, 0 - neither type refers to generic type parameters, 1 - one type, 2 - two types
            Dim bestMatchIsAmbiguous As Boolean = False
            Dim mostSpecificSourceType As TypeSymbol = Nothing
            Dim mostSpecificTargetType As TypeSymbol = Nothing
            Dim applicableCount As Integer = 0

            ' 8.11.1 Most Specific Widening Conversion
            ' -	First, all of the candidate conversion operators are collected. The candidate conversion operators are all
            '   of the user-defined widening conversion operators in the source type and all of the user-defined widening
            '   conversion operators in the target type. 
            ' -	Then, all non-applicable conversion operators are removed from the set. A conversion operator is applicable
            '   to a source type and target type if there is an intrinsic widening conversion operator from the source type
            '   to the operand type and there is an intrinsic widening conversion operator from the result of the operator
            '   to the target type. If there are no applicable conversion operators, then there is no most specific widening
            '   conversion.
            ' -	Then, the most specific source type of the applicable conversion operators is determined:
            '     -	If any of the conversion operators convert directly from the source type, then the source type is the
            '       most specific source type.
            '     -	Otherwise, the most specific source type is the most encompassed type in the combined set of source types
            '       of the conversion operators. If no most encompassed type can be found, then there is no most specific
            '       widening conversion.
            ' -	Then, the most specific target type of the applicable conversion operators is determined:
            '     -	If any of the conversion operators convert directly to the target type, then the target type is the most
            '       specific target type.
            '     -	Otherwise, the most specific target type is the most encompassing type in the combined set of target types
            '       of the conversion operators. If no most encompassing type can be found, then there is no most specific
            '       widening conversion.
            ' -	Then, if exactly one conversion operator converts from the most specific source type to the most specific target
            '   type, then this is the most specific conversion operator. If more than one such operator exists, then there is
            '   no most specific widening conversion.

            ' 8.11 User-Defined Conversions
            ' -	If an intrinsic widening conversion exists from a type A to a type B,
            '   and if neither A nor B are interfaces, then A is encompassed by B, and
            '   B encompasses A.
            ' -	The most encompassing type in a set of types is the one type that encompasses
            '   all other types in the set. If no single type encompasses all other types, then
            '   the set has no most encompassing type. In intuitive terms, the most encompassing
            '   type is the "largest" type in the set—the one type to which each of the other
            '   types can be converted through a widening conversion.
            ' -	The most encompassed type in a set of types is the one type that is encompassed
            '   by all other types in the set. If no single type is encompassed by all other types,
            '   then the set has no most encompassed type. In intuitive terms, the most encompassed
            '   type is the "smallest" type in the set—the one type that can be converted from each
            '   of the other types through a narrowing conversion.

            Dim viableCandidates As Integer = 0

            For i As Integer = 0 To opSet.Count - 1
                Dim method As MethodSymbol = opSet(i)

                Dim currentIndex As Integer = viableCandidates
                viableCandidates += 1

                If currentIndex < i Then
                    Debug.Assert(Not suppressViabilityChecks)
                    opSet(currentIndex) = method
                Else
                    Debug.Assert(currentIndex = i)
                End If

                If Not IsWidening(method) Then
                    Continue For
                End If

                Dim conversionIn As ConversionKind
                Dim conversionOut As ConversionKind

                If ClassifyConversionOperatorInOutConversions(source, destination, method, conversionIn, conversionOut, suppressViabilityChecks, useSiteDiagnostics) Then
                    conversionKinds(currentIndex) = New KeyValuePair(Of ConversionKind, ConversionKind)(conversionIn, conversionOut)
                Else
                    'opSet(currentIndex) = Nothing
                    Debug.Assert(Not suppressViabilityChecks)
                    viableCandidates = currentIndex
                    Continue For
                End If

                If bestMatch Is Nothing Then
                    If Not (Conversions.IsWideningConversion(conversionIn) AndAlso Conversions.IsWideningConversion(conversionOut)) Then
                        Continue For
                    End If
                Else
                    If Not (Conversions.IsIdentityConversion(conversionIn) AndAlso Conversions.IsIdentityConversion(conversionOut)) Then
                        Continue For
                    End If

                    ' Potential ambiguity, let's attempt to resolve based on genericity.
                    bestMatch = LeastGenericConversionOperator(bestMatch, method, bestMatchDegreeOfGenericity, bestMatchIsAmbiguous)

                    If bestMatchIsAmbiguous AndAlso bestMatchDegreeOfGenericity = 0 Then
                        ' We will not be able to get rid of this ambiguity.
                        Exit For
                    End If

                    Continue For
                End If

                If Conversions.IsIdentityConversion(conversionIn) AndAlso Conversions.IsIdentityConversion(conversionOut) Then
                    Debug.Assert(bestMatch Is Nothing)
                    bestMatch = method
                    applicable.Clear()
                    applicableCount = 0
                Else
                    If Conversions.IsIdentityConversion(conversionIn) Then
                        mostSpecificSourceType = source
                    End If

                    If Conversions.IsIdentityConversion(conversionOut) Then
                        mostSpecificTargetType = destination
                    End If

                    applicable(currentIndex) = True
                    applicableCount += 1
                End If
            Next

            opSet.Clip(viableCandidates)
            conversionKinds.Clip(viableCandidates)

#If DEBUG Then
            For i As Integer = 0 To opSet.Count - 1
                Debug.Assert(opSet(i) IsNot Nothing)
            Next
#End If

            If bestMatch IsNot Nothing Then
                If bestMatchIsAmbiguous Then
                    bestMatch = Nothing
                End If

                Return True
            End If

            If applicableCount > 0 Then
                Debug.Assert(bestMatch Is Nothing)
                Debug.Assert(Not bestMatchIsAmbiguous)

                If applicableCount > 1 Then
                    ' Try to choose most specific among applicable candidates. 
                    Dim typeSet As ArrayBuilder(Of TypeSymbol) = Nothing

                    If mostSpecificSourceType Is Nothing Then
                        typeSet = ArrayBuilder(Of TypeSymbol).GetInstance()

                        For i As Integer = 0 To opSet.Count - 1
                            If Not applicable(i) Then
                                Continue For
                            End If

                            typeSet.Add(opSet(i).Parameters(0).Type)
                        Next

                        Debug.Assert(typeSet.Count = applicableCount)

                        mostSpecificSourceType = MostEncompassed(typeSet, useSiteDiagnostics)
                    End If

                    If mostSpecificTargetType Is Nothing AndAlso mostSpecificSourceType IsNot Nothing Then
                        If typeSet Is Nothing Then
                            typeSet = ArrayBuilder(Of TypeSymbol).GetInstance()
                        Else
                            typeSet.Clear()
                        End If

                        For i As Integer = 0 To opSet.Count - 1
                            If Not applicable(i) Then
                                Continue For
                            End If

                            typeSet.Add(opSet(i).ReturnType)
                        Next

                        Debug.Assert(typeSet.Count = applicableCount)

                        mostSpecificTargetType = MostEncompassing(typeSet, useSiteDiagnostics)
                    End If

                    If typeSet IsNot Nothing Then
                        typeSet.Free()
                    End If

                    If mostSpecificSourceType IsNot Nothing AndAlso mostSpecificTargetType IsNot Nothing Then
                        bestMatch = ChooseMostSpecificConversionOperator(opSet, applicable, mostSpecificSourceType, mostSpecificTargetType, bestMatchIsAmbiguous)
                    End If

                    If bestMatch IsNot Nothing AndAlso bestMatchIsAmbiguous Then
                        bestMatch = Nothing
                    End If
                Else
                    For i As Integer = 0 To opSet.Count - 1
                        If applicable(i) Then
                            bestMatch = opSet(i)
                            Exit For
                        End If
                    Next

                    Debug.Assert(bestMatch IsNot Nothing)
                End If

                Return True
            End If

            Debug.Assert(bestMatch Is Nothing)
            Return False
        End Function

        Private Shared Function ChooseMostSpecificConversionOperator(
            opSet As ArrayBuilder(Of MethodSymbol),
            applicable As BitVector,
            mostSpecificSourceType As TypeSymbol,
            mostSpecificTargetType As TypeSymbol,
            <Out()> ByRef bestMatchIsAmbiguous As Boolean
        ) As MethodSymbol
            Dim bestMatchDegreeOfGenericity As Integer = -1 ' (-1) - unknown, 0 - neither type refers to generic type parameters, 1 - one type, 2 - two types
            Dim bestMatch As MethodSymbol = Nothing
            bestMatchIsAmbiguous = False

            For i As Integer = 0 To opSet.Count - 1
                If Not applicable(i) Then
                    Continue For
                End If

                Dim method As MethodSymbol = opSet(i)

                If Not (mostSpecificSourceType.IsSameTypeIgnoringAll(method.Parameters(0).Type) AndAlso
                        mostSpecificTargetType.IsSameTypeIgnoringAll(method.ReturnType)) Then
                    Continue For
                End If

                If bestMatch Is Nothing Then
                    bestMatch = method
                Else
                    ' Potential ambiguity, let's attempt to resolve based on genericity.
                    bestMatch = LeastGenericConversionOperator(bestMatch, method, bestMatchDegreeOfGenericity, bestMatchIsAmbiguous)

                    If bestMatchIsAmbiguous AndAlso bestMatchDegreeOfGenericity = 0 Then
                        ' We will not be able to get rid of this ambiguity.
                        Exit For
                    End If
                End If
            Next

            Return bestMatch
        End Function

        ''' <summary>
        ''' Returns false if operator should be ignored.
        ''' </summary>
        Private Shared Function ClassifyConversionOperatorInOutConversions(
            source As TypeSymbol,
            destination As TypeSymbol,
            method As MethodSymbol,
            <Out()> ByRef conversionIn As ConversionKind,
            <Out()> ByRef conversionOut As ConversionKind,
            suppressViabilityChecks As Boolean,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As Boolean
            Dim inputType As TypeSymbol = method.Parameters(0).Type
            Dim outputType As TypeSymbol = method.ReturnType


            If Not suppressViabilityChecks Then
                If Not IsConversionOperatorViableBasedOnTypesInvolved(method, inputType, outputType) Then
                    conversionIn = Nothing
                    conversionOut = Nothing
                    Return False
                End If
            Else
                Debug.Assert(IsConversionOperatorViableBasedOnTypesInvolved(method, inputType, outputType))
            End If

            ' If source is an array literal then use ClassifyArrayLiteralConversion
            Dim arrayLiteralType = TryCast(source, ArrayLiteralTypeSymbol)
            If arrayLiteralType IsNot Nothing Then
                Dim arrayLiteral = arrayLiteralType.ArrayLiteral
                conversionIn = Conversions.ClassifyArrayLiteralConversion(arrayLiteral, inputType, arrayLiteral.Binder, useSiteDiagnostics)
                If Conversions.IsWideningConversion(conversionIn) AndAlso
                    IsSameTypeIgnoringAll(arrayLiteralType, inputType) Then
                    conversionIn = ConversionKind.Identity
                End If
            Else
                conversionIn = Conversions.ClassifyPredefinedConversion(source, inputType, useSiteDiagnostics)
            End If

            conversionOut = Conversions.ClassifyPredefinedConversion(outputType, destination, useSiteDiagnostics)
            Return True
        End Function

        Private Shared Function IsConversionOperatorViableBasedOnTypesInvolved(
            method As MethodSymbol,
            inputType As TypeSymbol,
            outputType As TypeSymbol
        ) As Boolean
            If inputType.IsErrorType() OrElse outputType.IsErrorType() Then
                Return False
            End If

            ' Ignore user defined conversions between types that already have intrinsic conversions.
            ' This could happen for generics after generic param substitution.
            If Not method.ContainingType.IsDefinition Then
                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                If Conversions.ConversionExists(Conversions.ClassifyPredefinedConversion(inputType, outputType, useSiteDiagnostics)) OrElse
                   Not useSiteDiagnostics.IsNullOrEmpty Then
                    Return False
                End If
            End If

            Return True
        End Function

        ''' <summary>
        ''' Returns True if resolution of user defined conversions is complete, i.e. there was an operator
        ''' that converts from the most specific source type to the most specific target type. 
        ''' This, however, doesn't mean that resolution is successful.
        ''' </summary>
        Private Shared Function DetermineMostSpecificNarrowingConversion(
            source As TypeSymbol,
            destination As TypeSymbol,
            opSet As ArrayBuilder(Of MethodSymbol),
            conversionKinds As ArrayBuilder(Of KeyValuePair(Of ConversionKind, ConversionKind)),
            <[In]()> ByRef applicable As BitVector,
            <Out()> ByRef bestMatch As MethodSymbol,
            suppressViabilityChecks As Boolean,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As Boolean
#If DEBUG Then
            Debug.Assert(bestMatch Is Nothing)
            For i As Integer = 0 To opSet.Count - 1
                Debug.Assert(Not applicable(i))
            Next
#End If
            bestMatch = Nothing

            Dim bestMatchDegreeOfGenericity As Integer = -1 ' (-1) - unknown, 0 - neither type refers to generic type parameters, 1 - one type, 2 - two types
            Dim bestMatchIsAmbiguous As Boolean = False
            Dim mostSpecificSourceType As TypeSymbol = Nothing
            Dim mostSpecificTargetType As TypeSymbol = Nothing
            Dim applicableCount As Integer = 0

            ' 8.11.2 Most Specific Narrowing Conversion
            ' Determining the most specific user-defined narrowing conversion operator between two types is accomplished using the following steps:
            ' - First, all of the candidate conversion operators are collected. The candidate conversion operators are all
            '   of the user-defined conversion operators in the source type and all of the user-defined conversion operators
            '   in the target type. 
            ' - Then, all non-applicable conversion operators are removed from the set. A conversion operator is applicable
            '   to a source type and target type if there is an intrinsic conversion operator from the source type to the
            '   operand type and there is an intrinsic conversion operator from the result of the operator to the target type.
            '   If there are no applicable conversion operators, then there is no most specific narrowing conversion.
            ' - Then, the most specific source type of the applicable conversion operators is determined:
            '      - If any of the conversion operators convert directly from the source type, then the source type is
            '        the most specific source type.
            '      - Otherwise, if any of the conversion operators convert from types that encompass the source type,
            '        then the most specific source type is the most encompassed type in the combined set of source types
            '        of those conversion operators. If no most encompassed type can be found, then there is no most
            '        specific narrowing conversion.
            '      - Otherwise, the most specific source type is the most encompassing type in the combined set of source
            '        types of the conversion operators. If no most encompassing type can be found, then there is no most
            '        specific narrowing conversion.
            ' - Then, the most specific target type of the applicable conversion operators is determined:
            '      - If any of the conversion operators convert directly to the target type, then the target type is
            '        the most specific target type.
            '      - Otherwise, if any of the conversion operators convert to types that are encompassed by the target type,
            '        then the most specific target type is the most encompassing type in the combined set of source types of
            '        those conversion operators. If no most encompassing type can be found, then there is no most specific
            '        narrowing conversion.
            '      - Otherwise, the most specific target type is the most encompassed type in the combined set of target
            '        types of the conversion operators. If no most encompassed type can be found, then there is no most
            '        specific narrowing conversion.
            ' - Then, if exactly one conversion operator converts from the most specific source type to the most specific
            '   target type, then this is the most specific conversion operator. If more than one such operator exists,
            '   then there is no most specific narrowing conversion.

            ' 8.11 User-Defined Conversions
            ' -	If an intrinsic widening conversion exists from a type A to a type B,
            '   and if neither A nor B are interfaces, then A is encompassed by B, and
            '   B encompasses A.
            ' -	The most encompassing type in a set of types is the one type that encompasses
            '   all other types in the set. If no single type encompasses all other types, then
            '   the set has no most encompassing type. In intuitive terms, the most encompassing
            '   type is the "largest" type in the set—the one type to which each of the other
            '   types can be converted through a widening conversion.
            ' -	The most encompassed type in a set of types is the one type that is encompassed
            '   by all other types in the set. If no single type is encompassed by all other types,
            '   then the set has no most encompassed type. In intuitive terms, the most encompassed
            '   type is the "smallest" type in the set—the one type that can be converted from each
            '   of the other types through a narrowing conversion.

            Dim haveWideningInConversions As Integer = 0
            Dim haveWideningOutConversions As Integer = 0

            Dim viableCandidates As Integer = 0

            For i As Integer = 0 To opSet.Count - 1
                Dim method As MethodSymbol = opSet(i)

                Dim currentIndex As Integer = viableCandidates
                viableCandidates += 1

                If currentIndex < i Then
                    Debug.Assert(Not suppressViabilityChecks)
                    opSet(currentIndex) = method
                    conversionKinds(currentIndex) = conversionKinds(i)
                Else
                    Debug.Assert(currentIndex = i)
                End If

                Dim conversionIn As ConversionKind
                Dim conversionOut As ConversionKind

                If IsWidening(method) Then
                    ' When we reach this point, a widening operator is either removed from the opSet (because it was "bad" in one way or another)
                    ' or it was not applicable for the purpose of the "Most Specific Widening Conversion", but conversion information should have been 
                    ' captured in conversionKinds for it.
                    Dim conversion As KeyValuePair(Of ConversionKind, ConversionKind) = conversionKinds(currentIndex)
                    conversionIn = conversion.Key
                    conversionOut = conversion.Value

                    ' If the operator would be an acceptable candidate for the "Most Specific Widening Conversion", we shouldn't have
                    ' reached this place.
                    Debug.Assert(Not (Conversions.IsWideningConversion(conversionIn) AndAlso Conversions.IsWideningConversion(conversionOut)))

                ElseIf ClassifyConversionOperatorInOutConversions(source, destination, method, conversionIn, conversionOut, suppressViabilityChecks, useSiteDiagnostics) Then
                    conversionKinds(currentIndex) = New KeyValuePair(Of ConversionKind, ConversionKind)(conversionIn, conversionOut)
                Else
                    'opSet(currentIndex) = Nothing
                    Debug.Assert(Not suppressViabilityChecks)
                    viableCandidates = currentIndex
                    Continue For
                End If

                If bestMatch Is Nothing Then
                    If Not (Conversions.ConversionExists(conversionIn) AndAlso Conversions.ConversionExists(conversionOut)) Then
                        Continue For
                    End If
                Else
                    If Not (Conversions.IsIdentityConversion(conversionIn) AndAlso Conversions.IsIdentityConversion(conversionOut)) Then
                        Continue For
                    End If

                    ' Potential ambiguity, let's attempt to resolve based on genericity.
                    bestMatch = LeastGenericConversionOperator(bestMatch, method, bestMatchDegreeOfGenericity, bestMatchIsAmbiguous)

                    If bestMatchIsAmbiguous AndAlso bestMatchDegreeOfGenericity = 0 Then
                        ' We will not be able to get rid of this ambiguity.
                        Exit For
                    End If

                    Continue For
                End If

                If Conversions.IsIdentityConversion(conversionIn) AndAlso Conversions.IsIdentityConversion(conversionOut) Then
                    Debug.Assert(bestMatch Is Nothing)
                    bestMatch = method
                    applicable.Clear()
                    applicableCount = 0
                Else
                    If Conversions.IsWideningConversion(conversionIn) Then
                        If Conversions.IsIdentityConversion(conversionIn) Then
                            mostSpecificSourceType = source
                        Else
                            haveWideningInConversions += 1
                        End If

                        If Conversions.IsWideningConversion(conversionOut) Then
                            If Conversions.IsIdentityConversion(conversionOut) Then
                                mostSpecificTargetType = destination
                            Else
                                haveWideningOutConversions += 1
                            End If
                        Else
                            Debug.Assert(Conversions.IsNarrowingConversion(conversionOut))
                        End If
                    Else
                        Debug.Assert(Conversions.IsNarrowingConversion(conversionIn))

                        If Conversions.IsIdentityConversion(conversionOut) Then
                            mostSpecificTargetType = destination
                        ElseIf Not Conversions.IsNarrowingConversion(conversionOut) Then
                            Debug.Assert(Conversions.IsWideningConversion(conversionOut) AndAlso Not Conversions.IsIdentityConversion(conversionOut))
                            ' Note that {Narrowing in, Widening (non-identity) out} operator is not considered as an applicable candidate.
                            ' In fact, an operator like this cannot exist unless nullable in/out conversions are involved.
                            ' Basically we would be dealing with an operator that converts from type derived from source to type derived from destination,
                            ' it would have to be defined in one of those types. When we collect operators we only visit source, destination and their
                            ' bases. So, in order for such an operator to be found, there must be an inheritance relationship between source and
                            ' destination and the operator must be defined in a type that is in between of them in the inheritance hierarchy. Thus,
                            ' there would be an inheritance relationship between parameter type ant return type of the operator, which makes the operator
                            ' inapplicable - there would be a predefined conversion between the types.
                            ' Ignoring an operator like this even when nullable conversions are involved, allows us to consider it as a candidate for a 
                            ' lifting, otherwise it would be treated as a not-lifted narrowing.
                            Continue For
                        Else
                            Debug.Assert(Conversions.IsNarrowingConversion(conversionOut))
                            ' When intrinsic types are involved, conversion from intrinsic type T to Nullable(Of S) can be narrowing,
                            ' it is always widening (if exists) for user-defined types. For reasons described above, we might need to ignore 
                            ' an operator like this.
                            If source.IsNullableType() AndAlso destination.IsNullableType() AndAlso
                               method.ReturnType.IsIntrinsicType() Then
                                Continue For
                            End If
                        End If
                    End If

                    applicable(currentIndex) = True
                    applicableCount += 1
                End If
            Next

            opSet.Clip(viableCandidates)
            conversionKinds.Clip(viableCandidates)

#If DEBUG Then
            For i As Integer = 0 To opSet.Count - 1
                Debug.Assert(opSet(i) IsNot Nothing)
            Next
#End If
            If bestMatch IsNot Nothing Then
                If bestMatchIsAmbiguous Then
                    bestMatch = Nothing
                End If

                Return True
            End If

            If applicableCount > 0 Then
                Debug.Assert(bestMatch Is Nothing)
                Debug.Assert(Not bestMatchIsAmbiguous)

                If applicableCount > 1 Then
                    ' Try to choose most specific among applicable candidates. 
                    Dim typeSet As ArrayBuilder(Of TypeSymbol) = Nothing

                    If mostSpecificSourceType Is Nothing Then
                        typeSet = ArrayBuilder(Of TypeSymbol).GetInstance()

                        For i As Integer = 0 To opSet.Count - 1
                            If Not applicable(i) Then
                                Continue For
                            End If

                            If haveWideningInConversions <> 0 Then
                                If Not Conversions.IsWideningConversion(conversionKinds(i).Key) Then
                                    Continue For
                                End If
                            Else
                                Debug.Assert(Conversions.IsNarrowingConversion(conversionKinds(i).Key))
                            End If

                            typeSet.Add(opSet(i).Parameters(0).Type)
                        Next

                        Debug.Assert(typeSet.Count = applicableCount OrElse (haveWideningInConversions <> 0 AndAlso typeSet.Count = haveWideningInConversions))

                        If haveWideningInConversions <> 0 Then
                            mostSpecificSourceType = MostEncompassed(typeSet, useSiteDiagnostics)
                        Else
                            mostSpecificSourceType = MostEncompassing(typeSet, useSiteDiagnostics)
                        End If
                    End If

                    If mostSpecificTargetType Is Nothing AndAlso mostSpecificSourceType IsNot Nothing Then
                        If typeSet Is Nothing Then
                            typeSet = ArrayBuilder(Of TypeSymbol).GetInstance()
                        Else
                            typeSet.Clear()
                        End If

                        For i As Integer = 0 To opSet.Count - 1
                            If Not applicable(i) Then
                                Continue For
                            End If

                            If haveWideningOutConversions <> 0 Then
                                If Not Conversions.IsWideningConversion(conversionKinds(i).Value) Then
                                    Continue For
                                End If
                            Else
                                Debug.Assert(Conversions.IsNarrowingConversion(conversionKinds(i).Value))
                            End If

                            typeSet.Add(opSet(i).ReturnType)
                        Next

                        Debug.Assert(typeSet.Count = applicableCount OrElse (haveWideningOutConversions <> 0 AndAlso typeSet.Count = haveWideningOutConversions))

                        If haveWideningOutConversions <> 0 Then
                            mostSpecificTargetType = MostEncompassing(typeSet, useSiteDiagnostics)
                        Else
                            mostSpecificTargetType = MostEncompassed(typeSet, useSiteDiagnostics)
                        End If
                    End If

                    If typeSet IsNot Nothing Then
                        typeSet.Free()
                    End If

                    If mostSpecificSourceType IsNot Nothing AndAlso mostSpecificTargetType IsNot Nothing Then
                        bestMatch = ChooseMostSpecificConversionOperator(opSet, applicable, mostSpecificSourceType, mostSpecificTargetType, bestMatchIsAmbiguous)
                    End If

                    If bestMatch IsNot Nothing AndAlso bestMatchIsAmbiguous Then
                        bestMatch = Nothing
                        Return True
                    End If
                Else
                    For i As Integer = 0 To opSet.Count - 1
                        If applicable(i) Then
                            bestMatch = opSet(i)
                            Exit For
                        End If
                    Next

                    Debug.Assert(bestMatch IsNot Nothing)
                End If
            End If

            Return bestMatch IsNot Nothing
        End Function

        ''' <summary>
        ''' The most encompassed type in a set of types is the one type that is encompassed
        ''' by all other types in the set. If no single type is encompassed by all other types,
        ''' then the set has no most encompassed type. In intuitive terms, the most encompassed
        ''' type is the "smallest" type in the set—the one type that can be converted from each
        ''' of the other types through a narrowing conversion.
        ''' </summary>
        Private Shared Function MostEncompassed(typeSet As ArrayBuilder(Of TypeSymbol), <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As TypeSymbol
            Dim best As TypeSymbol = Nothing

            For i As Integer = 0 To typeSet.Count - 1
                Dim type As TypeSymbol = typeSet(i)

                If best IsNot Nothing AndAlso best.IsSameTypeIgnoringAll(type) Then
                    Continue For
                End If

                Debug.Assert(Not type.IsErrorType())

                For j As Integer = 0 To typeSet.Count - 1
                    If i = j Then
                        Continue For
                    End If

                    Dim conv As ConversionKind = Conversions.ClassifyPredefinedConversion(type, typeSet(j), useSiteDiagnostics)

                    If Not Conversions.IsWideningConversion(conv) Then
                        ' type is not encompassed by the other type
                        GoTo Next_i
                    End If
                Next

                If best Is Nothing Then
                    best = type
                Else
                    Debug.Assert(Not best.IsSameTypeIgnoringAll(type))
                    best = Nothing ' More than one type 
                    Exit For
                End If
Next_i:
            Next

            Return best
        End Function

        ''' <summary>
        ''' The most encompassing type in a set of types is the one type that encompasses
        ''' all other types in the set. If no single type encompasses all other types, then
        ''' the set has no most encompassing type. In intuitive terms, the most encompassing
        ''' type is the "largest" type in the set—the one type to which each of the other
        ''' types can be converted through a widening conversion.
        ''' </summary>
        Private Shared Function MostEncompassing(typeSet As ArrayBuilder(Of TypeSymbol), <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As TypeSymbol
            Dim best As TypeSymbol = Nothing

            For i As Integer = 0 To typeSet.Count - 1
                Dim type As TypeSymbol = typeSet(i)

                If best IsNot Nothing AndAlso best.IsSameTypeIgnoringAll(type) Then
                    Continue For
                End If

                Debug.Assert(Not type.IsErrorType())

                For j As Integer = 0 To typeSet.Count - 1
                    If i = j Then
                        Continue For
                    End If

                    Dim conv As ConversionKind = Conversions.ClassifyPredefinedConversion(typeSet(j), type, useSiteDiagnostics)

                    If Not Conversions.IsWideningConversion(conv) Then
                        ' type is not encompassing the other type
                        GoTo Next_i
                    End If
                Next

                If best Is Nothing Then
                    best = type
                Else
                    Debug.Assert(Not best.IsSameTypeIgnoringAll(type))
                    best = Nothing ' More than one type 
                    Exit For
                End If
Next_i:
            Next

            Return best
        End Function

        Private Shared Function LeastGenericConversionOperator(
            method1 As MethodSymbol,
            method2 As MethodSymbol,
            <[In], Out()> ByRef bestDegreeOfGenericity As Integer,
            <[In], Out()> ByRef isAmbiguous As Boolean
        ) As MethodSymbol
            If bestDegreeOfGenericity = -1 Then
                bestDegreeOfGenericity = DetermineConversionOperatorDegreeOfGenericity(method1)
            End If

            Dim method2DegreeOfGenericity As Integer = DetermineConversionOperatorDegreeOfGenericity(method2)

            ' The less degree of genericity, the better.
            If bestDegreeOfGenericity < method2DegreeOfGenericity Then
                ' isAmbiguous state doesn't change.
                Return method1
            ElseIf method2DegreeOfGenericity < bestDegreeOfGenericity Then
                isAmbiguous = False
                bestDegreeOfGenericity = method2DegreeOfGenericity
                Return method2
            End If

            isAmbiguous = True
            Return method1
        End Function

        ''' <summary>
        ''' Returns number of types in the list of {input type, output type} that refer to a generic type parameter.
        ''' </summary>
        Private Shared Function DetermineConversionOperatorDegreeOfGenericity(method As MethodSymbol) As Integer
            If Not method.ContainingType.IsGenericType Then
                Return 0
            End If

            Dim result As Integer = 0
            Dim definition As MethodSymbol = method.OriginalDefinition

            If DetectReferencesToGenericParameters(definition.Parameters(0).Type, TypeParameterKind.Type, BitVector.Null) <> TypeParameterKind.None Then
                result += 1
            End If

            If DetectReferencesToGenericParameters(definition.ReturnType, TypeParameterKind.Type, BitVector.Null) <> TypeParameterKind.None Then
                result += 1
            End If

            Debug.Assert(result > 0)
            Return result
        End Function

        ''' <summary>
        ''' A quick check whether given conversion operator is a widening operator.
        ''' </summary>
        Friend Shared Function IsWidening(method As MethodSymbol) As Boolean
            Debug.Assert(method.MethodKind = MethodKind.Conversion)
            Dim forth As Char = method.Name(3)

            If forth = "I"c OrElse forth = "i"c Then
                Return True
            End If

            Debug.Assert(forth = "E"c OrElse forth = "e"c)
            Return False
        End Function

        ''' <summary>
        ''' Collect user-defined conversion operators.
        ''' Operators declared in the same type are grouped together. 
        ''' Within a group, widening operators are followed by narrowing operators.
        ''' </summary>
        Private Shared Sub CollectUserDefinedConversionOperators(
            source As TypeSymbol,
            destination As TypeSymbol,
            opSet As ArrayBuilder(Of MethodSymbol),
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        )
            CollectUserDefinedOperators(source, destination, MethodKind.Conversion,
                                        WellKnownMemberNames.ImplicitConversionName, New OperatorInfo(UnaryOperatorKind.Implicit),
                                        WellKnownMemberNames.ExplicitConversionName, New OperatorInfo(UnaryOperatorKind.Explicit),
                                        opSet, useSiteDiagnostics)
        End Sub

        ''' <summary>
        ''' Collect user-defined operators.
        ''' Operators declared in the same type are grouped together. 
        ''' Within a group, name1 operators are followed by name2 operators.
        ''' </summary>
        Friend Shared Sub CollectUserDefinedOperators(
            type1 As TypeSymbol,
            type2 As TypeSymbol,
            opKind As MethodKind,
            name1 As String,
            name1Info As OperatorInfo,
            name2Opt As String,
            name2InfoOpt As OperatorInfo,
            opSet As ArrayBuilder(Of MethodSymbol),
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        )
            type1 = GetTypeToLookForOperatorsIn(type1, useSiteDiagnostics)

            If type2 IsNot Nothing Then
                type2 = GetTypeToLookForOperatorsIn(type2, useSiteDiagnostics)
            End If

            Dim commonAncestor As NamedTypeSymbol = Nothing

            If type1 IsNot Nothing AndAlso type1.Kind = SymbolKind.NamedType AndAlso Not type1.IsInterfaceType() Then
                Dim current = DirectCast(type1, NamedTypeSymbol)

                Do
                    If type2 IsNot Nothing AndAlso commonAncestor Is Nothing AndAlso
                       type2.IsOrDerivedFrom(current, useSiteDiagnostics) Then
                        commonAncestor = current
                    End If

                    ' Note, intentionally using non-short-circuiting Or operator.
                    If CollectUserDefinedOperators(current, name1, opKind, name1Info, opSet) Or
                       (name2Opt IsNot Nothing AndAlso CollectUserDefinedOperators(current, name2Opt, opKind, name2InfoOpt, opSet)) Then
                        Exit Do
                    End If

                    current = current.BaseTypeWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                Loop While current IsNot Nothing
            End If

            If type2 IsNot Nothing AndAlso type2.Kind = SymbolKind.NamedType AndAlso Not type2.IsInterfaceType() Then
                Dim current = DirectCast(type2, NamedTypeSymbol)

                Do
                    If commonAncestor IsNot Nothing AndAlso commonAncestor.IsSameTypeIgnoringAll(current) Then
                        Exit Do
                    End If

                    ' Note, intentionally using non-short-circuiting Or operator.
                    If CollectUserDefinedOperators(current, name1, opKind, name1Info, opSet) Or
                       (name2Opt IsNot Nothing AndAlso CollectUserDefinedOperators(current, name2Opt, opKind, name2InfoOpt, opSet)) Then
                        Exit Do
                    End If

                    current = current.BaseTypeWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                Loop While current IsNot Nothing
            End If

        End Sub

        ''' <summary>
        ''' Returns True if we should stop climbing inheritance hierarchy.
        ''' </summary>
        Private Shared Function CollectUserDefinedOperators(
            type As TypeSymbol,
            opName As String,
            opKind As MethodKind,
            opInfo As OperatorInfo,
            opSet As ArrayBuilder(Of MethodSymbol)
        ) As Boolean
            Dim stopClimbing As Boolean = False

            For Each member In type.GetMembers(opName)
                If member.Kind = SymbolKind.Method Then
                    Dim method = DirectCast(member, MethodSymbol)

                    If method.MethodKind = opKind Then
                        If method.IsShadows Then
                            stopClimbing = True
                        End If

                        ' Operators that were declared in syntax may not satisfy all the constraints on user-defined operators -
                        ' they require extra validation.
                        If Not method.IsMethodKindBasedOnSyntax OrElse ValidateOverloadedOperator(method.OriginalDefinition, opInfo) Then
                            opSet.Add(method)
                        End If
                    End If
                End If
            Next

            Return stopClimbing
        End Function

        ''' <summary>
        ''' Given the type of operator's argument, return corresponding type to
        ''' look for operator in. Can return Nothing.
        ''' </summary>
        Private Shared Function GetTypeToLookForOperatorsIn(type As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As TypeSymbol
            type = type.GetNullableUnderlyingTypeOrSelf()

            If type.Kind = SymbolKind.TypeParameter Then
                type = DirectCast(type, TypeParameterSymbol).GetNonInterfaceConstraint(useSiteDiagnostics)
            End If

            Return type
        End Function

        Public Shared Function ResolveIsTrueOperator(argument As BoundExpression, binder As Binder, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As OverloadResolutionResult
            Dim opSet = ArrayBuilder(Of MethodSymbol).GetInstance()

            CollectUserDefinedOperators(argument.Type, Nothing, MethodKind.UserDefinedOperator,
                                        WellKnownMemberNames.TrueOperatorName, New OperatorInfo(UnaryOperatorKind.IsTrue),
                                        Nothing, Nothing,
                                        opSet, useSiteDiagnostics)

            Dim result = OperatorInvocationOverloadResolution(opSet, argument, Nothing, binder, lateBindingIsAllowed:=False, includeEliminatedCandidates:=False,
                                                              useSiteDiagnostics:=useSiteDiagnostics)
            opSet.Free()
            Return result
        End Function

        Public Shared Function ResolveIsFalseOperator(argument As BoundExpression, binder As Binder, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As OverloadResolutionResult
            Dim opSet = ArrayBuilder(Of MethodSymbol).GetInstance()

            CollectUserDefinedOperators(argument.Type, Nothing, MethodKind.UserDefinedOperator,
                                        WellKnownMemberNames.FalseOperatorName, New OperatorInfo(UnaryOperatorKind.IsFalse),
                                        Nothing, Nothing,
                                        opSet, useSiteDiagnostics)

            Dim result = OperatorInvocationOverloadResolution(opSet, argument, Nothing, binder, lateBindingIsAllowed:=False, includeEliminatedCandidates:=False,
                                                              useSiteDiagnostics:=useSiteDiagnostics)
            opSet.Free()
            Return result
        End Function

        Public Shared Function ResolveUserDefinedUnaryOperator(
            argument As BoundExpression,
            opKind As UnaryOperatorKind,
            binder As Binder,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
            Optional includeEliminatedCandidates As Boolean = False
        ) As OverloadResolutionResult
            Dim opSet = ArrayBuilder(Of MethodSymbol).GetInstance()

            Select Case opKind
                Case UnaryOperatorKind.Not
                    Dim opInfo As New OperatorInfo(UnaryOperatorKind.Not)
                    CollectUserDefinedOperators(argument.Type, Nothing, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.OnesComplementOperatorName, opInfo,
                                                WellKnownMemberNames.LogicalNotOperatorName, opInfo,
                                                opSet, useSiteDiagnostics)
                Case UnaryOperatorKind.Minus
                    CollectUserDefinedOperators(argument.Type, Nothing, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.UnaryNegationOperatorName, New OperatorInfo(UnaryOperatorKind.Minus),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case UnaryOperatorKind.Plus
                    CollectUserDefinedOperators(argument.Type, Nothing, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.UnaryPlusOperatorName, New OperatorInfo(UnaryOperatorKind.Minus),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select

            Dim result = OperatorInvocationOverloadResolution(opSet, argument, Nothing, binder, lateBindingIsAllowed:=False, includeEliminatedCandidates:=includeEliminatedCandidates,
                                                              useSiteDiagnostics:=useSiteDiagnostics)
            opSet.Free()
            Return result
        End Function

        Public Shared Function ResolveUserDefinedBinaryOperator(
            left As BoundExpression,
            right As BoundExpression,
            opKind As BinaryOperatorKind,
            binder As Binder,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
            Optional includeEliminatedCandidates As Boolean = False
        ) As OverloadResolutionResult
            Dim opSet = ArrayBuilder(Of MethodSymbol).GetInstance()

            Select Case opKind
                Case BinaryOperatorKind.Add
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.AdditionOperatorName, New OperatorInfo(opKind),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.Subtract
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.SubtractionOperatorName, New OperatorInfo(opKind),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.Multiply
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.MultiplyOperatorName, New OperatorInfo(opKind),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.Divide
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.DivisionOperatorName, New OperatorInfo(opKind),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.IntegerDivide
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.IntegerDivisionOperatorName, New OperatorInfo(opKind),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.Modulo
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.ModulusOperatorName, New OperatorInfo(opKind),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.Power
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.ExponentOperatorName, New OperatorInfo(opKind),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.Equals
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.EqualityOperatorName, New OperatorInfo(opKind),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.NotEquals
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.InequalityOperatorName, New OperatorInfo(opKind),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.LessThan
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.LessThanOperatorName, New OperatorInfo(opKind),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.GreaterThan
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.GreaterThanOperatorName, New OperatorInfo(opKind),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.LessThanOrEqual
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.LessThanOrEqualOperatorName, New OperatorInfo(opKind),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.GreaterThanOrEqual
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.GreaterThanOrEqualOperatorName, New OperatorInfo(opKind),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.Like
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.LikeOperatorName, New OperatorInfo(opKind),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.Concatenate
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.ConcatenateOperatorName, New OperatorInfo(opKind),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.And, BinaryOperatorKind.AndAlso
                    Dim opInfo As New OperatorInfo(opKind)
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.BitwiseAndOperatorName, opInfo,
                                                WellKnownMemberNames.LogicalAndOperatorName, opInfo,
                                                opSet, useSiteDiagnostics)

                Case BinaryOperatorKind.Or, BinaryOperatorKind.OrElse
                    Dim opInfo As New OperatorInfo(opKind)
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.BitwiseOrOperatorName, opInfo,
                                                WellKnownMemberNames.LogicalOrOperatorName, opInfo,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.Xor
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.ExclusiveOrOperatorName, New OperatorInfo(opKind),
                                                Nothing, Nothing,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.LeftShift
                    Dim opInfo As New OperatorInfo(opKind)
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.LeftShiftOperatorName, opInfo,
                                                WellKnownMemberNames.UnsignedLeftShiftOperatorName, opInfo,
                                                opSet, useSiteDiagnostics)
                Case BinaryOperatorKind.RightShift
                    Dim opInfo As New OperatorInfo(opKind)
                    CollectUserDefinedOperators(left.Type, right.Type, MethodKind.UserDefinedOperator,
                                                WellKnownMemberNames.RightShiftOperatorName, opInfo,
                                                WellKnownMemberNames.UnsignedRightShiftOperatorName, opInfo,
                                                opSet, useSiteDiagnostics)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select

            Dim result = OperatorInvocationOverloadResolution(opSet, left, right, binder, lateBindingIsAllowed:=True, includeEliminatedCandidates:=includeEliminatedCandidates,
                                                              useSiteDiagnostics:=useSiteDiagnostics)
            opSet.Free()
            Return result
        End Function

        Private Shared Function OperatorInvocationOverloadResolution(
            opSet As ArrayBuilder(Of MethodSymbol),
            argument1 As BoundExpression,
            argument2 As BoundExpression,
            binder As Binder,
            lateBindingIsAllowed As Boolean,
            includeEliminatedCandidates As Boolean,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As OverloadResolutionResult

            If opSet.Count = 0 Then
                If lateBindingIsAllowed Then
                    Return New OverloadResolutionResult(ImmutableArray(Of CandidateAnalysisResult).Empty,
                                                        argument1.Type.IsObjectType OrElse argument2.Type.IsObjectType,
                                                        False,
                                                        asyncLambdaSubToFunctionMismatch:=Nothing)
                Else
                    Return New OverloadResolutionResult(ImmutableArray(Of CandidateAnalysisResult).Empty, False, False, asyncLambdaSubToFunctionMismatch:=Nothing)
                End If
            End If

            Dim nullableOfT As NamedTypeSymbol = opSet(0).ContainingAssembly.GetSpecialType(SpecialType.System_Nullable_T)
            Dim liftOperators As Boolean = nullableOfT.GetUseSiteErrorInfo() Is Nothing

            Dim candidates = ArrayBuilder(Of CandidateAnalysisResult).GetInstance()

            For Each method In opSet
                Debug.Assert(method.ParameterCount = If(argument2 Is Nothing, 1, 2))
                Debug.Assert(method.Arity = 0)
                Debug.Assert(Not method.IsSub)

                If method.HasUnsupportedMetadata Then
                    If includeEliminatedCandidates Then
                        candidates.Add(New CandidateAnalysisResult(New OperatorCandidate(method), CandidateAnalysisResultState.HasUnsupportedMetadata))
                    End If

                    Continue For
                End If

                Dim useSiteErrorInfo As DiagnosticInfo = method.GetUseSiteErrorInfo()

                If useSiteErrorInfo IsNot Nothing Then
                    If useSiteDiagnostics Is Nothing Then
                        useSiteDiagnostics = New HashSet(Of DiagnosticInfo)()
                    End If

                    useSiteDiagnostics.Add(useSiteErrorInfo)

                    If includeEliminatedCandidates Then
                        candidates.Add(New CandidateAnalysisResult(New OperatorCandidate(method), CandidateAnalysisResultState.HasUseSiteError))
                    End If

                    Continue For
                End If

                CombineCandidates(candidates, New CandidateAnalysisResult(New OperatorCandidate(method)), method.ParameterCount, Nothing, useSiteDiagnostics)

                If liftOperators Then
                    Dim param1 As ParameterSymbol = method.Parameters(0)
                    Dim type1 As TypeSymbol = param1.Type
                    Dim isNullable1 As Boolean = type1.IsNullableType()
                    Dim canLift1 As Boolean = Not isNullable1 AndAlso type1.IsValueType() AndAlso Not type1.IsRestrictedType()

                    Dim param2 As ParameterSymbol = Nothing
                    Dim type2 As TypeSymbol = Nothing
                    Dim isNullable2 As Boolean = False
                    Dim canLift2 As Boolean = False

                    If argument2 IsNot Nothing AndAlso Not isNullable1 Then
                        param2 = method.Parameters(1)
                        type2 = param2.Type
                        isNullable2 = type2.IsNullableType()
                        canLift2 = Not isNullable2 AndAlso type2.IsValueType() AndAlso Not type2.IsRestrictedType()
                    End If

                    If (canLift1 OrElse canLift2) AndAlso Not isNullable1 AndAlso Not isNullable2 Then
                        ' Should lift this operator.
                        If canLift1 Then
                            param1 = LiftParameterSymbol(param1, nullableOfT)
                        End If

                        If canLift2 Then
                            param2 = LiftParameterSymbol(param2, nullableOfT)
                        End If

                        Dim returnType As TypeSymbol = method.ReturnType
                        If CanLiftType(returnType) Then
                            returnType = nullableOfT.Construct(returnType)
                        End If

                        CombineCandidates(candidates,
                                          New CandidateAnalysisResult(New LiftedOperatorCandidate(method,
                                                                                               If(argument2 Is Nothing,
                                                                                                  ImmutableArray.Create(param1),
                                                                                                  ImmutableArray.Create(Of ParameterSymbol)(param1, param2)),
                                                                                              returnType)),
                                          method.ParameterCount, Nothing, useSiteDiagnostics)
                    End If
                End If
            Next

            ' TODO: Need to get rid of dependency on method group. For now create fake one.
            Dim methodGroup = New BoundMethodGroup(argument1.Syntax, Nothing, ImmutableArray(Of MethodSymbol).Empty, LookupResultKind.Good, Nothing, QualificationKind.Unqualified)

            Dim result As OverloadResolutionResult = ResolveOverloading(methodGroup, candidates,
                                                                        If(argument2 Is Nothing,
                                                                           ImmutableArray.Create(argument1),
                                                                           ImmutableArray.Create(Of BoundExpression)(argument1, argument2)),
                                                                        Nothing, Nothing, lateBindingIsAllowed, binder:=binder,
                                                                        asyncLambdaSubToFunctionMismatch:=Nothing,
                                                                        callerInfoOpt:=Nothing, forceExpandedForm:=False,
                                                                        useSiteDiagnostics:=useSiteDiagnostics)
            candidates.Free()

            Return result
        End Function

        Friend Shared Function CanLiftType(type As TypeSymbol) As Boolean
            Return Not type.IsNullableType AndAlso type.IsValueType() AndAlso Not type.IsRestrictedType()
        End Function

        Friend Shared Function IsValidInLiftedSignature(type As TypeSymbol) As Boolean
            Return type.IsNullableType() ' Note, Dev11 has changed implementation of this function, I've taken this into account. 
        End Function

        Private Shared Function LiftParameterSymbol(param As ParameterSymbol, nullableOfT As NamedTypeSymbol) As ParameterSymbol

            If param.IsDefinition Then
                Return New LiftedParameterSymbol(param, nullableOfT.Construct(param.Type))
            Else
                Dim definition As ParameterSymbol = param.OriginalDefinition

                Return SubstitutedParameterSymbol.CreateMethodParameter(DirectCast(param.ContainingSymbol, SubstitutedMethodSymbol),
                                                                        New LiftedParameterSymbol(definition, nullableOfT.Construct(definition.Type)))
            End If
        End Function

        Private NotInheritable Class LiftedParameterSymbol
            Inherits ParameterSymbol

            Private ReadOnly _parameterToLift As ParameterSymbol
            Private ReadOnly _type As TypeSymbol

            Public Sub New(parameter As ParameterSymbol, type As TypeSymbol)
                Debug.Assert(parameter.IsDefinition)
                Debug.Assert(type.IsNullableType())
                _parameterToLift = parameter
                _type = type
            End Sub

            Public Overrides ReadOnly Property Name As String
                Get
                    Return _parameterToLift.Name
                End Get
            End Property

            Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
                Return _parameterToLift.GetUseSiteErrorInfo()
            End Function

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return _parameterToLift.ContainingSymbol
                End Get
            End Property

            Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return _parameterToLift.CustomModifiers
                End Get
            End Property

            Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return _parameterToLift.RefCustomModifiers
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Return ImmutableArray(Of SyntaxReference).Empty
                End Get
            End Property

            Friend Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
                Get
                    Return _parameterToLift.ExplicitDefaultConstantValue(inProgress)
                End Get
            End Property

            Public Overrides ReadOnly Property HasExplicitDefaultValue As Boolean
                Get
                    Return _parameterToLift.HasExplicitDefaultValue
                End Get
            End Property

            Public Overrides ReadOnly Property IsByRef As Boolean
                Get
                    Return _parameterToLift.IsByRef
                End Get
            End Property

            Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
                Get
                    Return _parameterToLift.IsExplicitByRef
                End Get
            End Property

            Public Overrides ReadOnly Property IsOptional As Boolean
                Get
                    Return _parameterToLift.IsOptional
                End Get
            End Property

            Friend Overrides ReadOnly Property IsMetadataOut As Boolean
                Get
                    Return _parameterToLift.IsMetadataOut
                End Get
            End Property

            Friend Overrides ReadOnly Property IsMetadataIn As Boolean
                Get
                    Return _parameterToLift.IsMetadataIn
                End Get
            End Property

            Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
                Get
                    Return _parameterToLift.MarshallingInformation
                End Get
            End Property


            Friend Overrides ReadOnly Property HasOptionCompare As Boolean
                Get
                    Return _parameterToLift.HasOptionCompare
                End Get
            End Property

            Friend Overrides ReadOnly Property IsIDispatchConstant As Boolean
                Get
                    Return _parameterToLift.IsIDispatchConstant
                End Get
            End Property

            Friend Overrides ReadOnly Property IsIUnknownConstant As Boolean
                Get
                    Return _parameterToLift.IsIUnknownConstant
                End Get
            End Property

            Friend Overrides ReadOnly Property IsCallerLineNumber As Boolean
                Get
                    Return _parameterToLift.IsCallerLineNumber
                End Get
            End Property

            Friend Overrides ReadOnly Property IsCallerMemberName As Boolean
                Get
                    Return _parameterToLift.IsCallerMemberName
                End Get
            End Property

            Friend Overrides ReadOnly Property IsCallerFilePath As Boolean
                Get
                    Return _parameterToLift.IsCallerFilePath
                End Get
            End Property

            Public Overrides ReadOnly Property IsParamArray As Boolean
                Get
                    Return _parameterToLift.IsParamArray
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Return ImmutableArray(Of Location).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property Ordinal As Integer
                Get
                    Return _parameterToLift.Ordinal
                End Get
            End Property

            Public Overrides ReadOnly Property Type As TypeSymbol
                Get
                    Return _type
                End Get
            End Property
        End Class
    End Class

End Namespace
