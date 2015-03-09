﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundCall

        Public Sub New(
            syntax As VisualBasicSyntaxNode,
            method As MethodSymbol,
            methodGroup As BoundMethodGroup,
            receiver As BoundExpression,
            arguments As ImmutableArray(Of BoundExpression),
            constantValueOpt As ConstantValue,
            type As TypeSymbol,
            Optional suppressObjectClone As Boolean = False,
            Optional hasErrors As Boolean = False
        )
            Me.New(syntax, method, methodGroup, receiver, arguments,
                   constantValueOpt, suppressObjectClone, type, hasErrors)
        End Sub

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return Me.Method
            End Get
        End Property

#If DEBUG Then
        Private Sub Validate()
            ' if method group is specified it should not have receiver if it was moved to a bound call
            Debug.Assert(Me.ReceiverOpt Is Nothing OrElse Me.MethodGroupOpt Is Nothing OrElse Me.MethodGroupOpt.ReceiverOpt Is Nothing)

            ValidateConstantValue()

            Debug.Assert(Arguments.Length = Method.ParameterCount)
            Dim isOperator As Boolean = (Method.MethodKind = MethodKind.UserDefinedOperator)
            Dim isLifted? As Boolean = Nothing

            For i As Integer = 0 To Arguments.Length - 1
                Dim argument As BoundExpression = Arguments(i)
                Dim parameter As ParameterSymbol = Method.Parameters(i)

                AssertArgument(isOperator, argument.IsLateBound, isLifted, argument.Type, parameter.Type)

                If Not (parameter.IsByRef AndAlso argument.IsLValue) Then
                    argument.AssertRValue()
                End If
            Next

            If isLifted.GetValueOrDefault AndAlso Not Method.ReturnType.IsNullableType() Then
                Debug.Assert(OverloadResolution.CanLiftType(Method.ReturnType) AndAlso
                             Type.IsNullableType() AndAlso
                             Type.GetNullableUnderlyingType().IsSameTypeIgnoringCustomModifiers(Method.ReturnType))
            Else
                Debug.Assert(Type.IsSameTypeIgnoringCustomModifiers(Method.ReturnType))
            End If
        End Sub

        Private Shared Sub AssertArgument(
            isOperator As Boolean,
            isLateBound As Boolean,
            ByRef isLifted? As Boolean,
            type As TypeSymbol,
            signatureType As TypeSymbol
        )
            If isLateBound Then
                Debug.Assert(type.IsObjectType)
            ElseIf Not isOperator Then
                Debug.Assert(type.IsSameTypeIgnoringCustomModifiers(signatureType))
            ElseIf Not isLifted.HasValue Then
                If type.IsSameTypeIgnoringCustomModifiers(signatureType) Then
                    isLifted = False
                ElseIf OverloadResolution.CanLiftType(signatureType) AndAlso
                       type.IsNullableType() AndAlso
                       type.GetNullableUnderlyingType().IsSameTypeIgnoringCustomModifiers(signatureType) Then
                    isLifted = True
                Else
                    isLifted = False
                    Debug.Assert(type.IsSameTypeIgnoringCustomModifiers(signatureType))
                End If
            ElseIf isLifted.GetValueOrDefault Then
                Debug.Assert(OverloadResolution.CanLiftType(signatureType) AndAlso
                             type.IsNullableType() AndAlso
                             type.GetNullableUnderlyingType().IsSameTypeIgnoringCustomModifiers(signatureType))
            Else
                Debug.Assert(type.IsSameTypeIgnoringCustomModifiers(signatureType))
            End If
        End Sub
#End If

        Public Overrides ReadOnly Property ResultKind As LookupResultKind
            Get
                If MethodGroupOpt IsNot Nothing Then
                    Return MethodGroupOpt.ResultKind
                End If

                Return MyBase.ResultKind
            End Get
        End Property
    End Class

End Namespace
