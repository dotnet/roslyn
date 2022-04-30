' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundSequencePointExpression
        Public Overrides ReadOnly Property IsLValue As Boolean
            Get
                Return Me.Expression.IsLValue
            End Get
        End Property

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Return MakeRValue()
        End Function

        Public Shadows Function MakeRValue() As BoundSequencePointExpression
            If Expression.IsLValue Then
                Return Update(Expression.MakeRValue(), Type)
            End If

            Return Me
        End Function

    End Class

End Namespace
