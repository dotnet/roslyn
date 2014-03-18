' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Class BoundParameter

        Public Sub New(syntax As VisualBasicSyntaxNode, parameterSymbol As ParameterSymbol, type As TypeSymbol, hasErrors As Boolean)
            Me.New(syntax, parameterSymbol, True, type, hasErrors)
        End Sub

        Public Sub New(syntax As VisualBasicSyntaxNode, parameterSymbol As ParameterSymbol, type As TypeSymbol)
            Me.New(syntax, parameterSymbol, True, type)
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
                Return Me.Update(_ParameterSymbol, False, Type)
            End If

            Return Me
        End Function
    End Class

End Namespace