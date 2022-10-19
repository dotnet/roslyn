' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundUserDefinedUnaryOperator

        Public ReadOnly Property Operand As BoundExpression
            Get
                Return [Call].Arguments(0)
            End Get
        End Property

        Public ReadOnly Property [Call] As BoundCall
            Get
                Return DirectCast(UnderlyingExpression, BoundCall)
            End Get
        End Property

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(TypeSymbol.Equals(Type, UnderlyingExpression.Type, TypeCompareKind.ConsiderEverything))
            Debug.Assert((OperatorKind And UnaryOperatorKind.UserDefined) <> 0)
            Debug.Assert(UnderlyingExpression.Kind = BoundKind.BadExpression OrElse UnderlyingExpression.Kind = BoundKind.Call)

            If UnderlyingExpression.Kind = BoundKind.Call Then
                Dim underlyingCall = DirectCast(UnderlyingExpression, BoundCall)
                Debug.Assert(underlyingCall.Method.MethodKind = MethodKind.UserDefinedOperator AndAlso underlyingCall.Method.ParameterCount = 1)

                Dim argument As BoundExpression = underlyingCall.Arguments(0)
                Dim parameter As ParameterSymbol = underlyingCall.Method.Parameters(0)

                If (OperatorKind And UnaryOperatorKind.Lifted) <> 0 Then

                    Debug.Assert(OverloadResolution.CanLiftType(parameter.Type) AndAlso
                                 argument.Type.IsNullableType() AndAlso
                                 argument.Type.GetNullableUnderlyingType().IsSameTypeIgnoringAll(parameter.Type))

                    Debug.Assert(underlyingCall.Type.IsNullableType())
                    Debug.Assert(underlyingCall.Type.IsSameTypeIgnoringAll(underlyingCall.Method.ReturnType) OrElse
                                 (OverloadResolution.CanLiftType(underlyingCall.Method.ReturnType) AndAlso
                                  underlyingCall.Type.GetNullableUnderlyingType().IsSameTypeIgnoringAll(underlyingCall.Method.ReturnType)))
                Else
                    Debug.Assert(argument.Type.IsSameTypeIgnoringAll(parameter.Type))
                    Debug.Assert(underlyingCall.Type.IsSameTypeIgnoringAll(underlyingCall.Method.ReturnType))
                End If
            End If
        End Sub
#End If

    End Class

End Namespace
