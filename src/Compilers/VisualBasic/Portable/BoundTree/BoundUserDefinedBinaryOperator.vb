' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundUserDefinedBinaryOperator

        Public ReadOnly Property Left As BoundExpression
            Get
                Return [Call].Arguments(0)
            End Get
        End Property

        Public ReadOnly Property Right As BoundExpression
            Get
                Return [Call].Arguments(1)
            End Get
        End Property

        Public ReadOnly Property [Call] As BoundCall
            Get
                Return DirectCast(UnderlyingExpression, BoundCall)
            End Get
        End Property

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(Type.Equals(UnderlyingExpression.Type))
            Debug.Assert((OperatorKind And BinaryOperatorKind.UserDefined) <> 0)
            Debug.Assert(UnderlyingExpression.Kind = BoundKind.BadExpression OrElse UnderlyingExpression.Kind = BoundKind.Call)

            If UnderlyingExpression.Kind = BoundKind.Call Then
                Dim underlyingCall = DirectCast(UnderlyingExpression, BoundCall)
                Debug.Assert(underlyingCall.Method.MethodKind = MethodKind.UserDefinedOperator AndAlso underlyingCall.Method.ParameterCount = 2)

                If (OperatorKind And BinaryOperatorKind.Lifted) <> 0 Then
                    For i As Integer = 0 To underlyingCall.Arguments.Length - 1
                        Dim argument As BoundExpression = underlyingCall.Arguments(i)
                        Dim parameter As ParameterSymbol = underlyingCall.Method.Parameters(i)

                        Debug.Assert(OverloadResolution.CanLiftType(parameter.Type))
                        Debug.Assert(argument.Type.IsNullableType() AndAlso
                                     argument.Type.GetNullableUnderlyingType().IsSameTypeIgnoringCustomModifiers(parameter.Type))
                    Next

                    Debug.Assert(underlyingCall.Type.IsNullableType())
                    Debug.Assert(underlyingCall.Type.IsSameTypeIgnoringCustomModifiers(underlyingCall.Method.ReturnType) OrElse
                                 (OverloadResolution.CanLiftType(underlyingCall.Method.ReturnType) AndAlso
                                  underlyingCall.Type.GetNullableUnderlyingType().IsSameTypeIgnoringCustomModifiers(underlyingCall.Method.ReturnType)))
                Else
                    For i As Integer = 0 To underlyingCall.Arguments.Length - 1
                        Dim argument As BoundExpression = underlyingCall.Arguments(i)
                        Dim parameter As ParameterSymbol = underlyingCall.Method.Parameters(i)

                        Debug.Assert(argument.Type.IsSameTypeIgnoringCustomModifiers(parameter.Type))
                    Next

                    Debug.Assert(underlyingCall.Type.IsSameTypeIgnoringCustomModifiers(underlyingCall.Method.ReturnType))
                End If
            End If
        End Sub
#End If

    End Class

End Namespace
