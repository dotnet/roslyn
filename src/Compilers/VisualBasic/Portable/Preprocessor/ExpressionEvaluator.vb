' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------
' Contains expression evaluator for preprocessor expressions.
'-----------------------------------------------------------------------------

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Reflection
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.TypeHelpers

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Structure ExpressionEvaluator
        Private ReadOnly _symbols As ImmutableDictionary(Of String, CConst)

        ' PERF: Using Byte instead of SpecialType because we want the compiler to use array literal initialization.
        '       The most natural type choice, Enum arrays, are not blittable due to a CLR limitation.
        Private Shared ReadOnly s_dominantType(,) As Byte

        Shared Sub New()

            Const _____Byte = CType(SpecialType.System_Byte, Byte)
            Const ____SByte = CType(SpecialType.System_SByte, Byte)
            Const ____Int16 = CType(SpecialType.System_Int16, Byte)
            Const ___UInt16 = CType(SpecialType.System_UInt16, Byte)
            Const ____Int32 = CType(SpecialType.System_Int32, Byte)
            Const ___UInt32 = CType(SpecialType.System_UInt32, Byte)
            Const ____Int64 = CType(SpecialType.System_Int64, Byte)
            Const ___UInt64 = CType(SpecialType.System_UInt64, Byte)
            Const ___Single = CType(SpecialType.System_Single, Byte)
            Const ___Double = CType(SpecialType.System_Double, Byte)
            Const __Decimal = CType(SpecialType.System_Decimal, Byte)
            Const _DateTime = CType(SpecialType.System_DateTime, Byte)
            Const _____Char = CType(SpecialType.System_Char, Byte)
            Const __Boolean = CType(SpecialType.System_Boolean, Byte)
            Const ___String = CType(SpecialType.System_String, Byte)
            Const ___Object = CType(SpecialType.System_Object, Byte)

            '    _____Byte, ____SByte, ____Int16, ___UInt16, ____Int32, ___UInt32, ____Int64, ___UInt64, ___Single, ___Double, __Decimal, _DateTime, _____Char, __Boolean, ___String, ___Object
            s_dominantType =
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
            Debug.Assert(s_dominantType.GetLength(0) = s_dominantType.GetLength(1)) ' 2d array must be square
            For i As Integer = 0 To s_dominantType.GetLength(0) - 1
                For j As Integer = i + 1 To s_dominantType.GetLength(1) - 1
                    Debug.Assert(s_dominantType(i, j) = s_dominantType(j, i))
                Next
            Next
#End If
        End Sub

        Private Shared Function TypeCodeToDominantTypeIndex(specialType As SpecialType) As Integer
            Select Case specialType
                Case SpecialType.System_Byte
                    Return 0
                Case SpecialType.System_SByte
                    Return 1
                Case SpecialType.System_Int16
                    Return 2
                Case SpecialType.System_UInt16
                    Return 3
                Case SpecialType.System_Int32
                    Return 4
                Case SpecialType.System_UInt32
                    Return 5
                Case SpecialType.System_Int64
                    Return 6
                Case SpecialType.System_UInt64
                    Return 7
                Case SpecialType.System_Single
                    Return 8
                Case SpecialType.System_Double
                    Return 9
                Case SpecialType.System_Decimal
                    Return 10
                Case SpecialType.System_DateTime
                    Return 11
                Case SpecialType.System_Char
                    Return 12
                Case SpecialType.System_Boolean
                    Return 13
                Case SpecialType.System_String
                    Return 14
                Case SpecialType.System_Object
                    Return 15
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(specialType)
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

        Private Shared Function ReportSemanticError(id As ERRID, node As VisualBasicSyntaxNode) As BadCConst
            Return ReportSemanticError(id, node, SpecializedCollections.EmptyObjects)
        End Function

        Private Shared Function ReportSemanticError(id As ERRID, node As VisualBasicSyntaxNode, ParamArray args As Object()) As BadCConst
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
                    Return CConst.CreateChecked(typedToken.ObjectValue)

                Case SyntaxKind.IntegerLiteralToken
                    Dim typedToken = DirectCast(token, IntegerLiteralTokenSyntax)
                    Return CConst.CreateChecked(typedToken.ObjectValue)

                Case SyntaxKind.NothingKeyword
                    Return CConst.CreateNothing()

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
                Return CConst.CreateNothing()
            End If

            Dim ident = expr.Identifier

            Dim value As CConst = Nothing
            If Not _symbols.TryGetValue(ident.IdentifierText, value) Then
                Return CConst.CreateNothing()
            End If

            If value.IsBad Then
                ' we used to treat the const as bad without giving any error.
                ' not sure if this correct behavior.
                Return ReportSemanticError(0, expr)
            End If

            Dim typeChar = ident.TypeCharacter
            If typeChar <> TypeCharacter.None AndAlso typeChar <> AsTypeCharacter(value.SpecialType) Then
                Return ReportSemanticError(ERRID.ERR_TypecharNoMatch2, expr, GetDisplayString(typeChar), value.SpecialType.GetDisplayName())
            End If

            Return value
        End Function

        Private Shared Function GetDisplayString(typeChar As TypeCharacter) As String
            Select Case typeChar
                Case TypeCharacter.Integer
                    Return "%"

                Case TypeCharacter.Long
                    Return "&"

                Case TypeCharacter.Decimal
                    Return "@"

                Case TypeCharacter.Single
                    Return "!"

                Case TypeCharacter.Double
                    Return "#"

                Case TypeCharacter.String
                    Return "$"

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(typeChar)
            End Select
        End Function

        Private Shared Function AsTypeCharacter(specialType As SpecialType) As TypeCharacter
            Select Case specialType
                Case SpecialType.System_Int32
                    Return TypeCharacter.Integer

                Case SpecialType.System_Int64
                    Return TypeCharacter.Long

                Case SpecialType.System_Decimal
                    Return TypeCharacter.Decimal

                Case SpecialType.System_Single
                    Return TypeCharacter.Single

                Case SpecialType.System_Double
                    Return TypeCharacter.Double

                Case SpecialType.System_String
                    Return TypeCharacter.String

                Case Else
                    Return TypeCharacter.None
            End Select
        End Function

        Private Shared Function GetSpecialType(predefinedType As PredefinedTypeSyntax) As SpecialType
            Dim kind = predefinedType.Keyword.Kind
            Select Case (kind)
                Case SyntaxKind.ShortKeyword
                    Return SpecialType.System_Int16

                Case SyntaxKind.UShortKeyword
                    Return SpecialType.System_UInt16

                Case SyntaxKind.IntegerKeyword
                    Return SpecialType.System_Int32

                Case SyntaxKind.UIntegerKeyword
                    Return SpecialType.System_UInt32

                Case SyntaxKind.LongKeyword
                    Return SpecialType.System_Int64

                Case SyntaxKind.ULongKeyword
                    Return SpecialType.System_UInt64

                Case SyntaxKind.DecimalKeyword
                    Return SpecialType.System_Decimal

                Case SyntaxKind.SingleKeyword
                    Return SpecialType.System_Single

                Case SyntaxKind.DoubleKeyword
                    Return SpecialType.System_Double

                Case SyntaxKind.SByteKeyword
                    Return SpecialType.System_SByte

                Case SyntaxKind.ByteKeyword
                    Return SpecialType.System_Byte

                Case SyntaxKind.BooleanKeyword
                    Return SpecialType.System_Boolean

                Case SyntaxKind.CharKeyword
                    Return SpecialType.System_Char

                Case SyntaxKind.DateKeyword
                    Return SpecialType.System_DateTime

                Case SyntaxKind.StringKeyword
                    Return SpecialType.System_String

                Case SyntaxKind.VariantKeyword,
                    SyntaxKind.ObjectKeyword
                    Return SpecialType.System_Object

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(kind)
            End Select
        End Function

        Private Function EvaluateTryCastExpression(expr As CastExpressionSyntax) As CConst
            Dim value = EvaluateExpressionInternal(expr.Expression)

            Dim predefinedType = TryCast(expr.Type, PredefinedTypeSyntax)
            If predefinedType Is Nothing Then
                Return ReportSemanticError(ERRID.ERR_BadTypeInCCExpression, expr.Type)
            End If

            Dim specialType = GetSpecialType(predefinedType)

            If specialType <> SpecialType.System_Object AndAlso specialType <> SpecialType.System_String Then
                Return ReportSemanticError(ERRID.ERR_TryCastOfValueType1, expr.Type)
            End If

            If value.SpecialType = SpecialType.System_Object OrElse
               value.SpecialType = SpecialType.System_String Then

                Return Convert(value, specialType, expr)
            End If

            If value.SpecialType = specialType Then
                If specialType = SpecialType.System_Double OrElse specialType = SpecialType.System_Single Then
                    Return ReportSemanticError(ERRID.ERR_IdentityDirectCastForFloat, expr.Type)
                Else
                    Return ReportSemanticError(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, expr.Type)
                End If
            End If

            Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr.Type, value.SpecialType.GetDisplayName(), specialType.GetDisplayName())
        End Function

        Private Function EvaluateDirectCastExpression(expr As CastExpressionSyntax) As CConst
            Dim val = EvaluateExpressionInternal(expr.Expression)

            Dim predefinedType = TryCast(expr.Type, PredefinedTypeSyntax)
            If predefinedType Is Nothing Then
                Return ReportSemanticError(ERRID.ERR_BadTypeInCCExpression, expr.Type)
            End If

            Dim specialType = GetSpecialType(predefinedType)

            If val.SpecialType = SpecialType.System_Object OrElse
                val.SpecialType = SpecialType.System_String Then

                Return Convert(val, specialType, expr)
            End If

            If val.SpecialType = specialType Then
                If specialType = SpecialType.System_Double OrElse specialType = SpecialType.System_Single Then
                    Return ReportSemanticError(ERRID.ERR_IdentityDirectCastForFloat, expr.Type)
                Else
                    Dim result = Convert(val, specialType, expr)
                    result = result.WithError(ERRID.WRN_ObsoleteIdentityDirectCastForValueType)
                    Return result
                End If
            End If

            Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr.Type, val.SpecialType, specialType)
        End Function

        Private Function EvaluateCTypeExpression(expr As CastExpressionSyntax) As CConst
            Dim val = EvaluateExpressionInternal(expr.Expression)

            Dim predefinedType = TryCast(expr.Type, PredefinedTypeSyntax)
            If predefinedType Is Nothing Then
                Return ReportSemanticError(ERRID.ERR_BadTypeInCCExpression, expr.Type)
            End If

            Dim specialType = GetSpecialType(predefinedType)

            Return Convert(val, specialType, expr)
        End Function

        Private Function EvaluatePredefinedCastExpression(expr As PredefinedCastExpressionSyntax) As CConst
            Dim val = EvaluateExpressionInternal(expr.Expression)

            Dim specialType As SpecialType

            Select Case expr.Keyword.Kind
                Case SyntaxKind.CBoolKeyword
                    specialType = SpecialType.System_Boolean

                Case SyntaxKind.CDateKeyword
                    specialType = SpecialType.System_DateTime

                Case SyntaxKind.CDblKeyword
                    specialType = SpecialType.System_Double

                Case SyntaxKind.CSByteKeyword
                    specialType = SpecialType.System_SByte

                Case SyntaxKind.CByteKeyword
                    specialType = SpecialType.System_Byte

                Case SyntaxKind.CCharKeyword
                    specialType = SpecialType.System_Char

                Case SyntaxKind.CShortKeyword
                    specialType = SpecialType.System_Int16

                Case SyntaxKind.CUShortKeyword
                    specialType = SpecialType.System_UInt16

                Case SyntaxKind.CIntKeyword
                    specialType = SpecialType.System_Int32

                Case SyntaxKind.CUIntKeyword
                    specialType = SpecialType.System_UInt32

                Case SyntaxKind.CLngKeyword
                    specialType = SpecialType.System_Int64

                Case SyntaxKind.CULngKeyword
                    specialType = SpecialType.System_UInt64

                Case SyntaxKind.CSngKeyword
                    specialType = SpecialType.System_Single

                Case SyntaxKind.CStrKeyword
                    specialType = SpecialType.System_String

                Case SyntaxKind.CDecKeyword
                    specialType = SpecialType.System_Decimal

                Case SyntaxKind.CObjKeyword
                    Return ConvertToObject(val, expr)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(expr.Keyword.Kind)
            End Select

            Return Convert(val, specialType, expr)
        End Function

        Private Function EvaluateBinaryIfExpression(expr As BinaryConditionalExpressionSyntax) As CConst
            Dim op = EvaluateExpressionInternal(expr.FirstExpression)

            Dim value As Object = op.ValueAsObject

            If value IsNot Nothing Then
                If value.GetType().GetTypeInfo().IsValueType Then
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
                        If Not IsNothing(whenFalse) AndAlso whenFalse.SpecialType <> SpecialType.System_Object Then
                            whenTrue = Convert(whenTrue, whenFalse.SpecialType, expr.WhenTrue)
                        End If
                    ElseIf IsNothing(whenFalse) Then
                        If whenTrue.SpecialType <> SpecialType.System_Object Then
                            whenFalse = Convert(whenFalse, whenTrue.SpecialType, expr.WhenFalse)
                        End If
                    Else
                        Dim dominantType As SpecialType = CType(s_dominantType(TypeCodeToDominantTypeIndex(whenTrue.SpecialType), TypeCodeToDominantTypeIndex(whenFalse.SpecialType)), SpecialType)

                        If dominantType <> whenTrue.SpecialType Then
                            whenTrue = Convert(whenTrue, dominantType, expr.WhenTrue)
                        End If

                        If dominantType <> whenFalse.SpecialType Then
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

        Private Shared Function ConvertToBool(value As CConst, expr As ExpressionSyntax) As CConst
            If value.IsBad Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            Dim specialType = value.SpecialType
            If specialType = SpecialType.System_Boolean Then
                Return DirectCast(value, CConst(Of Boolean))
            End If

            If specialType.IsNumericType() Then
                Return CConst.Create(CBool(value.ValueAsObject))
            End If

            Select Case specialType
                Case SpecialType.System_Char
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, SpecialType.System_Char.GetDisplayName(), SpecialType.System_Boolean.GetDisplayName())
                Case SpecialType.System_DateTime
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, SpecialType.System_DateTime.GetDisplayName(), SpecialType.System_Boolean.GetDisplayName())
                Case SpecialType.System_Object
                    If value.ValueAsObject Is Nothing Then
                        Return CConst.Create(CBool(Nothing))
                    Else
                        Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
                    End If
                Case SpecialType.System_String
                    Return ReportSemanticError(ERRID.ERR_RequiredConstConversion2, expr, SpecialType.System_String.GetDisplayName(), SpecialType.System_Boolean.GetDisplayName())
                Case Else
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, specialType, SpecialType.System_Boolean.GetDisplayName())
            End Select
        End Function

        Private Shared Function ConvertToNumeric(value As CConst, toSpecialType As SpecialType, expr As ExpressionSyntax) As CConst
            Debug.Assert(toSpecialType.IsNumericType())

            If value.IsBad Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            ' nothing for numeric conversions is as good as 0
            If IsNothing(value) Then
                value = CConst.Create(0)
            End If

            Dim fromSpecialType = value.SpecialType
            If fromSpecialType = toSpecialType Then
                Return value
            End If

            If fromSpecialType.IsNumericType() Then
                Return ConvertNumericToNumeric(value, toSpecialType, expr)
            End If

            Select Case fromSpecialType
                Case SpecialType.System_Boolean
                    Dim tv = DirectCast(value, CConst(Of Boolean))
                    Dim numericVal As Long = CLng(tv.Value)
                    If toSpecialType.IsUnsignedIntegralType() Then
                        numericVal = NarrowIntegralResult(numericVal, SpecialType.System_Int64, toSpecialType, False)
                    End If
                    Return CConst.CreateChecked(System.Convert.ChangeType(numericVal, toSpecialType.ToRuntimeType(), CultureInfo.InvariantCulture))

                Case SpecialType.System_Char
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, SpecialType.System_Char.GetDisplayName(), toSpecialType.GetDisplayName())

                Case SpecialType.System_DateTime
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, SpecialType.System_DateTime.GetDisplayName(), toSpecialType.GetDisplayName())

                Case SpecialType.System_Object
                    Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)

                Case SpecialType.System_String
                    Return ReportSemanticError(ERRID.ERR_RequiredConstConversion2, expr, SpecialType.System_String.GetDisplayName(), toSpecialType.GetDisplayName())

                Case Else
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, fromSpecialType.GetDisplayName(), toSpecialType.GetDisplayName())
            End Select
        End Function

        Private Shared Function ConvertNumericToNumeric(value As CConst, toSpecialType As SpecialType, expr As ExpressionSyntax) As CConst
            Debug.Assert(value.SpecialType.IsNumericType())
            Debug.Assert(toSpecialType.IsNumericType())

            Try
                Return CConst.CreateChecked(System.Convert.ChangeType(value.ValueAsObject, toSpecialType.ToRuntimeType(), CultureInfo.InvariantCulture))
            Catch ex As OverflowException
                Return ReportSemanticError(ERRID.ERR_ExpressionOverflow1, expr, toSpecialType.GetDisplayName())
            End Try
        End Function

        Private Shared Function Convert(value As CConst, toSpecialType As SpecialType, expr As ExpressionSyntax) As CConst
            If value.IsBad Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            Dim fromSpecialType = value.SpecialType
            If fromSpecialType = toSpecialType Then
                Return value
            End If

            If toSpecialType.IsNumericType() Then
                Return ConvertToNumeric(value, toSpecialType, expr)
            End If

            Select Case toSpecialType
                Case SpecialType.System_Boolean
                    Return ConvertToBool(value, expr)
                Case SpecialType.System_Char
                    Return ConvertToChar(value, expr)
                Case SpecialType.System_DateTime
                    Return ConvertToDate(value, expr)
                Case SpecialType.System_Object
                    Return ConvertToObject(value, expr)
                Case SpecialType.System_String
                    Return ConvertToString(value, expr)
                Case Else
                    Return ReportSemanticError(ERRID.ERR_CannotConvertValue2, expr, fromSpecialType.GetDisplayName(), toSpecialType.GetDisplayName())
            End Select
        End Function

        Private Shared Function ConvertToChar(value As CConst, expr As ExpressionSyntax) As CConst
            If value.IsBad Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            Dim fromSpecialType = value.SpecialType
            If fromSpecialType = SpecialType.System_Char Then
                Return DirectCast(value, CConst(Of Char))
            End If

            If fromSpecialType.IsIntegralType() Then
                Return ReportSemanticError(ERRID.ERR_IntegralToCharTypeMismatch1, expr, fromSpecialType.GetDisplayName())
            End If

            Select Case fromSpecialType
                Case SpecialType.System_Boolean
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, fromSpecialType, SpecialType.System_Char.GetDisplayName())

                Case SpecialType.System_DateTime
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, fromSpecialType, SpecialType.System_Char.GetDisplayName())

                Case SpecialType.System_Object
                    If value.ValueAsObject Is Nothing Then
                        Return CConst.Create(CChar(Nothing))
                    Else
                        Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
                    End If

                Case SpecialType.System_String
                    Dim tv = DirectCast(value, CConst(Of String))
                    Dim ch = If(tv.Value Is Nothing, CChar(Nothing), CChar(tv.Value))
                    Return CConst.Create(ch)

                Case Else
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, fromSpecialType, SpecialType.System_Char.GetDisplayName())
            End Select
        End Function

        Private Shared Function ConvertToDate(value As CConst, expr As ExpressionSyntax) As CConst
            If value.IsBad Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            Dim fromSpecialType = value.SpecialType
            If fromSpecialType = SpecialType.System_DateTime Then
                Return DirectCast(value, CConst(Of DateTime))
            End If

            If fromSpecialType.IsIntegralType() Then
                Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, fromSpecialType, SpecialType.System_DateTime.GetDisplayName())
            End If

            Select Case fromSpecialType
                Case SpecialType.System_Boolean
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, fromSpecialType, SpecialType.System_DateTime.GetDisplayName())

                Case SpecialType.System_Char
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, fromSpecialType, SpecialType.System_DateTime.GetDisplayName())

                Case SpecialType.System_String
                    Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)

                Case SpecialType.System_Object
                    If value.ValueAsObject Is Nothing Then
                        Return CConst.Create(CDate(Nothing))
                    Else
                        Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
                    End If

                Case Else
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, fromSpecialType, SpecialType.System_DateTime.GetDisplayName())
            End Select
        End Function

        Private Shared Function ConvertToString(value As CConst, expr As ExpressionSyntax) As CConst
            If value.IsBad Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            Dim specialType = value.SpecialType
            If specialType = SpecialType.System_String Then
                Return DirectCast(value, CConst(Of String))
            End If

            If specialType.IsIntegralType() Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            Select Case specialType
                Case SpecialType.System_Boolean
                    Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)

                Case SpecialType.System_Char
                    Dim tv = DirectCast(value, CConst(Of Char))
                    Return CConst.Create(CStr(tv.Value))

                Case SpecialType.System_DateTime
                    Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)

                Case SpecialType.System_Object
                    If value.ValueAsObject Is Nothing Then
                        Return CConst.Create(CStr(Nothing))
                    Else
                        Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
                    End If

                Case Else
                    Return ReportSemanticError(ERRID.ERR_TypeMismatch2, expr, specialType, SpecialType.System_String.GetDisplayName())
            End Select
        End Function

        Private Shared Function ConvertToObject(value As CConst, expr As ExpressionSyntax) As CConst
            If value.IsBad Then
                Return value
            End If

            If IsNothing(value) Then
                Return ReportSemanticError(ERRID.ERR_RequiredConstExpr, expr)
            End If

            Return ReportSemanticError(ERRID.ERR_RequiredConstConversion2, expr, value.SpecialType.GetDisplayName(), SpecialType.System_Object.GetDisplayName())
        End Function

        Private Function EvaluateUnaryExpression(expr As UnaryExpressionSyntax) As CConst
            Dim val = EvaluateExpressionInternal(expr.Operand)
            Dim specialType = val.SpecialType

            If specialType = SpecialType.None Then
                Return ReportSemanticError(ERRID.ERR_BadCCExpression, expr)
            End If

            If specialType = SpecialType.System_String OrElse
               (specialType = SpecialType.System_Object AndAlso Not IsNothing(val)) OrElse
               specialType = SpecialType.System_Char OrElse specialType = SpecialType.System_DateTime Then
                Return ReportSemanticError(ERRID.ERR_UnaryOperand2, expr, expr.OperatorToken.ValueText, specialType.GetDisplayName())
            End If

            Try
                Select Case expr.Kind
                    Case SyntaxKind.UnaryMinusExpression
                        If IsNothing(val) Then
                            Return CConst.Create(-Nothing)
                        End If
                        Select Case specialType
                            Case SpecialType.System_Boolean
                                Return CConst.Create(-(CShort(DirectCast(val, CConst(Of Boolean)).Value)))
                            Case SpecialType.System_Byte
                                Return CConst.Create(-(DirectCast(val, CConst(Of Byte)).Value))
                            Case SpecialType.System_Decimal
                                Return CConst.Create(-(DirectCast(val, CConst(Of Decimal)).Value))
                            Case SpecialType.System_Double
                                Return CConst.Create(-(DirectCast(val, CConst(Of Double)).Value))
                            Case SpecialType.System_Int16
                                Return CConst.Create(-(DirectCast(val, CConst(Of Int16)).Value))
                            Case SpecialType.System_Int32
                                Return CConst.Create(-(DirectCast(val, CConst(Of Int32)).Value))
                            Case SpecialType.System_Int64
                                Return CConst.Create(-(DirectCast(val, CConst(Of Int64)).Value))
                            Case SpecialType.System_SByte
                                Return CConst.Create(-(DirectCast(val, CConst(Of SByte)).Value))
                            Case SpecialType.System_Single
                                Return CConst.Create(-(DirectCast(val, CConst(Of Single)).Value))
                            Case SpecialType.System_UInt16
                                Return CConst.Create(-(DirectCast(val, CConst(Of UInt16)).Value))
                            Case SpecialType.System_UInt32
                                Return CConst.Create(-(DirectCast(val, CConst(Of UInt32)).Value))
                            Case SpecialType.System_UInt64
                                Return CConst.Create(-(DirectCast(val, CConst(Of UInt64)).Value))
                            Case Else
                                Throw ExceptionUtilities.UnexpectedValue(specialType)
                        End Select

                    Case SyntaxKind.UnaryPlusExpression
                        If specialType = SpecialType.System_Boolean Then
                            Return CConst.Create(+(CShort(DirectCast(val, CConst(Of Boolean)).Value)))
                        End If
                        Return val

                    Case SyntaxKind.NotExpression
                        If IsNothing(val) Then
                            Return CConst.Create(Not Nothing)
                        End If
                        Select Case specialType
                            Case SpecialType.System_Boolean
                                Return CConst.Create(Not (DirectCast(val, CConst(Of Boolean)).Value))
                            Case SpecialType.System_Byte
                                Return CConst.Create(Not (DirectCast(val, CConst(Of Byte)).Value))
                            Case SpecialType.System_Decimal
                                Return CConst.Create(Not CLng(DirectCast(val, CConst(Of Decimal)).Value))
                            Case SpecialType.System_Double
                                Return CConst.Create(Not CLng(DirectCast(val, CConst(Of Double)).Value))
                            Case SpecialType.System_Int16
                                Return CConst.Create(Not (DirectCast(val, CConst(Of Int16)).Value))
                            Case SpecialType.System_Int32
                                Return CConst.Create(Not (DirectCast(val, CConst(Of Int32)).Value))
                            Case SpecialType.System_Int64
                                Return CConst.Create(Not (DirectCast(val, CConst(Of Int64)).Value))
                            Case SpecialType.System_SByte
                                Return CConst.Create(Not (DirectCast(val, CConst(Of SByte)).Value))
                            Case SpecialType.System_Single
                                Return CConst.Create(Not CLng(DirectCast(val, CConst(Of Single)).Value))
                            Case SpecialType.System_UInt16
                                Return CConst.Create(Not (DirectCast(val, CConst(Of UInt16)).Value))
                            Case SpecialType.System_UInt32
                                Return CConst.Create(Not (DirectCast(val, CConst(Of UInt32)).Value))
                            Case SpecialType.System_UInt64
                                Return CConst.Create(Not (DirectCast(val, CConst(Of UInt64)).Value))
                            Case Else
                                Throw ExceptionUtilities.UnexpectedValue(specialType)
                        End Select
                End Select
            Catch ex As OverflowException
                Return ReportSemanticError(ERRID.ERR_ExpressionOverflow1, expr)
            End Try

            Throw ExceptionUtilities.UnexpectedValue(expr)
        End Function

        Private Shared Function IsNothing(val As CConst) As Boolean
            Return val.SpecialType = SpecialType.System_Object AndAlso val.ValueAsObject Is Nothing
        End Function

        Private Function EvaluateBinaryExpression(expr As BinaryExpressionSyntax) As CConst
            Dim Left = EvaluateExpressionInternal(expr.Left)
            Dim Right = EvaluateExpressionInternal(expr.Right)
            Dim BoundOpcode = expr.Kind

            If Left.IsBad OrElse Right.IsBad Then
                Return ReportSemanticError(ERRID.ERR_BadCCExpression, expr)
            End If

            Dim OperandType As SpecialType = SpecialType.None

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

                            Right = ConvertToNumeric(Right, SpecialType.System_Int32, expr.Right)

                        Case SyntaxKind.OrExpression,
                            SyntaxKind.AndExpression,
                            SyntaxKind.ExclusiveOrExpression

                            Right = ConvertToNumeric(Right, SpecialType.System_Int32, expr.Right)

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(expr)
                    End Select
                End If

                If IsNothing(Left) Then
                    OperandType = Right.SpecialType

                    Select Case (BoundOpcode)

                        Case SyntaxKind.ConcatenateExpression,
                            SyntaxKind.LikeExpression

                            ' // For & and Like, a Nothing operand is typed String unless the other operand
                            ' // is non-intrinsic 
                            OperandType = SpecialType.System_String

                        Case SyntaxKind.LeftShiftExpression,
                            SyntaxKind.RightShiftExpression

                            ' // Nothing should default to Integer for Shift operations.
                            OperandType = SpecialType.System_Int32

                    End Select

                    Left = Convert(Left, OperandType, expr.Left)

                ElseIf IsNothing(Right) Then
                    OperandType = Left.SpecialType

                    Select Case (BoundOpcode)

                        Case SyntaxKind.ConcatenateExpression,
                            SyntaxKind.LikeExpression

                            ' // For & and Like, a Nothing operand is typed String unless the other operand
                            ' // is non-intrinsic
                            OperandType = SpecialType.System_String
                    End Select

                    Right = Convert(Right, OperandType, expr.Right)
                End If
            End If

            ' // For comparison operators, the result type computed here is not
            ' // the result type of the comparison (which is typically boolean),
            ' // but is the type to which the operands are to be converted. For
            ' // other operators, the type computed here is both the result type
            ' // and the common operand type.

            Dim ResultType = LookupInOperatorTables(BoundOpcode, Left.SpecialType, Right.SpecialType)

            If ResultType = SpecialType.None Then
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
                    If ResultType = SpecialType.System_String Then
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

                    ResultType = SpecialType.System_Boolean
            End Select

            Dim Result = PerformCompileTimeBinaryOperation(
                    BoundOpcode,
                    ResultType,
                    Left,
                    Right,
                    expr)

            Return Result
        End Function

        Private Shared Function PerformCompileTimeBinaryOperation(opcode As SyntaxKind,
                                                                  resultType As SpecialType,
                                                                  left As CConst,
                                                                  right As CConst,
                                                                  expr As ExpressionSyntax) As CConst

            Debug.Assert(opcode = SyntaxKind.LeftShiftExpression OrElse
                     opcode = SyntaxKind.RightShiftExpression OrElse
                     left.SpecialType = right.SpecialType, "Binary operation on mismatched types.")

            If left.SpecialType.IsIntegralType() OrElse left.SpecialType = SpecialType.System_Char OrElse left.SpecialType = SpecialType.System_DateTime Then
                Dim LeftValue As Int64 = TypeHelpers.UncheckedCLng(left)
                Dim RightValue As Int64 = TypeHelpers.UncheckedCLng(right)

                If resultType = SpecialType.System_Boolean Then
                    Dim ComparisonSucceeds As Boolean = False

                    Select Case (opcode)
                        Case SyntaxKind.EqualsExpression
                            ComparisonSucceeds =
                                If(left.SpecialType.IsUnsignedIntegralType(),
                                    UncheckedCULng(LeftValue) = UncheckedCULng(RightValue),
                                    LeftValue = RightValue)

                        Case SyntaxKind.NotEqualsExpression
                            ComparisonSucceeds =
                                If(left.SpecialType.IsUnsignedIntegralType(),
                                    UncheckedCULng(LeftValue) <> UncheckedCULng(RightValue),
                                    LeftValue <> RightValue)

                        Case SyntaxKind.LessThanOrEqualExpression
                            ComparisonSucceeds =
                                If(left.SpecialType.IsUnsignedIntegralType(),
                                    UncheckedCULng(LeftValue) <= UncheckedCULng(RightValue),
                                    LeftValue <= RightValue)

                        Case SyntaxKind.GreaterThanOrEqualExpression
                            ComparisonSucceeds =
                                If(left.SpecialType.IsUnsignedIntegralType(),
                                    UncheckedCULng(LeftValue) >= UncheckedCULng(RightValue),
                                    LeftValue >= RightValue)

                        Case SyntaxKind.LessThanExpression
                            ComparisonSucceeds = If(left.SpecialType.IsUnsignedIntegralType(),
                                UncheckedCULng(LeftValue) < UncheckedCULng(RightValue),
                                LeftValue < RightValue)

                        Case SyntaxKind.GreaterThanExpression
                            ComparisonSucceeds =
                                If(left.SpecialType.IsUnsignedIntegralType(),
                                    UncheckedCULng(LeftValue) > UncheckedCULng(RightValue),
                                    LeftValue > RightValue)

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(opcode)
                    End Select
                    Return CConst.Create(ComparisonSucceeds)
                Else
                    ' // Compute the result in 64-bit arithmetic, and determine if the
                    ' // operation overflows the result type.

                    Dim ResultValue As Int64 = 0
                    Dim Overflow As Boolean = False

                    Select Case (opcode)
                        Case SyntaxKind.AddExpression
                            ResultValue = NarrowIntegralResult(
                                LeftValue + RightValue,
                                left.SpecialType,
                                resultType,
                                Overflow)

                            If Not resultType.IsUnsignedIntegralType() Then
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
                                left.SpecialType,
                                resultType,
                                Overflow)

                            If Not resultType.IsUnsignedIntegralType() Then
                                If (RightValue > 0 AndAlso ResultValue > LeftValue) OrElse
                                   (RightValue < 0 AndAlso ResultValue < LeftValue) Then

                                    Overflow = True
                                End If

                            ElseIf UncheckedCULng(ResultValue) > UncheckedCULng(LeftValue) Then
                                Overflow = True
                            End If

                        Case SyntaxKind.MultiplyExpression
                            ResultValue = Multiply(LeftValue, RightValue, left.SpecialType, resultType, Overflow)

                        Case SyntaxKind.IntegerDivideExpression
                            If RightValue = 0 Then
                                Return ReportSemanticError(ERRID.ERR_ZeroDivide, expr)
                            End If

                            ResultValue = NarrowIntegralResult(
                                If(resultType.IsUnsignedIntegralType(),
                                    CompileTimeCalculations.UncheckedCLng(UncheckedCULng(LeftValue) \ UncheckedCULng(RightValue)),
                                    UncheckedIntegralDiv(LeftValue, RightValue)),
                                left.SpecialType,
                                resultType,
                                Overflow)

                            If Not resultType.IsUnsignedIntegralType() AndAlso LeftValue = Int64.MinValue AndAlso RightValue = -1 Then
                                Overflow = True
                            End If

                        Case SyntaxKind.ModuloExpression
                            If RightValue = 0 Then
                                Return ReportSemanticError(ERRID.ERR_ZeroDivide, expr)
                            End If

                            If resultType.IsUnsignedIntegralType() Then
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
                            RightValue = RightValue And left.SpecialType.GetShiftSizeMask()
                            ResultValue = LeftValue << CType(RightValue, Integer)

                            ' // Round-trip the result through a cast.  We do this for two reasons:
                            ' // a) Bits may have shifted off the end and need to be stripped away.
                            ' // b) The sign bit may have changed which requires the result to be sign-extended.

                            Dim overflowTemp As Boolean = False
                            ResultValue = NarrowIntegralResult(ResultValue, left.SpecialType, resultType, overflowTemp)

                        Case SyntaxKind.RightShiftExpression
                            RightValue = RightValue And left.SpecialType.GetShiftSizeMask()
                            If resultType.IsUnsignedIntegralType() Then
                                ResultValue = CompileTimeCalculations.UncheckedCLng((UncheckedCULng(LeftValue) >> CType(RightValue, Integer)))
                            Else
                                ResultValue = LeftValue >> CType(RightValue, Integer)
                            End If

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(opcode)
                    End Select

                    If Overflow Then
                        Return ReportSemanticError(ERRID.ERR_ExpressionOverflow1, expr, resultType.GetDisplayName())
                    End If

                    Return Convert(CConst.Create(ResultValue), resultType, expr)
                End If
            ElseIf left.SpecialType.IsFloatingType() Then
                Dim LeftValue As Double = CDbl(left.ValueAsObject)
                Dim RightValue As Double = CDbl(right.ValueAsObject)

                If resultType = SpecialType.System_Boolean Then
                    Dim ComparisonSucceeds As Boolean = False
                    Select Case (opcode)
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
                            Throw ExceptionUtilities.UnexpectedValue(opcode)
                    End Select

                    Return CConst.Create(If(ComparisonSucceeds, True, False))
                Else
                    Dim resultValue As Double = 0
                    Dim overflow As Boolean = False

                    Select Case (opcode)
                        Case SyntaxKind.AddExpression
                            resultValue = LeftValue + RightValue

                        Case SyntaxKind.SubtractExpression
                            resultValue = LeftValue - RightValue

                        Case SyntaxKind.MultiplyExpression
                            resultValue = LeftValue * RightValue

                        Case SyntaxKind.ExponentiateExpression
                            'IS_DBL_INFINITY(RightValue) 
                            If (
                                Double.IsInfinity(RightValue)
                            ) Then
                                'IS_DBL_ONE(LeftValue)
                                If LeftValue = 1.0 Then
                                    resultValue = LeftValue
                                    Exit Select
                                End If

                                'IS_DBL_NEGATIVEONE(LeftValue)
                                If LeftValue = -1.0 Then
                                    resultValue = Double.NaN
                                    Exit Select
                                End If

                            ElseIf (
                                Double.IsNaN(RightValue)
                            ) Then
                                resultValue = Double.NaN
                                Exit Select
                            End If
                            resultValue = Math.Pow(LeftValue, RightValue)

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
                            resultValue = LeftValue / RightValue

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
                            resultValue = Math.IEEERemainder(LeftValue, RightValue)
                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(opcode)
                    End Select

                    resultValue = NarrowFloatingResult(resultValue, resultType, overflow)

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

                    Return Convert(CConst.Create(resultValue), resultType, expr)
                End If
            ElseIf left.SpecialType = SpecialType.System_Decimal Then
                Dim LeftValue As Decimal = CDec(left.ValueAsObject)
                Dim RightValue As Decimal = CDec(right.ValueAsObject)

                If resultType = SpecialType.System_Boolean Then
                    Dim ComparisonSucceeds As Boolean = False
                    Dim ComparisonResult As Integer = LeftValue.CompareTo(RightValue)

                    Select Case (opcode)
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
                            Throw ExceptionUtilities.UnexpectedValue(opcode)
                    End Select

                    Return CConst.Create(ComparisonSucceeds)
                Else
                    Dim ResultValue As Decimal
                    Dim Overflow As Boolean = False

                    Select Case (opcode)
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
                            ' //                        for "Decimal.MaxValue Mod 2" when it should
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
                            Throw ExceptionUtilities.UnexpectedValue(opcode)
                    End Select

                    If Overflow Then
                        Return ReportSemanticError(ERRID.ERR_ExpressionOverflow1, expr, resultType.GetDisplayName())
                    End If

                    Return CConst.Create(ResultValue)
                End If
            ElseIf left.SpecialType = SpecialType.System_String Then

                ' Nothing strings should be treated the same as ""
                Dim LeftSpelling = If(CStr(left.ValueAsObject), "")
                Dim RightSpelling = If(CStr(right.ValueAsObject), "")

                Select Case (opcode)
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

                        Select Case (opcode)
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
                        Throw ExceptionUtilities.UnexpectedValue(opcode)
                End Select

            ElseIf left.SpecialType = SpecialType.System_Boolean Then
                Dim LeftValue As Boolean = CBool(left.ValueAsObject)
                Dim RightValue As Boolean = CBool(right.ValueAsObject)

                Dim OperationSucceeds As Boolean = False

                Select Case (opcode)
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
                        Throw ExceptionUtilities.UnexpectedValue(opcode)
                End Select

                Return CConst.Create(OperationSucceeds)
            End If

            Throw ExceptionUtilities.Unreachable
        End Function
    End Structure
End Namespace
