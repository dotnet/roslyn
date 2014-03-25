' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------
' Contains expression evaluator for preprocessor expressions.
'-----------------------------------------------------------------------------

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.TypeHelpers
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Structure ExpressionEvaluator
        Private ReadOnly _symbols As ImmutableDictionary(Of String, CConst)

        ' PERF: Using Byte instead of TypeCode because we want the compiler to use array literal initialization.
        '       The most natural type choice, Enum arrays, are not blittable due to a CLR limitation.
        Private Shared ReadOnly _dominantType(,) As Byte

        Shared Sub New()

            Const _____Byte = CType(TypeCode.Byte, Byte)
            Const ____SByte = CType(TypeCode.SByte, Byte)
            Const ____Int16 = CType(TypeCode.Int16, Byte)
            Const ___UInt16 = CType(TypeCode.UInt16, Byte)
            Const ____Int32 = CType(TypeCode.Int32, Byte)
            Const ___UInt32 = CType(TypeCode.UInt32, Byte)
            Const ____Int64 = CType(TypeCode.Int64, Byte)
            Const ___UInt64 = CType(TypeCode.UInt64, Byte)
            Const ___Single = CType(TypeCode.Single, Byte)
            Const ___Double = CType(TypeCode.Double, Byte)
            Const __Decimal = CType(TypeCode.Decimal, Byte)
            Const _DateTime = CType(TypeCode.DateTime, Byte)
            Const _____Char = CType(TypeCode.Char, Byte)
            Const __Boolean = CType(TypeCode.Boolean, Byte)
            Const ___String = CType(TypeCode.String, Byte)
            Const ___Object = CType(TypeCode.Object, Byte)

            '    _____Byte, ____SByte, ____Int16, ___UInt16, ____Int32, ___UInt32, ____Int64, ___UInt64, ___Single, ___Double, __Decimal, _DateTime, _____Char, __Boolean, ___String, ___Object
            _dominantType =
            {
                {_____Byte, ___Object, ____Int16, ___UInt16, ____Int32, ___UInt32, ____Int64, ___UInt64, ___Single, ___Double, __Decimal, ___Object, ___Object, ___Object, ___Object, ___Object}, ' Byte
                {___Object, ____SByte, ____Int16, ___Object, ____Int32, ___Object, ____Int64, ___Object, ___Single, ___Double, __Decimal, ___Object, ___Object, ___Object, ___Object, ___Object}, ' SByte
                {____Int16, ____Int16, ____Int16, ___Object, ____Int32, ___Object, ____Int64, ___Object, ___Single, ___Double, __Decimal, ___Object, ___Object, ___Object, ___Object, ___Object}, ' Int16
                {___UInt16, ___Object, ___Object, ___UInt16, ____Int32, ___UInt32, ____Int64, ___UInt64, ___Single, ___Double, __Decimal, ___Object, ___Object, ___Object, ___Object, ___Object}, ' UInt16
                {____Int32, ____Int32, ____Int32, ____Int32, ____Int32, ___Object, ____Int64, ___Object, ___Single, ___Double, __Decimal, ___Object, ___Object, ___Object, ___Object, ___Object}, ' Int32
                {___UInt32, ___Object, ___Object, ___UInt32, ___Object, ___UInt32, ____Int64, ___UInt64, ___Single, ___Double, __Decimal, ___Object, ___Object, ___Object, ___Object, ___Object}, ' UInt32
                {____Int64, ____Int64, ____Int64, ____Int64, ____Int64, ____Int64, ____Int64, ___Object, ___Single, ___Double, __Decimal, ___Object, ___Object, ___Object, ___Object, ___Object}, ' Int64
                {___UInt64, ___Object, ___Object, ___UInt64, ___Object, ___UInt64, ___Object, ___UInt64, ___Single, ___Double, __Decimal, ___Object, ___Object, ___Object, ___Object, ___Object}, ' UInt64
                {___Single, ___Single, ___Single, ___Single, ___Single, ___Single, ___Single, ___Single, ___Single, ___Double, ___Single, ___Object, ___Object, ___Object, ___Object, ___Object}, ' Single
                {___Double, ___Double, ___Double, ___Double, ___Double, ___Double, ___Double, ___Double, ___Double, ___Double, ___Double, ___Object, ___Object, ___Object, ___Object, ___Object}, ' Double
                {__Decimal, __Decimal, __Decimal, __Decimal, __Decimal, __Decimal, __Decimal, __Decimal, ___Single, ___Double, __Decimal, ___Object, ___Object, ___Object, ___Object, ___Object}, ' Decimal
                {___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, _DateTime, ___Object, ___Object, ___Object, ___Object}, ' DateTime
                {___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, _____Char, ___Object, ___String, ___Object}, ' Char
                {___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, __Boolean, ___Object, ___Object}, ' Boolean
                {___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___String, ___Object, ___String, ___Object}, ' String
                {___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object, ___Object}  ' Object
            }

#If DEBUG Then
            Debug.Assert(_dominantType.GetLength(0) = _dominantType.GetLength(1)) ' 2d array must be square
            For i As Integer = 0 To _dominantType.GetLength(0) - 1
                For j As Integer = i + 1 To _dominantType.GetLength(1) - 1
                    Debug.Assert(_dominantType(i, j) = _dominantType(j, i))
                Next
            Next
#End If
        End Sub

        Private Shared Function TypeCodeToDominantTypeIndex(code As TypeCode) As Integer
            Select Case code
                Case TypeCode.Byte
                    Return 0
                Case TypeCode.SByte
                    Return 1
                Case TypeCode.Int16
                    Return 2
                Case TypeCode.UInt16
                    Return 3
                Case TypeCode.Int32
                    Return 4
                Case TypeCode.UInt32
                    Return 5
                Case TypeCode.Int64
                    Return 6
                Case TypeCode.UInt64
                    Return 7
                Case TypeCode.Single
                    Return 8
                Case TypeCode.Double
                    Return 9
                Case TypeCode.Decimal
                    Return 10
                Case TypeCode.DateTime
                    Return 11
                Case TypeCode.Char
                    Return 12
                Case TypeCode.Boolean
                    Return 13
                Case TypeCode.String
                    Return 14
                Case TypeCode.Object
                    Return 15
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(code)
            End Select
        End Function

        Private Sub New(symbols As ImmutableDictionary(Of String, CConst))
            _symbols = symbols
        End Sub

        Public Shared Function EvaluateCondition(expr As ExpressionSyntax,
                    Optional symbols As ImmutableDictionary(Of String, CConst) = Nothing) As CConst

            If expr.ContainsDiagnostics Then
                Return New BadCConst(0)
            End If

            Dim value = EvaluateExpression(expr, symbols)
            If value.IsBad Then
                Return value
            End If

            Return ConvertToBool(value, expr)
        End Function

        Public Shared Function EvaluateExpression(expr As ExpressionSyntax,
                    Optional symbols As ImmutableDictionary(Of String, CConst) = Nothing) As CConst

            If expr.ContainsDiagnostics Then
                Return New BadCConst(0)
            End If

            Dim eval As New ExpressionEvaluator(symbols)

            Return eval.EvaluateExpressionInternal(expr)
        End Function

        Private Function EvaluateExpressionInternal(expr As ExpressionSyntax) As CConst
            Debug.Assert(expr IsNot Nothing)

            Select Case expr.Kind
                Case SyntaxKind.TrueLiteralExpression,
                    SyntaxKind.FalseLiteralExpression,
                    SyntaxKind.CharacterLiteralExpression,
                    SyntaxKind.DateLiteralExpression,
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxKind.NothingLiteralExpression,
                    SyntaxKind.StringLiteralExpression

                    Return EvaluateLiteralExpression(DirectCast(expr, LiteralExpressionSyntax))

                Case SyntaxKind.ParenthesizedExpression
                    Return EvaluateParenthesizedExpression(DirectCast(expr, ParenthesizedExpressionSyntax))

                Case SyntaxKind.IdentifierName
                    Return EvaluateIdentifierNameExpression(DirectCast(expr, IdentifierNameSyntax))

                Case SyntaxKind.PredefinedCastExpression
                    Return EvaluatePredefinedCastExpression(DirectCast(expr, PredefinedCastExpressionSyntax))

                Case SyntaxKind.CTypeExpression
                    Return EvaluateCTypeExpression(DirectCast(expr, CastExpressionSyntax))

                Case SyntaxKind.DirectCastExpression
                    Return EvaluateDirectCastExpression(DirectCast(expr, CastExpressionSyntax))

                Case SyntaxKind.TryCastExpression
                    Return EvaluateTryCastExpression(DirectCast(expr, CastExpressionSyntax))

                Case SyntaxKind.UnaryMinusExpression,
                    SyntaxKind.UnaryPlusExpression,
                    SyntaxKind.NotExpression

                    Return EvaluateUnaryExpression(DirectCast(expr, UnaryExpressionSyntax))

                Case SyntaxKind.AddExpression,
                    SyntaxKind.SubtractExpression,
                    SyntaxKind.MultiplyExpression,
                    SyntaxKind.DivideExpression,
                    SyntaxKind.IntegerDivideExpression,
                    SyntaxKind.ModuloExpression,
                    SyntaxKind.ExponentiateExpression,
                    SyntaxKind.EqualsExpression,
                    SyntaxKind.NotEqualsExpression,
                    SyntaxKind.LessThanExpression,
                    SyntaxKind.GreaterThanExpression,
                    SyntaxKind.LessThanOrEqualExpression,
                    SyntaxKind.GreaterThanOrEqualExpression,
                    SyntaxKind.ConcatenateExpression,
                    SyntaxKind.AndExpression,
                    SyntaxKind.OrExpression,
                    SyntaxKind.ExclusiveOrExpression,
                    SyntaxKind.AndAlsoExpression,
                    SyntaxKind.OrElseExpression,
                    SyntaxKind.LeftShiftExpression,
                    SyntaxKind.RightShiftExpression

                    Return EvaluateBinaryExpression(DirectCast(expr, BinaryExpressionSyntax))

                Case SyntaxKind.BinaryConditionalExpression
                    Return EvaluateBinaryIfExpression(DirectCast(expr, BinaryConditionalExpressionSyntax))

                Case SyntaxKind.TernaryConditionalExpression
                    Return EvaluateTernaryIfExpression(DirectCast(expr, TernaryConditionalExpressionSyntax))

            End Select
            Return ReportSemanticError(ERRID.ERR_BadCCExpression, expr)
        End Function

        Private Shared Function ReportSemanticError(id As ERRID,
                                            ExpressionLocation As VisualBasicSyntaxNode,
                                            ParamArray args As Object()) As BadCConst

            ' TODO: should we use the node?
            Return New BadCConst(id, args)
        End Function

        Private Shared Function EvaluateLiteralExpression(expr As LiteralExpressionSyntax) As CConst
            Dim token = expr.Token
            If expr.ContainsDiagnostics Then
                Return ReportSemanticError(ERRID.ERR_BadCCExpression, expr)
            End If

            Select Case token.Kind
                Case SyntaxKind.TrueKeyword
                    Return CConst.Create(True)

                Case SyntaxKind.FalseKeyword
                    Return CConst.Create(False)

                Case SyntaxKind.CharacterLiteralToken
                    Dim typedToken = DirectCast(token, CharacterLiteralTokenSyntax)
                    Return CConst.Create(typedToken.Value)

                Case SyntaxKind.DateLiteralToken
                    Dim typedToken = DirectCast(token, DateLiteralTokenSyntax)
                    Return CConst.Create(typedToken.Value)

                Case SyntaxKind.DecimalLiteralToken
                    Dim typedToken = DirectCast(token, DecimalLiteralTokenSyntax)
                    Return CConst.Create(typedToken.Value)

                Case SyntaxKind.FloatingLiteralToken
                    Dim typedToken = DirectCast(token, FloatingLiteralTokenSyntax)
                    Return CConst.Create(typedToken.ObjectValue)

                Case SyntaxKind.IntegerLiteralToken
                    Dim typedToken = DirectCast(token, IntegerLiteralTokenSyntax)
                    Return CConst.Create(typedToken.ObjectValue)

                Case SyntaxKind.NothingKeyword
                    Return CConst.Create(Nothing)

                Case SyntaxKind.StringLiteralToken
                    Dim typedToken = DirectCast(token, StringLiteralTokenSyntax)
                    Return CConst.Create(typedToken.Value)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(token.Kind)
            End Select

        End Function

        Private Function EvaluateParenthesizedExpression(expr As ParenthesizedExpressionSyntax) As CConst
            Return EvaluateExpressionInternal(expr.Expression)
        End Function

        Private Function EvaluateIdentifierNameExpression(expr As IdentifierNameSyntax) As CConst
            If _symbols Is Nothing Then
                Return CConst.Create(Nothing)
            End If

            Dim identOrKw = expr.Identifier

            Dim ident = TryCast(identOrKw, IdentifierTokenSyntax)
            Dim name As String
            If ident IsNot Nothing Then
                name = ident.IdentifierText
            Else
                name = identOrKw.Text
            End If

            Dim value As CConst = Nothing
            If Not _symbols.TryGetValue(name, value) Then
                Return CConst.Create(Nothing)
            End If

            If value.IsBad Then
                ' we used to treat the const as bad without giving any error.
                ' not sure if this correct behavior.
                Return ReportSemanticError(0, expr)
            End If

            Dim typeChar = ident.TypeCharacter
            If typeChar <> TypeCharacter.None Then
                Dim expectedTypechar = AsTypeChar(value.TypeCode)
                If expectedTypechar <> typeChar Then
                    Dim spelling As Char = TypeCharSpelling(typeChar)
                    Return ReportSemanticError(ERRID.ERR_TypecharNoMatch2, expr, spelling, expectedTypechar)
                End If
            End If

            Return value
        End Function

        Private Shared Function TypeCharSpelling(typeChar As TypeCharacter) As Char
            Select Case (typeChar)
                Case TypeCharacter.Integer
                    Return "%"c

                Case TypeCharacter.Long
                    Return "&"c

                Case TypeCharacter.Decimal
                    Return "@"c

                Case TypeCharacter.Single
                    Return "!"c

                Case TypeCharacter.Double
                    Return "#"c

                Case TypeCharacter.String
                    Return "$"c

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(typeChar)
            End Select
        End Function

        Private Shared Function AsTypeChar(code As TypeCode) As TypeCharacter
            Select Case (code)
                Case TypeCode.Int32
                    Return TypeCharacter.Integer

                Case TypeCode.Int64
                    Return TypeCharacter.Long

                Case TypeCode.Decimal
                    Return TypeCharacter.Decimal

                Case TypeCode.Single
                    Return TypeCharacter.Single

                Case TypeCode.Double
                    Return TypeCharacter.Double

                Case TypeCode.String
                    Return TypeCharacter.String

                Case Else
                    Return TypeCharacter.None
            End Select
        End Function

        Private Shared Function AsTypeCode(predefinedType As PredefinedTypeSyntax) As TypeCode
            Dim kind = predefinedType.Keyword.Kind
            Select Case (kind)
                Case SyntaxKind.ShortKeyword
                    Return TypeCode.Int16

                Case SyntaxKind.UShortKeyword
                    Return TypeCode.UInt16

                Case SyntaxKind.IntegerKeyword
                    Return TypeCode.Int32

                Case SyntaxKind.UIntegerKeyword
                    Return TypeCode.UInt32

                Case SyntaxKind.LongKeyword
                    Return TypeCode.Int64

                Case SyntaxKind.ULongKeyword
                    Return TypeCode.UInt64

                Case SyntaxKind.DecimalKeyword
                    Return TypeCode.Decimal

                Case SyntaxKind.SingleKeyword
                    Return TypeCode.Single

                Case SyntaxKind.DoubleKeyword
                    Return TypeCode.Double

                Case SyntaxKind.SByteKeyword
                    Return TypeCode.SByte

                Case SyntaxKind.ByteKeyword
                    Return TypeCode.Byte

                Case SyntaxKind.BooleanKeyword
                    Return TypeCode.Boolean

                Case SyntaxKind.CharKeyword
                    Return TypeCode.Char

                Case SyntaxKind.DateKeyword
                    Return TypeCode.DateTime

                Case SyntaxKind.StringKeyword
                    Return TypeCode.String

                Case SyntaxKind.VariantKeyword,
                    SyntaxKind.ObjectKeyword
                    Return TypeCode.Object

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(kind)
            End Select
        End Function

        Private Function EvaluateTryCastExpression(expr As CastExpressionSyntax) As CConst
            Dim val = EvaluateExpressionInternal(expr.Expression)

            Dim predefinedType = TryCast(expr.Type, PredefinedTypeSyntax)
            If predefinedType Is Nothing Then
                Return ReportSemanticError(ERRID.ERR_BadTypeInCCExpression, expr.Type)
            End If

            Dim tc = AsTypeCode(predefinedType)

            If tc <> TypeCode.Object AndAlso tc <> TypeCode.String Then
                Return ReportSemanticError(ERRID.ERR_TryCastOfValueType1, expr.Type)
            End If

            If val.TypeCode = TypeCode.Object OrElse
                val.TypeCode = TypeCode.String Then

                Return Convert(val, tc, expr)
            End If

            If val.TypeCode = tc Then
                If tc = TypeCode.Double OrElse tc = TypeCode.Single Then
                    Return ReportSemanticError(ERRID.ERR_IdentityDirectCastForFloat, expr.Type)
                Else
                    Return ReportSemanticError(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, expr.Type)
                End If
            End If

            Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr.Type, val.TypeCode, tc)
        End Function

        Private Function EvaluateDirectCastExpression(expr As CastExpressionSyntax) As CConst
            Dim val = EvaluateExpressionInternal(expr.Expression)

            Dim predefinedType = TryCast(expr.Type, PredefinedTypeSyntax)
            If predefinedType Is Nothing Then
                Return ReportSemanticError(ERRID.ERR_BadTypeInCCExpression, expr.Type)
            End If

            Dim tc = AsTypeCode(predefinedType)

            If val.TypeCode = TypeCode.Object OrElse
                val.TypeCode = TypeCode.String Then

                Return Convert(val, tc, expr)
            End If

            If val.TypeCode = tc Then
                If tc = TypeCode.Double OrElse tc = TypeCode.Single Then
                    Return ReportSemanticError(ERRID.ERR_IdentityDirectCastForFloat, expr.Type)
                Else
                    Dim result = Convert(val, tc, expr)
                    result = result.WithError(ERRID.WRN_ObsoleteIdentityDirectCastForValueType)
                    Return result
                End If
            End If

            Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr.Type, val.TypeCode, tc)
        End Function

        Private Function EvaluateCTypeExpression(expr As CastExpressionSyntax) As CConst
            Dim val = EvaluateExpressionInternal(expr.Expression)

            Dim predefinedType = TryCast(expr.Type, PredefinedTypeSyntax)
            If predefinedType Is Nothing Then
                Return ReportSemanticError(ERRID.ERR_BadTypeInCCExpression, expr.Type)
            End If

            Dim tc = AsTypeCode(predefinedType)

            Return Convert(val, tc, expr)
        End Function

        Private Function EvaluatePredefinedCastExpression(expr As PredefinedCastExpressionSyntax) As CConst
            Dim val = EvaluateExpressionInternal(expr.Expression)

            Dim tc As TypeCode

            Select Case expr.Keyword.Kind
                Case SyntaxKind.CBoolKeyword
                    tc = TypeCode.Boolean

                Case SyntaxKind.CDateKeyword
                    tc = TypeCode.DateTime

                Case SyntaxKind.CDblKeyword
                    tc = TypeCode.Double

                Case SyntaxKind.CSByteKeyword
                    tc = TypeCode.SByte

                Case SyntaxKind.CByteKeyword
                    tc = TypeCode.Byte

                Case SyntaxKind.CCharKeyword
                    tc = TypeCode.Char

                Case SyntaxKind.CShortKeyword
                    tc = TypeCode.Int16

                Case SyntaxKind.CUShortKeyword
                    tc = TypeCode.UInt16

                Case SyntaxKind.CIntKeyword
                    tc = TypeCode.Int32

                Case SyntaxKind.CUIntKeyword
                    tc = TypeCode.UInt32

                Case SyntaxKind.CLngKeyword
                    tc = TypeCode.Int64

                Case SyntaxKind.CULngKeyword
                    tc = TypeCode.UInt64

                Case SyntaxKind.CSngKeyword
                    tc = TypeCode.Single

                Case SyntaxKind.CStrKeyword
                    tc = TypeCode.String

                Case SyntaxKind.CDecKeyword
                    tc = TypeCode.Decimal

                Case SyntaxKind.CObjKeyword
                    Return ConvertToObject(val, expr)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(expr.Keyword.Kind)
            End Select

            Return Convert(val, tc, expr)
        End Function

        Private Function EvaluateBinaryIfExpression(expr As BinaryConditionalExpressionSyntax) As CConst
            Dim op = EvaluateExpressionInternal(expr.FirstExpression)

            Dim value As Object = op.ValueAsObject

            If value IsNot Nothing Then
                If value.GetType.IsValueType Then
                    Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
                Else
                    Return op
                End If
            End If

            Return EvaluateExpressionInternal(expr.SecondExpression)
        End Function

        Private Function EvaluateTernaryIfExpression(expr As TernaryConditionalExpressionSyntax) As CConst
            Dim condition = EvaluateExpressionInternal(expr.Condition)

            If condition.IsBad Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            Dim cond = ConvertToBool(condition, expr)
            If cond.IsBad Then
                Return cond
            Else
                Dim whenTrue As CConst = EvaluateExpressionInternal(expr.WhenTrue)
                Dim whenFalse As CConst = EvaluateExpressionInternal(expr.WhenFalse)

                If Not whenTrue.IsBad AndAlso Not whenFalse.IsBad Then
                    If IsNothing(whenTrue) Then
                        If Not IsNothing(whenFalse) AndAlso whenFalse.TypeCode <> TypeCode.Object Then
                            whenTrue = Convert(whenTrue, whenFalse.TypeCode, expr.WhenTrue)
                        End If
                    ElseIf IsNothing(whenFalse) Then
                        If whenTrue.TypeCode <> TypeCode.Object Then
                            whenFalse = Convert(whenFalse, whenTrue.TypeCode, expr.WhenFalse)
                        End If
                    Else
                        Dim dominantType As TypeCode = CType(_dominantType(TypeCodeToDominantTypeIndex(whenTrue.TypeCode), TypeCodeToDominantTypeIndex(whenFalse.TypeCode)), TypeCode)

                        If dominantType <> whenTrue.TypeCode Then
                            whenTrue = Convert(whenTrue, dominantType, expr.WhenTrue)
                        End If

                        If dominantType <> whenFalse.TypeCode Then
                            whenFalse = Convert(whenFalse, dominantType, expr.WhenFalse)
                        End If
                    End If
                End If

                If whenTrue.IsBad Then
                    Return whenTrue
                End If

                If whenFalse.IsBad Then
                    Return whenFalse
                End If

                Return If(DirectCast(cond, CConst(Of Boolean)).Value, whenTrue, whenFalse)
            End If
        End Function

        Private Shared Function ConvertToBool(val As CConst, expr As ExpressionSyntax) As CConst
            If val.IsBad Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            Dim tc = val.TypeCode
            If tc = TypeCode.Boolean Then
                Return DirectCast(val, CConst(Of Boolean))
            End If

            If TypeHelpers.IsNumericType(tc) Then
                Return CConst.Create(CBool(val.ValueAsObject))
            End If

            Select Case tc
                Case TypeCode.Char
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, TypeCode.Char, TypeCode.Boolean)
                Case TypeCode.DateTime
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, TypeCode.DateTime, TypeCode.Boolean)
                Case TypeCode.Object
                    If val.ValueAsObject Is Nothing Then
                        Return CConst.Create(CBool(Nothing))
                    Else
                        Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
                    End If
                Case TypeCode.String
                    Return ReportSemanticError(ERRID.ERR_RequiredConstConversion2, expr, TypeCode.String, TypeCode.Boolean)
                Case Else
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, tc, TypeCode.Boolean)
            End Select
        End Function

        Private Shared Function ConvertToNumeric(val As CConst, tcTo As TypeCode, expr As ExpressionSyntax) As CConst
            Debug.Assert(TypeHelpers.IsNumericType(tcTo))

            If val.IsBad Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            ' nothing for numeric conversions is as good as 0
            If IsNothing(val) Then
                val = CConst.Create(0)
            End If

            Dim tc = val.TypeCode
            If tc = tcTo Then
                Return val
            End If

            Try
                If TypeHelpers.IsNumericType(tc) Then
                    Return CConst.Create(System.Convert.ChangeType(val.ValueAsObject, tcTo))
                End If
            Catch ex As OverflowException
                Return ReportSemanticError(ERRID.ERR_ExpressionOverflow1, expr, tcTo)
            End Try

            Select Case tc
                Case TypeCode.Boolean
                    Dim tv = DirectCast(val, CConst(Of Boolean))
                    Dim numericVal As Long = CLng(tv.Value)
                    If TypeHelpers.IsUnsignedIntegralType(tcTo) Then
                        numericVal = NarrowIntegralResult(numericVal, TypeCode.Int64, tcTo, False)
                    End If
                    Return CConst.Create(System.Convert.ChangeType(numericVal, tcTo))
                Case TypeCode.Char
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, TypeCode.Char, tcTo)
                Case TypeCode.DateTime
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, TypeCode.DateTime, tcTo)
                Case TypeCode.Object
                    Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
                Case TypeCode.String
                    Return ReportSemanticError(ERRID.ERR_RequiredConstConversion2, expr, TypeCode.String, tcTo)
                Case Else
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, tc, tcTo)
            End Select
        End Function

        Private Shared Function Convert(val As CConst, tcTo As TypeCode, expr As ExpressionSyntax) As CConst
            If val.IsBad Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            Dim tc = val.TypeCode
            If tc = tcTo Then
                Return val
            End If

            If TypeHelpers.IsNumericType(tcTo) Then
                Return ConvertToNumeric(val, tcTo, expr)
            End If

            Select Case tcTo
                Case TypeCode.Boolean
                    Return ConvertToBool(val, expr)
                Case TypeCode.Char
                    Return ConvertToChar(val, expr)
                Case TypeCode.DateTime
                    Return ConvertToDate(val, expr)
                Case TypeCode.Object
                    Return ConvertToObject(val, expr)
                Case TypeCode.String
                    Return ConvertToString(val, expr)
                Case Else
                    Return ReportSemanticError(ERRID.ERR_CannotConvertValue2, expr, tc, tcTo)
            End Select
        End Function

        Private Shared Function ConvertToChar(val As CConst, expr As ExpressionSyntax) As CConst
            If val.IsBad Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            Dim tc = val.TypeCode
            If tc = TypeCode.Char Then
                Return DirectCast(val, CConst(Of Char))
            End If

            If TypeHelpers.IsIntegralType(tc) Then
                Return ReportSemanticError(ERRID.ERR_IntegralToCharTypeMismatch1, expr, tc)
            End If

            Select Case tc
                Case TypeCode.Boolean
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, tc, TypeCode.Char)
                Case TypeCode.DateTime
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, tc, TypeCode.Char)
                Case TypeCode.Object
                    If val.ValueAsObject Is Nothing Then
                        Return CConst.Create(CChar(Nothing))
                    Else
                        Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
                    End If
                Case TypeCode.String
                    Dim tv = DirectCast(val, CConst(Of String))
                    Dim ch = If(tv.Value Is Nothing, CChar(Nothing), CChar(tv.Value))
                    Return CConst.Create(ch)
                Case Else
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, tc, TypeCode.Char)
            End Select
        End Function

        Private Shared Function ConvertToDate(val As CConst, expr As ExpressionSyntax) As CConst
            If val.IsBad Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            Dim tc = val.TypeCode
            If tc = TypeCode.DateTime Then
                Return DirectCast(val, CConst(Of DateTime))
            End If

            If TypeHelpers.IsIntegralType(tc) Then
                Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, tc, TypeCode.DateTime)
            End If

            Select Case tc
                Case TypeCode.Boolean
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, tc, TypeCode.DateTime)
                Case TypeCode.Char
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, tc, TypeCode.DateTime)
                Case TypeCode.String
                    Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
                Case TypeCode.Object
                    If val.ValueAsObject Is Nothing Then
                        Return CConst.Create(CDate(Nothing))
                    Else
                        Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
                    End If
                Case Else
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, tc, TypeCode.DateTime)
            End Select
        End Function

        Private Shared Function ConvertToString(val As CConst, expr As ExpressionSyntax) As CConst
            If val.IsBad Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            Dim tc = val.TypeCode
            If tc = TypeCode.String Then
                Return DirectCast(val, CConst(Of String))
            End If

            If TypeHelpers.IsIntegralType(tc) Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            Select Case tc
                Case TypeCode.Boolean
                    Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
                Case TypeCode.Char
                    Dim tv = DirectCast(val, CConst(Of Char))
                    Return CConst.Create(CStr(tv.Value))
                Case TypeCode.DateTime
                    Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
                Case TypeCode.Object
                    If val.ValueAsObject Is Nothing Then
                        Return CConst.Create(CStr(Nothing))
                    Else
                        Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
                    End If
                Case Else
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, tc, TypeCode.String)
            End Select
        End Function

        Private Shared Function ConvertToObject(value As CConst, expr As ExpressionSyntax) As CConst
            If value.IsBad Then
                Return value
            End If

            If IsNothing(value) Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            Return ReportSemanticError(ERRID.ERR_RequiredConstConversion2, expr, value.TypeCode, TypeCode.Object)
        End Function

        Private Function EvaluateUnaryExpression(expr As UnaryExpressionSyntax) As CConst
            Dim val = EvaluateExpressionInternal(expr.Operand)
            Dim tc = val.TypeCode

            If tc = TypeCode.Empty Then
                Return ReportSemanticError(ERRID.ERR_BadCCExpression, expr)
            End If

            If tc = TypeCode.String Then
                Return ReportSemanticError(ERRID.ERR_CannotConvertValue2, expr)
            End If

            If tc = TypeCode.Object AndAlso Not IsNothing(val) Then
                Return ReportSemanticError(ERRID.ERR_CannotConvertValue2, expr)
            End If

            If tc = TypeCode.Char OrElse tc = TypeCode.DateTime Then
                Return ReportSemanticError(ERRID.ERR_CannotConvertValue2, expr)
            End If

            Try
                Select Case expr.Kind
                    Case SyntaxKind.UnaryMinusExpression
                        If IsNothing(val) Then
                            Return CConst.Create(-Nothing)
                        End If
                        Select Case tc
                            Case TypeCode.Boolean
                                Return CConst.Create(-(CShort(DirectCast(val, CConst(Of Boolean)).Value)))
                            Case TypeCode.Byte
                                Return CConst.Create(-(DirectCast(val, CConst(Of Byte)).Value))
                            Case TypeCode.Decimal
                                Return CConst.Create(-(DirectCast(val, CConst(Of Decimal)).Value))
                            Case TypeCode.Double
                                Return CConst.Create(-(DirectCast(val, CConst(Of Double)).Value))
                            Case TypeCode.Int16
                                Return CConst.Create(-(DirectCast(val, CConst(Of Int16)).Value))
                            Case TypeCode.Int32
                                Return CConst.Create(-(DirectCast(val, CConst(Of Int32)).Value))
                            Case TypeCode.Int64
                                Return CConst.Create(-(DirectCast(val, CConst(Of Int64)).Value))
                            Case TypeCode.SByte
                                Return CConst.Create(-(DirectCast(val, CConst(Of SByte)).Value))
                            Case TypeCode.Single
                                Return CConst.Create(-(DirectCast(val, CConst(Of Single)).Value))
                            Case TypeCode.UInt16
                                Return CConst.Create(-(DirectCast(val, CConst(Of UInt16)).Value))
                            Case TypeCode.UInt32
                                Return CConst.Create(-(DirectCast(val, CConst(Of UInt32)).Value))
                            Case TypeCode.UInt64
                                Return CConst.Create(-(DirectCast(val, CConst(Of UInt64)).Value))
                            Case Else
                                Throw ExceptionUtilities.UnexpectedValue(tc)
                        End Select

                    Case SyntaxKind.UnaryPlusExpression
                        If tc = TypeCode.Boolean Then
                            Return CConst.Create(+(CShort(DirectCast(val, CConst(Of Boolean)).Value)))
                        End If
                        Return val

                    Case SyntaxKind.NotExpression
                        If IsNothing(val) Then
                            Return CConst.Create(Not Nothing)
                        End If
                        Select Case tc
                            Case TypeCode.Boolean
                                Return CConst.Create(Not (DirectCast(val, CConst(Of Boolean)).Value))
                            Case TypeCode.Byte
                                Return CConst.Create(Not (DirectCast(val, CConst(Of Byte)).Value))
                            Case TypeCode.Decimal
                                Return CConst.Create(Not CLng(DirectCast(val, CConst(Of Decimal)).Value))
                            Case TypeCode.Double
                                Return CConst.Create(Not CLng(DirectCast(val, CConst(Of Double)).Value))
                            Case TypeCode.Int16
                                Return CConst.Create(Not (DirectCast(val, CConst(Of Int16)).Value))
                            Case TypeCode.Int32
                                Return CConst.Create(Not (DirectCast(val, CConst(Of Int32)).Value))
                            Case TypeCode.Int64
                                Return CConst.Create(Not (DirectCast(val, CConst(Of Int64)).Value))
                            Case TypeCode.SByte
                                Return CConst.Create(Not (DirectCast(val, CConst(Of SByte)).Value))
                            Case TypeCode.Single
                                Return CConst.Create(Not CLng(DirectCast(val, CConst(Of Single)).Value))
                            Case TypeCode.UInt16
                                Return CConst.Create(Not (DirectCast(val, CConst(Of UInt16)).Value))
                            Case TypeCode.UInt32
                                Return CConst.Create(Not (DirectCast(val, CConst(Of UInt32)).Value))
                            Case TypeCode.UInt64
                                Return CConst.Create(Not (DirectCast(val, CConst(Of UInt64)).Value))
                            Case Else
                                Throw ExceptionUtilities.UnexpectedValue(tc)
                        End Select
                End Select
            Catch ex As OverflowException
                Return ReportSemanticError(ERRID.ERR_ExpressionOverflow1, expr)
            End Try

            Throw ExceptionUtilities.UnexpectedValue(expr)
        End Function

        Private Shared Function IsNothing(val As CConst) As Boolean
            Return val.TypeCode = TypeCode.Object AndAlso val.ValueAsObject Is Nothing
        End Function

        Private Function EvaluateBinaryExpression(expr As BinaryExpressionSyntax) As CConst
            Dim Left = EvaluateExpressionInternal(expr.Left)
            Dim Right = EvaluateExpressionInternal(expr.Right)
            Dim BoundOpcode = expr.Kind

            If Left.IsBad OrElse Right.IsBad Then
                Return ReportSemanticError(ERRID.ERR_BadCCExpression, expr)
            End If

            Dim OperandType As TypeCode = Nothing

            If IsNothing(Left) OrElse IsNothing(Right) Then
                If IsNothing(Left) AndAlso IsNothing(Right) Then
                    ' // Comparing Nothing and Nothing succeeds, and operations
                    ' // that provide an explicit type succeed.
                    ' // And and Or succeed with a result type of Integer.
                    ' // Everything else is rejected.
                    ' //
                    ' // The only reason these matter is for conditional compilation
                    ' // expressions that refer to undefined constants.

                    Select Case BoundOpcode
                        Case SyntaxKind.ConcatenateExpression,
                            SyntaxKind.LikeExpression

                            Right = ConvertToString(Right, expr.Right)

                        Case SyntaxKind.OrElseExpression,
                            SyntaxKind.AndAlsoExpression

                            Right = ConvertToBool(Right, expr.Right)

                        Case SyntaxKind.IsExpression,
                            SyntaxKind.IsNotExpression,
                            SyntaxKind.EqualsExpression,
                            SyntaxKind.NotEqualsExpression,
                            SyntaxKind.LessThanExpression,
                            SyntaxKind.LessThanOrEqualExpression,
                            SyntaxKind.GreaterThanOrEqualExpression,
                            SyntaxKind.GreaterThanExpression,
                            SyntaxKind.AddExpression,
                            SyntaxKind.MultiplyExpression,
                            SyntaxKind.DivideExpression,
                            SyntaxKind.SubtractExpression,
                            SyntaxKind.ExponentiateExpression,
                            SyntaxKind.IntegerDivideExpression,
                            SyntaxKind.LeftShiftExpression,
                            SyntaxKind.RightShiftExpression,
                            SyntaxKind.ModuloExpression

                            ' // Treating the operation as if its operands are integers
                            ' // gives correct results.

                            Right = ConvertToNumeric(Right, TypeCode.Int32, expr.Right)

                        Case SyntaxKind.OrExpression,
                            SyntaxKind.AndExpression,
                            SyntaxKind.ExclusiveOrExpression

                            Right = ConvertToNumeric(Right, TypeCode.Int32, expr.Right)

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(expr)
                    End Select
                End If

                If IsNothing(Left) Then
                    OperandType = Right.TypeCode

                    Select Case (BoundOpcode)

                        Case SyntaxKind.ConcatenateExpression,
                            SyntaxKind.LikeExpression

                            ' // For & and Like, a Nothing operand is typed String unless the other operand
                            ' // is non-intrinsic 
                            OperandType = TypeCode.String

                        Case SyntaxKind.LeftShiftExpression,
                            SyntaxKind.RightShiftExpression

                            ' // Nothing should default to Integer for Shift operations.
                            OperandType = TypeCode.Int32

                    End Select

                    Left = Convert(Left, OperandType, expr.Left)

                ElseIf IsNothing(Right) Then
                    OperandType = Left.TypeCode

                    Select Case (BoundOpcode)

                        Case SyntaxKind.ConcatenateExpression,
                            SyntaxKind.LikeExpression

                            ' // For & and Like, a Nothing operand is typed String unless the other operand
                            ' // is non-intrinsic
                            OperandType = TypeCode.String
                    End Select

                    Right = Convert(Right, OperandType, expr.Right)
                End If
            End If

            ' // For comparison operators, the result type computed here is not
            ' // the result type of the comparison (which is typically boolean),
            ' // but is the type to which the operands are to be converted. For
            ' // other operators, the type computed here is both the result type
            ' // and the common operand type.

            Dim ResultType = LookupInOperatorTables(BoundOpcode, Left.TypeCode, Right.TypeCode)

            If ResultType = TypeCode.Empty Then
                Return ReportSemanticError(ERRID.ERR_BadTypeInCCExpression, expr)
            End If

            OperandType = ResultType
            Left = Convert(Left, OperandType, expr.Left)
            If Left.IsBad Then
                Return Left
            End If

            Right = Convert(Right, OperandType, expr.Right)
            If Right.IsBad Then
                Return Right
            End If

            Select Case BoundOpcode
                Case SyntaxKind.AddExpression
                    If ResultType = TypeCode.String Then
                        ' // Transform the addition into a string concatenation.
                        BoundOpcode = SyntaxKind.ConcatenateExpression
                    End If
                Case _
                        SyntaxKind.EqualsExpression,
                        SyntaxKind.NotEqualsExpression,
                        SyntaxKind.LessThanOrEqualExpression,
                        SyntaxKind.GreaterThanOrEqualExpression,
                        SyntaxKind.LessThanExpression,
                        SyntaxKind.GreaterThanExpression

                    ResultType = TypeCode.Boolean
            End Select

            Dim Result = PerformCompileTimeBinaryOperation(
                    BoundOpcode,
                    ResultType,
                    Left,
                    Right,
                    expr)

            Return Result
        End Function

        Private Shared Function PerformCompileTimeBinaryOperation(Opcode As SyntaxKind,
                                                                  ResultType As TypeCode,
                                                                  Left As CConst,
                                                                  Right As CConst,
                                                                  expr As ExpressionSyntax) As CConst

            Debug.Assert(Opcode = SyntaxKind.LeftShiftExpression OrElse
                     Opcode = SyntaxKind.RightShiftExpression OrElse
                     Left.TypeCode = Right.TypeCode, "Binary operation on mismatched types.")

            If TypeHelpers.IsIntegralType(Left.TypeCode) OrElse Left.TypeCode = TypeCode.Char OrElse Left.TypeCode = TypeCode.DateTime Then
                Dim LeftValue As Int64 = TypeHelpers.UncheckedCLng(Left)
                Dim RightValue As Int64 = TypeHelpers.UncheckedCLng(Right)

                If ResultType = TypeCode.Boolean Then
                    Dim ComparisonSucceeds As Boolean = False

                    Select Case (Opcode)
                        Case SyntaxKind.EqualsExpression
                            ComparisonSucceeds =
                                If(TypeHelpers.IsUnsignedIntegralType(Left.TypeCode),
                                    UncheckedCULng(LeftValue) = UncheckedCULng(RightValue),
                                    LeftValue = RightValue)

                        Case SyntaxKind.NotEqualsExpression
                            ComparisonSucceeds =
                                If(TypeHelpers.IsUnsignedIntegralType(Left.TypeCode),
                                    UncheckedCULng(LeftValue) <> UncheckedCULng(RightValue),
                                    LeftValue <> RightValue)

                        Case SyntaxKind.LessThanOrEqualExpression
                            ComparisonSucceeds =
                                If(TypeHelpers.IsUnsignedIntegralType(Left.TypeCode),
                                    UncheckedCULng(LeftValue) <= UncheckedCULng(RightValue),
                                    LeftValue <= RightValue)

                        Case SyntaxKind.GreaterThanOrEqualExpression
                            ComparisonSucceeds =
                                If(TypeHelpers.IsUnsignedIntegralType(Left.TypeCode),
                                    UncheckedCULng(LeftValue) >= UncheckedCULng(RightValue),
                                    LeftValue >= RightValue)

                        Case SyntaxKind.LessThanExpression
                            ComparisonSucceeds = If(TypeHelpers.IsUnsignedIntegralType(Left.TypeCode),
                                UncheckedCULng(LeftValue) < UncheckedCULng(RightValue),
                                LeftValue < RightValue)

                        Case SyntaxKind.GreaterThanExpression
                            ComparisonSucceeds =
                                If(TypeHelpers.IsUnsignedIntegralType(Left.TypeCode),
                                    UncheckedCULng(LeftValue) > UncheckedCULng(RightValue),
                                    LeftValue > RightValue)

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(Opcode)
                    End Select
                    Return CConst.Create(ComparisonSucceeds)
                Else
                    ' // Compute the result in 64-bit arithmetic, and determine if the
                    ' // operation overflows the result type.

                    Dim ResultValue As Int64 = 0
                    Dim Overflow As Boolean = False

                    Select Case (Opcode)
                        Case SyntaxKind.AddExpression
                            ResultValue = NarrowIntegralResult(
                                LeftValue + RightValue,
                                Left.TypeCode,
                                ResultType,
                                Overflow)

                            If Not TypeHelpers.IsUnsignedIntegralType(ResultType) Then
                                If (RightValue > 0 AndAlso ResultValue < LeftValue) OrElse
                                    (RightValue < 0 AndAlso ResultValue > LeftValue) Then

                                    Overflow = True
                                End If

                            ElseIf (
                                UncheckedCULng(ResultValue) < UncheckedCULng(LeftValue)
                            ) Then
                                Overflow = True
                            End If

                        Case SyntaxKind.SubtractExpression
                            ResultValue = NarrowIntegralResult(
                                LeftValue - RightValue,
                                Left.TypeCode,
                                ResultType,
                                Overflow)

                            If Not TypeHelpers.IsUnsignedIntegralType(ResultType) Then
                                If (RightValue > 0 AndAlso ResultValue > LeftValue) OrElse
                                   (RightValue < 0 AndAlso ResultValue < LeftValue) Then

                                    Overflow = True
                                End If

                            ElseIf UncheckedCULng(ResultValue) > UncheckedCULng(LeftValue) Then
                                Overflow = True
                            End If

                        Case SyntaxKind.MultiplyExpression
                            ResultValue = Multiply(LeftValue, RightValue, Left.TypeCode, ResultType, Overflow)

                        Case SyntaxKind.IntegerDivideExpression
                            If RightValue = 0 Then
                                Return ReportSemanticError(ERRID.ERR_ZeroDivide, expr)
                            End If

                            ResultValue = NarrowIntegralResult(
                                If(TypeHelpers.IsUnsignedIntegralType(ResultType),
                                    CompileTimeCalculations.UncheckedCLng(UncheckedCULng(LeftValue) \ UncheckedCULng(RightValue)),
                                    UncheckedIntegralDiv(LeftValue, RightValue)),
                                Left.TypeCode,
                                ResultType,
                                Overflow)

                            If Not TypeHelpers.IsUnsignedIntegralType(ResultType) AndAlso LeftValue = Int64.MinValue AndAlso RightValue = -1 Then
                                Overflow = True
                            End If

                        Case SyntaxKind.ModuloExpression
                            If RightValue = 0 Then
                                Return ReportSemanticError(ERRID.ERR_ZeroDivide, expr)
                            End If

                            If TypeHelpers.IsUnsignedIntegralType(ResultType) Then
                                ResultValue = CompileTimeCalculations.UncheckedCLng(UncheckedCULng(LeftValue) Mod UncheckedCULng(RightValue))

                                ' // 64-bit processors crash on 0, -1 (Bug: dd71694)
                            ElseIf RightValue <> Not CType(0, Int64) Then
                                ResultValue = LeftValue Mod RightValue
                            Else
                                ResultValue = 0
                            End If

                        Case SyntaxKind.ExclusiveOrExpression
                            ResultValue = LeftValue Xor RightValue

                        Case SyntaxKind.OrExpression
                            ResultValue = LeftValue Or RightValue

                        Case SyntaxKind.AndExpression
                            ResultValue = LeftValue And RightValue

                        Case SyntaxKind.LeftShiftExpression
                            RightValue = RightValue And CodeGen.CodeGenerator.GetShiftSizeMask(Left.TypeCode)
                            ResultValue = LeftValue << CType(RightValue, Integer)

                            ' // Round-trip the result through a cast.  We do this for two reasons:
                            ' // a) Bits may have shifted off the end and need to be stripped away.
                            ' // b) The sign bit may have changed which requires the result to be sign-extended.

                            Dim OverflowTemp As Boolean = False
                            ResultValue = NarrowIntegralResult(ResultValue, Left.TypeCode, ResultType, OverflowTemp)

                        Case SyntaxKind.RightShiftExpression
                            RightValue = RightValue And CodeGen.CodeGenerator.GetShiftSizeMask(Left.TypeCode)
                            If TypeHelpers.IsUnsignedIntegralType(ResultType) Then
                                ResultValue = CompileTimeCalculations.UncheckedCLng((UncheckedCULng(LeftValue) >> CType(RightValue, Integer)))
                            Else
                                ResultValue = LeftValue >> CType(RightValue, Integer)
                            End If

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(Opcode)
                    End Select

                    If Overflow Then
                        Return ReportSemanticError(ERRID.ERR_ExpressionOverflow1, expr, ResultType)
                    End If

                    Return Convert(CConst.Create(ResultValue), ResultType, expr)
                End If
            ElseIf TypeHelpers.IsFloatingType(Left.TypeCode) Then
                Dim LeftValue As Double = CDbl(Left.ValueAsObject)
                Dim RightValue As Double = CDbl(Right.ValueAsObject)

                If ResultType = TypeCode.Boolean Then
                    Dim ComparisonSucceeds As Boolean = False
                    Select Case (Opcode)
                        Case SyntaxKind.EqualsExpression
                            ComparisonSucceeds = LeftValue = RightValue

                        Case SyntaxKind.NotEqualsExpression
                            ComparisonSucceeds = LeftValue <> RightValue

                        Case SyntaxKind.LessThanOrEqualExpression
                            ComparisonSucceeds = LeftValue <= RightValue

                        Case SyntaxKind.GreaterThanOrEqualExpression
                            ComparisonSucceeds = LeftValue >= RightValue

                        Case SyntaxKind.LessThanExpression
                            ComparisonSucceeds = LeftValue < RightValue

                        Case SyntaxKind.GreaterThanExpression
                            ComparisonSucceeds = LeftValue > RightValue

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(Opcode)
                    End Select

                    Return CConst.Create(If(ComparisonSucceeds, True, False))
                Else
                    Dim ResultValue As Double = 0
                    Dim Overflow As Boolean = False

                    Select Case (Opcode)
                        Case SyntaxKind.AddExpression
                            ResultValue = LeftValue + RightValue

                        Case SyntaxKind.SubtractExpression
                            ResultValue = LeftValue - RightValue

                        Case SyntaxKind.MultiplyExpression
                            ResultValue = LeftValue * RightValue

                        Case SyntaxKind.ExponentiateExpression
                            'IS_DBL_INFINITY(RightValue) 
                            If (
                                Double.IsInfinity(RightValue)
                            ) Then
                                'IS_DBL_ONE(LeftValue)
                                If LeftValue = 1.0 Then
                                    ResultValue = LeftValue
                                    Exit Select
                                End If

                                'IS_DBL_NEGATIVEONE(LeftValue)
                                If LeftValue = -1.0 Then
                                    ResultValue = Double.NaN
                                    Exit Select
                                End If

                            ElseIf (
                                Double.IsNaN(RightValue)
                            ) Then
                                ResultValue = Double.NaN
                                Exit Select
                            End If
                            ResultValue = Math.Pow(LeftValue, RightValue)

                        Case SyntaxKind.DivideExpression

                            ' TODO: verify with native code.

                            ' // We have decided not to detect zerodivide in compile-time
                            ' // evaluation of floating expressions.
#If 0 Then

                        If ( 
                            RightValue = 0 
                        )
                            ReportSemanticError( 
                                ERRID.ERR_ZeroDivide,
                                ExpressionLocation) : 

                            return AllocateBadExpression(ExpressionLocation) : 
                        End If
#End If
                            ResultValue = LeftValue / RightValue

                        Case SyntaxKind.ModuloExpression
                            ' // We have decided not to detect zerodivide in compile-time
                            ' // evaluation of floating expressions.
#If 0 Then

                        If ( 
                            RightValue = 0 
                        )
                            ReportSemanticError( 
                            ERRID.ERR_ZeroDivide,
                            ExpressionLocation) : 

                            return AllocateBadExpression(ExpressionLocation) : 
                        End If
#End If
                            ResultValue = Math.IEEERemainder(LeftValue, RightValue)
                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(Opcode)
                    End Select

                    ResultValue = NarrowFloatingResult(ResultValue, ResultType, Overflow)

                    ' // We have decided not to detect overflow in compile-time
                    ' // evaluation of floating expressions.
#If 0 Then

                If ( 
                    Overflow 
                )
                    ReportSemanticError( 
                    ERRID.ERR_ExpressionOverflow1,
                    ExpressionLocation,
                    ResultType) : 

                    return AllocateBadExpression(ExpressionLocation) : 
                End If
#End If

                    Return Convert(CConst.Create(ResultValue), ResultType, expr)
                End If
            ElseIf Left.TypeCode = TypeCode.Decimal Then
                Dim LeftValue As Decimal = CDec(Left.ValueAsObject)
                Dim RightValue As Decimal = CDec(Right.ValueAsObject)

                If ResultType = TypeCode.Boolean Then
                    Dim ComparisonSucceeds As Boolean = False
                    Dim ComparisonResult As Integer = LeftValue.CompareTo(RightValue)

                    Select Case (Opcode)
                        Case SyntaxKind.EqualsExpression
                            ComparisonSucceeds = (ComparisonResult = 0)

                        Case SyntaxKind.NotEqualsExpression
                            ComparisonSucceeds = Not (ComparisonResult = 0)

                        Case SyntaxKind.LessThanOrEqualExpression
                            ComparisonSucceeds = (ComparisonResult <= 0)

                        Case SyntaxKind.GreaterThanOrEqualExpression
                            ComparisonSucceeds = (ComparisonResult >= 0)

                        Case SyntaxKind.LessThanExpression
                            ComparisonSucceeds = (ComparisonResult < 0)

                        Case SyntaxKind.GreaterThanExpression
                            ComparisonSucceeds = (ComparisonResult > 0)

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(Opcode)
                    End Select

                    Return CConst.Create(ComparisonSucceeds)
                Else
                    Dim ResultValue As Decimal
                    Dim Overflow As Boolean = False

                    Select Case (Opcode)
                        Case SyntaxKind.AddExpression
                            Overflow = VarDecAdd(LeftValue, RightValue, ResultValue)

                        Case SyntaxKind.SubtractExpression
                            Overflow = VarDecSub(LeftValue, RightValue, ResultValue)

                        Case SyntaxKind.MultiplyExpression
                            Overflow = VarDecMul(LeftValue, RightValue, ResultValue)

                        Case SyntaxKind.DivideExpression
                            If RightValue = Decimal.Zero Then
                                Return ReportSemanticError(ERRID.ERR_ZeroDivide, expr)
                            End If
                            Overflow = VarDecDiv(LeftValue, RightValue, ResultValue)

                        Case SyntaxKind.ModuloExpression
                            If RightValue = Decimal.Zero Then
                                Return ReportSemanticError(ERRID.ERR_ZeroDivide, expr)
                            End If

                            ' // There is no VarDecMod, so we have to do this by hand
                            ' // result = L - (Fix(L / R) * R)

                            ' // CONSIDER (9/12/2003):  Doing this by hand generates Overflow
                            ' //                        for "Deciaml.MaxValue Mod 2" when it should
                            ' //                        generate a result of 1.  How to fix?

                            Overflow = VarDecDiv(LeftValue, RightValue, ResultValue)

                            If Not Overflow Then
                                ResultValue = Decimal.Truncate(ResultValue)
                                Overflow = VarDecMul(ResultValue, RightValue, ResultValue)
                                If Not Overflow Then
                                    Overflow = VarDecSub(LeftValue, ResultValue, ResultValue)
                                End If
                            End If

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(Opcode)
                    End Select

                    If Overflow Then
                        Return ReportSemanticError(ERRID.ERR_ExpressionOverflow1, expr, ResultType)
                    End If

                    Return CConst.Create(ResultValue)
                End If
            ElseIf Left.TypeCode = TypeCode.String Then

                ' Nothing strings should be treated the same as ""
                Dim LeftSpelling = If(CStr(Left.ValueAsObject), "")
                Dim RightSpelling = If(CStr(Right.ValueAsObject), "")

                Select Case (Opcode)
                    Case SyntaxKind.ConcatenateExpression
                        Dim ResultString As String = String.Concat(LeftSpelling, RightSpelling)
                        Return CConst.Create(ResultString)

                    Case SyntaxKind.GreaterThanExpression,
                        SyntaxKind.LessThanExpression,
                        SyntaxKind.GreaterThanOrEqualExpression,
                        SyntaxKind.LessThanOrEqualExpression,
                        SyntaxKind.EqualsExpression,
                        SyntaxKind.NotEqualsExpression

                        Dim StringComparisonSucceeds As Boolean = False

                        ' // ignore Option Text when conditional compilation(b112186)
                        Dim ComparisonResult = StringComparer.Ordinal.Compare(LeftSpelling, RightSpelling)

                        Select Case (Opcode)
                            Case SyntaxKind.EqualsExpression
                                StringComparisonSucceeds = ComparisonResult = 0

                            Case SyntaxKind.NotEqualsExpression
                                StringComparisonSucceeds = ComparisonResult <> 0

                            Case SyntaxKind.GreaterThanExpression
                                StringComparisonSucceeds = ComparisonResult > 0

                            Case SyntaxKind.GreaterThanOrEqualExpression
                                StringComparisonSucceeds = ComparisonResult >= 0

                            Case SyntaxKind.LessThanExpression
                                StringComparisonSucceeds = ComparisonResult < 0

                            Case SyntaxKind.LessThanOrEqualExpression
                                StringComparisonSucceeds = ComparisonResult <= 0

                        End Select
                        Return CConst.Create(StringComparisonSucceeds)

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(Opcode)
                End Select

            ElseIf Left.TypeCode = TypeCode.Boolean Then
                Dim LeftValue As Boolean = CBool(Left.ValueAsObject)
                Dim RightValue As Boolean = CBool(Right.ValueAsObject)

                Dim OperationSucceeds As Boolean = False

                Select Case (Opcode)
                    Case SyntaxKind.EqualsExpression
                        OperationSucceeds = LeftValue = RightValue

                    Case SyntaxKind.NotEqualsExpression
                        OperationSucceeds = LeftValue <> RightValue

                        ' // Amazingly, False > True.

                    Case SyntaxKind.GreaterThanExpression
                        OperationSucceeds = LeftValue = False AndAlso RightValue <> False

                    Case SyntaxKind.GreaterThanOrEqualExpression
                        OperationSucceeds = LeftValue = False OrElse RightValue <> False

                    Case SyntaxKind.LessThanExpression
                        OperationSucceeds = LeftValue <> False AndAlso RightValue = False

                    Case SyntaxKind.LessThanOrEqualExpression
                        OperationSucceeds = LeftValue <> False OrElse RightValue = False

                    Case SyntaxKind.ExclusiveOrExpression
                        OperationSucceeds = LeftValue Xor RightValue

                    Case SyntaxKind.OrElseExpression, SyntaxKind.OrExpression
                        OperationSucceeds = LeftValue Or RightValue

                    Case SyntaxKind.AndAlsoExpression, SyntaxKind.AndExpression
                        OperationSucceeds = LeftValue And RightValue

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(Opcode)
                End Select

                Return CConst.Create(OperationSucceeds)
            End If

            Throw ExceptionUtilities.Unreachable
        End Function
    End Structure
End Namespace
