' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundLocal

        Public Sub New(syntax As SyntaxNode, localSymbol As LocalSymbol, type As TypeSymbol, hasErrors As Boolean)
            Me.New(syntax, localSymbol, Not localSymbol.IsReadOnly, type:=type, hasErrors:=hasErrors)
        End Sub

        Public Sub New(syntax As SyntaxNode, localSymbol As LocalSymbol, type As TypeSymbol)
            Me.New(syntax, localSymbol, Not localSymbol.IsReadOnly, type:=type)
        End Sub

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return Me.LocalSymbol
            End Get
        End Property

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Return MakeRValue()
        End Function

        Public Shadows Function MakeRValue() As BoundLocal
            If _IsLValue Then
                Return Update(_LocalSymbol, False, Type)
            End If

            Return Me
        End Function

        Public Overrides ReadOnly Property ConstantValueOpt As ConstantValue
            Get
                If HasErrors OrElse Type.IsErrorType Then
                    ' Don't even bother to look at the bound expression if the
                    ' bound node has errors. 
                    Return Nothing
                End If

                Dim result As ConstantValue = LocalSymbol.GetConstantValue(Nothing)
#If DEBUG Then
                ValidateConstantValue(Me.Type, result)
#End If
                Return result
            End Get
        End Property

#If DEBUG Then
        Private Sub Validate()
        End Sub
#End If
    End Class

End Namespace
