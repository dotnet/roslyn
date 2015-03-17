' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundSequencePointExpression

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(Type Is _Expression.Type)
        End Sub
#End If
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
