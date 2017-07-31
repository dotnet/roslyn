' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundBadVariable
        Inherits BoundExpression

        Public Sub New(syntax As SyntaxNode, expression As BoundExpression, type As TypeSymbol, Optional hasErrors As Boolean = False)
            Me.New(syntax, expression, True, type, hasErrors)
        End Sub

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Return MakeRValue()
        End Function

        Public Shadows Function MakeRValue() As BoundBadVariable
            If _IsLValue Then
                Return Update(_Expression, False, Type)
            End If

            Return Me
        End Function

        Public Overrides ReadOnly Property ResultKind As LookupResultKind
            Get
                Return LookupResultKind.NotAValue
            End Get
        End Property
    End Class
End Namespace
