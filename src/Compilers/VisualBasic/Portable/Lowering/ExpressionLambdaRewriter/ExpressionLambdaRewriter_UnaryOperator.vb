' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
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

        Private Function VisitUnaryOperator(node As BoundUnaryOperator) As BoundExpression
            Dim origArg As BoundExpression = node.Operand
            Dim origArgType As TypeSymbol = origArg.Type
            Dim origArgNotNullableType As TypeSymbol = origArgType.GetNullableUnderlyingTypeOrSelf
            Dim origArgUnderlyingType As TypeSymbol = origArgNotNullableType.GetEnumUnderlyingTypeOrSelf
            Dim origArgUnderlyingSpecialType As SpecialType = origArgUnderlyingType.SpecialType
            Debug.Assert(origArgType = node.Type)

            Dim argument As BoundExpression = Visit(origArg)

            Dim opKind As UnaryOperatorKind = node.OperatorKind And UnaryOperatorKind.OpMask
            Dim isChecked As Boolean = node.Checked AndAlso origArgUnderlyingType.IsIntegralType
            Dim helperName As String = Nothing

            Debug.Assert((node.OperatorKind And UnaryOperatorKind.UserDefined) = 0)

            Select Case opKind
                Case UnaryOperatorKind.Plus
                    ' DEV11 comment: 
                    '   Note that the unary plus operator itself is not encoded in the tree and treated
                    '   as a nop currently. This is the orcas behavior.
                    If Not origArgType.IsReferenceType Then
                        Return argument
                    End If

                    Dim method As MethodSymbol = GetHelperForObjectUnaryOperation(opKind)
                    Return If(method Is Nothing, argument, ConvertRuntimeHelperToExpressionTree("UnaryPlus", argument, Me._factory.MethodInfo(method)))

                Case UnaryOperatorKind.Minus
                    helperName = If(isChecked, "NegateChecked", "Negate")
                    GoTo lNotAndMinus

                Case UnaryOperatorKind.Not
                    helperName = "Not"

lNotAndMinus:
                    ' NOTE: Both '-' and 'Not' processed by the code below

                    Dim method As MethodSymbol = Nothing
                    If origArgType.IsReferenceType Then
                        method = GetHelperForObjectUnaryOperation(opKind)
                    ElseIf origArgUnderlyingType.IsDecimalType Then
                        method = GetHelperForDecimalUnaryOperation(opKind)
                    End If

                    If method IsNot Nothing Then
                        Return ConvertRuntimeHelperToExpressionTree(helperName, argument, Me._factory.MethodInfo(method))
                    End If

                    ' No standard method

                    ' convert i1, u1 to i4 if needed 
                    Dim needToCastBackToByteOrSByte As Boolean = origArgUnderlyingSpecialType = SpecialType.System_Byte OrElse
                                                                 origArgUnderlyingSpecialType = SpecialType.System_SByte

                    Dim origArgTypeIsNullable As Boolean = origArgType.IsNullableType

                    argument = GenerateCastsForBinaryAndUnaryOperator(argument,
                                                                      origArgTypeIsNullable,
                                                                      origArgNotNullableType,
                                                                      isChecked AndAlso IsIntegralType(origArgUnderlyingType),
                                                                      needToCastBackToByteOrSByte)

                    Dim result As BoundExpression = ConvertRuntimeHelperToExpressionTree(helperName, argument)

                    ' convert i4 back to i1, u1 
                    If needToCastBackToByteOrSByte Then
                        result = Convert(result, If(origArgTypeIsNullable, Me._factory.NullableOf(origArgUnderlyingType), origArgUnderlyingType), isChecked)
                    End If

                    ' back to nullable
                    If origArgNotNullableType.IsEnumType Then
                        result = Convert(result, origArgType, False)
                    End If

                    Return result

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select
        End Function

        Private Function VisitNullableIsTrueOperator(node As BoundNullableIsTrueOperator) As BoundExpression
            Debug.Assert(node.Type.IsBooleanType())
            Debug.Assert(node.Operand.Type.IsNullableOfBoolean())

            ' Rewrite into binary conditional expression
            Dim operand As BoundExpression = node.Operand

            Select Case operand.Kind
                Case BoundKind.UserDefinedUnaryOperator
                    Dim userDefinedOperator = DirectCast(operand, BoundUserDefinedUnaryOperator)

                    Dim opKind As UnaryOperatorKind = userDefinedOperator.OperatorKind
                    If (opKind And UnaryOperatorKind.OpMask) <> UnaryOperatorKind.IsTrue OrElse (opKind And UnaryOperatorKind.Lifted) = 0 Then
                        Exit Select
                    End If

                    Dim [call] As BoundCall = userDefinedOperator.Call

                    ' Type of the parameter
                    Dim udoOperandType As TypeSymbol = userDefinedOperator.Operand.Type

                    Dim paramSymbol As ParameterSymbol = CreateCoalesceLambdaParameterSymbol(udoOperandType)
                    Dim lambdaBody As BoundExpression = BuildLambdaBodyForCoalesce(userDefinedOperator.OperatorKind, [call], node.Type, paramSymbol)
                    Dim coalesceLambda As BoundExpression = BuildLambdaForCoalesceCall(node.Type, paramSymbol, lambdaBody)
                    Return ConvertRuntimeHelperToExpressionTree("Coalesce", Visit(userDefinedOperator.Operand), Visit(Me._factory.Literal(False)), coalesceLambda)
            End Select

            If operand.Kind = BoundKind.ObjectCreationExpression Then
                Dim objCreation = DirectCast(operand, BoundObjectCreationExpression)
                ' Nullable<T> has only one ctor with parameters and only that one sets hasValue = true
                If objCreation.Arguments.Length = 1 Then
                    Return VisitInternal(objCreation.Arguments(0))
                End If
            End If

            Return ConvertRuntimeHelperToExpressionTree("Coalesce", Visit(operand), Visit(Me._factory.Literal(False)))
        End Function

        Private Function BuildLambdaBodyForCoalesce(opKind As UnaryOperatorKind, [call] As BoundCall, resultType As TypeSymbol, lambdaParameter As ParameterSymbol) As BoundExpression
            Debug.Assert(resultType.IsBooleanType)
            Debug.Assert(lambdaParameter.Type.IsNullableType)

            ' NOTE: AdjustCallForLiftedOperator will check if [operator] is a valid operator
            Return AdjustCallForLiftedOperator(opKind,
                                               [call].Update([call].Method,
                                                             Nothing,
                                                             Nothing,
                                                             ImmutableArray.Create(Of BoundExpression)(
                                                                 CreateCoalesceLambdaParameter(lambdaParameter)),
                                                             Nothing,
                                                             Nothing,
                                                             [call].IsLValue,
                                                             suppressObjectClone:=True,
                                                             type:=[call].Type),
                                               resultType)
        End Function

#Region "User Defined Operator Call"

        Private Function VisitUserDefinedUnaryOperator(node As BoundUserDefinedUnaryOperator) As BoundExpression
            Dim opKind As UnaryOperatorKind = node.OperatorKind And UnaryOperatorKind.OpMask
            Dim isLifted As Boolean = (node.OperatorKind And UnaryOperatorKind.Lifted) <> 0

            Select Case opKind
                Case UnaryOperatorKind.IsTrue,
                     UnaryOperatorKind.IsFalse
                    Return RewriteUserDefinedOperator(node)

                Case UnaryOperatorKind.Minus,
                     UnaryOperatorKind.Plus,
                     UnaryOperatorKind.Not

                    ' See description in DiagnosticsPass.VisitUserDefinedUnaryOperator
                    Debug.Assert(Not isLifted OrElse Not node.Call.Method.ReturnType.IsNullableType)

                    Return ConvertRuntimeHelperToExpressionTree(GetUnaryOperatorMethodName(opKind, False),
                                                                Visit(node.Operand),
                                                                _factory.MethodInfo(node.Call.Method))

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select
        End Function

        Private Function RewriteUserDefinedOperator(node As BoundUserDefinedUnaryOperator) As BoundExpression
            Dim [call] As BoundCall = node.Call
            Dim opKind As UnaryOperatorKind = node.OperatorKind

            If (opKind And UnaryOperatorKind.Lifted) = 0 Then
                Return VisitInternal([call])
            End If

            Return VisitInternal(AdjustCallForLiftedOperator(opKind, [call], node.Type))
        End Function

#End Region

#Region "Utility"

        Private Function GetHelperForDecimalUnaryOperation(opKind As UnaryOperatorKind) As MethodSymbol
            opKind = opKind And UnaryOperatorKind.OpMask

            Dim specialHelper As SpecialMember
            Select Case opKind
                Case UnaryOperatorKind.Minus,
                     UnaryOperatorKind.Not
                    specialHelper = SpecialMember.System_Decimal__NegateDecimal

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select

            Return DirectCast(_factory.SpecialMember(specialHelper), MethodSymbol)
        End Function

        Private Function GetHelperForObjectUnaryOperation(opKind As UnaryOperatorKind) As MethodSymbol
            opKind = opKind And UnaryOperatorKind.OpMask

            Dim wellKnownHelper As WellKnownMember
            Select Case opKind
                Case UnaryOperatorKind.Plus
                    wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__PlusObjectObject
                Case UnaryOperatorKind.Minus
                    wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__NegateObjectObject
                Case UnaryOperatorKind.Not
                    wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__NotObjectObject

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select

            Return Me._factory.WellKnownMember(Of MethodSymbol)(wellKnownHelper)
        End Function

        ''' <summary>
        ''' Get the name of the expression tree function for a particular unary operator
        ''' </summary>
        Private Shared Function GetUnaryOperatorMethodName(opKind As UnaryOperatorKind, isChecked As Boolean) As String
            Select Case (opKind And UnaryOperatorKind.OpMask)
                Case UnaryOperatorKind.Not
                    Return "Not"
                Case UnaryOperatorKind.Plus
                    Return "UnaryPlus"
                Case UnaryOperatorKind.Minus
                    Return If(isChecked, "NegateChecked", "Negate")
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select
        End Function

#End Region

    End Class
End Namespace
