' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundParameter

        Public Sub New(syntax As SyntaxNode, parameterSymbol As ParameterSymbol, isLValue As Boolean, type As TypeSymbol, hasErrors As Boolean)
            Me.New(syntax, parameterSymbol, isLValue, suppressVirtualCalls:=False, type:=type, hasErrors:=hasErrors)
        End Sub

        Public Sub New(syntax As SyntaxNode, parameterSymbol As ParameterSymbol, isLValue As Boolean, type As TypeSymbol)
            Me.New(syntax, parameterSymbol, isLValue, suppressVirtualCalls:=False, type:=type)
        End Sub

        Public Sub New(syntax As SyntaxNode, parameterSymbol As ParameterSymbol, type As TypeSymbol, hasErrors As Boolean)
            Me.New(syntax, parameterSymbol, isLValue:=True, suppressVirtualCalls:=False, type:=type, hasErrors:=hasErrors)
        End Sub

        Public Sub New(syntax As SyntaxNode, parameterSymbol As ParameterSymbol, type As TypeSymbol)
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
