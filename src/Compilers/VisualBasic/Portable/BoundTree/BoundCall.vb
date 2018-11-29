' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundCall

        Public Sub New(
            syntax As SyntaxNode,
            method As MethodSymbol,
            methodGroupOpt As BoundMethodGroup,
            receiverOpt As BoundExpression,
            arguments As ImmutableArray(Of BoundExpression),
            constantValueOpt As ConstantValue,
            type As TypeSymbol,
            Optional suppressObjectClone As Boolean = False,
            Optional hasErrors As Boolean = False,
            Optional defaultArguments As BitVector = Nothing
        )
            Me.New(syntax, method, methodGroupOpt, receiverOpt, arguments, defaultArguments,
                   constantValueOpt,
                   isLValue:=method.ReturnsByRef,
                   suppressObjectClone:=suppressObjectClone,
                   type:=type,
                   hasErrors:=hasErrors)
        End Sub

        Public Sub New(syntax As SyntaxNode,
                       method As MethodSymbol,
                       methodGroupOpt As BoundMethodGroup,
                       receiverOpt As BoundExpression,
                       arguments As ImmutableArray(Of BoundExpression),
                       constantValueOpt As ConstantValue,
                       isLValue As Boolean,
                       suppressObjectClone As Boolean,
                       type As TypeSymbol,
                       Optional hasErrors As Boolean = False)
            Me.New(syntax, method, methodGroupOpt, receiverOpt, arguments, defaultArguments:=BitVector.Null, constantValueOpt, isLValue, suppressObjectClone, type, hasErrors)
        End Sub

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Return MakeRValue()
        End Function

        Public Shadows Function MakeRValue() As BoundCall
            If _IsLValue Then
                Return Update(
                    Method,
                    MethodGroupOpt,
                    ReceiverOpt,
                    Arguments,
                    DefaultArguments,
                    ConstantValueOpt,
                    isLValue:=False,
                    suppressObjectClone:=SuppressObjectClone,
                    type:=Type)
            End If

            Return Me
        End Function

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

            ' Null DefaultArguments doesn't indicate that Arguments is non-null, but if DefaultArguments is non-null we must have some arguments.
            Debug.Assert(DefaultArguments.IsNull OrElse Not Arguments.IsEmpty)

            If isLifted.GetValueOrDefault AndAlso Not Method.ReturnType.IsNullableType() Then
                Debug.Assert(OverloadResolution.CanLiftType(Method.ReturnType) AndAlso
                             Type.IsNullableType() AndAlso
                             Type.GetNullableUnderlyingType().IsSameTypeIgnoringAll(Method.ReturnType))
            Else
                Debug.Assert(Type.IsSameTypeIgnoringAll(Method.ReturnType))
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
                Debug.Assert(type.IsSameTypeIgnoringAll(signatureType))
            ElseIf Not isLifted.HasValue Then
                If type.IsSameTypeIgnoringAll(signatureType) Then
                    isLifted = False
                ElseIf OverloadResolution.CanLiftType(signatureType) AndAlso
                       type.IsNullableType() AndAlso
                       type.GetNullableUnderlyingType().IsSameTypeIgnoringAll(signatureType) Then
                    isLifted = True
                Else
                    isLifted = False
                    Debug.Assert(type.IsSameTypeIgnoringAll(signatureType))
                End If
            ElseIf isLifted.GetValueOrDefault Then
                Debug.Assert(OverloadResolution.CanLiftType(signatureType) AndAlso
                             type.IsNullableType() AndAlso
                             type.GetNullableUnderlyingType().IsSameTypeIgnoringAll(signatureType))
            Else
                Debug.Assert(type.IsSameTypeIgnoringAll(signatureType))
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
