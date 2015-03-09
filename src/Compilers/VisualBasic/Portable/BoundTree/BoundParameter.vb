' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundParameter

        Public Sub New(syntax As VisualBasicSyntaxNode, parameterSymbol As ParameterSymbol, isLValue As Boolean, type As TypeSymbol, hasErrors As Boolean)
            Me.New(syntax, parameterSymbol, isLValue, suppressVirtualCalls:=False, type:=type, hasErrors:=hasErrors)
        End Sub

        Public Sub New(syntax As VisualBasicSyntaxNode, parameterSymbol As ParameterSymbol, isLValue As Boolean, type As TypeSymbol)
            Me.New(syntax, parameterSymbol, isLValue, suppressVirtualCalls:=False, type:=type)
        End Sub

        Public Sub New(syntax As VisualBasicSyntaxNode, parameterSymbol As ParameterSymbol, type As TypeSymbol, hasErrors As Boolean)
            Me.New(syntax, parameterSymbol, isLValue:=True, suppressVirtualCalls:=False, type:=type, hasErrors:=hasErrors)
        End Sub

        Public Sub New(syntax As VisualBasicSyntaxNode, parameterSymbol As ParameterSymbol, type As TypeSymbol)
            Me.New(syntax, parameterSymbol, isLValue:=True, suppressVirtualCalls:=False, type:=type)
        End Sub

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return Me.ParameterSymbol
            End Get
        End Property

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Return MakeRValue()
        End Function

        Public Shadows Function MakeRValue() As BoundParameter
            If _IsLValue Then
                Return Me.Update(_ParameterSymbol, False, SuppressVirtualCalls, Type)
            End If

            Return Me
        End Function
    End Class

End Namespace
